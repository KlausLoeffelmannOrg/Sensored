using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

/// <content>
///  Actionable fusion readiness and vehicle mounting configuration with explicit assumptions.
/// </content>
public partial class MainForm
{
    private readonly ToolStripStatusLabel _tabletFrameLabel = new()
    {
        Name = "_tabletFrameLabel",
        Text = "Fusion unavailable",
        BorderSides = ToolStripStatusLabelBorderSides.Left
    };

    private readonly SymbolToolStripButton _calibrateButton = new()
    {
        Name = "_calibrateButton",
        Text = "Fusion setup"
    };

    private string? _lastReadinessSummary;
    private VehicleMountReference? _activeVehicleMount;

    private void EnsureCalibrationUi()
    {
        if (!_statusStrip.Items.Contains(_tabletFrameLabel))
        {
            _statusStrip.Items.Insert(1, _tabletFrameLabel);
        }

        if (!_toolStrip.Items.Contains(_calibrateButton))
        {
            int optionsIndex = _toolStrip.Items.IndexOf(_optionsButton);
            _toolStrip.Items.Insert(Math.Max(optionsIndex + 1, 0), _calibrateButton);
        }

        int symbolSize = (int)_options.ToolbarSize;
        _calibrateButton.ConfigureItem(
            FluentSymbols.AllSymbols.Ruler,
            (OnCalibrateCommand, true),
            "Show fusion support; capture the current tablet pose or edit the mounting reference for the next session",
            symbolSize);
    }

    private void PrepareSessionFrame(string? replayPath)
    {
        _lastReadinessSummary = null;
        _log.Information(replayPath is not null
            ? "Replay: the current live mounting profile is not assumed for historical inputs."
            : _options.VehicleMount is { } mount
                ? $"Configured physical installation: {mount.Id}, revision {mount.Version}; {mount.Provenance}. "
                    + "Gravity still requires a supported quiet stop; mounting assumptions remain as declared above."
                : "No measured physical installation configured. Horizon and vehicle acceleration remain unavailable; open Fusion setup.");
    }

    private void OnCalibrateCommand(object? sender, EventArgs e)
    {
        if (_transitioning || _shutdownStarted)
        {
            return;
        }

        try
        {
            using VehicleFusionSetupDialog dialog = new(ReadFusionReadiness,
                _options.VehicleMount, canEdit: true, Font, ReadCalibrationAcceleration);
            UpDownHeightWorkaround.Attach(dialog);

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            DemoOptions changed = _options with { VehicleMount = dialog.Mount };
            DemoSettings.Save(_userSettings, changed);
            _userSettings?.Flush();
            _options = changed;
            if (_lease is null && _replayPlayer is null)
            {
                _fusionEngine = null;
                _lastFusionEstimate = null;
            }

            SetStatusAndLog(dialog.Mount is { } mount
                ? $"Saved mount {mount.Id}, revision {mount.Version} for the next live session. "
                    + $"Active fusion is unchanged. Assumptions: {mount.Provenance}"
                : "Mount disabled for the next live session. Active fusion is unchanged.");
            UpdateTabletFrameStatus();
        }
        catch (Exception exception)
        {
            ShowError("Could not save the physical mounting reference", exception);
        }
    }

    private Vec3? ReadCalibrationAcceleration()
    {
        if (_replayPlayer is not null)
        {
            return null;
        }

        if (_lease is not null
            && DateTimeOffset.UtcNow - _lastRawImuReceivedAt <= TimeSpan.FromSeconds(3)
            && _lastRawImu is RawImuPayload sample)
        {
            return sample.Acceleration;
        }

        Windows.Devices.Sensors.AccelerometerReading? reading =
            Windows.Devices.Sensors.Accelerometer.GetDefault()?.GetCurrentReading();

        return reading is not null && DateTimeOffset.UtcNow >= reading.Timestamp
            && DateTimeOffset.UtcNow - reading.Timestamp <= TimeSpan.FromSeconds(3)
                ? new Vec3(reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ)
                : null;
    }

    private FusionReadinessSnapshot ReadFusionReadiness()
    {
        FusionEstimate? estimate = _fusionSession?.Latest ?? _lastFusionEstimate;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool fresh = !_paused && (_replayPlayer is { IsPaused: false }
            || (_replayPlayer is null && estimate is not null
                && now >= estimate.Timestamp && now - estimate.Timestamp <= TimeSpan.FromSeconds(3)));
        FusionReadinessSnapshot readiness = FusionReadinessSnapshot.Create(estimate,
            _replayPlayer is null ? (_lease is not null ? _activeVehicleMount : _options.VehicleMount)
                : (_fusionEngine as RecordedFusionEngine)?.VehicleMount,
            _lease is not null, fresh, _fusionSession?.Fault?.Message);

        return _replayPlayer is null ? readiness : readiness with
        {
            Details = readiness.Details + Environment.NewLine
                + "REPLAY: the current live mounting profile is not applied to historical data. "
                + "A down-lock companion cannot replace a recorded full physical installation."
        };
    }

    private void UpdateTabletFrameStatus()
    {
        FusionReadinessSnapshot readiness = ReadFusionReadiness();
        _tabletFrameLabel.Text = readiness.Summary;
        CheckpointFusionLearning();
        _tabletFrameLabel.ToolTipText = readiness.Details;
        if (_lastReadinessSummary != readiness.Summary)
        {
            _lastReadinessSummary = readiness.Summary;
            _log.Information($"Fusion readiness: {readiness.Summary}");
            NotifyStatusChange(readiness.Summary);
        }
    }
}
