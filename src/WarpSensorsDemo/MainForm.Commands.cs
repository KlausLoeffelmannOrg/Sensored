using System.Globalization;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

/// <summary>
///  Contains the restored dashboard commands and their enabled and checked state.
/// </summary>
public partial class MainForm
{
    private static readonly int[] s_dampingChoices = [0, 50, 100, 200, 500];
    private static readonly double[] s_gScaleChoices = [0.75d, 1d, 1.5d, 2d, 3d];
    private static readonly float[] s_baseFontChoices = [10f, 12f, 14f, 18f, 24f];

    private void SetupCommands()
    {
        ConfigureCommandImages();

        _windowsGpsMenuItem.Tag = WindowsSensorSourceKind.ImuAndGps;
        _serialGpsMenuItem.Tag = WindowsSensorSourceKind.ImuAndSerialNmeaGps;
        _windowsGpsMenuItem.Symbol = SensorDemoSymbols.GpsWindows;
        _serialGpsMenuItem.Symbol = SensorDemoSymbols.GpsSerial;
        _windowsGpsMenuItem.Click += OnGpsSourceCommand;
        _serialGpsMenuItem.Click += OnGpsSourceCommand;
        _mapEnabledMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapRowSpanMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapColumnSpanMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapEnabledMenuItem.Click += OnMapEnabledCommand;
        _mapRowSpanMenuItem.Click += OnMapRowSpanCommand;
        _mapColumnSpanMenuItem.Click += OnMapColumnSpanCommand;

        SymbolToolStripMenuItem[] dampingItems =
        [
            _dampingOffMenuItem,
            _damping50MenuItem,
            _damping100MenuItem,
            _damping200MenuItem,
            _damping500MenuItem
        ];

        for (int index = 0; index < dampingItems.Length; index++)
        {
            dampingItems[index].Symbol = SensorDemoSymbols.OptionsDisplay;
            dampingItems[index].Tag = s_dampingChoices[index];
            dampingItems[index].Click += OnDampingCommand;
        }

        SymbolToolStripMenuItem[] scaleItems =
        [
            _gScale075MenuItem,
            _gScale100MenuItem,
            _gScale150MenuItem,
            _gScale200MenuItem,
            _gScale300MenuItem
        ];

        for (int index = 0; index < scaleItems.Length; index++)
        {
            scaleItems[index].Symbol = SensorDemoSymbols.OptionsDisplay;
            scaleItems[index].Tag = s_gScaleChoices[index];
            scaleItems[index].Click += OnGScaleCommand;
        }

        SymbolToolStripMenuItem[] fontItems =
        [
            _baseFont10MenuItem,
            _baseFont12MenuItem,
            _baseFont14MenuItem,
            _baseFont18MenuItem,
            _baseFont24MenuItem
        ];

        for (int index = 0; index < fontItems.Length; index++)
        {
            fontItems[index].Symbol = SensorDemoSymbols.OptionsDisplay;
            fontItems[index].Tag = s_baseFontChoices[index];
            fontItems[index].Click += OnBaseFontCommand;
        }
    }

    private void ConfigureCommandImages()
    {
        int symbolSize = (int)_options.ToolbarSize;
        _startButton.ConfigureItem(
            FluentSymbols.AllSymbols.Play,
            (OnStartCommand, true),
            "Start sensor acquisition",
            symbolSize);
        _restartButton.ConfigureItem(
            FluentSymbols.AllSymbols.Refresh,
            (OnRestartCommand, true),
            "Restart acquisition with the selected source",
            symbolSize);
        _pauseButton.ConfigureItem(
            FluentSymbols.AllSymbols.Pause,
            (OnPauseCommand, true),
            "Freeze visuals while acquisition and recording continue",
            symbolSize);
        _stopButton.ConfigureItem(
            FluentSymbols.AllSymbols.Stop,
            (OnStopCommand, true),
            "Stop sensor acquisition",
            symbolSize);
        _gpsSourceButton.ConfigureItem(
            FluentSymbols.AllSymbols.Location,
            tooltipText: "Select the GPS source",
            size: symbolSize);
        _optionsButton.ConfigureItem(
            FluentSymbols.AllSymbols.Settings,
            (OnOptionsCommand, true),
            "Source, display, route, and recording options",
            symbolSize);
        _mapButton.ConfigureItem(
            FluentSymbols.AllSymbols.Location,
            tooltipText: "Configure the map layout",
            size: symbolSize);
        _dampingButton.ConfigureItem(
            FluentSymbols.AllSymbols.SpeedMedium,
            tooltipText: "Select display damping",
            size: symbolSize);
        _gScaleButton.ConfigureItem(
            FluentSymbols.AllSymbols.Ruler,
            tooltipText: "Select the acceleration scale",
            size: symbolSize);
        _rawSensorsButton.ConfigureItem(
            FluentSymbols.AllSymbols.Sensor,
            (OnRawSensorsCommand, true),
            "Open raw Windows sensor diagnostics",
            symbolSize);
        _rawLogButton.ConfigureItem(
            FluentSymbols.AllSymbols.OpenFile,
            (OnRawLogCommand, true),
            "Open a current service-owned recording",
            symbolSize);
        _baseFontButton.ConfigureItem(
            FluentSymbols.AllSymbols.FontSize,
            tooltipText: "Select the dashboard font size",
            size: symbolSize);
        _recordingButton.ConfigureItem(
            FluentSymbols.AllSymbols.Record,
            (OnRecordingCommand, true),
            "Start or stop service-owned recording",
            symbolSize);
        _fullScreenButton.ConfigureItem(
            FluentSymbols.AllSymbols.FullScreen,
            (OnFullScreenCommand, true),
            "Toggle full-screen mode (F11)",
            symbolSize);
        _exitButton.ConfigureItem(
            FluentSymbols.AllSymbols.Cancel,
            (OnExitCommand, true),
            "Exit the dashboard",
            symbolSize);
    }

    private void ApplyToolbarAppearance()
    {
        Size imageSize = GetToolbarImageSize(_options.ToolbarSize);
        SymbolToolStripAppearance.Apply(_toolStrip, imageSize);
        ConfigureCommandImages();
        _exceptionHistoryForm?.ApplyToolbarSize(_options.ToolbarSize);
    }

    private static Size GetToolbarImageSize(DashboardToolbarSize toolbarSize)
    {
        int size = (int)toolbarSize;
        return new Size(size, size);
    }

    private void UpdateCommandStates()
    {
        SynchronizeRecordingState();

        bool running = _lease is not null;
        bool available = !_transitioning && !_shutdownStarted;

        _startButton.Enabled = available && !running;
        _restartButton.Enabled = available && running;
        _pauseButton.Enabled = available && running;
        _pauseButton.Checked = _paused;
        _stopButton.Enabled = available && running;
        _gpsSourceButton.Enabled = available;
        _optionsButton.Enabled = available;
        _mapButton.Enabled = available;
        _mapButton.Text = _mapEnabled ? "Map" : "Map off";
        _mapEnabledMenuItem.Checked = _mapEnabled;
        _mapRowSpanMenuItem.Checked = _mapRowSpan == 2;
        _mapColumnSpanMenuItem.Checked = _mapColumnSpan == 2;
        _mapRowSpanMenuItem.Enabled = _mapEnabled;
        _mapColumnSpanMenuItem.Enabled = _mapEnabled;
        _dampingButton.Enabled = available;
        _gScaleButton.Enabled = available;
        _rawSensorsButton.Enabled = available;
        _rawLogButton.Enabled = available;
        _baseFontButton.Enabled = available;
        _recordingButton.Enabled = available && running;
        _recordingButton.Checked = _recording is not null;
        _recordingButton.Text = _recording is null ? "Record" : "Stop recording";
        _fullScreenButton.Enabled = available;
        _fullScreenButton.Checked = _fullScreenController.IsFullScreen;
        _fullScreenButton.Text = _fullScreenController.IsFullScreen
            ? "Exit full screen"
            : "Full screen";
        _exitButton.Enabled = !_shutdownStarted;

        _dampingButton.Text = _options.DampingMilliseconds == 0
            ? "Damping: off"
            : $"Damping: {_options.DampingMilliseconds} ms";
        _gScaleButton.Text =
            $"Scale: {_options.GScale.ToString("0.00", CultureInfo.CurrentCulture)} g";
        _baseFontButton.Text =
            $"Font: {_options.BaseFontPointSize.ToString("0", CultureInfo.CurrentCulture)} pt";
        _gpsSourceButton.Text = _options.SourceKind
            == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                ? "GPS: serial"
                : "GPS: Windows";

        _windowsGpsMenuItem.Checked =
            _options.SourceKind == WindowsSensorSourceKind.ImuAndGps;
        _serialGpsMenuItem.Checked =
            _options.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps;

        SymbolToolStripMenuItem[] dampingItems =
        [
            _dampingOffMenuItem,
            _damping50MenuItem,
            _damping100MenuItem,
            _damping200MenuItem,
            _damping500MenuItem
        ];

        for (int index = 0; index < dampingItems.Length; index++)
        {
            dampingItems[index].Checked =
                s_dampingChoices[index] == _options.DampingMilliseconds;
        }

        SymbolToolStripMenuItem[] scaleItems =
        [
            _gScale075MenuItem,
            _gScale100MenuItem,
            _gScale150MenuItem,
            _gScale200MenuItem,
            _gScale300MenuItem
        ];

        for (int index = 0; index < scaleItems.Length; index++)
        {
            scaleItems[index].Checked =
                Math.Abs(s_gScaleChoices[index] - _options.GScale) < 0.001d;
        }

        SymbolToolStripMenuItem[] fontItems =
        [
            _baseFont10MenuItem,
            _baseFont12MenuItem,
            _baseFont14MenuItem,
            _baseFont18MenuItem,
            _baseFont24MenuItem
        ];

        for (int index = 0; index < fontItems.Length; index++)
        {
            fontItems[index].Checked =
                Math.Abs(s_baseFontChoices[index] - _options.BaseFontPointSize) < 0.01f;
        }
    }

    private async void OnStartCommand(object? sender, EventArgs e)
    {
        try
        {
            await StartSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not start sensors", exception);
        }
    }

    private async void OnRestartCommand(object? sender, EventArgs e)
    {
        try
        {
            await RestartSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not restart sensors", exception);
        }
    }

    private void OnPauseCommand(object? sender, EventArgs e)
    {
        _paused = _pauseButton.Checked;

        if (_paused)
        {
            SetStatus("Visuals paused. Acquisition and recording continue.");
        }
        else
        {
            ResumeVisuals();
            SetStatus("Visuals resumed.");
        }

        UpdateCommandStates();
    }

    private async void OnStopCommand(object? sender, EventArgs e)
    {
        try
        {
            await StopSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not stop sensors", exception);
        }
    }

    private async void OnRecordingCommand(object? sender, EventArgs e)
    {
        _recordingButton.Enabled = false;

        try
        {
            if (_recording is null)
            {
                await StartRecordingAsync().ConfigureAwait(true);
            }
            else
            {
                await StopRecordingAsync().ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            ShowError("Recording operation failed", exception);
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    private void OnGpsSourceCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: WindowsSensorSourceKind sourceKind })
        {
            return;
        }

        _options = _options with
        {
            SourceKind = sourceKind
        };
        UpdateCommandStates();
        SetStatus(_lease is null
            ? "GPS source selected."
            : "GPS source selected. Use Restart to apply it to the running session.");
    }

    private void OnDampingCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: int milliseconds })
        {
            return;
        }

        _options = _options with
        {
            DampingMilliseconds = milliseconds
        };
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private void OnGScaleCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: double scale })
        {
            return;
        }

        _options = _options with
        {
            GScale = scale
        };
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private void OnBaseFontCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: float pointSize })
        {
            return;
        }

        _options = _options with
        {
            BaseFontPointSize = pointSize
        };
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private void OnOptionsCommand(object? sender, EventArgs e)
    {
        using OptionsDialog dialog = new(_options);

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        DemoOptions previousOptions = _options;
        WindowsSensorSourceKind previousSource = previousOptions.SourceKind;
        string? previousPort = previousOptions.SerialPortName;
        _options = dialog.Options;
        _userSettings?.Set(ToolbarSizeSettingsKey, _options.ToolbarSize);
        _userSettings?.Set(AutoRecordingSettingsKey, _options.AutoRecording);

        try
        {
            ApplyDisplayOptions();
            UpdateCommandStates();
        }
        catch (Exception exception)
        {
            _options = previousOptions;
            ApplyDisplayOptions();
            UpdateCommandStates();
            ShowError("Could not apply dashboard options", exception);

            return;
        }

        bool sourceChanged = previousSource != _options.SourceKind
            || !string.Equals(previousPort, _options.SerialPortName, StringComparison.OrdinalIgnoreCase);
        SetStatus(sourceChanged && _lease is not null
            ? "Options updated. Use Restart to apply source changes."
            : "Options updated.");
    }

    private void OnRawSensorsCommand(object? sender, EventArgs e)
    {
        try
        {
            using RawSensorDialog dialog = new(
                _sensorService,
                () =>
                {
                    _sensorSourceFactory?.Configure(
                        _options.CreateWindowsSourceOptions());
                    return DemoOptions.CreateSensorServiceOptions();
                },
                CreateRecordingRequest,
                _options.ReadingFormats,
                _options.ToolbarSize);
            dialog.ShowDialog(this);
            UpdateCommandStates();
        }
        catch (Exception exception)
        {
            ShowError("Raw sensor diagnostics failed", exception);
        }
    }

    private void OnRawLogCommand(object? sender, EventArgs e)
    {
        try
        {
            string? initialPath = _recording?.FolderPath;
            using RawLogViewerDialog dialog = new(
                _options.ReadingFormats,
                _options.ToolbarSize,
                initialPath);
            dialog.ShowDialog(this);
        }
        catch (Exception exception)
        {
            ShowError("Raw recording viewer failed", exception);
        }
    }

    private void OnExitCommand(object? sender, EventArgs e)
        => Close();

    private void OnFullScreenCommand(object? sender, EventArgs e)
        => _fullScreenController.Toggle();

    private void OnFullScreenChanged(object? sender, EventArgs e)
        => UpdateCommandStates();

    private void OnMapEnabledCommand(object? sender, EventArgs e)
    {
        _mapEnabled = _mapEnabledMenuItem.Checked;
        PersistMapLayout();
        ApplyMapLayout();
        UpdateCommandStates();
    }

    private void OnMapRowSpanCommand(object? sender, EventArgs e)
    {
        _mapRowSpan = _mapRowSpanMenuItem.Checked ? 2 : 1;
        PersistMapLayout();
        ApplyMapLayout();
        UpdateCommandStates();
    }

    private void OnMapColumnSpanCommand(object? sender, EventArgs e)
    {
        _mapColumnSpan = _mapColumnSpanMenuItem.Checked ? 2 : 1;
        PersistMapLayout();
        ApplyMapLayout();
        UpdateCommandStates();
    }

    private void SynchronizeRecordingState()
    {
        if (_recording is not null && !_recording.Status.IsActive)
        {
            _recording = null;
        }

        if (_recording is null && DemoRecordingRegistry.Active is { } active)
        {
            _recording = active.Status.IsActive ? active : null;
        }

        if (_recording is null && DemoRecordingRegistry.Active is not null)
        {
            DemoRecordingRegistry.Set(null);
        }
    }
}
