namespace Sensored;

partial class ObdTelemetryView
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
        components = new System.ComponentModel.Container();
        _layout = new TableLayoutPanel();
        _gauges = new WarpToolkit.WinForms.DirectX.Gauges.D2DVehicleGaugePanel();
        _rpmMenu = new ContextMenuStrip(components);
        _rpm4000 = new ToolStripMenuItem();
        _rpm6000 = new ToolStripMenuItem();
        _rpm8000 = new ToolStripMenuItem();
        _rpm10000 = new ToolStripMenuItem();
        _rpm12000 = new ToolStripMenuItem();
        _layout.SuspendLayout();
        _rpmMenu.SuspendLayout();
        SuspendLayout();
        //
        // _layout
        //
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_gauges, 0, 0);
        _layout.Dock = DockStyle.Fill;
        _layout.Location = new Point(0, 0);
        _layout.Name = "_layout";
        _layout.RowCount = 1;
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.Size = new Size(331, 448);
        _layout.TabIndex = 1;
        //
        // _gauges
        //
        _gauges.AccessibleName = "Engine RPM, coolant temperature, fuel and battery";
        _gauges.AccessibleRole = AccessibleRole.Graphic;
        _gauges.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _gauges.ContextMenuStrip = _rpmMenu;
        _gauges.Location = new Point(3, 3);
        _gauges.Name = "_gauges";
        _gauges.Size = new Size(325, 442);
        _gauges.TabIndex = 0;
        //
        // _rpmMenu
        //
        _rpmMenu.ImageScalingSize = new Size(24, 24);
        _rpmMenu.Items.AddRange(new ToolStripItem[] { _rpm4000, _rpm6000, _rpm8000, _rpm10000, _rpm12000 });
        _rpmMenu.Name = "_rpmMenu";
        _rpmMenu.Size = new Size(228, 164);
        //
        // _rpm4000
        //
        _rpm4000.Name = "_rpm4000";
        _rpm4000.Size = new Size(227, 32);
        _rpm4000.Tag = 4000;
        _rpm4000.Text = "RPM scale: 4,000";
        _rpm4000.Click += OnRpmScaleCommand;
        //
        // _rpm6000
        //
        _rpm6000.Name = "_rpm6000";
        _rpm6000.Size = new Size(227, 32);
        _rpm6000.Tag = 6000;
        _rpm6000.Text = "RPM scale: 6,000";
        _rpm6000.Click += OnRpmScaleCommand;
        //
        // _rpm8000
        //
        _rpm8000.Name = "_rpm8000";
        _rpm8000.Size = new Size(227, 32);
        _rpm8000.Tag = 8000;
        _rpm8000.Text = "RPM scale: 8,000";
        _rpm8000.Click += OnRpmScaleCommand;
        //
        // _rpm10000
        //
        _rpm10000.Name = "_rpm10000";
        _rpm10000.Size = new Size(227, 32);
        _rpm10000.Tag = 10000;
        _rpm10000.Text = "RPM scale: 10,000";
        _rpm10000.Click += OnRpmScaleCommand;
        //
        // _rpm12000
        //
        _rpm12000.Name = "_rpm12000";
        _rpm12000.Size = new Size(227, 32);
        _rpm12000.Tag = 12000;
        _rpm12000.Text = "RPM scale: 12,000";
        _rpm12000.Click += OnRpmScaleCommand;
        //
        // ObdTelemetryView
        //
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Controls.Add(_layout);
        Name = "ObdTelemetryView";
        Size = new Size(331, 448);
        _layout.ResumeLayout(false);
        _rpmMenu.ResumeLayout(false);
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private WarpToolkit.WinForms.DirectX.Gauges.D2DVehicleGaugePanel _gauges;
    private ContextMenuStrip _rpmMenu;
    private ToolStripMenuItem _rpm4000;
    private ToolStripMenuItem _rpm6000;
    private ToolStripMenuItem _rpm8000;
    private ToolStripMenuItem _rpm10000;
    private ToolStripMenuItem _rpm12000;
}
