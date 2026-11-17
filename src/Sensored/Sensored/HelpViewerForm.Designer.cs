#nullable disable

using WarpToolkit.WinForms.DirectX.Controls;

namespace Sensored;

partial class HelpViewerForm
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
        _markdownPreview = new D2DMarkdownPreview();
        SuspendLayout();
        //
        // _markdownPreview
        //
        _markdownPreview.AccessibleName = "Help document";
        _markdownPreview.Dock = DockStyle.Fill;
        _markdownPreview.Location = new Point(0, 0);
        _markdownPreview.Name = "_markdownPreview";
        _markdownPreview.Padding = new Padding(20);
        _markdownPreview.Size = new Size(820, 600);
        _markdownPreview.TabIndex = 0;
        //
        // HelpViewerForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(820, 600);
        Controls.Add(_markdownPreview);
        MinimumSize = new Size(400, 300);
        Name = "HelpViewerForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Dashboard help";
        ResumeLayout(false);
    }

    private D2DMarkdownPreview _markdownPreview;
}
