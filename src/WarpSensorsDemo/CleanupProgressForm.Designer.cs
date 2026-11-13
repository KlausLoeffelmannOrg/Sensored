#nullable disable

namespace WarpSensorsDemo;

partial class CleanupProgressForm
{
    private System.ComponentModel.IContainer components;
    private TableLayoutPanel _layout;
    private Label _messageLabel;
    private ProgressBar _progressBar;

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
        _messageLabel = new Label();
        _progressBar = new ProgressBar();
        _layout.SuspendLayout();
        SuspendLayout();
        //
        // _layout
        //
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.Controls.Add(_messageLabel, 0, 0);
        _layout.Controls.Add(_progressBar, 0, 1);
        _layout.Dock = DockStyle.Fill;
        _layout.Location = new Point(0, 0);
        _layout.Name = "_layout";
        _layout.Padding = new Padding(18);
        _layout.RowCount = 2;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Size = new Size(460, 112);
        _layout.TabIndex = 0;
        //
        // _messageLabel
        //
        _messageLabel.AutoSize = true;
        _messageLabel.Dock = DockStyle.Fill;
        _messageLabel.Location = new Point(21, 18);
        _messageLabel.Margin = new Padding(3, 0, 3, 12);
        _messageLabel.Name = "_messageLabel";
        _messageLabel.Size = new Size(418, 25);
        _messageLabel.TabIndex = 0;
        _messageLabel.Text = "Finishing sensor cleanup...";
        _messageLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _progressBar
        //
        _progressBar.AutoSize = true;
        _progressBar.Dock = DockStyle.Fill;
        _progressBar.Location = new Point(21, 58);
        _progressBar.MarqueeAnimationSpeed = 24;
        _progressBar.Name = "_progressBar";
        _progressBar.Size = new Size(418, 28);
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.TabIndex = 1;
        //
        // CleanupProgressForm
        //
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(460, 112);
        ControlBox = false;
        Controls.Add(_layout);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CleanupProgressForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "WARP Sensors Dashboard";
        TopMost = true;
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
