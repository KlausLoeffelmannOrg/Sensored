namespace WinFormsFusionGauges.WinFormsApp;

partial class InstrumentsView
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _layout = new TableLayoutPanel();
        _speed = new WarpToolkit.WinForms.Sensors.DirectX.D2DSpeedometer();
        _horizon = new WarpToolkit.WinForms.Sensors.DirectX.D2DArtificialHorizon();
        _acceleration = new WarpToolkit.WinForms.Sensors.DirectX.D2DAccelerationGauge();
        _compass = new WarpToolkit.WinForms.Sensors.DirectX.D2DCompassGauge();
        _telemetry = new TelemetryValuesView();
        _calibrationLabel = new Label();
        _mercColumn = new TableLayoutPanel();
        _mercGauges = new TableLayoutPanel();
        _rpmGauge = new WarpToolkit.WinForms.DirectX.Gauges.D2DRpmGauge();
        _temperatureGauge = new WarpToolkit.WinForms.DirectX.Gauges.D2DTemperatureGauge();
        _fuelEnergyGauge = new WarpToolkit.WinForms.DirectX.Gauges.D2DFuelBatteryGauge();
        _throttleGauge = new WarpToolkit.WinForms.DirectX.Gauges.D2DThrottleGauge();
        _canDiagnostics = new GroupBox();
        _canLog = new RichTextBox();
        _layout.SuspendLayout();
        _mercColumn.SuspendLayout();
        _mercGauges.SuspendLayout();
        _canDiagnostics.SuspendLayout();
        SuspendLayout();
        // 
        // _layout
        // 
        _layout.ColumnCount = 3;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _layout.ColumnStyles.Add(new ColumnStyle());
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _layout.Controls.Add(_speed, 0, 0);
        _layout.Controls.Add(_horizon, 2, 0);
        _layout.Controls.Add(_acceleration, 0, 1);
        _layout.Controls.Add(_compass, 2, 1);
        _layout.Controls.Add(_telemetry, 1, 0);
        _layout.Controls.Add(_calibrationLabel, 0, 2);
        _layout.Dock = DockStyle.Fill;
        _layout.Location = new Point(0, 0);
        _layout.Name = "_layout";
        _layout.RowCount = 3;
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _layout.RowStyles.Add(new RowStyle());
        _layout.Size = new Size(1995, 1243);
        _layout.TabIndex = 0;
        // 
        // _speed
        // 
        _speed.AccessibleDescription = "Speed unavailable. GPS receiver speed unavailable.";
        _speed.AccessibleName = "Fusion speed and independent GPS receiver speed";
        _speed.AccelerationMode = WarpToolkit.WinForms.Sensors.DirectX.SpeedometerAccelerationMode.Auto;
        _speed.AccessibleRole = AccessibleRole.Graphic;
        _speed.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _speed.Location = new Point(8, 8);
        _speed.Margin = new Padding(8);
        _speed.Name = "_speed";
        _speed.Padding = new Padding(8);
        _speed.ShowGpsSpeed = true;
        _speed.Size = new Size(710, 581);
        _speed.TabIndex = 0;
        _speed.TabStop = false;
        // 
        // _horizon
        // 
        _horizon.AccessibleName = "Calibrated vehicle artificial horizon";
        _horizon.AccessibleRole = AccessibleRole.Graphic;
        _horizon.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _horizon.Location = new Point(1276, 8);
        _horizon.Margin = new Padding(8);
        _horizon.Name = "_horizon";
        _horizon.Padding = new Padding(8);
        _horizon.Size = new Size(711, 581);
        _horizon.TabIndex = 1;
        _horizon.TabStop = false;
        // 
        // _acceleration
        // 
        _acceleration.AccessibleName = "Gravity-compensated vehicle acceleration";
        _acceleration.AccessibleRole = AccessibleRole.Graphic;
        _acceleration.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _acceleration.Location = new Point(8, 605);
        _acceleration.Margin = new Padding(8);
        _acceleration.Name = "_acceleration";
        _acceleration.Padding = new Padding(8);
        _acceleration.Size = new Size(710, 581);
        _acceleration.TabIndex = 2;
        _acceleration.TabStop = false;
        // 
        // _compass
        // 
        _compass.AccessibleDescription = "Fusion yaw —. Magnetic compass —. GPS course —";
        _compass.AccessibleName = "Vehicle heading from fusion";
        _compass.AccessibleRole = AccessibleRole.Graphic;
        _compass.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _compass.Location = new Point(1276, 605);
        _compass.Margin = new Padding(8);
        _compass.Name = "_compass";
        _compass.Padding = new Padding(8);
        _compass.Size = new Size(711, 581);
        _compass.TabIndex = 3;
        _compass.TabStop = false;
        // 
        // _telemetry
        // 
        _telemetry.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _telemetry.AutoScroll = true;
        _telemetry.Location = new Point(734, 8);
        _telemetry.Margin = new Padding(8);
        _telemetry.MinimumSize = new Size(526, 0);
        _telemetry.Name = "_telemetry";
        _layout.SetRowSpan(_telemetry, 2);
        _telemetry.Size = new Size(526, 1178);
        _telemetry.TabIndex = 4;
        // 
        // _calibrationLabel
        // 
        _calibrationLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _calibrationLabel.AutoSize = true;
        _layout.SetColumnSpan(_calibrationLabel, 3);
        _calibrationLabel.Location = new Point(3, 1194);
        _calibrationLabel.Name = "_calibrationLabel";
        _calibrationLabel.Padding = new Padding(8);
        _calibrationLabel.Size = new Size(1989, 48);
        _calibrationLabel.TabIndex = 5;
        _calibrationLabel.Text = "Horizon / vehicle acceleration need a measured physical mount; open Fusion setup.";
        //
        // _mercColumn
        //
        _mercColumn.Name = "_mercColumn";
        _mercColumn.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _mercColumn.ColumnCount = 1;
        _mercColumn.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mercColumn.RowCount = 2;
        _mercColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33333F));
        _mercColumn.RowStyles.Add(new RowStyle(SizeType.Percent, 66.66667F));
        _mercColumn.Controls.Add(_mercGauges, 0, 0);
        _mercColumn.Controls.Add(_canDiagnostics, 0, 1);
        _mercColumn.Visible = false;
        //
        // _mercGauges
        //
        _mercGauges.Name = "_mercGauges";
        _mercGauges.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _mercGauges.Margin = new Padding(0);
        _mercGauges.ColumnCount = 2;
        _mercGauges.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _mercGauges.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _mercGauges.RowCount = 2;
        _mercGauges.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _mercGauges.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _mercGauges.Controls.Add(_rpmGauge, 0, 0);
        _mercGauges.Controls.Add(_temperatureGauge, 1, 0);
        _mercGauges.Controls.Add(_fuelEnergyGauge, 0, 1);
        _mercGauges.Controls.Add(_throttleGauge, 1, 1);
        //
        // _rpmGauge
        //
        _rpmGauge.Name = "_rpmGauge";
        _rpmGauge.AccessibleName = "Engine speed";
        _rpmGauge.Caption = "Engine speed";
        _rpmGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _rpmGauge.Margin = new Padding(3);
        //
        // _temperatureGauge
        //
        _temperatureGauge.Name = "_temperatureGauge";
        _temperatureGauge.AccessibleName = "Coolant temperature";
        _temperatureGauge.Caption = "Coolant";
        _temperatureGauge.Kind = WarpToolkit.WinForms.DirectX.Gauges.D2DGaugeKind.Temperature;
        _temperatureGauge.Maximum = 140D;
        _temperatureGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _temperatureGauge.Margin = new Padding(3);
        //
        // _fuelEnergyGauge
        //
        _fuelEnergyGauge.Name = "_fuelEnergyGauge";
        _fuelEnergyGauge.AccessibleName = "Fuel level and battery state of charge";
        _fuelEnergyGauge.Caption = "Fuel / Battery";
        _fuelEnergyGauge.Kind = WarpToolkit.WinForms.DirectX.Gauges.D2DGaugeKind.FuelAndBattery;
        _fuelEnergyGauge.Maximum = 100D;
        _fuelEnergyGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _fuelEnergyGauge.Margin = new Padding(3);
        //
        // _throttleGauge
        //
        _throttleGauge.Name = "_throttleGauge";
        _throttleGauge.AccessibleName = "Throttle actual and commanded";
        _throttleGauge.Caption = "Throttle (act/comm'd)";
        _throttleGauge.Kind = WarpToolkit.WinForms.DirectX.Gauges.D2DGaugeKind.ThrottleActualAndCommanded;
        _throttleGauge.Maximum = 100D;
        _throttleGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _throttleGauge.Margin = new Padding(3);
        //
        // _canDiagnostics
        //
        _canDiagnostics.Name = "_canDiagnostics";
        _canDiagnostics.Text = "CAN Diagnostics";
        _canDiagnostics.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _canDiagnostics.Padding = new Padding(8);
        _canDiagnostics.Controls.Add(_canLog);
        //
        // _canLog
        //
        _canLog.Name = "_canLog";
        _canLog.Dock = DockStyle.Fill;
        _canLog.ReadOnly = true;
        _canLog.HideSelection = false;
        _canLog.DetectUrls = false;
        _canLog.BorderStyle = BorderStyle.None;
        _canLog.BackColor = Color.FromArgb(16, 18, 22);
        _canLog.ForeColor = Color.FromArgb(220, 224, 230);
        _canLog.AccessibleDescription = "Double-click to inspect investigative commands, raw readings and decoded candidate values.";
        _canLog.DoubleClick += OnCanDiagnosticDoubleClick;
        // 
        // InstrumentsView
        // 
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Controls.Add(_layout);
        Name = "InstrumentsView";
        Size = new Size(1995, 1243);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        _canDiagnostics.ResumeLayout(false);
        _mercGauges.ResumeLayout(false);
        _mercColumn.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private WarpToolkit.WinForms.Sensors.DirectX.D2DArtificialHorizon _horizon;
    private WarpToolkit.WinForms.Sensors.DirectX.D2DAccelerationGauge _acceleration;
    private WarpToolkit.WinForms.Sensors.DirectX.D2DCompassGauge _compass;
    private WarpToolkit.WinForms.Sensors.DirectX.D2DSpeedometer _speed;
    private Label _calibrationLabel;
    private TelemetryValuesView _telemetry;
    private TableLayoutPanel _mercColumn;
    private TableLayoutPanel _mercGauges;
    private WarpToolkit.WinForms.DirectX.Gauges.D2DRpmGauge _rpmGauge;
    private WarpToolkit.WinForms.DirectX.Gauges.D2DTemperatureGauge _temperatureGauge;
    private WarpToolkit.WinForms.DirectX.Gauges.D2DFuelBatteryGauge _fuelEnergyGauge;
    private WarpToolkit.WinForms.DirectX.Gauges.D2DThrottleGauge _throttleGauge;
    private GroupBox _canDiagnostics;
    private RichTextBox _canLog;
}
