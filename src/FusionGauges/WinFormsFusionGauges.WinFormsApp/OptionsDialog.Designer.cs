namespace WinFormsFusionGauges.WinFormsApp;

partial class OptionsDialog
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
        _tabs = new WarpToolkit.WinForms.Containers.FluentTabControl();
        _appearance = new AppearanceOptionsView();
        _sources = new SourceOptionsView();
        _recording = new RecordingOptionsView();
        _mirror = new MirrorOptionsView();
        _buttons = new FlowLayoutPanel();
        _ok = new Button();
        _cancel = new Button();
        _error = new Label();
        _buttons.SuspendLayout();
        SuspendLayout();
        _tabs.Dock = DockStyle.Fill;
        _tabs.Name = "_tabs";
        _buttons.Dock = DockStyle.Bottom;
        _buttons.AutoSize = true;
        _buttons.FlowDirection = FlowDirection.RightToLeft;
        _buttons.Padding = new Padding(8);
        _buttons.Controls.Add(_cancel);
        _buttons.Controls.Add(_ok);
        _buttons.Controls.Add(_error);
        _ok.Text = "Save options";
        _ok.AutoSize = true;
        _ok.MinimumSize = new Size(140, 48);
        _ok.Margin = new Padding(8);
        _ok.Click += OnAccept;
        _cancel.Text = "Cancel";
        _cancel.AutoSize = true;
        _cancel.MinimumSize = new Size(140, 48);
        _cancel.Margin = new Padding(8);
        _cancel.DialogResult = DialogResult.Cancel;
        _error.AutoSize = true;
        _error.MaximumSize = new Size(440, 0);
        _error.Margin = new Padding(8);
        _error.AccessibleName = "Options validation error";
        AcceptButton = _ok;
        CancelButton = _cancel;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 720);
        MinimumSize = new Size(680, 540);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Text = "Tools / Options - configure while parked";
        Controls.Add(_tabs);
        Controls.Add(_buttons);
        _buttons.ResumeLayout(false);
        _buttons.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private WarpToolkit.WinForms.Containers.FluentTabControl _tabs;
    private AppearanceOptionsView _appearance;
    private SourceOptionsView _sources;
    private RecordingOptionsView _recording;
    private MirrorOptionsView _mirror;
    private FlowLayoutPanel _buttons;
    private Button _ok;
    private Button _cancel;
    private Label _error;
}
