#nullable disable

namespace Sensored.TabViews;

partial class ObdRawTabView
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
        _toolbar = new ToolStrip();
        _recordButton = new ToolStripButton();
        _stopButton = new ToolStripButton();
        _captureStatus = new ToolStripLabel();
        _messages = new TextBox();
        _toolbar.SuspendLayout();
        SuspendLayout();
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Items.AddRange(new ToolStripItem[] { _recordButton, _stopButton, _captureStatus });
        _toolbar.Name = "_toolbar";
        _toolbar.TabIndex = 0;
        _recordButton.Name = "_recordButton";
        _recordButton.Text = "Record (1 minute max)";
        _recordButton.AccessibleName = "Record raw OBD messages for at most one minute";
        _recordButton.Click += OnRecordClick;
        _stopButton.Name = "_stopButton";
        _stopButton.Text = "Stop";
        _stopButton.Enabled = false;
        _stopButton.AccessibleName = "Stop raw OBD recording";
        _stopButton.Click += OnStopClick;
        _captureStatus.Name = "_captureStatus";
        _captureStatus.Text = "Live adapter TX/RX; latest 500 messages";
        _messages.Dock = DockStyle.Fill;
        _messages.Multiline = true;
        _messages.ReadOnly = true;
        _messages.ScrollBars = ScrollBars.Both;
        _messages.WordWrap = false;
        _messages.Name = "_messages";
        _messages.AccessibleName = "Raw OBD adapter messages";
        _messages.TabIndex = 1;
        AutoScaleMode = AutoScaleMode.Inherit;
        AccessibleName = "Raw OBD2 messages";
        Controls.Add(_messages);
        Controls.Add(_toolbar);
        Name = "ObdRawTabView";
        Size = new Size(800, 300);
        _toolbar.ResumeLayout(false);
        _toolbar.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private ToolStrip _toolbar;
    private ToolStripButton _recordButton;
    private ToolStripButton _stopButton;
    private ToolStripLabel _captureStatus;
    private TextBox _messages;
}
