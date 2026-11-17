using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Composes the three compact OBD instruments beneath the speedometer.</summary>
public sealed partial class ObdTelemetryView : UserControl
{
    /// <summary>Creates instruments without starting vehicle acquisition.</summary>
    public ObdTelemetryView()
    {
        InitializeComponent();
        AutoScroll = false;
    }

    internal event Action<int>? RpmScaleChanged;

    private void OnRpmScaleCommand(object? sender, EventArgs e)
    {
        if (sender is ToolStripItem { Tag: int maximum })
        {
            RpmScaleChanged?.Invoke(maximum);
        }
    }

    internal void Present(ObdTelemetrySnapshot snapshot, DateTimeOffset now, int maximumRpm)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumRpm, 1000);
        _gauges.Rpm.Maximum = maximumRpm;
        _gauges.Rpm.Value = ReadMetric(snapshot.Readings.Rpm, snapshot.Connected, now, 3d);
        _gauges.Rpm.SecondaryValue = ReadMetric(
            snapshot.Readings.CommandedThrottle, snapshot.Connected, now, 3d);
        _gauges.Rpm.TertiaryValue = ReadMetric(
            snapshot.Readings.ActualThrottle, snapshot.Connected, now, 3d);
        _gauges.Temperature.Value = ReadMetric(
            snapshot.Readings.CoolantTemperature, snapshot.Connected, now, 5d);
        _gauges.FuelAndBattery.Value = ReadMetric(
            snapshot.Readings.FuelLevel, snapshot.Connected, now, 5d);
        _gauges.FuelAndBattery.BatteryValue = ReadMetric(
            snapshot.Readings.Battery, snapshot.Connected, now, 5d);
        string description = $"{_gauges.Rpm.Readout}; {_gauges.Temperature.Readout}; fuel/battery {_gauges.FuelAndBattery.Readout}";
        if (_gauges.AccessibleDescription != description)
        {
            _gauges.AccessibleDescription = description;
        }
        foreach (ToolStripMenuItem item in _rpmMenu.Items.OfType<ToolStripMenuItem>())
        {
            item.Checked = item.Tag is int scale && scale == maximumRpm;
        }
    }

    /// <inheritdoc/>
    public override Size GetPreferredSize(Size proposedSize)
    {
        Size instrument = _gauges?.GetPreferredSize(proposedSize) ?? base.GetPreferredSize(proposedSize);

        return new(instrument.Width + Padding.Horizontal + 6, instrument.Height + Padding.Vertical + 6);
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        PerformLayout();
    }

    internal static double ReadMetric(
        ObdMetric metric, bool connected, DateTimeOffset now, double maximumAgeSeconds)
        => connected && metric is { Status: ObdReadingStatus.Valid, Value: { } value, Timestamp: { } time }
            && double.IsFinite(value) && now >= time
            && now - time <= TimeSpan.FromSeconds(maximumAgeSeconds) ? value : double.NaN;
}
