using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.Drawing.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace WinFormsFusionGauges.WinFormsApp;

public partial class MainForm
{
    private void RefreshGpsConfiguration()
    {
        bool carMode = _toolbar.CarMode;
        _toolbar.CarMode = false;
        ToolStripItem[] previous = _gpsConfigurationButton.DropDownItems.Cast<ToolStripItem>().ToArray();
        _gpsConfigurationButton.DropDownItems.Clear();
        foreach (ToolStripItem item in previous)
        {
            item.Dispose();
        }

        _gpsConfigurationButton.Symbol = GpsConfigurationSymbols.Configuration;
        _gpsConfigurationButton.Text = "Speed source";
        _gpsConfigurationButton.AccessibleName = "Speed source";
        _gpsConfigurationButton.ToolTipText = $"Selected: {(_options.UseObdSpeed ? "OBD2 (unfused)" : "GPS")}. "
            + $"GPS: {_options.SourceKind}; {_options.GnssProfile}; "
            + $"target {RateCaption(_options.TargetFixIntervalMilliseconds)}. "
            + "Passive leaves the receiver unchanged; actual rate is measured, not guaranteed.";
        bool serial = _options.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps;
        AddGpsChoice(_gpsConfigurationButton, "OBD2", GpsConfigurationSymbols.Serial, _options.UseObdSpeed,
            new GpsCommand(GpsChoice.SpeedSource, true));
        SymbolToolStripMenuItem gps = new("GPS")
        {
            Symbol = GpsConfigurationSymbols.Configuration,
            Checked = !_options.UseObdSpeed,
            ToolTipText = "Select GPS to restore normal GPS/fusion speed and receiver settings."
        };
        _gpsConfigurationButton.DropDownItems.Add(gps);
        AddGpsChoice(gps, "Use GPS", GpsConfigurationSymbols.Windows, !_options.UseObdSpeed,
            new GpsCommand(GpsChoice.SpeedSource, false));
        AddGpsChoice(gps, "Windows", GpsConfigurationSymbols.Windows, !serial,
            new GpsCommand(GpsChoice.Source, WindowsSensorSourceKind.ImuAndGps));
        AddGpsChoice(gps, "Serial", GpsConfigurationSymbols.Serial, serial,
            new GpsCommand(GpsChoice.Source, WindowsSensorSourceKind.ImuAndSerialNmeaGps));

        SymbolToolStripMenuItem types = new("GPS Type") { Symbol = GpsConfigurationSymbols.Configuration };
        gps.DropDownItems.Add(types);
        foreach (SerialGnssProfile profile in Enum.GetValues<SerialGnssProfile>())
        {
            string caption = profile switch
            {
                SerialGnssProfile.Passive => "Passive (unchanged)",
                SerialGnssProfile.Gps => "GPS + QZSS",
                SerialGnssProfile.GpsBeiDouQzss => "GPS + BeiDou + QZSS",
                SerialGnssProfile.Current => "Current constellations (rate only)",
                _ => profile.ToString()
            };
            AddGpsChoice(types, caption, profile == SerialGnssProfile.Current
                ? GpsConfigurationSymbols.Clock : GpsConfigurationSymbols.Constellation((int)profile),
                _options.GnssProfile == profile, new GpsCommand(GpsChoice.Profile, profile));
        }

        SymbolToolStripMenuItem ports = new("COM Port") { Symbol = GpsConfigurationSymbols.Port };
        gps.DropDownItems.Add(ports);
        string? selectedPort = string.IsNullOrWhiteSpace(_options.SerialPortName)
            ? null : _options.SerialPortName.Trim();
        if (selectedPort is not null)
        {
            AddGpsChoice(ports, $"{selectedPort} (last setting)", GpsConfigurationSymbols.Port,
                true, new GpsCommand(GpsChoice.Port, selectedPort));
        }

        for (int number = 1; number <= 9; number++)
        {
            string port = $"COM{number}";
            if (!port.Equals(selectedPort, StringComparison.OrdinalIgnoreCase))
            {
                AddGpsChoice(ports, port, GpsConfigurationSymbols.Port, false,
                    new GpsCommand(GpsChoice.Port, port));
            }
        }

        SymbolToolStripMenuItem rates = new("Target Res.") { Symbol = GpsConfigurationSymbols.Clock };
        gps.DropDownItems.Add(rates);
        int? selectedRate = _options.TargetFixIntervalMilliseconds;
        string RateCaption(int? rate) => rate is { } value ? $"{value:N0} ms" : "Dynamic";
        AddGpsChoice(rates, $"{RateCaption(selectedRate)} (last setting)", GpsConfigurationSymbols.Clock,
            true, new GpsCommand(GpsChoice.Interval, selectedRate));
        foreach (int? rate in new int?[] { 1000, 500, 200, 100, null })
        {
            if (rate != selectedRate)
            {
                AddGpsChoice(rates, RateCaption(rate), GpsConfigurationSymbols.Clock, false,
                    new GpsCommand(GpsChoice.Interval, rate));
            }
        }

        _toolbar.CarMode = carMode;
    }

    private void AddGpsChoice(ToolStripDropDownItem parent, string caption, ISymbol symbol,
        bool selected, GpsCommand command)
    {
        SymbolToolStripMenuItem item = new(caption)
        {
            Symbol = symbol,
            Checked = selected,
            Tag = command,
            ToolTipText = command.Choice == GpsChoice.SpeedSource
                ? "Select speed now; OBD2 is authoritative, undamped, and never falls back to GPS."
                : "Applies GPS live after safe release; IMU, drive and recording are not restarted."
        };
        item.Click += OnGpsConfigurationCommand;
        parent.DropDownItems.Add(item);
    }

    private async void OnGpsConfigurationCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripItem { Tag: GpsCommand command })
        {
            return;
        }

        await RunCommandAsync(async () =>
        {
            FusionGaugesOptions updated = command.Choice switch
            {
                GpsChoice.SpeedSource => _options with { UseObdSpeed = (bool)command.Value! },
                GpsChoice.Source => _options with { SourceKind = (WindowsSensorSourceKind)command.Value! },
                GpsChoice.Port => _options with { SerialPortName = (string)command.Value! },
                GpsChoice.Profile => _options with { GnssProfile = (SerialGnssProfile)command.Value! },
                GpsChoice.Interval => _options with
                {
                    TargetFixIntervalMilliseconds = (int?)command.Value,
                    GpsReportIntervalMilliseconds = (uint)((int?)command.Value ?? 100),
                    GnssProfile = _options.GnssProfile == SerialGnssProfile.Passive
                        ? SerialGnssProfile.Current : _options.GnssProfile
                },
                _ => throw new InvalidOperationException("Unknown GPS configuration command.")
            };

            if (updated.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && updated.GnssProfile != SerialGnssProfile.Passive
                && !updated.AllowReceiverCommandsThisSession)
            {
                if (!WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(
                    this, updated.CreateSourceOptions().SerialGpsOptions))
                {
                    return;
                }

                updated = updated with { AllowReceiverCommandsThisSession = true };
            }

            updated.Validate();
            if (command.Choice != GpsChoice.SpeedSource)
            {
                await _hub.ApplyGpsAsync(updated, allowReceiverWrites: true);
            }
            _options = updated;
            _hub.SelectObdSpeed(updated.UseObdSpeed);
            _settings?.Set(OptionsKey, _options);
            _settings?.Flush();
            RefreshGpsConfiguration();
            _statusLabel.Text = command.Choice == GpsChoice.SpeedSource
                ? $"Speed source: {(updated.UseObdSpeed ? "OBD2 (100% speed confidence while fresh; no fusion)" : "GPS")}."
                : "GPS configuration applied without restarting the drive or recording. "
                    + "Target rate is a request; diagnostics report the actual measured rate.";
        });
    }

    /// <summary>Identifies an immediately applied GPS or speed-source choice.</summary>
    private enum GpsChoice { Source, Profile, Port, Interval, SpeedSource }

    /// <summary>Carries a typed menu action without using display text as configuration.</summary>
    private sealed record GpsCommand(GpsChoice Choice, object? Value);
}
