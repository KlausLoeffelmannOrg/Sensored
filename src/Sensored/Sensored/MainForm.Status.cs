using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <summary>Owns the compact live status row and its timed last-exception popup.</summary>
public partial class MainForm
{
    private string _statusDetail = string.Empty;
    private string? _statusWarning;
    private string _statusCleanupTask = "finishing";
    private int _statusCleanupItem;
    private bool _statusStopping;
    private ToolStripDropDown? _exceptionPopup;
    private string _gpsHealthSummary = string.Empty;
    private string _gpsHealthDetails = string.Empty;
    private string? _bluetoothDiscoveryStatus;
    private (bool Enabled, bool Connected, bool Searching, bool Fresh)? _lastObdNotificationState;

    private void ReportBluetoothDiscoveryStatus(string status)
    {
        if (_shutdownStarted || IsDisposed || Disposing)
        {
            return;
        }

        _bluetoothDiscoveryStatus = status;
        RefreshStructuredStatus();
        NotifyStatusChange(status);
    }

    private void RefreshStructuredStatus()
    {
        _statusStrip.SuspendLayout();
        try
        {
            RefreshStructuredStatusCore();
        }
        finally
        {
            _statusStrip.ResumeLayout(performLayout: true);
        }
    }

    private void RefreshStructuredStatusCore()
    {
        SensorServiceState state = _shutdownStarted || _statusStopping || IsReportingCleanup
            || (_transitioning && _statusCleanupItem > 0)
                ? SensorServiceState.Stopping
                : _transitioning ? SensorServiceState.Starting
                : _lease is not null ? _sensorService.State : SensorServiceState.Stopped;
        SensorRateSnapshot rates = _sensorRates.GetSnapshot(DateTimeOffset.UtcNow);
        bool stopping = state == SensorServiceState.Stopping;
        _statusLabel.Spring = stopping;
        _gpsRateLabel.Available = !stopping;
        _obdStatusLabel.Available = !stopping;
        _rawStatusLabel.Available = !stopping;
        _statusLabel.Text = DashboardStatusText.Session(state, rates, _statusCleanupTask, _statusCleanupItem)
            + (_statusWarning is null ? string.Empty : $" · {_statusWarning}");
        _statusLabel.ForeColor = StatusColor(_statusWarning is not null ? TabletFrameQuality.Red
            : state == SensorServiceState.Running ? TabletFrameQuality.Green
            : state is SensorServiceState.Starting or SensorServiceState.Stopping
                ? TabletFrameQuality.Yellow : TabletFrameQuality.Unavailable);
        string plausibility = _plausibilityMonitor is { } monitor
            ? string.Join(Environment.NewLine, monitor.Latest.Select(result => result.Reason)) : string.Empty;
        _statusLabel.ToolTipText = $"{_statusDetail}\n{_statusWarning}\n{_tabletFrameLabel.ToolTipText}\n{plausibility}";
        _gpsRateLabel.Text = DescribeStatusGps();
        _gpsRateLabel.ToolTipText = $"{_gpsRateLabel.Text}\n{_gpsHealthSummary}\n{_gpsHealthDetails}\n{_gpsSourceButton.ToolTipText}";
        ObdTelemetrySnapshot obd = _obdReplayActive
            ? Volatile.Read(ref _presentedObd)
            : _obdSource?.Latest ?? ObdTelemetrySnapshot.Disabled;
        bool fresh = obd.Speed?.IsFreshAt(DateTimeOffset.UtcNow) == true;
        _obdStatusLabel.Text = obd.Connected
            ? $"OBD2: {obd.DeviceName ?? "selected adapter"} {(fresh ? obd.Hertz : 0):0.0} HZ"
            : obd.ModeEstablished ? $"OBD2: {obd.DeviceName ?? "selected adapter"} {(obd.Searching ? "reconnecting..." : "unavailable")}"
            : obd.Searching ? "Checking For OBD2..." : "No OBD2";
        _obdStatusLabel.ForeColor = StatusColor(fresh ? TabletFrameQuality.Green
            : obd.Searching ? TabletFrameQuality.Yellow : TabletFrameQuality.Red);
        _obdStatusLabel.ToolTipText = $"{_obdStatusLabel.Text}\n{obd.Status}";
        var obdState = (obd.Enabled, obd.Connected, obd.Searching, Fresh: fresh);
        if (_lastObdNotificationState is { } previous && previous != obdState)
        {
            NotifyStatusChange(obd.Connected && !fresh
                ? $"{obd.Status}\nOBD2 speed telemetry is unavailable or stale."
                : obd.Status);
        }

        _lastObdNotificationState = obdState;
        _rawStatusLabel.Text = DashboardStatusText.Raw(_lastRawImu,
            _displayedGpsFresh ? _gpsMotionTracker.Latest.ReceiverSpeedMetersPerSecond : double.NaN);
        _rawStatusLabel.ToolTipText = _rawStatusLabel.Text;
        _imuRateLabel.Available = !stopping;
        _imuRateLabel.Text = _bluetoothDiscoveryStatus ?? BluetoothSensorsStatus;
        _imuRateLabel.ToolTipText = _imuRateLabel.Text;
        _imuRateLabel.ForeColor = StatusColor(
            _bluetoothDiscoveryStatus is not null || _bluetoothSensorSource?.Latest.Searching == true
                ? TabletFrameQuality.Yellow
                : BluetoothSensorsAvailable ? TabletFrameQuality.Green : TabletFrameQuality.Unavailable);
        _tabletFrameLabel.Available = false;
        FitStatusFields();
    }

    private void OnStatusStripSizeChanged(object? sender, EventArgs e)
        => FitStatusFields();

    private void FitStatusFields()
    {
        if (IsDisposed || Disposing || _statusStrip.IsDisposed || _statusStrip.Disposing)
        {
            return;
        }

        int rowHeight = Math.Max(_statusStrip.Font.Height + LogicalToDeviceUnits(12),
            _statusStrip.ImageScalingSize.Height + LogicalToDeviceUnits(8));
        _statusStrip.AutoSize = false;
        if (_statusStrip.Height != rowHeight)
        {
            _statusStrip.Height = rowHeight;
        }

        foreach (ToolStripItem item in _statusStrip.Items)
        {
            // Exception messages and adapter identities can contain embedded line breaks.
            item.Text = item.Text?.Replace('\r', ' ').Replace('\n', ' ');
            if (item is ToolStripStatusLabel label && !label.Spring)
            {
                label.AutoSize = false;
                int desired = MeasureStatusTextWidth(label);
                int limit = label == _statusLabel ? 240 : 200;
                label.Size = new Size(
                    Math.Min(desired,
                        Math.Min(LogicalToDeviceUnits(limit), Math.Max(0, _statusStrip.Width / 4))),
                    rowHeight - label.Margin.Vertical);
            }
        }

        int remaining = _statusStrip.DisplayRectangle.Width - _lastExceptionButton.Margin.Horizontal;
        foreach (ToolStripItem item in _statusStrip.Items)
        {
            if (item.Available && item != _lastExceptionButton
                && item != _gpsRateLabel && item != _obdStatusLabel && item != _rawStatusLabel
                && item is not ToolStripStatusLabel { Spring: true })
            {
                remaining -= item.Width + item.Margin.Horizontal;
            }
        }

        Size preferred = _lastExceptionButton.GetPreferredSize(Size.Empty);
        int exceptionWidth = Math.Min(preferred.Width, LogicalToDeviceUnits(120));
        int contentBudget = Math.Max(0, remaining - exceptionWidth
            - (_gpsRateLabel.Available ? _gpsRateLabel.Margin.Horizontal : 0)
            - (_obdStatusLabel.Available ? _obdStatusLabel.Margin.Horizontal : 0)
            - (_rawStatusLabel.Available ? _rawStatusLabel.Margin.Horizontal : 0));
        int gpsDesired = _gpsRateLabel.Available ? MeasureStatusTextWidth(_gpsRateLabel) : 0;
        int obdDesired = _obdStatusLabel.Available ? MeasureStatusTextWidth(_obdStatusLabel) : 0;
        int identityBudget = Math.Min(gpsDesired + obdDesired, contentBudget);
        int gpsWidth = Math.Min(gpsDesired, Math.Max(identityBudget - obdDesired, identityBudget / 2));
        int obdWidth = Math.Min(obdDesired, identityBudget - gpsWidth);
        Size gpsSize = new(gpsWidth, rowHeight - _gpsRateLabel.Margin.Vertical);
        Size obdSize = new(obdWidth, rowHeight - _obdStatusLabel.Margin.Vertical);
        _rawStatusLabel.Size = new Size(_rawStatusLabel.Available
            ? Math.Min(MeasureStatusTextWidth(_rawStatusLabel), contentBudget - gpsWidth - obdWidth) : 0,
            rowHeight - _rawStatusLabel.Margin.Vertical);
        Size exceptionSize = new(
            exceptionWidth,
            rowHeight - _lastExceptionButton.Margin.Vertical);
        if (_gpsRateLabel.Size != gpsSize)
        {
            _gpsRateLabel.Size = gpsSize;
        }

        if (_obdStatusLabel.Size != obdSize)
        {
            _obdStatusLabel.Size = obdSize;
        }

        if (_lastExceptionButton.Size != exceptionSize)
        {
            _lastExceptionButton.Size = exceptionSize;
        }
    }

    private static int MeasureStatusTextWidth(ToolStripStatusLabel label)
        => DashboardStatusRenderer.MeasureTextWidth(label);

    private string DescribeStatusGps()
    {
        if (_replayPlayer is not null)
        {
            return "GPS replay";
        }

        DemoOptions? source = _lease is not null ? _runningSourceOptions : null;
        string? port = _sensorSourceFactory?.ActiveSerialSource?.ActivePort?.PortName ?? source?.SerialPortName;
        if (source?.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps && _sensorSourceFactory?.GpsFallbackActive == true)
        {
            return "Tablet GPS (serial fallback)";
        }
        return source?.SourceKind switch
        {
            WindowsSensorSourceKind.ImuAndGps => "Internal GPS",
            WindowsSensorSourceKind.ImuAndSerialNmeaGps => $"GPS {source.SerialGnssProfile switch
            {
                SerialGnssProfile.Gps => "GPS+QZSS",
                SerialGnssProfile.BeiDou => "BDS",
                SerialGnssProfile.Galileo => "GAL",
                SerialGnssProfile.Glonass => "GLO",
                SerialGnssProfile.GpsBeiDouQzss => "GPS+BDS+QZSS",
                _ => "NMEA"
            }} on {port ?? "auto COM"}",
            _ => "No GPS"
        };
    }

    private static Color StatusColor(TabletFrameQuality quality)
        => TabletFrameStatusColors.GetColor(quality, Application.IsDarkModeEnabled);

    private void CloseExceptionPopup(object? sender, EventArgs e)
    {
        _exceptionPopupTimer.Stop();
        _exceptionPopup?.Close();
    }

    private void ShowLastExceptionPopup()
    {
        if (_exceptionPopup is { Visible: true })
        {
            CloseExceptionPopup(this, EventArgs.Empty);

            return;
        }

        ExceptionHistoryEntry? latest = _exceptionHistory.Latest;
        if (latest is null)
        {
            return;
        }

        _exceptionPopup?.Dispose();
        TextBox details = new()
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = $"{latest.Timestamp:G} · {latest.Context}{Environment.NewLine}{latest.Details}",
            Size = new Size(LogicalToDeviceUnits(640), LogicalToDeviceUnits(300)),
            BackColor = SystemColors.Window,
            ForeColor = SystemColors.WindowText,
            AccessibleName = "Last exception details; click to dismiss"
        };
        details.MouseDown += CloseExceptionPopup;
        _exceptionPopup = new ToolStripDropDown();
        _exceptionPopup.Items.Add(new ToolStripControlHost(details));
        _exceptionPopup.Closed += (_, _) => _exceptionPopupTimer.Stop();
        _exceptionPopupTimer.Interval = checked(_options.ExceptionPopupSeconds * 1000);
        _exceptionPopup.Show(_statusStrip,
            new Point(Math.Max(0, _lastExceptionButton.Bounds.Right - _exceptionPopup.Width), 0),
            ToolStripDropDownDirection.AboveRight);
        _exceptionPopupTimer.Start();
    }
}
