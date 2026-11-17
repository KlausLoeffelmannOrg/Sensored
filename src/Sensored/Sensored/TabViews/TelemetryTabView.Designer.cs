#nullable disable

using WarpToolkit.WinForms.DirectX.Components;
using WarpToolkit.WinForms.DirectX.Controls;

namespace Sensored.TabViews;

partial class TelemetryTabView
{
    private System.ComponentModel.IContainer components;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _telemetryLayout = new TableLayoutPanel();
        _gpsGroupBox = new GroupBox();
        _gpsValueLayout = new TableLayoutPanel();
        _longitudeLabel = new AnimatedValueLabel();
        _gpsValueLayoutManager = new ValueLabelLayoutManager();
        _latitudeLabel = new AnimatedValueLabel();
        _heightLabel = new AnimatedValueLabel();
        _gpsSpeedLabel = new AnimatedValueLabel();
        _gpsFourPointSpeedLabel = new AnimatedValueLabel();
        _gpsCourseLabel = new AnimatedValueLabel();
        _gpsHorizontalAccuracyLabel = new AnimatedValueLabel();
        _gpsVerticalAccuracyLabel = new AnimatedValueLabel();
        _gpsSatelliteLabel = new AnimatedValueLabel();
        _gpsSourceLabel = new Label();
        _motionValueLayout = new TableLayoutPanel();
        _horizonGroupBox = new GroupBox();
        _horizonValueLayout = new TableLayoutPanel();
        _yawLabel = new AnimatedValueLabel();
        _horizonValueLayoutManager = new ValueLabelLayoutManager();
        _rollLabel = new AnimatedValueLabel();
        _pitchLabel = new AnimatedValueLabel();
        _fusionTrustDiagnostic = new Label();
        _accelerationGroupBox = new GroupBox();
        _accelerationValueLayout = new TableLayoutPanel();
        _forwardAccelerationLabel = new AnimatedValueLabel();
        _accelerationValueLayoutManager = new ValueLabelLayoutManager();
        _lateralAccelerationLabel = new AnimatedValueLabel();
        _verticalAccelerationLabel = new AnimatedValueLabel();
        _rawAccelerationDiagnostic = new Label();
        _telemetryLayout.SuspendLayout();
        _gpsGroupBox.SuspendLayout();
        _gpsValueLayout.SuspendLayout();
        _motionValueLayout.SuspendLayout();
        _horizonGroupBox.SuspendLayout();
        _horizonValueLayout.SuspendLayout();
        _accelerationGroupBox.SuspendLayout();
        _accelerationValueLayout.SuspendLayout();
        SuspendLayout();
        // 
        // _telemetryLayout
        // 
        _telemetryLayout.AutoSize = true;
        _telemetryLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _telemetryLayout.ColumnCount = 2;
        _telemetryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _telemetryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _telemetryLayout.Controls.Add(_gpsGroupBox, 0, 0);
        _telemetryLayout.Controls.Add(_motionValueLayout, 1, 0);
        _telemetryLayout.Dock = DockStyle.Fill;
        _telemetryLayout.Location = new Point(0, 0);
        _telemetryLayout.Margin = new Padding(4, 5, 4, 5);
        _telemetryLayout.Name = "_telemetryLayout";
        _telemetryLayout.RowCount = 2;
        _telemetryLayout.RowStyles.Add(new RowStyle());
        _telemetryLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _telemetryLayout.Size = new Size(1415, 809);
        _telemetryLayout.TabIndex = 0;
        // 
        // _gpsGroupBox
        // 
        _gpsGroupBox.AutoSize = true;
        _gpsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _gpsGroupBox.Controls.Add(_gpsValueLayout);
        _gpsGroupBox.Dock = DockStyle.Fill;
        _gpsGroupBox.FlatStyle = FlatStyle.Popup;
        _gpsGroupBox.Location = new Point(8, 10);
        _gpsGroupBox.Margin = new Padding(8, 10, 8, 10);
        _gpsGroupBox.Name = "_gpsGroupBox";
        _gpsGroupBox.Padding = new Padding(9, 10, 9, 10);
        _gpsGroupBox.Size = new Size(691, 697);
        _gpsGroupBox.TabIndex = 0;
        _gpsGroupBox.TabStop = false;
        _gpsGroupBox.Text = "GPS";
        // 
        // _gpsValueLayout
        // 
        _gpsValueLayout.AutoSize = true;
        _gpsValueLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _gpsValueLayout.ColumnCount = 1;
        _gpsValueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gpsValueLayout.Controls.Add(_longitudeLabel, 0, 0);
        _gpsValueLayout.Controls.Add(_latitudeLabel, 0, 2);
        _gpsValueLayout.Controls.Add(_heightLabel, 0, 6);
        _gpsValueLayout.Controls.Add(_gpsSpeedLabel, 0, 7);
        _gpsValueLayout.Controls.Add(_gpsFourPointSpeedLabel, 0, 8);
        _gpsValueLayout.Controls.Add(_gpsCourseLabel, 0, 9);
        _gpsValueLayout.Controls.Add(_gpsHorizontalAccuracyLabel, 0, 3);
        _gpsValueLayout.Controls.Add(_gpsVerticalAccuracyLabel, 0, 1);
        _gpsValueLayout.Controls.Add(_gpsSatelliteLabel, 0, 4);
        _gpsValueLayout.Controls.Add(_gpsSourceLabel, 0, 10);
        _gpsValueLayout.Dock = DockStyle.Fill;
        _gpsValueLayout.Location = new Point(13, 67);
        _gpsValueLayout.Margin = new Padding(4, 5, 4, 5);
        _gpsValueLayout.Name = "_gpsValueLayout";
        _gpsValueLayout.RowCount = 12;
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle());
        _gpsValueLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _gpsValueLayout.Size = new Size(665, 616);
        _gpsValueLayout.TabIndex = 0;
        // 
        // _longitudeLabel
        // 
        _longitudeLabel.AccessibleName = "Lng";
        _longitudeLabel.AccessibleRole = AccessibleRole.StaticText;
        _longitudeLabel.BackColor = SystemColors.Control;
        _longitudeLabel.BoldCaption = true;
        _longitudeLabel.Caption = "Lng";
        _longitudeLabel.DecimalPlaces = 7;
        _longitudeLabel.Dimension = "°";
        _longitudeLabel.Dock = DockStyle.Fill;
        _longitudeLabel.FixedDecimalPointPosition = 96;
        _longitudeLabel.ForeColor = SystemColors.ControlText;
        _longitudeLabel.LayoutManager = _gpsValueLayoutManager;
        _longitudeLabel.Location = new Point(10, 10);
        _longitudeLabel.Margin = new Padding(10);
        _longitudeLabel.MaximumValue = 180D;
        _longitudeLabel.MinimumValue = -180D;
        _longitudeLabel.Name = "_longitudeLabel";
        _longitudeLabel.Padding = new Padding(5);
        _longitudeLabel.Size = new Size(645, 46);
        _longitudeLabel.TabIndex = 0;
        _longitudeLabel.ValueEffectRendering = ValueEffectRendering.None;
        // 
        // _latitudeLabel
        // 
        _latitudeLabel.AccessibleName = "Lat";
        _latitudeLabel.AccessibleRole = AccessibleRole.StaticText;
        _latitudeLabel.BackColor = SystemColors.Control;
        _latitudeLabel.BoldCaption = true;
        _latitudeLabel.Caption = "Lat";
        _latitudeLabel.DecimalPlaces = 7;
        _latitudeLabel.Dimension = "°";
        _latitudeLabel.Dock = DockStyle.Fill;
        _latitudeLabel.FixedDecimalPointPosition = 96;
        _latitudeLabel.ForeColor = SystemColors.ControlText;
        _latitudeLabel.LayoutManager = _gpsValueLayoutManager;
        _latitudeLabel.Location = new Point(10, 142);
        _latitudeLabel.Margin = new Padding(10);
        _latitudeLabel.MaximumValue = 90D;
        _latitudeLabel.MinimumValue = -90D;
        _latitudeLabel.Name = "_latitudeLabel";
        _latitudeLabel.Padding = new Padding(5);
        _latitudeLabel.Size = new Size(645, 46);
        _latitudeLabel.TabIndex = 1;
        _latitudeLabel.ValueEffectRendering = ValueEffectRendering.None;
        // 
        // _heightLabel
        // 
        _heightLabel.AccessibleName = "Hgt";
        _heightLabel.AccessibleRole = AccessibleRole.StaticText;
        _heightLabel.BackColor = SystemColors.Control;
        _heightLabel.BoldCaption = true;
        _heightLabel.Caption = "Hgt";
        _heightLabel.DecimalPlaces = 1;
        _heightLabel.Dimension = "m";
        _heightLabel.Dock = DockStyle.Fill;
        _heightLabel.FixedDecimalPointPosition = 96;
        _heightLabel.ForeColor = SystemColors.ControlText;
        _heightLabel.LayoutManager = _gpsValueLayoutManager;
        _heightLabel.Location = new Point(10, 340);
        _heightLabel.Margin = new Padding(10);
        _heightLabel.MaximumValue = 300D;
        _heightLabel.MinimumValue = -50D;
        _heightLabel.Name = "_heightLabel";
        _heightLabel.Padding = new Padding(5);
        _heightLabel.Size = new Size(645, 46);
        _heightLabel.TabIndex = 2;
        // 
        // _gpsSpeedLabel
        // 
        _gpsSpeedLabel.AccessibleName = "Speed km/h";
        _gpsSpeedLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsSpeedLabel.BackColor = SystemColors.Control;
        _gpsSpeedLabel.Caption = "Speed km/h";
        _gpsSpeedLabel.DecimalPlaces = 1;
        _gpsSpeedLabel.Dock = DockStyle.Fill;
        _gpsSpeedLabel.FixedDecimalPointPosition = 96;
        _gpsSpeedLabel.ForeColor = SystemColors.ControlText;
        _gpsSpeedLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsSpeedLabel.Location = new Point(3, 401);
        _gpsSpeedLabel.MaximumValue = 250D;
        _gpsSpeedLabel.MinimumValue = 0D;
        _gpsSpeedLabel.Name = "_gpsSpeedLabel";
        _gpsSpeedLabel.Padding = new Padding(5);
        _gpsSpeedLabel.Size = new Size(659, 46);
        _gpsSpeedLabel.TabIndex = 3;
        _gpsSpeedLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _gpsFourPointSpeedLabel
        //
        _gpsFourPointSpeedLabel.AccessibleName = "Four-position GPS speed km/h";
        _gpsFourPointSpeedLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsFourPointSpeedLabel.BackColor = SystemColors.Control;
        _gpsFourPointSpeedLabel.Caption = "GPS 4-point km/h";
        _gpsFourPointSpeedLabel.DecimalPlaces = 1;
        _gpsFourPointSpeedLabel.Dock = DockStyle.Fill;
        _gpsFourPointSpeedLabel.FixedDecimalPointPosition = 96;
        _gpsFourPointSpeedLabel.ForeColor = SystemColors.ControlText;
        _gpsFourPointSpeedLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsFourPointSpeedLabel.Location = new Point(3, 457);
        _gpsFourPointSpeedLabel.MaximumValue = 250D;
        _gpsFourPointSpeedLabel.MinimumValue = 0D;
        _gpsFourPointSpeedLabel.Name = "_gpsFourPointSpeedLabel";
        _gpsFourPointSpeedLabel.Padding = new Padding(5);
        _gpsFourPointSpeedLabel.Size = new Size(659, 46);
        _gpsFourPointSpeedLabel.TabIndex = 4;
        _gpsFourPointSpeedLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _gpsCourseLabel
        // 
        _gpsCourseLabel.AccessibleName = "Course";
        _gpsCourseLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsCourseLabel.BackColor = SystemColors.Control;
        _gpsCourseLabel.Caption = "Course";
        _gpsCourseLabel.DecimalPlaces = 1;
        _gpsCourseLabel.Dimension = "°";
        _gpsCourseLabel.Dock = DockStyle.Fill;
        _gpsCourseLabel.FixedDecimalPointPosition = 96;
        _gpsCourseLabel.ForeColor = SystemColors.ControlText;
        _gpsCourseLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsCourseLabel.Location = new Point(3, 513);
        _gpsCourseLabel.MaximumValue = 360D;
        _gpsCourseLabel.MinimumValue = 0D;
        _gpsCourseLabel.Name = "_gpsCourseLabel";
        _gpsCourseLabel.Padding = new Padding(5);
        _gpsCourseLabel.Size = new Size(659, 46);
        _gpsCourseLabel.TabIndex = 4;
        // 
        // _gpsHorizontalAccuracyLabel
        // 
        _gpsHorizontalAccuracyLabel.AccessibleName = "Acc H";
        _gpsHorizontalAccuracyLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsHorizontalAccuracyLabel.BackColor = SystemColors.Control;
        _gpsHorizontalAccuracyLabel.Caption = "Acc H";
        _gpsHorizontalAccuracyLabel.DecimalPlaces = 1;
        _gpsHorizontalAccuracyLabel.Dimension = "m";
        _gpsHorizontalAccuracyLabel.Dock = DockStyle.Fill;
        _gpsHorizontalAccuracyLabel.FixedDecimalPointPosition = 96;
        _gpsHorizontalAccuracyLabel.ForeColor = SystemColors.ControlText;
        _gpsHorizontalAccuracyLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsHorizontalAccuracyLabel.Location = new Point(10, 208);
        _gpsHorizontalAccuracyLabel.Margin = new Padding(10);
        _gpsHorizontalAccuracyLabel.MaximumValue = 100D;
        _gpsHorizontalAccuracyLabel.MinimumValue = 0D;
        _gpsHorizontalAccuracyLabel.Name = "_gpsHorizontalAccuracyLabel";
        _gpsHorizontalAccuracyLabel.Padding = new Padding(5);
        _gpsHorizontalAccuracyLabel.Size = new Size(645, 46);
        _gpsHorizontalAccuracyLabel.TabIndex = 5;
        // 
        // _gpsVerticalAccuracyLabel
        // 
        _gpsVerticalAccuracyLabel.AccessibleName = "Acc V";
        _gpsVerticalAccuracyLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsVerticalAccuracyLabel.BackColor = SystemColors.Control;
        _gpsVerticalAccuracyLabel.Caption = "Acc V";
        _gpsVerticalAccuracyLabel.DecimalPlaces = 1;
        _gpsVerticalAccuracyLabel.Dimension = "m";
        _gpsVerticalAccuracyLabel.Dock = DockStyle.Fill;
        _gpsVerticalAccuracyLabel.FixedDecimalPointPosition = 96;
        _gpsVerticalAccuracyLabel.ForeColor = SystemColors.ControlText;
        _gpsVerticalAccuracyLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsVerticalAccuracyLabel.Location = new Point(10, 76);
        _gpsVerticalAccuracyLabel.Margin = new Padding(10);
        _gpsVerticalAccuracyLabel.MaximumValue = 100D;
        _gpsVerticalAccuracyLabel.MinimumValue = 0D;
        _gpsVerticalAccuracyLabel.Name = "_gpsVerticalAccuracyLabel";
        _gpsVerticalAccuracyLabel.Padding = new Padding(5);
        _gpsVerticalAccuracyLabel.Size = new Size(645, 46);
        _gpsVerticalAccuracyLabel.TabIndex = 6;
        // 
        // _gpsSatelliteLabel
        // 
        _gpsSatelliteLabel.AccessibleName = "Satellites";
        _gpsSatelliteLabel.AccessibleRole = AccessibleRole.StaticText;
        _gpsSatelliteLabel.BackColor = SystemColors.Control;
        _gpsSatelliteLabel.Caption = "Satellites";
        _gpsSatelliteLabel.DecimalPlaces = 0;
        _gpsSatelliteLabel.Dock = DockStyle.Fill;
        _gpsSatelliteLabel.FixedDecimalPointPosition = 96;
        _gpsSatelliteLabel.ForeColor = SystemColors.ControlText;
        _gpsSatelliteLabel.LayoutManager = _gpsValueLayoutManager;
        _gpsSatelliteLabel.Location = new Point(10, 274);
        _gpsSatelliteLabel.Margin = new Padding(10);
        _gpsSatelliteLabel.MaximumValue = 32D;
        _gpsSatelliteLabel.MinimumValue = 0D;
        _gpsSatelliteLabel.Name = "_gpsSatelliteLabel";
        _gpsSatelliteLabel.Padding = new Padding(5);
        _gpsSatelliteLabel.Size = new Size(645, 46);
        _gpsSatelliteLabel.TabIndex = 7;
        // 
        // _gpsSourceLabel
        // 
        _gpsSourceLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _gpsSourceLabel.AutoEllipsis = true;
        _gpsSourceLabel.AutoSize = true;
        _gpsSourceLabel.Location = new Point(10, 574);
        _gpsSourceLabel.Margin = new Padding(10);
        _gpsSourceLabel.Name = "_gpsSourceLabel";
        _gpsSourceLabel.Size = new Size(645, 32);
        _gpsSourceLabel.TabIndex = 8;
        _gpsSourceLabel.Text = "Waiting for a position.";
        _gpsSourceLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _motionValueLayout
        // 
        _motionValueLayout.AutoSize = true;
        _motionValueLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _motionValueLayout.ColumnCount = 1;
        _motionValueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _motionValueLayout.Controls.Add(_horizonGroupBox, 0, 0);
        _motionValueLayout.Controls.Add(_accelerationGroupBox, 0, 1);
        _motionValueLayout.Dock = DockStyle.Fill;
        _motionValueLayout.Location = new Point(711, 5);
        _motionValueLayout.Margin = new Padding(4, 5, 4, 5);
        _motionValueLayout.Name = "_motionValueLayout";
        _motionValueLayout.RowCount = 3;
        _motionValueLayout.RowStyles.Add(new RowStyle());
        _motionValueLayout.RowStyles.Add(new RowStyle());
        _motionValueLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _motionValueLayout.Size = new Size(700, 707);
        _motionValueLayout.TabIndex = 1;
        // 
        // _horizonGroupBox
        // 
        _horizonGroupBox.AutoSize = true;
        _horizonGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _horizonGroupBox.Controls.Add(_horizonValueLayout);
        _horizonGroupBox.Dock = DockStyle.Fill;
        _horizonGroupBox.FlatStyle = FlatStyle.Popup;
        _horizonGroupBox.Location = new Point(4, 5);
        _horizonGroupBox.Margin = new Padding(4, 5, 4, 5);
        _horizonGroupBox.Name = "_horizonGroupBox";
        _horizonGroupBox.Size = new Size(692, 317);
        _horizonGroupBox.TabIndex = 0;
        _horizonGroupBox.TabStop = false;
        _horizonGroupBox.Text = "Vehicle horizon";
        // 
        // _horizonValueLayout
        // 
        _horizonValueLayout.AutoSize = true;
        _horizonValueLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _horizonValueLayout.ColumnCount = 1;
        _horizonValueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _horizonValueLayout.Controls.Add(_yawLabel, 0, 2);
        _horizonValueLayout.Controls.Add(_rollLabel, 0, 0);
        _horizonValueLayout.Controls.Add(_pitchLabel, 0, 1);
        _horizonValueLayout.Controls.Add(_fusionTrustDiagnostic, 0, 3);
        _horizonValueLayout.Dock = DockStyle.Fill;
        _horizonValueLayout.Location = new Point(7, 60);
        _horizonValueLayout.Margin = new Padding(4, 5, 4, 5);
        _horizonValueLayout.Name = "_horizonValueLayout";
        _horizonValueLayout.RowCount = 4;
        _horizonValueLayout.RowStyles.Add(new RowStyle());
        _horizonValueLayout.RowStyles.Add(new RowStyle());
        _horizonValueLayout.RowStyles.Add(new RowStyle());
        _horizonValueLayout.RowStyles.Add(new RowStyle());
        _horizonValueLayout.Size = new Size(678, 250);
        _horizonValueLayout.TabIndex = 0;
        // 
        // _yawLabel
        // 
        _yawLabel.AccessibleName = "Yaw";
        _yawLabel.AccessibleRole = AccessibleRole.StaticText;
        _yawLabel.BackColor = SystemColors.Control;
        _yawLabel.BoldCaption = true;
        _yawLabel.Caption = "Yaw";
        _yawLabel.Dimension = "°";
        _yawLabel.Dock = DockStyle.Fill;
        _yawLabel.FixedDecimalPointPosition = 96;
        _yawLabel.ForeColor = SystemColors.ControlText;
        _yawLabel.LayoutManager = _horizonValueLayoutManager;
        _yawLabel.Location = new Point(10, 142);
        _yawLabel.Margin = new Padding(10);
        _yawLabel.MaximumValue = 360D;
        _yawLabel.MinimumValue = 0D;
        _yawLabel.Name = "_yawLabel";
        _yawLabel.Padding = new Padding(5);
        _yawLabel.Size = new Size(658, 46);
        _yawLabel.TabIndex = 2;
        _yawLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _rollLabel
        // 
        _rollLabel.AccessibleName = "Roll";
        _rollLabel.AccessibleRole = AccessibleRole.StaticText;
        _rollLabel.BackColor = SystemColors.Control;
        _rollLabel.BoldCaption = true;
        _rollLabel.Caption = "Roll";
        _rollLabel.Dimension = "°";
        _rollLabel.Dock = DockStyle.Fill;
        _rollLabel.FixedDecimalPointPosition = 96;
        _rollLabel.ForeColor = SystemColors.ControlText;
        _rollLabel.LayoutManager = _horizonValueLayoutManager;
        _rollLabel.Location = new Point(10, 10);
        _rollLabel.Margin = new Padding(10);
        _rollLabel.MaximumValue = 90D;
        _rollLabel.MinimumValue = -90D;
        _rollLabel.Name = "_rollLabel";
        _rollLabel.Padding = new Padding(5);
        _rollLabel.Size = new Size(658, 46);
        _rollLabel.TabIndex = 0;
        _rollLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _pitchLabel
        // 
        _pitchLabel.AccessibleName = "Pitch";
        _pitchLabel.AccessibleRole = AccessibleRole.StaticText;
        _pitchLabel.BackColor = SystemColors.Control;
        _pitchLabel.BoldCaption = true;
        _pitchLabel.Caption = "Pitch";
        _pitchLabel.Dimension = "°";
        _pitchLabel.Dock = DockStyle.Fill;
        _pitchLabel.FixedDecimalPointPosition = 96;
        _pitchLabel.ForeColor = SystemColors.ControlText;
        _pitchLabel.LayoutManager = _horizonValueLayoutManager;
        _pitchLabel.Location = new Point(10, 76);
        _pitchLabel.Margin = new Padding(10);
        _pitchLabel.MaximumValue = 90D;
        _pitchLabel.MinimumValue = -90D;
        _pitchLabel.Name = "_pitchLabel";
        _pitchLabel.Padding = new Padding(5);
        _pitchLabel.Size = new Size(658, 46);
        _pitchLabel.TabIndex = 1;
        _pitchLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _fusionTrustDiagnostic
        // 
        _fusionTrustDiagnostic.AccessibleName = "Vehicle horizon calibration and confidence";
        _fusionTrustDiagnostic.AutoEllipsis = true;
        _fusionTrustDiagnostic.AutoSize = true;
        _fusionTrustDiagnostic.Dock = DockStyle.Fill;
        _fusionTrustDiagnostic.Location = new Point(10, 208);
        _fusionTrustDiagnostic.Margin = new Padding(10);
        _fusionTrustDiagnostic.Name = "_fusionTrustDiagnostic";
        _fusionTrustDiagnostic.Size = new Size(658, 32);
        _fusionTrustDiagnostic.TabIndex = 3;
        _fusionTrustDiagnostic.Text = "Vehicle attitude unavailable.";
        _fusionTrustDiagnostic.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _accelerationGroupBox
        // 
        _accelerationGroupBox.AutoSize = true;
        _accelerationGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _accelerationGroupBox.Controls.Add(_accelerationValueLayout);
        _accelerationGroupBox.Dock = DockStyle.Fill;
        _accelerationGroupBox.FlatStyle = FlatStyle.Popup;
        _accelerationGroupBox.Location = new Point(4, 342);
        _accelerationGroupBox.Margin = new Padding(4, 15, 4, 5);
        _accelerationGroupBox.Name = "_accelerationGroupBox";
        _accelerationGroupBox.Padding = new Padding(9, 10, 9, 10);
        _accelerationGroupBox.Size = new Size(692, 331);
        _accelerationGroupBox.TabIndex = 1;
        _accelerationGroupBox.TabStop = false;
        _accelerationGroupBox.Text = "Vehicle acceleration (fusion)";
        // 
        // _accelerationValueLayout
        // 
        _accelerationValueLayout.AutoSize = true;
        _accelerationValueLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _accelerationValueLayout.ColumnCount = 1;
        _accelerationValueLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _accelerationValueLayout.Controls.Add(_forwardAccelerationLabel, 0, 0);
        _accelerationValueLayout.Controls.Add(_lateralAccelerationLabel, 0, 2);
        _accelerationValueLayout.Controls.Add(_verticalAccelerationLabel, 0, 1);
        _accelerationValueLayout.Controls.Add(_rawAccelerationDiagnostic, 0, 3);
        _accelerationValueLayout.Dock = DockStyle.Fill;
        _accelerationValueLayout.Location = new Point(13, 67);
        _accelerationValueLayout.Margin = new Padding(4, 5, 4, 5);
        _accelerationValueLayout.Name = "_accelerationValueLayout";
        _accelerationValueLayout.RowCount = 4;
        _accelerationValueLayout.RowStyles.Add(new RowStyle());
        _accelerationValueLayout.RowStyles.Add(new RowStyle());
        _accelerationValueLayout.RowStyles.Add(new RowStyle());
        _accelerationValueLayout.RowStyles.Add(new RowStyle());
        _accelerationValueLayout.Size = new Size(666, 250);
        _accelerationValueLayout.TabIndex = 0;
        // 
        // _forwardAccelerationLabel
        // 
        _forwardAccelerationLabel.AccessibleName = "Forward / X";
        _forwardAccelerationLabel.AccessibleRole = AccessibleRole.StaticText;
        _forwardAccelerationLabel.BackColor = SystemColors.Control;
        _forwardAccelerationLabel.BoldCaption = true;
        _forwardAccelerationLabel.Caption = "Forward / X";
        _forwardAccelerationLabel.DecimalPlaces = 4;
        _forwardAccelerationLabel.Dimension = "g";
        _forwardAccelerationLabel.Dock = DockStyle.Fill;
        _forwardAccelerationLabel.FixedDecimalPointPosition = 96;
        _forwardAccelerationLabel.ForeColor = SystemColors.ControlText;
        _forwardAccelerationLabel.LayoutManager = _accelerationValueLayoutManager;
        _forwardAccelerationLabel.Location = new Point(10, 10);
        _forwardAccelerationLabel.Margin = new Padding(10);
        _forwardAccelerationLabel.Name = "_forwardAccelerationLabel";
        _forwardAccelerationLabel.Padding = new Padding(5);
        _forwardAccelerationLabel.Size = new Size(646, 46);
        _forwardAccelerationLabel.TabIndex = 0;
        _forwardAccelerationLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _lateralAccelerationLabel
        // 
        _lateralAccelerationLabel.AccessibleName = "Lateral / Y";
        _lateralAccelerationLabel.AccessibleRole = AccessibleRole.StaticText;
        _lateralAccelerationLabel.BackColor = SystemColors.Control;
        _lateralAccelerationLabel.BoldCaption = true;
        _lateralAccelerationLabel.Caption = "Lateral / Y";
        _lateralAccelerationLabel.DecimalPlaces = 4;
        _lateralAccelerationLabel.Dimension = "g";
        _lateralAccelerationLabel.Dock = DockStyle.Fill;
        _lateralAccelerationLabel.FixedDecimalPointPosition = 96;
        _lateralAccelerationLabel.ForeColor = SystemColors.ControlText;
        _lateralAccelerationLabel.LayoutManager = _accelerationValueLayoutManager;
        _lateralAccelerationLabel.Location = new Point(10, 142);
        _lateralAccelerationLabel.Margin = new Padding(10);
        _lateralAccelerationLabel.MaximumValue = 2D;
        _lateralAccelerationLabel.MinimumValue = -2D;
        _lateralAccelerationLabel.Name = "_lateralAccelerationLabel";
        _lateralAccelerationLabel.Padding = new Padding(5);
        _lateralAccelerationLabel.Size = new Size(646, 46);
        _lateralAccelerationLabel.TabIndex = 1;
        _lateralAccelerationLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _verticalAccelerationLabel
        // 
        _verticalAccelerationLabel.AccessibleName = "Vertical / Z";
        _verticalAccelerationLabel.AccessibleRole = AccessibleRole.StaticText;
        _verticalAccelerationLabel.BackColor = SystemColors.Control;
        _verticalAccelerationLabel.BoldCaption = true;
        _verticalAccelerationLabel.Caption = "Vertical / Z";
        _verticalAccelerationLabel.DecimalPlaces = 4;
        _verticalAccelerationLabel.Dimension = "g";
        _verticalAccelerationLabel.Dock = DockStyle.Fill;
        _verticalAccelerationLabel.FixedDecimalPointPosition = 96;
        _verticalAccelerationLabel.ForeColor = SystemColors.ControlText;
        _verticalAccelerationLabel.LayoutManager = _accelerationValueLayoutManager;
        _verticalAccelerationLabel.Location = new Point(10, 76);
        _verticalAccelerationLabel.Margin = new Padding(10);
        _verticalAccelerationLabel.MaximumValue = 2D;
        _verticalAccelerationLabel.MinimumValue = -2D;
        _verticalAccelerationLabel.Name = "_verticalAccelerationLabel";
        _verticalAccelerationLabel.Padding = new Padding(5);
        _verticalAccelerationLabel.Size = new Size(646, 46);
        _verticalAccelerationLabel.TabIndex = 2;
        _verticalAccelerationLabel.ValueEffectRendering = ValueEffectRendering.Bar | ValueEffectRendering.NewValueInertia | ValueEffectRendering.ResetInertia | ValueEffectRendering.IndicateLastValue | ValueEffectRendering.IndicateHighestValue | ValueEffectRendering.HighlightAverage;
        // 
        // _rawAccelerationDiagnostic
        // 
        _rawAccelerationDiagnostic.AccessibleName = "Raw device accelerometer XYZ (gravity included)";
        _rawAccelerationDiagnostic.AutoEllipsis = true;
        _rawAccelerationDiagnostic.AutoSize = true;
        _rawAccelerationDiagnostic.Dock = DockStyle.Fill;
        _rawAccelerationDiagnostic.Location = new Point(10, 208);
        _rawAccelerationDiagnostic.Margin = new Padding(10);
        _rawAccelerationDiagnostic.Name = "_rawAccelerationDiagnostic";
        _rawAccelerationDiagnostic.Size = new Size(646, 32);
        _rawAccelerationDiagnostic.TabIndex = 3;
        _rawAccelerationDiagnostic.Text = "Raw device X/Y/Z (gravity included): —";
        _rawAccelerationDiagnostic.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // TelemetryTabView
        // 
        AccessibleName = "Telemetry";
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Controls.Add(_telemetryLayout);
        Margin = new Padding(4, 5, 4, 5);
        Name = "TelemetryTabView";
        Size = new Size(1415, 809);
        VisualStylesMode = VisualStylesMode.Net11;
        _telemetryLayout.ResumeLayout(false);
        _telemetryLayout.PerformLayout();
        _gpsGroupBox.ResumeLayout(false);
        _gpsGroupBox.PerformLayout();
        _gpsValueLayout.ResumeLayout(false);
        _gpsValueLayout.PerformLayout();
        _motionValueLayout.ResumeLayout(false);
        _motionValueLayout.PerformLayout();
        _horizonGroupBox.ResumeLayout(false);
        _horizonGroupBox.PerformLayout();
        _horizonValueLayout.ResumeLayout(false);
        _horizonValueLayout.PerformLayout();
        _accelerationGroupBox.ResumeLayout(false);
        _accelerationGroupBox.PerformLayout();
        _accelerationValueLayout.ResumeLayout(false);
        _accelerationValueLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _telemetryLayout;
    private TableLayoutPanel _motionValueLayout;
    private GroupBox _gpsGroupBox;
    private TableLayoutPanel _gpsValueLayout;
    private AnimatedValueLabel _longitudeLabel;
    private AnimatedValueLabel _latitudeLabel;
    private AnimatedValueLabel _heightLabel;
    private AnimatedValueLabel _gpsSpeedLabel;
    private AnimatedValueLabel _gpsFourPointSpeedLabel;
    private AnimatedValueLabel _gpsCourseLabel;
    private AnimatedValueLabel _gpsHorizontalAccuracyLabel;
    private AnimatedValueLabel _gpsVerticalAccuracyLabel;
    private AnimatedValueLabel _gpsSatelliteLabel;
    private Label _gpsSourceLabel;
    private GroupBox _horizonGroupBox;
    private TableLayoutPanel _horizonValueLayout;
    private Label _fusionTrustDiagnostic;
    private AnimatedValueLabel _rollLabel;
    private AnimatedValueLabel _pitchLabel;
    private GroupBox _accelerationGroupBox;
    private TableLayoutPanel _accelerationValueLayout;
    private Label _rawAccelerationDiagnostic;
    private AnimatedValueLabel _forwardAccelerationLabel;
    private AnimatedValueLabel _lateralAccelerationLabel;
    private AnimatedValueLabel _verticalAccelerationLabel;
    private ValueLabelLayoutManager _gpsValueLayoutManager;
    private ValueLabelLayoutManager _horizonValueLayoutManager;
    private ValueLabelLayoutManager _accelerationValueLayoutManager;
    private AnimatedValueLabel _yawLabel;
}
