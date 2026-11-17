namespace WinFormsFusionGauges.WinFormsApp;

partial class TelemetryValuesView
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
        _groups = new TableLayoutPanel();
        _speedGroup = new GroupBox();
        _speedRows = new TableLayoutPanel();
        _fusionKmh = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _receiverKmh = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _calculatedKmh = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _fusionMph = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _receiverMph = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _tripKm = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _tripMiles = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _gpsGroup = new GroupBox();
        _gpsRows = new TableLayoutPanel();
        _latitude = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _longitude = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _altitude = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _heading = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _accuracy = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _source = new Label();
        _accelerationGroup = new GroupBox();
        _accelerationRows = new TableLayoutPanel();
        _x = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _y = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _z = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _inclinometerGroup = new GroupBox();
        _inclinometerRows = new TableLayoutPanel();
        _pitch = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _roll = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _yaw = new WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel();
        _yawAccuracy = new Label();
        _groups.SuspendLayout();
        _speedGroup.SuspendLayout();
        _speedRows.SuspendLayout();
        _gpsGroup.SuspendLayout();
        _gpsRows.SuspendLayout();
        _accelerationGroup.SuspendLayout();
        _accelerationRows.SuspendLayout();
        _inclinometerGroup.SuspendLayout();
        _inclinometerRows.SuspendLayout();
        SuspendLayout();
        // 
        // _groups
        // 
        _groups.AutoSize = true;
        _groups.ColumnCount = 1;
        _groups.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _groups.Controls.Add(_speedGroup, 0, 0);
        _groups.Controls.Add(_gpsGroup, 0, 1);
        _groups.Controls.Add(_accelerationGroup, 0, 2);
        _groups.Controls.Add(_inclinometerGroup, 0, 3);
        _groups.Dock = DockStyle.Top;
        _groups.Location = new Point(0, 0);
        _groups.Name = "_groups";
        _groups.RowCount = 4;
        _groups.RowStyles.Add(new RowStyle());
        _groups.RowStyles.Add(new RowStyle());
        _groups.RowStyles.Add(new RowStyle());
        _groups.RowStyles.Add(new RowStyle());
        _groups.Size = new Size(705, 1485);
        _groups.TabIndex = 0;
        // 
        // _speedGroup
        // 
        _speedGroup.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _speedGroup.AutoSize = true;
        _speedGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _speedGroup.Controls.Add(_speedRows);
        _speedGroup.FlatStyle = FlatStyle.Popup;
        _speedGroup.Location = new Point(3, 3);
        _speedGroup.Name = "_speedGroup";
        _speedGroup.Size = new Size(699, 499);
        _speedGroup.TabIndex = 0;
        _speedGroup.TabStop = false;
        _speedGroup.Text = "SPEED";
        // 
        // _speedRows
        // 
        _speedRows.AutoSize = true;
        _speedRows.ColumnCount = 1;
        _speedRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _speedRows.Controls.Add(_fusionKmh, 0, 0);
        _speedRows.Controls.Add(_receiverKmh, 0, 1);
        _speedRows.Controls.Add(_calculatedKmh, 0, 2);
        _speedRows.Controls.Add(_fusionMph, 0, 3);
        _speedRows.Controls.Add(_receiverMph, 0, 4);
        _speedRows.Controls.Add(_tripKm, 0, 5);
        _speedRows.Controls.Add(_tripMiles, 0, 6);
        _speedRows.Dock = DockStyle.Top;
        _speedRows.Location = new Point(7, 60);
        _speedRows.Name = "_speedRows";
        _speedRows.RowCount = 7;
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.RowStyles.Add(new RowStyle());
        _speedRows.Size = new Size(685, 432);
        _speedRows.TabIndex = 0;
        // 
        // _fusionKmh
        // 
        _fusionKmh.AccessibleName = "KM/H - Fusion:";
        _fusionKmh.AccessibleRole = AccessibleRole.StaticText;
        _fusionKmh.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _fusionKmh.BackColor = SystemColors.Control;
        _fusionKmh.Caption = "KM/H - Fusion:";
        _fusionKmh.DecimalPlaces = 1;
        _fusionKmh.ForeColor = SystemColors.ControlText;
        _fusionKmh.Location = new Point(3, 5);
        _fusionKmh.MaximumValue = 300D;
        _fusionKmh.MinimumValue = 0D;
        _fusionKmh.Name = "_fusionKmh";
        _fusionKmh.Size = new Size(679, 48);
        _fusionKmh.TabIndex = 0;
        _fusionKmh.Value = double.NaN;
        // 
        // _receiverKmh
        // 
        _receiverKmh.AccessibleName = "KM/H - GPS sensor:";
        _receiverKmh.AccessibleRole = AccessibleRole.StaticText;
        _receiverKmh.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _receiverKmh.BackColor = SystemColors.Control;
        _receiverKmh.Caption = "KM/H - GPS sensor:";
        _receiverKmh.DecimalPlaces = 1;
        _receiverKmh.ForeColor = SystemColors.ControlText;
        _receiverKmh.Location = new Point(3, 63);
        _receiverKmh.MaximumValue = 300D;
        _receiverKmh.MinimumValue = 0D;
        _receiverKmh.Name = "_receiverKmh";
        _receiverKmh.Size = new Size(679, 48);
        _receiverKmh.TabIndex = 1;
        _receiverKmh.Value = double.NaN;
        // 
        // _calculatedKmh
        // 
        _calculatedKmh.AccessibleName = "KM/H - GPS, calc:";
        _calculatedKmh.AccessibleRole = AccessibleRole.StaticText;
        _calculatedKmh.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _calculatedKmh.BackColor = SystemColors.Control;
        _calculatedKmh.Caption = "KM/H - GPS, calc:";
        _calculatedKmh.DecimalPlaces = 1;
        _calculatedKmh.ForeColor = SystemColors.ControlText;
        _calculatedKmh.Location = new Point(3, 121);
        _calculatedKmh.MaximumValue = 300D;
        _calculatedKmh.MinimumValue = 0D;
        _calculatedKmh.Name = "_calculatedKmh";
        _calculatedKmh.Size = new Size(679, 48);
        _calculatedKmh.TabIndex = 2;
        _calculatedKmh.Value = double.NaN;
        // 
        // _fusionMph
        // 
        _fusionMph.AccessibleName = "MP/H - Fusion:";
        _fusionMph.AccessibleRole = AccessibleRole.StaticText;
        _fusionMph.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _fusionMph.BackColor = SystemColors.Control;
        _fusionMph.Caption = "MP/H - Fusion:";
        _fusionMph.DecimalPlaces = 1;
        _fusionMph.ForeColor = SystemColors.ControlText;
        _fusionMph.Location = new Point(3, 192);
        _fusionMph.Margin = new Padding(3, 18, 3, 5);
        _fusionMph.MaximumValue = 300D;
        _fusionMph.MinimumValue = 0D;
        _fusionMph.Name = "_fusionMph";
        _fusionMph.Size = new Size(679, 48);
        _fusionMph.TabIndex = 3;
        _fusionMph.Value = double.NaN;
        // 
        // _receiverMph
        // 
        _receiverMph.AccessibleName = "MP/H - GPS sensor:";
        _receiverMph.AccessibleRole = AccessibleRole.StaticText;
        _receiverMph.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _receiverMph.BackColor = SystemColors.Control;
        _receiverMph.Caption = "MP/H - GPS sensor:";
        _receiverMph.DecimalPlaces = 1;
        _receiverMph.ForeColor = SystemColors.ControlText;
        _receiverMph.Location = new Point(3, 250);
        _receiverMph.MaximumValue = 300D;
        _receiverMph.MinimumValue = 0D;
        _receiverMph.Name = "_receiverMph";
        _receiverMph.Size = new Size(679, 48);
        _receiverMph.TabIndex = 4;
        _receiverMph.Value = double.NaN;
        // 
        // _tripKm
        // 
        _tripKm.AccessibleName = "KM Trip:";
        _tripKm.AccessibleRole = AccessibleRole.StaticText;
        _tripKm.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _tripKm.BackColor = SystemColors.Control;
        _tripKm.Caption = "KM Trip:";
        _tripKm.ForeColor = SystemColors.ControlText;
        _tripKm.Location = new Point(3, 321);
        _tripKm.Margin = new Padding(3, 18, 3, 5);
        _tripKm.MaximumValue = 9999D;
        _tripKm.MinimumValue = 0D;
        _tripKm.Name = "_tripKm";
        _tripKm.Size = new Size(679, 48);
        _tripKm.TabIndex = 5;
        // 
        // _tripMiles
        // 
        _tripMiles.AccessibleName = "Miles Trip:";
        _tripMiles.AccessibleRole = AccessibleRole.StaticText;
        _tripMiles.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _tripMiles.BackColor = SystemColors.Control;
        _tripMiles.Caption = "Miles Trip:";
        _tripMiles.ForeColor = SystemColors.ControlText;
        _tripMiles.Location = new Point(3, 379);
        _tripMiles.MaximumValue = 9999D;
        _tripMiles.MinimumValue = 0D;
        _tripMiles.Name = "_tripMiles";
        _tripMiles.Size = new Size(679, 48);
        _tripMiles.TabIndex = 6;
        // 
        // _gpsGroup
        // 
        _gpsGroup.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _gpsGroup.AutoSize = true;
        _gpsGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _gpsGroup.Controls.Add(_gpsRows);
        _gpsGroup.FlatStyle = FlatStyle.Popup;
        _gpsGroup.Location = new Point(3, 508);
        _gpsGroup.Name = "_gpsGroup";
        _gpsGroup.Size = new Size(699, 425);
        _gpsGroup.TabIndex = 1;
        _gpsGroup.TabStop = false;
        _gpsGroup.Text = "GPS";
        // 
        // _gpsRows
        // 
        _gpsRows.AutoSize = true;
        _gpsRows.ColumnCount = 1;
        _gpsRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _gpsRows.Controls.Add(_latitude, 0, 0);
        _gpsRows.Controls.Add(_longitude, 0, 1);
        _gpsRows.Controls.Add(_altitude, 0, 2);
        _gpsRows.Controls.Add(_heading, 0, 3);
        _gpsRows.Controls.Add(_accuracy, 0, 4);
        _gpsRows.Controls.Add(_source, 0, 5);
        _gpsRows.Dock = DockStyle.Top;
        _gpsRows.Location = new Point(7, 60);
        _gpsRows.Name = "_gpsRows";
        _gpsRows.RowCount = 6;
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.RowStyles.Add(new RowStyle());
        _gpsRows.Size = new Size(685, 358);
        _gpsRows.TabIndex = 0;
        // 
        // _latitude
        // 
        _latitude.AccessibleName = "Lat.:";
        _latitude.AccessibleRole = AccessibleRole.StaticText;
        _latitude.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _latitude.BackColor = SystemColors.Control;
        _latitude.Caption = "Lat.:";
        _latitude.DecimalPlaces = 7;
        _latitude.ForeColor = SystemColors.ControlText;
        _latitude.Location = new Point(3, 5);
        _latitude.MaximumValue = 90D;
        _latitude.MinimumValue = -90D;
        _latitude.Name = "_latitude";
        _latitude.Size = new Size(679, 48);
        _latitude.TabIndex = 0;
        _latitude.Value = double.NaN;
        // 
        // _longitude
        // 
        _longitude.AccessibleName = "Lon.:";
        _longitude.AccessibleRole = AccessibleRole.StaticText;
        _longitude.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _longitude.BackColor = SystemColors.Control;
        _longitude.Caption = "Lon.:";
        _longitude.DecimalPlaces = 7;
        _longitude.ForeColor = SystemColors.ControlText;
        _longitude.Location = new Point(3, 63);
        _longitude.MaximumValue = 180D;
        _longitude.MinimumValue = -180D;
        _longitude.Name = "_longitude";
        _longitude.Size = new Size(679, 48);
        _longitude.TabIndex = 1;
        _longitude.Value = double.NaN;
        // 
        // _altitude
        // 
        _altitude.AccessibleName = "Alt:";
        _altitude.AccessibleRole = AccessibleRole.StaticText;
        _altitude.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _altitude.BackColor = SystemColors.Control;
        _altitude.Caption = "Alt:";
        _altitude.Dimension = "m";
        _altitude.ForeColor = SystemColors.ControlText;
        _altitude.Location = new Point(3, 121);
        _altitude.MaximumValue = 9999D;
        _altitude.MinimumValue = -500D;
        _altitude.Name = "_altitude";
        _altitude.Size = new Size(679, 48);
        _altitude.TabIndex = 2;
        _altitude.Value = double.NaN;
        // 
        // _heading
        // 
        _heading.AccessibleName = "Heading:";
        _heading.AccessibleRole = AccessibleRole.StaticText;
        _heading.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _heading.BackColor = SystemColors.Control;
        _heading.Caption = "Heading:";
        _heading.Dimension = "deg";
        _heading.ForeColor = SystemColors.ControlText;
        _heading.Location = new Point(3, 192);
        _heading.Margin = new Padding(3, 18, 3, 5);
        _heading.MaximumValue = 360D;
        _heading.MinimumValue = 0D;
        _heading.Name = "_heading";
        _heading.Size = new Size(679, 48);
        _heading.TabIndex = 3;
        _heading.Value = double.NaN;
        // 
        // _accuracy
        // 
        _accuracy.AccessibleName = "Accuracy:";
        _accuracy.AccessibleRole = AccessibleRole.StaticText;
        _accuracy.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _accuracy.BackColor = SystemColors.Control;
        _accuracy.Caption = "Accuracy:";
        _accuracy.Dimension = "m";
        _accuracy.ForeColor = SystemColors.ControlText;
        _accuracy.Location = new Point(3, 263);
        _accuracy.Margin = new Padding(3, 18, 3, 5);
        _accuracy.MaximumValue = 9999D;
        _accuracy.MinimumValue = 0D;
        _accuracy.Name = "_accuracy";
        _accuracy.Size = new Size(679, 48);
        _accuracy.TabIndex = 4;
        _accuracy.Value = double.NaN;
        // 
        // _source
        // 
        _source.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _source.AutoSize = true;
        _source.Location = new Point(6, 321);
        _source.Margin = new Padding(6, 5, 3, 5);
        _source.Name = "_source";
        _source.Size = new Size(676, 32);
        _source.TabIndex = 5;
        _source.Text = "Source: Unavailable";
        // 
        // _accelerationGroup
        // 
        _accelerationGroup.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _accelerationGroup.AutoSize = true;
        _accelerationGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _accelerationGroup.Controls.Add(_accelerationRows);
        _accelerationGroup.FlatStyle = FlatStyle.Popup;
        _accelerationGroup.Location = new Point(3, 939);
        _accelerationGroup.Name = "_accelerationGroup";
        _accelerationGroup.Size = new Size(699, 241);
        _accelerationGroup.TabIndex = 2;
        _accelerationGroup.TabStop = false;
        _accelerationGroup.Text = "ACCELERATION (Sensor)";
        // 
        // _accelerationRows
        // 
        _accelerationRows.AutoSize = true;
        _accelerationRows.ColumnCount = 1;
        _accelerationRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _accelerationRows.Controls.Add(_x, 0, 0);
        _accelerationRows.Controls.Add(_y, 0, 1);
        _accelerationRows.Controls.Add(_z, 0, 2);
        _accelerationRows.Dock = DockStyle.Top;
        _accelerationRows.Location = new Point(7, 60);
        _accelerationRows.Name = "_accelerationRows";
        _accelerationRows.RowCount = 3;
        _accelerationRows.RowStyles.Add(new RowStyle());
        _accelerationRows.RowStyles.Add(new RowStyle());
        _accelerationRows.RowStyles.Add(new RowStyle());
        _accelerationRows.Size = new Size(685, 174);
        _accelerationRows.TabIndex = 0;
        // 
        // _x
        // 
        _x.AccessibleName = "X:";
        _x.AccessibleRole = AccessibleRole.StaticText;
        _x.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _x.BackColor = SystemColors.Control;
        _x.Caption = "X:";
        _x.Dimension = "g";
        _x.ForeColor = SystemColors.ControlText;
        _x.Location = new Point(3, 5);
        _x.MaximumValue = 5D;
        _x.MinimumValue = -5D;
        _x.Name = "_x";
        _x.Size = new Size(679, 48);
        _x.TabIndex = 0;
        _x.Value = double.NaN;
        // 
        // _y
        // 
        _y.AccessibleName = "Y:";
        _y.AccessibleRole = AccessibleRole.StaticText;
        _y.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _y.BackColor = SystemColors.Control;
        _y.Caption = "Y:";
        _y.Dimension = "g";
        _y.ForeColor = SystemColors.ControlText;
        _y.Location = new Point(3, 63);
        _y.MaximumValue = 5D;
        _y.MinimumValue = -5D;
        _y.Name = "_y";
        _y.Size = new Size(679, 48);
        _y.TabIndex = 1;
        _y.Value = double.NaN;
        // 
        // _z
        // 
        _z.AccessibleName = "Z:";
        _z.AccessibleRole = AccessibleRole.StaticText;
        _z.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _z.BackColor = SystemColors.Control;
        _z.Caption = "Z:";
        _z.Dimension = "g";
        _z.ForeColor = SystemColors.ControlText;
        _z.Location = new Point(3, 121);
        _z.MaximumValue = 5D;
        _z.MinimumValue = -5D;
        _z.Name = "_z";
        _z.Size = new Size(679, 48);
        _z.TabIndex = 2;
        _z.Value = double.NaN;
        // 
        // _inclinometerGroup
        // 
        _inclinometerGroup.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _inclinometerGroup.AutoSize = true;
        _inclinometerGroup.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _inclinometerGroup.Controls.Add(_inclinometerRows);
        _inclinometerGroup.FlatStyle = FlatStyle.Popup;
        _inclinometerGroup.Location = new Point(3, 1186);
        _inclinometerGroup.Name = "_inclinometerGroup";
        _inclinometerGroup.Size = new Size(699, 296);
        _inclinometerGroup.TabIndex = 3;
        _inclinometerGroup.TabStop = false;
        _inclinometerGroup.Text = "INCLINOMETER (Sensor)";
        // 
        // _inclinometerRows
        // 
        _inclinometerRows.AutoSize = true;
        _inclinometerRows.ColumnCount = 1;
        _inclinometerRows.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _inclinometerRows.Controls.Add(_pitch, 0, 0);
        _inclinometerRows.Controls.Add(_roll, 0, 1);
        _inclinometerRows.Controls.Add(_yaw, 0, 2);
        _inclinometerRows.Controls.Add(_yawAccuracy, 0, 3);
        _inclinometerRows.Dock = DockStyle.Top;
        _inclinometerRows.Location = new Point(7, 60);
        _inclinometerRows.Name = "_inclinometerRows";
        _inclinometerRows.RowCount = 4;
        _inclinometerRows.RowStyles.Add(new RowStyle());
        _inclinometerRows.RowStyles.Add(new RowStyle());
        _inclinometerRows.RowStyles.Add(new RowStyle());
        _inclinometerRows.RowStyles.Add(new RowStyle());
        _inclinometerRows.Size = new Size(685, 229);
        _inclinometerRows.TabIndex = 0;
        // 
        // _pitch
        // 
        _pitch.AccessibleName = "Pitch:";
        _pitch.AccessibleRole = AccessibleRole.StaticText;
        _pitch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _pitch.BackColor = SystemColors.Control;
        _pitch.Caption = "Pitch:";
        _pitch.Dimension = "deg";
        _pitch.ForeColor = SystemColors.ControlText;
        _pitch.Location = new Point(3, 5);
        _pitch.MaximumValue = 180D;
        _pitch.MinimumValue = -180D;
        _pitch.Name = "_pitch";
        _pitch.Size = new Size(679, 48);
        _pitch.TabIndex = 0;
        _pitch.Value = double.NaN;
        // 
        // _roll
        // 
        _roll.AccessibleName = "Roll:";
        _roll.AccessibleRole = AccessibleRole.StaticText;
        _roll.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _roll.BackColor = SystemColors.Control;
        _roll.Caption = "Roll:";
        _roll.Dimension = "deg";
        _roll.ForeColor = SystemColors.ControlText;
        _roll.Location = new Point(3, 63);
        _roll.MaximumValue = 180D;
        _roll.MinimumValue = -180D;
        _roll.Name = "_roll";
        _roll.Size = new Size(679, 48);
        _roll.TabIndex = 1;
        _roll.Value = double.NaN;
        // 
        // _yaw
        // 
        _yaw.AccessibleName = "Yaw:";
        _yaw.AccessibleRole = AccessibleRole.StaticText;
        _yaw.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _yaw.BackColor = SystemColors.Control;
        _yaw.Caption = "Yaw:";
        _yaw.Dimension = "deg";
        _yaw.ForeColor = SystemColors.ControlText;
        _yaw.Location = new Point(3, 121);
        _yaw.MaximumValue = 360D;
        _yaw.MinimumValue = 0D;
        _yaw.Name = "_yaw";
        _yaw.Size = new Size(679, 48);
        _yaw.TabIndex = 2;
        _yaw.Value = double.NaN;
        // 
        // _yawAccuracy
        // 
        _yawAccuracy.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _yawAccuracy.AutoSize = true;
        _yawAccuracy.Location = new Point(6, 192);
        _yawAccuracy.Margin = new Padding(6, 18, 3, 5);
        _yawAccuracy.Name = "_yawAccuracy";
        _yawAccuracy.Size = new Size(676, 32);
        _yawAccuracy.TabIndex = 3;
        _yawAccuracy.Text = "Yaw Accuracy: Unavailable";
        // 
        // TelemetryValuesView
        // 
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Controls.Add(_groups);
        Name = "TelemetryValuesView";
        Size = new Size(705, 1254);
        VisualStylesMode = VisualStylesMode.Net11;
        _groups.ResumeLayout(false);
        _groups.PerformLayout();
        _speedGroup.ResumeLayout(false);
        _speedGroup.PerformLayout();
        _speedRows.ResumeLayout(false);
        _speedRows.PerformLayout();
        _gpsGroup.ResumeLayout(false);
        _gpsGroup.PerformLayout();
        _gpsRows.ResumeLayout(false);
        _gpsRows.PerformLayout();
        _accelerationGroup.ResumeLayout(false);
        _accelerationGroup.PerformLayout();
        _accelerationRows.ResumeLayout(false);
        _accelerationRows.PerformLayout();
        _inclinometerGroup.ResumeLayout(false);
        _inclinometerGroup.PerformLayout();
        _inclinometerRows.ResumeLayout(false);
        _inclinometerRows.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _groups;
    private GroupBox _speedGroup;
    private GroupBox _gpsGroup;
    private GroupBox _accelerationGroup;
    private GroupBox _inclinometerGroup;
    private TableLayoutPanel _speedRows;
    private TableLayoutPanel _gpsRows;
    private TableLayoutPanel _accelerationRows;
    private TableLayoutPanel _inclinometerRows;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _fusionKmh;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _receiverKmh;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _calculatedKmh;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _fusionMph;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _receiverMph;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _tripKm;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _tripMiles;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _latitude;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _longitude;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _altitude;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _heading;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _accuracy;
    private Label _source;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _x;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _y;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _z;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _pitch;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _roll;
    private WarpToolkit.WinForms.DirectX.Controls.AnimatedValueLabel _yaw;
    private Label _yawAccuracy;
}
