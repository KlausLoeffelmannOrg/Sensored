#nullable disable

namespace Sensored;

partial class SensorReferenceFrameDialog
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
        components = new System.ComponentModel.Container();
        _layout = new TableLayoutPanel();
        _captionLabel = new Label();
        _noteLabel = new Label();
        _view = new WarpToolkit.WinForms.Sensors.DirectX.TabletOrientationView();
        _liveLabel = new Label();
        _offsetLayout = new TableLayoutPanel();
        _yawLabel = new Label();
        _yawUpDown = new NumericUpDown();
        _pitchLabel = new Label();
        _pitchUpDown = new NumericUpDown();
        _rollLabel = new Label();
        _rollUpDown = new NumericUpDown();
        _yawQuarterTurnButton = new Button();
        _pitchQuarterTurnButton = new Button();
        _rollQuarterTurnButton = new Button();
        _swapPitchAndRollCheckBox = new CheckBox();
        _buttonLayout = new TableLayoutPanel();
        _captureButton = new Button();
        _resetButton = new Button();
        _okButton = new Button();
        _cancelButton = new Button();
        _sampleTimer = new System.Windows.Forms.Timer(components);
        _layout.SuspendLayout();
        _offsetLayout.SuspendLayout();
        _buttonLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_yawUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_pitchUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_rollUpDown).BeginInit();
        SuspendLayout();
        //
        // _layout
        //
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_captionLabel, 0, 0);
        _layout.Controls.Add(_noteLabel, 0, 1);
        _layout.Controls.Add(_view, 0, 2);
        _layout.Controls.Add(_liveLabel, 0, 3);
        _layout.Controls.Add(_offsetLayout, 0, 4);
        _layout.Controls.Add(_swapPitchAndRollCheckBox, 0, 5);
        _layout.Controls.Add(_buttonLayout, 0, 6);
        _layout.Dock = DockStyle.Top;
        _layout.Location = new Point(0, 0);
        _layout.Margin = new Padding(3);
        _layout.Name = "_layout";
        _layout.Padding = new Padding(12);
        _layout.RowCount = 7;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Size = new Size(640, 640);
        _layout.TabIndex = 0;
        //
        // _captionLabel
        //
        _captionLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _captionLabel.AutoSize = true;
        _captionLabel.Location = new Point(15, 15);
        _captionLabel.Margin = new Padding(3);
        _captionLabel.Name = "_captionLabel";
        _captionLabel.Size = new Size(610, 15);
        _captionLabel.TabIndex = 0;
        _captionLabel.Text = "Reference frame";
        //
        // _noteLabel
        //
        _noteLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _noteLabel.AutoSize = true;
        _noteLabel.Location = new Point(15, 36);
        _noteLabel.Margin = new Padding(3, 3, 3, 12);
        _noteLabel.MaximumSize = new Size(640, 0);
        _noteLabel.Name = "_noteLabel";
        _noteLabel.Size = new Size(610, 45);
        _noteLabel.TabIndex = 1;
        _noteLabel.Text = "Capture the current pose as the display zero, or enter offsets manually. Reset restores the all-zero identity. These offsets do not change the fusion measured mount. Raw gravity is apparent tilt affected by vehicle acceleration. Bluetooth yaw is device-fused and is not a certified independent compass.";
        //
        // _view
        //
        _view.AccessibleName = "Live tablet pose";
        _view.Anchor = AnchorStyles.None;
        _view.AutoSize = true;
        _view.Location = new Point(180, 96);
        _view.Margin = new Padding(3);
        _view.MinimumSize = new Size(240, 160);
        _view.Name = "_view";
        _view.Size = new Size(280, 280);
        _view.TabIndex = 2;
        //
        // _liveLabel
        //
        _liveLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _liveLabel.AutoSize = true;
        _liveLabel.Location = new Point(15, 382);
        _liveLabel.Margin = new Padding(3);
        _liveLabel.MaximumSize = new Size(640, 0);
        _liveLabel.Name = "_liveLabel";
        _liveLabel.Size = new Size(610, 30);
        _liveLabel.TabIndex = 3;
        _liveLabel.Text = "Waiting for a live pose.";
        //
        // _offsetLayout
        //
        _offsetLayout.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _offsetLayout.AutoSize = true;
        _offsetLayout.ColumnCount = 3;
        _offsetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _offsetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _offsetLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _offsetLayout.Controls.Add(_yawLabel, 0, 0);
        _offsetLayout.Controls.Add(_yawUpDown, 1, 0);
        _offsetLayout.Controls.Add(_pitchLabel, 0, 1);
        _offsetLayout.Controls.Add(_pitchUpDown, 1, 1);
        _offsetLayout.Controls.Add(_rollLabel, 0, 2);
        _offsetLayout.Controls.Add(_rollUpDown, 1, 2);
        _offsetLayout.Controls.Add(_yawQuarterTurnButton, 2, 0);
        _offsetLayout.Controls.Add(_pitchQuarterTurnButton, 2, 1);
        _offsetLayout.Controls.Add(_rollQuarterTurnButton, 2, 2);
        _offsetLayout.Location = new Point(15, 418);
        _offsetLayout.Margin = new Padding(3);
        _offsetLayout.Name = "_offsetLayout";
        _offsetLayout.RowCount = 3;
        _offsetLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _offsetLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _offsetLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _offsetLayout.Size = new Size(610, 87);
        _offsetLayout.TabIndex = 4;
        //
        // _yawLabel
        //
        _yawLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _yawLabel.AutoSize = true;
        _yawLabel.Location = new Point(3, 8);
        _yawLabel.Margin = new Padding(3);
        _yawLabel.Name = "_yawLabel";
        _yawLabel.Size = new Size(86, 15);
        _yawLabel.TabIndex = 0;
        _yawLabel.Text = "Yaw (degrees):";
        //
        // _yawUpDown
        //
        _yawUpDown.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _yawUpDown.DecimalPlaces = 3;
        _yawUpDown.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        _yawUpDown.Location = new Point(95, 3);
        _yawUpDown.Margin = new Padding(3);
        _yawUpDown.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        _yawUpDown.Minimum = new decimal(new int[] { 360, 0, 0, -2147483648 });
        _yawUpDown.Name = "_yawUpDown";
        _yawUpDown.Size = new Size(512, 23);
        _yawUpDown.TabIndex = 1;
        _yawUpDown.ValueChanged += OnOffsetsChanged;
        //
        // _pitchLabel
        //
        _pitchLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _pitchLabel.AutoSize = true;
        _pitchLabel.Location = new Point(3, 37);
        _pitchLabel.Margin = new Padding(3);
        _pitchLabel.Name = "_pitchLabel";
        _pitchLabel.Size = new Size(92, 15);
        _pitchLabel.TabIndex = 2;
        _pitchLabel.Text = "Pitch (degrees):";
        //
        // _pitchUpDown
        //
        _pitchUpDown.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _pitchUpDown.DecimalPlaces = 3;
        _pitchUpDown.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        _pitchUpDown.Location = new Point(95, 32);
        _pitchUpDown.Margin = new Padding(3);
        _pitchUpDown.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        _pitchUpDown.Minimum = new decimal(new int[] { 360, 0, 0, -2147483648 });
        _pitchUpDown.Name = "_pitchUpDown";
        _pitchUpDown.Size = new Size(512, 23);
        _pitchUpDown.TabIndex = 3;
        _pitchUpDown.ValueChanged += OnOffsetsChanged;
        //
        // _rollLabel
        //
        _rollLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _rollLabel.AutoSize = true;
        _rollLabel.Location = new Point(3, 66);
        _rollLabel.Margin = new Padding(3);
        _rollLabel.Name = "_rollLabel";
        _rollLabel.Size = new Size(88, 15);
        _rollLabel.TabIndex = 4;
        _rollLabel.Text = "Roll (degrees):";
        //
        // _rollUpDown
        //
        _rollUpDown.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _rollUpDown.DecimalPlaces = 3;
        _rollUpDown.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
        _rollUpDown.Location = new Point(95, 61);
        _rollUpDown.Margin = new Padding(3);
        _rollUpDown.Maximum = new decimal(new int[] { 360, 0, 0, 0 });
        _rollUpDown.Minimum = new decimal(new int[] { 360, 0, 0, -2147483648 });
        _rollUpDown.Name = "_rollUpDown";
        _rollUpDown.Size = new Size(512, 23);
        _rollUpDown.TabIndex = 5;
        _rollUpDown.ValueChanged += OnOffsetsChanged;
        //
        // Quarter-turn reference buttons
        //
        _yawQuarterTurnButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _yawQuarterTurnButton.AutoSize = true;
        _yawQuarterTurnButton.Name = "_yawQuarterTurnButton";
        _yawQuarterTurnButton.TabIndex = 6;
        _yawQuarterTurnButton.Text = "Yaw +90°";
        _yawQuarterTurnButton.UseVisualStyleBackColor = true;
        _yawQuarterTurnButton.Click += OnYawQuarterTurnClick;
        _pitchQuarterTurnButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _pitchQuarterTurnButton.AutoSize = true;
        _pitchQuarterTurnButton.Name = "_pitchQuarterTurnButton";
        _pitchQuarterTurnButton.TabIndex = 7;
        _pitchQuarterTurnButton.Text = "Pitch +90°";
        _pitchQuarterTurnButton.UseVisualStyleBackColor = true;
        _pitchQuarterTurnButton.Click += OnPitchQuarterTurnClick;
        _rollQuarterTurnButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _rollQuarterTurnButton.AutoSize = true;
        _rollQuarterTurnButton.Name = "_rollQuarterTurnButton";
        _rollQuarterTurnButton.TabIndex = 8;
        _rollQuarterTurnButton.Text = "Roll +90°";
        _rollQuarterTurnButton.UseVisualStyleBackColor = true;
        _rollQuarterTurnButton.Click += OnRollQuarterTurnClick;
        //
        // _swapPitchAndRollCheckBox
        //
        _swapPitchAndRollCheckBox.AutoSize = true;
        _swapPitchAndRollCheckBox.AccessibleName = "Swap Bluetooth pitch and roll";
        _swapPitchAndRollCheckBox.Margin = new Padding(3, 6, 3, 3);
        _swapPitchAndRollCheckBox.Name = "_swapPitchAndRollCheckBox";
        _swapPitchAndRollCheckBox.TabIndex = 5;
        _swapPitchAndRollCheckBox.Text = "Swap pitch and roll after mounting correction (display only)";
        _swapPitchAndRollCheckBox.UseVisualStyleBackColor = true;
        _swapPitchAndRollCheckBox.CheckedChanged += OnOffsetsChanged;
        //
        // _buttonLayout
        //
        _buttonLayout.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _buttonLayout.AutoSize = true;
        _buttonLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttonLayout.ColumnCount = 4;
        _buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        _buttonLayout.Controls.Add(_captureButton, 0, 0);
        _buttonLayout.Controls.Add(_resetButton, 1, 0);
        _buttonLayout.Controls.Add(_okButton, 2, 0);
        _buttonLayout.Controls.Add(_cancelButton, 3, 0);
        _buttonLayout.Location = new Point(15, 511);
        _buttonLayout.Margin = new Padding(3, 12, 3, 3);
        _buttonLayout.Name = "_buttonLayout";
        _buttonLayout.RowCount = 1;
        _buttonLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _buttonLayout.Size = new Size(610, 29);
        _buttonLayout.TabIndex = 6;
        //
        // _captureButton
        //
        _captureButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _captureButton.AutoSize = true;
        _captureButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _captureButton.Location = new Point(3, 3);
        _captureButton.Margin = new Padding(3);
        _captureButton.Name = "_captureButton";
        _captureButton.Size = new Size(146, 23);
        _captureButton.TabIndex = 0;
        _captureButton.Text = "Capture current pose as zero";
        _captureButton.UseVisualStyleBackColor = true;
        _captureButton.Click += OnCaptureClick;
        //
        // _resetButton
        //
        _resetButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _resetButton.AutoSize = true;
        _resetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _resetButton.Location = new Point(155, 3);
        _resetButton.Margin = new Padding(3);
        _resetButton.Name = "_resetButton";
        _resetButton.Size = new Size(146, 23);
        _resetButton.TabIndex = 1;
        _resetButton.Text = "Reset to all zero";
        _resetButton.UseVisualStyleBackColor = true;
        _resetButton.Click += OnResetClick;
        //
        // _okButton
        //
        _okButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _okButton.AutoSize = true;
        _okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _okButton.DialogResult = DialogResult.OK;
        _okButton.Location = new Point(307, 3);
        _okButton.Margin = new Padding(3);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(146, 23);
        _okButton.TabIndex = 2;
        _okButton.Text = "OK";
        _okButton.UseVisualStyleBackColor = true;
        //
        // _cancelButton
        //
        _cancelButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _cancelButton.AutoSize = true;
        _cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(459, 3);
        _cancelButton.Margin = new Padding(3);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(146, 23);
        _cancelButton.TabIndex = 3;
        _cancelButton.Text = "Cancel";
        _cancelButton.UseVisualStyleBackColor = true;
        //
        // _sampleTimer
        //
        _sampleTimer.Interval = 100;
        _sampleTimer.Tick += OnSampleTick;
        //
        // SensorReferenceFrameDialog
        //
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        AutoSize = false;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        CancelButton = _cancelButton;
        ClientSize = new Size(640, 640);
        Controls.Add(_layout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(520, 560);
        Name = "SensorReferenceFrameDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Sensor reference";
        _offsetLayout.ResumeLayout(false);
        _offsetLayout.PerformLayout();
        _buttonLayout.ResumeLayout(false);
        _buttonLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_yawUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_pitchUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_rollUpDown).EndInit();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private Label _captionLabel;
    private Label _noteLabel;
    private WarpToolkit.WinForms.Sensors.DirectX.TabletOrientationView _view;
    private Label _liveLabel;
    private TableLayoutPanel _offsetLayout;
    private Label _yawLabel;
    private NumericUpDown _yawUpDown;
    private Label _pitchLabel;
    private NumericUpDown _pitchUpDown;
    private Label _rollLabel;
    private NumericUpDown _rollUpDown;
    private Button _yawQuarterTurnButton;
    private Button _pitchQuarterTurnButton;
    private Button _rollQuarterTurnButton;
    private CheckBox _swapPitchAndRollCheckBox;
    private TableLayoutPanel _buttonLayout;
    private Button _captureButton;
    private Button _resetButton;
    private Button _okButton;
    private Button _cancelButton;
    private System.Windows.Forms.Timer _sampleTimer;
}
