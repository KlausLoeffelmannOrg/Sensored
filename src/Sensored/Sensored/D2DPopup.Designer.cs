#nullable disable

namespace Sensored;

partial class D2DPopup
{
    private System.ComponentModel.IContainer components;

    /// <inheritdoc/>
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
        _animationTimer = new System.Windows.Forms.Timer(components);
        SuspendLayout();
        _animationTimer.Interval = 16;
        _animationTimer.Tick += OnAnimationTick;
        AccessibleName = "Status notification";
        AccessibleRole = AccessibleRole.Alert;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.FromArgb(0, 0, 0, 0);
        ClientSize = new Size(640, 160);
        ControlBox = false;
        FormBorderStyle = FormBorderStyle.None;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "D2DPopup";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        ResumeLayout(false);
    }

    private System.Windows.Forms.Timer _animationTimer;
}
