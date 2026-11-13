#nullable disable

using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

partial class ExceptionHistoryForm
{
    private System.ComponentModel.IContainer components;
    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _closeButton;
    private TextBox _historyTextBox;

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
        _closeButton = new SymbolToolStripButton();
        _historyTextBox = new TextBox();
        _toolStrip.SuspendLayout();
        SuspendLayout();
        //
        // _toolStrip
        //
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.ImageScalingSize = new Size(64, 64);
        _toolStrip.Items.AddRange(new ToolStripItem[] { _closeButton });
        _toolStrip.Location = new Point(0, 0);
        _toolStrip.Name = "_toolStrip";
        _toolStrip.Padding = new Padding(6, 3, 6, 3);
        _toolStrip.Size = new Size(900, 73);
        _toolStrip.TabIndex = 0;
        //
        // _closeButton
        //
        _closeButton.Name = "_closeButton";
        _closeButton.Size = new Size(68, 67);
        _closeButton.Text = "Close";
        //
        // _historyTextBox
        //
        _historyTextBox.Dock = DockStyle.Fill;
        _historyTextBox.Location = new Point(0, 73);
        _historyTextBox.Multiline = true;
        _historyTextBox.Name = "_historyTextBox";
        _historyTextBox.ReadOnly = true;
        _historyTextBox.ScrollBars = ScrollBars.Both;
        _historyTextBox.Size = new Size(900, 527);
        _historyTextBox.TabIndex = 1;
        _historyTextBox.WordWrap = false;
        //
        // ExceptionHistoryForm
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 600);
        Controls.Add(_historyTextBox);
        Controls.Add(_toolStrip);
        MinimumSize = new Size(600, 400);
        Name = "ExceptionHistoryForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Sensor Exception History";
        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
