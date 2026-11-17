namespace WinFormsFusionGauges.WinFormsApp;

partial class DiagnosticsView
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
        _details = new TextBox();
        SuspendLayout();
        _details.Dock = DockStyle.Fill;
        _details.Multiline = true;
        _details.ReadOnly = true;
        _details.ScrollBars = ScrollBars.Vertical;
        _details.AccessibleName = "Raw sensor and fusion diagnostics";
        _details.Text = "Sensors stopped.";
        AutoScaleMode = AutoScaleMode.Inherit;
        Padding = new Padding(12);
        Size = new Size(900, 600);
        Controls.Add(_details);
        ResumeLayout(false);
    }

    private TextBox _details;
}
