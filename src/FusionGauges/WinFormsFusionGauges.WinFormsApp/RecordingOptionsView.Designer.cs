namespace WinFormsFusionGauges.WinFormsApp;

partial class RecordingOptionsView
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
        _folderLabel = new Label();
        _prefixLabel = new Label();
        _markerLabel = new Label();
        _folder = new TextBox();
        _prefix = new TextBox();
        _marker = new TextBox();
        _browse = new Button();
        _automatic = new CheckBox();
        _autostart = new CheckBox();
        _jsonl = new CheckBox();
        _compact = new CheckBox();
        _csv = new CheckBox();
        _hint = new Label();
        _rawRate = new NumericUpDown();
        _rawRateLabel = new Label();
        ((System.ComponentModel.ISupportInitialize)_rawRate).BeginInit();
        _layout.SuspendLayout();
        SuspendLayout();
        _layout.Dock = DockStyle.Top;
        _layout.AutoSize = true;
        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 11;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_folderLabel, 0, 0);
        _layout.Controls.Add(_folder, 1, 0);
        _layout.Controls.Add(_browse, 1, 1);
        _layout.Controls.Add(_prefixLabel, 0, 2);
        _layout.Controls.Add(_prefix, 1, 2);
        _layout.Controls.Add(_markerLabel, 0, 3);
        _layout.Controls.Add(_marker, 1, 3);
        _layout.Controls.Add(_automatic, 0, 4);
        _layout.SetColumnSpan(_automatic, 2);
        _layout.Controls.Add(_autostart, 0, 5);
        _layout.SetColumnSpan(_autostart, 2);
        _layout.Controls.Add(_jsonl, 0, 6);
        _layout.SetColumnSpan(_jsonl, 2);
        _layout.Controls.Add(_compact, 0, 7);
        _layout.SetColumnSpan(_compact, 2);
        _layout.Controls.Add(_csv, 0, 8);
        _layout.SetColumnSpan(_csv, 2);
        _layout.Controls.Add(_hint, 0, 9);
        _layout.SetColumnSpan(_hint, 2);
        _layout.Controls.Add(_rawRateLabel, 0, 10);
        _layout.Controls.Add(_rawRate, 1, 10);
        _rawRateLabel.Text = "RawV2 snapshots per second (next recording):";
        _rawRateLabel.AutoSize = true;
        _rawRateLabel.Anchor = AnchorStyles.Left;
        _rawRateLabel.Margin = new Padding(8, 14, 8, 14);
        _rawRate.Minimum = 1;
        _rawRate.Maximum = 20;
        _rawRate.Value = 10;
        _rawRate.Margin = new Padding(8, 14, 8, 14);
        _rawRate.AccessibleName = "RawV2 snapshots per second";
        _folderLabel.Text = "Local base folder:";
        _folderLabel.AutoSize = true;
        _folderLabel.Anchor = AnchorStyles.Left;
        _folderLabel.Margin = new Padding(8, 14, 8, 14);
        _prefixLabel.Text = "Automatic name prefix:";
        _prefixLabel.AutoSize = true;
        _prefixLabel.Anchor = AnchorStyles.Left;
        _prefixLabel.Margin = new Padding(8, 14, 8, 14);
        _markerLabel.Text = "Marker label:";
        _markerLabel.AutoSize = true;
        _markerLabel.Anchor = AnchorStyles.Left;
        _markerLabel.Margin = new Padding(8, 14, 8, 14);
        _folder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _folder.Margin = new Padding(8, 14, 8, 14);
        _folder.AccessibleName = "Local recording base folder";
        _prefix.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _prefix.Margin = new Padding(8, 14, 8, 14);
        _prefix.AccessibleName = "Automatic recording filename prefix";
        _marker.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _marker.Margin = new Padding(8, 14, 8, 14);
        _marker.AccessibleName = "Generated marker label";
        _browse.Text = "Choose local folder";
        _browse.AutoSize = true;
        _browse.MinimumSize = new Size(180, 44);
        _browse.Margin = new Padding(8);
        _browse.Click += OnBrowse;
        _automatic.Text = "Automatic filenames (prefix + timestamp + unique session id)";
        _automatic.AutoSize = true;
        _automatic.Margin = new Padding(8, 12, 8, 12);
        _autostart.Text = "Start recording automatically when sensors start";
        _autostart.AutoSize = true;
        _autostart.Margin = new Padding(8, 12, 8, 12);
        _jsonl.Text = "Verbose JSON Lines";
        _jsonl.AutoSize = true;
        _jsonl.Margin = new Padding(8, 12, 8, 12);
        _compact.Text = "Full-rate compact JSON replay journal (required)";
        _compact.Checked = true;
        _compact.Enabled = false;
        _compact.AutoSize = true;
        _compact.Margin = new Padding(8, 12, 8, 12);
        _csv.Text = "Matched RawV2 / FuseV2 semicolon CSV pair (required)";
        _csv.Checked = true;
        _csv.Enabled = false;
        _csv.AutoSize = true;
        _csv.Margin = new Padding(8, 12, 8, 12);
        _hint.Text = "V2 raw snapshots use the selected 1..20 Hz rate (default 10). Fusion and replay journals stay full-rate. Markers and notes are timestamped. Choose filenames only while parked.";
        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(620, 0);
        _hint.Margin = new Padding(8, 16, 8, 8);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Padding = new Padding(12);
        Size = new Size(720, 650);
        Controls.Add(_layout);
        ((System.ComponentModel.ISupportInitialize)_rawRate).EndInit();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private Label _folderLabel;
    private Label _prefixLabel;
    private Label _markerLabel;
    private TextBox _folder;
    private TextBox _prefix;
    private TextBox _marker;
    private Button _browse;
    private CheckBox _automatic;
    private CheckBox _autostart;
    private CheckBox _jsonl;
    private CheckBox _compact;
    private CheckBox _csv;
    private Label _hint;
    private NumericUpDown _rawRate;
    private Label _rawRateLabel;
}
