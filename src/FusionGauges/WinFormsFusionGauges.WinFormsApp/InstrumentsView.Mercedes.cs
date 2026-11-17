using WarpToolkit.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <content>
///  Mercedes mode replaces only the right instrument column. The default horizon and compass stay alive.
/// </content>
public sealed partial class InstrumentsView
{
    private const int CanLogLineLimit = 400;
    private bool _mercedesLayout;
    private readonly CanDiagnosticSummary _canSummary = new();
    private CanDiagnosticResultsDialog? _canResultsDialog;

    /// <summary>Gets all retained CAN diagnostic text, including when Mercedes mode is hidden.</summary>
    internal string CanDiagnosticText => _canLog.Text;

    /// <summary>Gets whether the right column is showing Mercedes instruments.</summary>
    internal bool IsMercedesLayout => _mercedesLayout;

    /// <summary>Swaps the right column between horizon/compass and four vehicle gauges above CAN diagnostics.</summary>
    /// <param name="enabled">Whether Mercedes instruments should occupy the right column.</param>
    internal void SetMercedesLayout(bool enabled)
    {
        if (_mercedesLayout == enabled)
        {
            return;
        }

        TableLayoutPanel column = _mercColumn;
        _mercedesLayout = enabled;
        if (enabled)
        {
            _layout.Controls.Remove(_horizon);
            _layout.Controls.Remove(_compass);
            _horizon.Visible = false;
            _compass.Visible = false;
            _layout.Controls.Add(column, 2, 0);
            _layout.SetRowSpan(column, 2);
            column.Visible = true;

            return;
        }

        _layout.Controls.Remove(column);
        column.Visible = false;
        _horizon.Visible = true;
        _compass.Visible = true;
        _layout.Controls.Add(_horizon, 2, 0);
        _layout.Controls.Add(_compass, 2, 1);
    }

    /// <summary>Presents independently fresh vehicle measurements without substituting missing values.</summary>
    /// <param name="telemetry">The latest vehicle telemetry, or null when none is available.</param>
    internal void PresentVehicle(VehicleTelemetryPayload? telemetry)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        _rpmGauge.Value = FreshMetric(telemetry?.Rpm, now, TimeSpan.FromSeconds(3));
        _temperatureGauge.Value = FreshMetric(telemetry?.CoolantCelsius, now, TimeSpan.FromSeconds(5));
        _fuelEnergyGauge.Value = FreshMetric(telemetry?.FuelPercent, now, TimeSpan.FromSeconds(5));
        _fuelEnergyGauge.BatteryValue = FreshMetric(telemetry?.BatteryPercent, now, TimeSpan.FromSeconds(5));
        _throttleGauge.Value = FreshMetric(telemetry?.ActualThrottle, now, TimeSpan.FromSeconds(3));
        _throttleGauge.SecondaryValue = FreshMetric(telemetry?.CommandedThrottle, now, TimeSpan.FromSeconds(3));
    }

    /// <summary>Appends one CAN diagnostic line. The log is diagnostic only.</summary>
    /// <param name="entry">The adapter-local diagnostic entry.</param>
    internal void AppendCanDiagnostic(ObdDiagnosticEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        RichTextBox log = _canLog;
        if (log.IsDisposed)
        {
            return;
        }

        _canSummary.Append(entry);
        bool warning = entry.IsWarning
            || entry.Message.Contains("WARNING", StringComparison.OrdinalIgnoreCase)
            || entry.Message.Contains("ERROR", StringComparison.OrdinalIgnoreCase);
        log.SelectionStart = log.TextLength;
        log.SelectionLength = 0;
        log.SelectionColor = warning ? Color.FromArgb(180, 90, 20) : log.ForeColor;
        log.AppendText($"{entry.Timestamp:HH:mm:ss.fff} {entry.Direction} {entry.Message}{Environment.NewLine}");
        int excessLines = log.Lines.Length - CanLogLineLimit;
        if (excessLines > 0)
        {
            string text = log.Text;
            int cut = 0;
            for (int line = 0; line < excessLines; line++)
            {
                cut = text.IndexOf('\n', cut) + 1;
            }

            // RichEdit selection replacement honors ReadOnly; trim synchronously, then lock it again.
            log.ReadOnly = false;
            try
            {
                log.Select(0, cut);
                log.SelectedText = string.Empty;
            }
            finally
            {
                log.ReadOnly = true;
            }
        }

        log.SelectionStart = log.TextLength;
        log.ScrollToCaret();
    }

    private void OnCanDiagnosticDoubleClick(object? sender, EventArgs e)
    {
        if (_canResultsDialog is { IsDisposed: false })
        {
            _canResultsDialog.Activate();

            return;
        }

        _canResultsDialog = new CanDiagnosticResultsDialog(_canSummary.Capture());
        _canResultsDialog.Font = Font;
        _canResultsDialog.Show(FindForm());
    }

    private void OnMercedesDisposed(object? sender, EventArgs e)
    {
        _canResultsDialog?.Dispose();
        _canLog.Font.Dispose();
        if (_mercColumn.Parent is null)
        {
            _mercColumn.Dispose();
        }

        if (_horizon.Parent is null)
        {
            _horizon.Dispose();
        }

        if (_compass.Parent is null)
        {
            _compass.Dispose();
        }
    }

    private static double FreshMetric(SensorMetricReading? reading, DateTimeOffset now, TimeSpan window)
    {
        if (reading is not { State: SensorReadingState.Valid, Value: double value, Timestamp: DateTimeOffset stamp })
        {
            return double.NaN;
        }

        return double.IsFinite(value) && now >= stamp && now - stamp <= window ? value : double.NaN;
    }

    private static Font CreateDiagnosticFont()
    {
        foreach (string family in new[] { "Cascadia Mono", "Cascadia Code", "Consolas" })
        {
            Font candidate = new(family, 12f, FontStyle.Regular, GraphicsUnit.Point);
            if (candidate.FontFamily.Name.Equals(family, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            candidate.Dispose();
        }

        return new Font(FontFamily.GenericMonospace, 12f, FontStyle.Regular, GraphicsUnit.Point);
    }
}
