namespace WinFormsFusionGauges.WinFormsApp;

partial class SourceOptionsView
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
        _layout = new TableLayoutPanel();
        _sourceLabel = new Label();
        _portLabel = new Label();
        _profileLabel = new Label();
        _intervalLabel = new Label();
        _sources = new ComboBox();
        _ports = new ComboBox();
        _profiles = new ComboBox();
        _interval = new NumericUpDown();
        _refreshPorts = new Button();
        _authorize = new CheckBox();
        _hint = new Label();
        _obdAdapterLabel = new Label();
        _obdAdapter = new ComboBox();
        _layout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_interval).BeginInit();
        SuspendLayout();
        _layout.Dock = DockStyle.Top;
        _layout.AutoSize = true;
        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 8;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_sourceLabel, 0, 0);
        _layout.Controls.Add(_sources, 1, 0);
        _layout.Controls.Add(_intervalLabel, 0, 1);
        _layout.Controls.Add(_interval, 1, 1);
        _layout.Controls.Add(_portLabel, 0, 2);
        _layout.Controls.Add(_ports, 1, 2);
        _layout.Controls.Add(_profileLabel, 0, 3);
        _layout.Controls.Add(_profiles, 1, 3);
        _layout.Controls.Add(_refreshPorts, 1, 4);
        _layout.Controls.Add(_authorize, 0, 5);
        _layout.SetColumnSpan(_authorize, 2);
        _layout.Controls.Add(_hint, 0, 6);
        _layout.SetColumnSpan(_hint, 2);
        _layout.Controls.Add(_obdAdapterLabel, 0, 7);
        _layout.Controls.Add(_obdAdapter, 1, 7);
        _obdAdapterLabel.Text = "Paired OBD2 adapter:";
        _obdAdapterLabel.AutoSize = true;
        _obdAdapterLabel.Anchor = AnchorStyles.Left;
        _obdAdapterLabel.Margin = new Padding(8, 14, 8, 14);
        _obdAdapter.DropDownStyle = ComboBoxStyle.DropDownList;
        _obdAdapter.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _obdAdapter.Margin = new Padding(8, 14, 8, 14);
        _obdAdapter.AccessibleName = "Paired OBD2 adapter (applies after restarting the app)";
        _obdAdapter.FormattingEnabled = true;
        _obdAdapter.Format += OnAdapterFormat;
        _sourceLabel.Text = "Sources:";
        _sourceLabel.AutoSize = true;
        _sourceLabel.Anchor = AnchorStyles.Left;
        _sourceLabel.Margin = new Padding(8, 14, 8, 14);
        _portLabel.Text = "Serial COM port:";
        _portLabel.AutoSize = true;
        _portLabel.Anchor = AnchorStyles.Left;
        _portLabel.Margin = new Padding(8, 14, 8, 14);
        _profileLabel.Text = "GNSS profile:";
        _profileLabel.AutoSize = true;
        _profileLabel.Anchor = AnchorStyles.Left;
        _profileLabel.Margin = new Padding(8, 14, 8, 14);
        _intervalLabel.Text = "Windows GPS interval (ms):";
        _intervalLabel.AutoSize = true;
        _intervalLabel.Anchor = AnchorStyles.Left;
        _intervalLabel.Margin = new Padding(8, 14, 8, 14);
        _sources.DropDownStyle = ComboBoxStyle.DropDownList;
        _sources.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sources.Margin = new Padding(8, 14, 8, 14);
        _sources.AccessibleName = "Sensor source combination";
        _sources.SelectedIndexChanged += OnSourceChanged;
        _ports.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _ports.Margin = new Padding(8, 14, 8, 14);
        _ports.AccessibleName = "Serial COM port, blank for automatic discovery";
        _profiles.DropDownStyle = ComboBoxStyle.DropDownList;
        _profiles.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _profiles.Margin = new Padding(8, 14, 8, 14);
        _profiles.AccessibleName = "Receiver constellation profile";
        _interval.Minimum = 100;
        _interval.Maximum = 60000;
        _interval.Increment = 100;
        _interval.Value = 200;
        _interval.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _interval.Margin = new Padding(8, 14, 8, 14);
        _interval.AccessibleName = "Requested Windows GPS report interval in milliseconds";
        _refreshPorts.Text = "Refresh COM ports";
        _refreshPorts.AutoSize = true;
        _refreshPorts.MinimumSize = new Size(180, 44);
        _refreshPorts.Margin = new Padding(8);
        _refreshPorts.Click += OnRefreshPorts;
        _authorize.Text = "Authorize receiver commands for this app session only (parked)";
        _authorize.AutoSize = true;
        _authorize.Margin = new Padding(8, 16, 8, 16);
        _hint.Text = "OBD adapter changes apply after restarting the app. Pair the selected adapter in Windows first; AD20 requires ELM-compatible firmware. Blank COM port selects automatic discovery. Passive leaves the receiver unchanged. Report interval is requested, not guaranteed. Configure only while parked.";
        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(620, 0);
        _hint.Margin = new Padding(8, 16, 8, 8);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Padding = new Padding(12);
        Size = new Size(720, 500);
        Controls.Add(_layout);
        ((System.ComponentModel.ISupportInitialize)_interval).EndInit();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private Label _sourceLabel;
    private Label _portLabel;
    private Label _profileLabel;
    private Label _intervalLabel;
    private ComboBox _sources;
    private ComboBox _ports;
    private ComboBox _profiles;
    private NumericUpDown _interval;
    private Button _refreshPorts;
    private CheckBox _authorize;
    private Label _hint;
    private Label _obdAdapterLabel;
    private ComboBox _obdAdapter;
}
