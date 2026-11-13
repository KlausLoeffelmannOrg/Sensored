#nullable disable

using WarpToolkit.WinForms.ToolStrips;
using WarpToolkit.WinForms.Containers;

namespace WarpSensorsDemo;

partial class RawSensorDialog
{
    private System.ComponentModel.IContainer components;
    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _recordingButton;
    private SymbolToolStripButton _openRecordingButton;
    private ToolStripSeparator _commandSeparator;
    private SymbolToolStripButton _closeButton;
    private FluentTabControl _sensorTabs;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private System.Windows.Forms.Timer _refreshTimer;
    private Label _recordingStateLabel;
    private TextBox _recordingPathTextBox;

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
        _toolStrip = new SymbolToolStrip();
        _recordingButton = new SymbolToolStripButton();
        _openRecordingButton = new SymbolToolStripButton();
        _commandSeparator = new ToolStripSeparator();
        _closeButton = new SymbolToolStripButton();
        _sensorTabs = new FluentTabControl();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _refreshTimer = new System.Windows.Forms.Timer(components);
        _toolStrip.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // _toolStrip
        //
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.ImageScalingSize = new Size(64, 64);
        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _recordingButton,
            _openRecordingButton,
            _commandSeparator,
            _closeButton
        });
        _toolStrip.Location = new Point(0, 0);
        _toolStrip.Name = "_toolStrip";
        _toolStrip.Padding = new Padding(6, 3, 6, 3);
        _toolStrip.Size = new Size(1400, 88);
        _toolStrip.TabIndex = 0;
        //
        // _recordingButton
        //
        _recordingButton.Name = "_recordingButton";
        _recordingButton.Size = new Size(84, 82);
        _recordingButton.Text = "Start recording";
        _recordingButton.Click += OnRecordingClick;
        //
        // _openRecordingButton
        //
        _openRecordingButton.Name = "_openRecordingButton";
        _openRecordingButton.Size = new Size(110, 82);
        _openRecordingButton.Text = "Open recording";
        _openRecordingButton.Click += OnOpenRecordingClick;
        //
        // _commandSeparator
        //
        _commandSeparator.Name = "_commandSeparator";
        _commandSeparator.Size = new Size(6, 85);
        //
        // _closeButton
        //
        _closeButton.Name = "_closeButton";
        _closeButton.Size = new Size(40, 82);
        _closeButton.Text = "Close";
        _closeButton.Click += OnCloseClick;
        //
        // _sensorTabs
        //
        _sensorTabs.Dock = DockStyle.Fill;
        _sensorTabs.Location = new Point(0, 88);
        _sensorTabs.Name = "_sensorTabs";
        _sensorTabs.Size = new Size(1400, 700);
        _sensorTabs.TabIndex = 1;
        _sensorTabs.TabChanged += OnSelectedTabChanged;
        //
        // _statusStrip
        //
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel });
        _statusStrip.Location = new Point(0, 788);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new Size(1400, 22);
        _statusStrip.TabIndex = 2;
        //
        // _statusLabel
        //
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(1385, 17);
        _statusLabel.Spring = true;
        _statusLabel.Text = "Opening raw sensor diagnostics...";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _refreshTimer
        //
        _refreshTimer.Interval = 50;
        _refreshTimer.Tick += OnRefreshTick;
        //
        // RawSensorDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1400, 810);
        Controls.Add(_sensorTabs);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "RawSensorDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Raw sensor diagnostics";
        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
