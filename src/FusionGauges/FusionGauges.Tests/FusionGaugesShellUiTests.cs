using System.Reflection;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Bluetooth;
using WarpToolkit.WinForms.DirectX.Gauges;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Covers the combined toolbar, Mercedes column, and shutdown terminal.</summary>
public sealed class FusionGaugesShellUiTests
{
    [Fact]
    public Task Toolbar_uses_the_combined_command_order()
        => RunStaAsync(() =>
        {
            using MainForm form = new(new SensorHub());
            ToolStrip toolbar = Field<ToolStrip>(form, "_toolbar");
            ToolStripItem[] root = toolbar.Items.Cast<ToolStripItem>().ToArray();
            Assert.Equal(
            [
                "Start sensors", "Start recording", "Marker", "|",
                "Instruments", "Diagnostics", "Log", "Retry ZIPs", "|",
                "Fusion setup", "Options", "Speed source", "Full screen", "|",
                "Special", "Clipboard", "|",
                "Exit"
            ], root.Select(item => item is ToolStripSeparator ? "|" : item.Text).ToArray());
            Assert.Equal(4, root.Count(item => item is ToolStripSeparator));
            Assert.Equal([3, 8, 13, 16], root
                .Select((item, index) => item is ToolStripSeparator ? index : -1)
                .Where(index => index >= 0)
                .ToArray());
            Assert.Equal("Log", Field<ToolStripButton>(form, "_recordingTabButton").Text);
            Assert.Equal("Fusion setup", Field<ToolStripButton>(form, "_calibrateButton").Text);
            Assert.True(Field<ToolStripButton>(form, "_calibrateButton").Enabled);
            Assert.Equal(5, toolbar is WarpToolkit.WinForms.ToolStrips.SymbolToolStrip strip
                ? strip.CarModeCommandTimeoutSeconds : -1);
            ToolStripMenuItem clipboard = Field<ToolStripMenuItem>(form, "_clipboardButton");
            Assert.Equal(["Copy Diagnostics", "Copy Log", "Copy CAN Diagnostics", "Copy all"],
                clipboard.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text).ToArray());
        });

    [Fact]
    public Task Clipboard_snapshot_contains_full_text_from_hidden_panels()
        => RunStaAsync(() =>
        {
            using MainForm form = new(new SensorHub());
            DiagnosticsView diagnostics = Field<DiagnosticsView>(form, "_diagnostics");
            RecordingView recording = Field<RecordingView>(form, "_recordingView");
            InstrumentsView instruments = Field<InstrumentsView>(form, "_instruments");
            Field<TextBox>(diagnostics, "_details").Text = "diagnostic first\r\ndiagnostic last";
            Field<TextBox>(recording, "_details").Text = "log first\r\nlog last";
            instruments.AppendCanDiagnostic(new ObdDiagnosticEntry(DateTimeOffset.UtcNow, "RX", "CAN payload"));
            instruments.SetMercedesLayout(false);
            Assert.Equal("diagnostic first\r\ndiagnostic last", diagnostics.DiagnosticText);
            Assert.Equal("log first\r\nlog last", recording.DiagnosticText);
            Assert.Contains("CAN payload", instruments.CanDiagnosticText);
            Assert.Equal(
                $"Diagnostics{Environment.NewLine}{diagnostics.DiagnosticText}{Environment.NewLine}{Environment.NewLine}"
                    + $"Log{Environment.NewLine}{recording.DiagnosticText}{Environment.NewLine}{Environment.NewLine}"
                    + $"CAN Diagnostics{Environment.NewLine}{instruments.CanDiagnosticText}",
                form.AllDiagnosticText);
            instruments.SetMercedesLayout(true);
            Assert.Contains("CAN payload", instruments.CanDiagnosticText);
        });

    [Fact]
    public Task Can_results_remain_available_after_the_rolling_terminal_is_trimmed()
        => RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "TX", "STM"));
            view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "RX", "105 09 C4"));
            view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "INFO", "Result: STM: 1 raw frames, 1 signal candidates."));
            for (int index = 0; index < 450; index++)
            {
                view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "INFO", $"terminal entry {index}"));
            }

            Assert.DoesNotContain("105 09 C4", view.CanDiagnosticText);
            Assert.InRange(Field<RichTextBox>(view, "_canLog").Lines.Length, 1, 400);
            CanDiagnosticSummary summary = Field<CanDiagnosticSummary>(view, "_canSummary");
            using CanDiagnosticResultsDialog dialog = new(summary.Capture());
            DataGridView grid = Field<DataGridView>(dialog, "_results");
            Assert.True(grid.ReadOnly);
            Assert.False(grid.AllowUserToAddRows);
            Assert.Equal(DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText, grid.ClipboardCopyMode);
            Assert.Equal(10, grid.Columns.Count);
            DataGridViewRow rpm = grid.Rows.Cast<DataGridViewRow>()
                .Single(row => Equals(row.Cells[1].Value, "EngineRpm"));
            Assert.Equal("105 09 C4", rpm.Cells[5].Value);
            Assert.Equal("2500", rpm.Cells[7].Value);
            Assert.Contains(grid.Rows.Cast<DataGridViewRow>(), row =>
                Equals(row.Cells[1].Value, "EngineTorque")
                    && row.Cells[2].Value is string result && result.StartsWith("No data observed"));
        });

    [Fact]
    public Task Speedometer_enables_automatic_forward_acceleration_with_four_measurements()
        => RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            D2DSpeedometer speed = Field<D2DSpeedometer>(view, "_speed");
            Assert.Equal(SpeedometerAccelerationMode.Auto, speed.AccelerationMode);
            Assert.Equal(4, speed.AccelerationSampleCount);
        });

    [Fact]
    public Task Double_clicking_CAN_log_opens_a_result_snapshot_without_new_adapter_commands()
        => RunStaAsync(() =>
        {
            using Form host = new();
            using InstrumentsView view = new();
            host.Controls.Add(view);
            view.SetMercedesLayout(true);
            view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "TX", "ATCSM1"));
            view.AppendCanDiagnostic(new(DateTimeOffset.UtcNow, "RX", "OK"));
            string before = view.CanDiagnosticText;
            typeof(Control).GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Field<RichTextBox>(view, "_canLog"), [EventArgs.Empty]);
            using CanDiagnosticResultsDialog dialog = Field<CanDiagnosticResultsDialog>(view, "_canResultsDialog");
            Assert.True(dialog.Visible);
            Assert.Equal(before, view.CanDiagnosticText);
            typeof(Control).GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(Field<RichTextBox>(view, "_canLog"), [EventArgs.Empty]);
            Assert.Same(dialog, Field<CanDiagnosticResultsDialog>(view, "_canResultsDialog"));
            dialog.Close();
        });

    [Theory]
    [InlineData(600, 900, 12)]
    [InlineData(1200, 1800, 24)]
    public Task Mercedes_layout_reserves_two_thirds_for_diagnostics(int width, int height, int fontSize)
        => RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            using Font font = new("Segoe UI", fontSize);
            view.Font = font;
            view.Size = new Size(width, height);
            view.SetMercedesLayout(true);
            TableLayoutPanel column = Field<TableLayoutPanel>(view, "_mercColumn");
            column.PerformLayout();
            int[] rows = column.GetRowHeights();
            int availableHeight = column.ClientSize.Height;
            Assert.True(availableHeight > 0);
            Assert.InRange(Math.Abs(rows[0] - availableHeight / 3d), 0d, 1d);
            Assert.InRange(Math.Abs(rows[1] - availableHeight * 2d / 3d), 0d, 1d);
            TableLayoutPanel gauges = Field<TableLayoutPanel>(view, "_mercGauges");
            Assert.Equal(4, gauges.Controls.Count);
            Assert.Equal(new TableLayoutPanelCellPosition(0, 1),
                gauges.GetPositionFromControl(Field<Control>(view, "_fuelEnergyGauge")));
            Assert.Equal(new TableLayoutPanelCellPosition(1, 1),
                gauges.GetPositionFromControl(Field<Control>(view, "_throttleGauge")));
        });

    [Fact]
    public Task Automatic_recording_disables_manual_start_until_a_recording_exists()
        => RunStaAsync(() =>
        {
            using MainForm form = new(new SensorHub());
            typeof(MainForm).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, new FusionGaugesOptions { AutoRecording = true });
            typeof(MainForm).GetMethod("UpdateCommands", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            ToolStripButton record = Field<ToolStripButton>(form, "_recordButton");
            Assert.False(record.Enabled);
            Assert.Equal("Start recording", record.Text);
        });

    [Fact]
    public Task Mercedes_layout_replaces_only_the_right_column_and_restores_it()
        => RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            TableLayoutPanel layout = Field<TableLayoutPanel>(view, "_layout");
            Control horizon = Field<Control>(view, "_horizon");
            Control compass = Field<Control>(view, "_compass");
            Assert.Equal(new TableLayoutPanelCellPosition(2, 0), layout.GetPositionFromControl(horizon));
            view.SetMercedesLayout(true);
            Assert.False(horizon.IsDisposed);
            Assert.False(compass.IsDisposed);
            Assert.False(layout.Controls.Contains(horizon));
            Assert.True(layout.Controls.Contains(Field<Control>(view, "_mercColumn")));
            view.AppendCanDiagnostic(new ObdDiagnosticEntry(
                DateTimeOffset.Parse("2026-01-02T03:04:05Z"), "RX", "STN capture", IsWarning: true));
            RichTextBox log = Field<RichTextBox>(view, "_canLog");
            Assert.Contains("STN capture", log.Text);
            Assert.Equal(12f, log.Font.SizeInPoints);
            view.PresentVehicle(new VehicleTelemetryPayload
            {
                Rpm = new SensorMetricReading(1500, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                CoolantCelsius = new SensorMetricReading(90, DateTimeOffset.UtcNow.AddMinutes(-1), SensorReadingState.Valid),
                FuelPercent = new SensorMetricReading(75, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                BatteryPercent = new SensorMetricReading(42, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                ActualThrottle = new SensorMetricReading(19, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                CommandedThrottle = new SensorMetricReading(23, DateTimeOffset.UtcNow, SensorReadingState.Valid)
            });
            Assert.Equal(1500d, Field<WarpToolkit.WinForms.DirectX.Gauges.D2DLedGauge>(view, "_rpmGauge").Value);
            Assert.True(double.IsNaN(Field<WarpToolkit.WinForms.DirectX.Gauges.D2DLedGauge>(view, "_temperatureGauge").Value));
            D2DLedGauge fuelEnergy = Field<D2DLedGauge>(view, "_fuelEnergyGauge");
            Assert.Equal(D2DGaugeKind.FuelAndBattery, fuelEnergy.Kind);
            Assert.Equal(75d, fuelEnergy.Value);
            Assert.Equal(42d, fuelEnergy.BatteryValue);
            D2DLedGauge throttle = Field<D2DLedGauge>(view, "_throttleGauge");
            Assert.Equal("Throttle (act/comm'd)", throttle.Caption);
            Assert.Equal(D2DGaugeKind.ThrottleActualAndCommanded, throttle.Kind);
            Assert.Equal(19d, throttle.Value);
            Assert.Equal(23d, throttle.SecondaryValue);
            view.PresentVehicle(new VehicleTelemetryPayload
            {
                FuelPercent = new SensorMetricReading(75, DateTimeOffset.UtcNow.AddMinutes(-1), SensorReadingState.Valid),
                BatteryPercent = new SensorMetricReading(42, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                ActualThrottle = new SensorMetricReading(19, DateTimeOffset.UtcNow, SensorReadingState.Valid),
                CommandedThrottle = new SensorMetricReading(23, DateTimeOffset.UtcNow.AddMinutes(1), SensorReadingState.Valid)
            });
            Assert.True(double.IsNaN(fuelEnergy.Value));
            Assert.Equal(42d, fuelEnergy.BatteryValue);
            Assert.Equal(19d, throttle.Value);
            Assert.True(double.IsNaN(throttle.SecondaryValue));
            view.PresentVehicle(null);
            Assert.All(new[] { fuelEnergy.Value, fuelEnergy.BatteryValue, throttle.Value, throttle.SecondaryValue },
                value => Assert.True(double.IsNaN(value)));
            view.SetMercedesLayout(false);
            Assert.Equal(new TableLayoutPanelCellPosition(2, 0), layout.GetPositionFromControl(horizon));
            Assert.Equal(new TableLayoutPanelCellPosition(2, 1), layout.GetPositionFromControl(compass));
            Assert.False(horizon.IsDisposed);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task Disposing_instruments_also_disposes_the_detached_column(bool mercedes)
        => RunStaAsync(() =>
        {
            InstrumentsView view = new();
            view.SetMercedesLayout(mercedes);
            Control horizon = Field<Control>(view, "_horizon");
            Control compass = Field<Control>(view, "_compass");
            Control column = Field<Control>(view, "_mercColumn");
            view.Dispose();
            Assert.True(horizon.IsDisposed);
            Assert.True(compass.IsDisposed);
            Assert.True(column.IsDisposed);
        });

    [Fact]
    public Task Shutdown_terminal_stays_selectable_and_keeps_the_shell_enabled()
        => RunStaAsync(() =>
        {
            using MainForm form = new(new SensorHub());
            form.ShowShutdownTerminal();
            form.AppendShutdown("BEGIN: Stop Bluetooth polling and CAN monitoring");
            form.AppendShutdown("WARNING: Pending ZIPs are retained locally; use Retry ZIPs next time.");
            form.AppendShutdown("ERROR: Dispose acquisition service: boom");
            Assert.True(form.Enabled);
            RichTextBox log = (RichTextBox)form.Controls["_shutdownLog"]!;
            Assert.True(log.Enabled);
            Assert.True(log.ReadOnly);
            Assert.False(log.HideSelection);
            Assert.Equal(12f, log.Font.SizeInPoints);
            Assert.Contains("WARNING", log.Text);
            Assert.Contains("ERROR", log.Text);
            Assert.Contains("BEGIN", log.Text);
        });

    private static T Field<T>(object owner, string name)
        => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static Task RunStaAsync(Action action)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() =>
        {
            try
            {
                action();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }
}
