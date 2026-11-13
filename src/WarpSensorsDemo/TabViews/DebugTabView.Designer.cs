#nullable disable

using WarpToolkit.WinForms.DirectX.Controls;

namespace WarpSensorsDemo.TabViews;

partial class DebugTabView
{
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
        _debugConsole = new D2DConsole();
        SuspendLayout();
        // 
        // _debugConsole
        // 
        _debugConsole.AccessibleName = "Debug log";
        _debugConsole.BackColor = Color.Black;
        _debugConsole.Dock = DockStyle.Fill;
        _debugConsole.ForeColor = Color.White;
        _debugConsole.Location = new Point(10, 10);
        _debugConsole.Name = "_debugConsole";
        _debugConsole.Size = new Size(954, 531);
        _debugConsole.TabIndex = 0;
        // 
        // DebugTabView
        // 
        AccessibleName = "Debug";
        AutoScaleMode = AutoScaleMode.Inherit;
        Controls.Add(_debugConsole);
        Margin = new Padding(4, 5, 4, 5);
        Name = "DebugTabView";
        Padding = new Padding(10);
        Size = new Size(974, 551);
        ResumeLayout(false);
    }

    private System.ComponentModel.IContainer components;
    private D2DConsole _debugConsole;
}
