namespace WinFormsFusionGauges.WinFormsApp;

partial class MirrorOptionsView
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
        _enabled = new CheckBox();
        _folderLabel = new Label();
        _compressionLabel = new Label();
        _folder = new TextBox();
        _compression = new ComboBox();
        _browse = new Button();
        _hint = new Label();
        _layout.SuspendLayout();
        SuspendLayout();
        _layout.Dock = DockStyle.Top;
        _layout.AutoSize = true;
        _layout.ColumnCount = 2;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 5;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_enabled, 0, 0);
        _layout.SetColumnSpan(_enabled, 2);
        _layout.Controls.Add(_folderLabel, 0, 1);
        _layout.Controls.Add(_folder, 1, 1);
        _layout.Controls.Add(_browse, 1, 2);
        _layout.Controls.Add(_compressionLabel, 0, 3);
        _layout.Controls.Add(_compression, 1, 3);
        _layout.Controls.Add(_hint, 0, 4);
        _layout.SetColumnSpan(_hint, 2);
        _enabled.Text = "Publish finalized recordings as ZIPs to a OneDrive sync folder";
        _enabled.AutoSize = true;
        _enabled.Margin = new Padding(8, 16, 8, 16);
        _folderLabel.Text = "Sync-folder base path:";
        _folderLabel.AutoSize = true;
        _folderLabel.Anchor = AnchorStyles.Left;
        _folderLabel.Margin = new Padding(8, 16, 8, 16);
        _folder.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _folder.Margin = new Padding(8, 16, 8, 16);
        _folder.AccessibleName = "Locally synchronized OneDrive base folder";
        _browse.Text = "Choose sync folder";
        _browse.AutoSize = true;
        _browse.MinimumSize = new Size(180, 44);
        _browse.Margin = new Padding(8);
        _browse.Click += OnBrowse;
        _compressionLabel.Text = "ZIP compression:";
        _compressionLabel.AutoSize = true;
        _compressionLabel.Anchor = AnchorStyles.Left;
        _compressionLabel.Margin = new Padding(8, 16, 8, 16);
        _compression.DropDownStyle = ComboBoxStyle.DropDownList;
        _compression.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _compression.Margin = new Padding(8, 16, 8, 16);
        _compression.AccessibleName = "ZIP compression level";
        _hint.Text = "Local recording continues if the mirror fails. Completed parts are streamed into ZIPs off the sensor thread. Incomplete ZIPs stay local. Published means copied to the sync folder, not uploaded. Retry ZIPs resumes durable pending work after restart.";
        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(620, 0);
        _hint.Margin = new Padding(8, 24, 8, 8);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Padding = new Padding(12);
        Size = new Size(720, 440);
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private CheckBox _enabled;
    private Label _folderLabel;
    private Label _compressionLabel;
    private TextBox _folder;
    private ComboBox _compression;
    private Button _browse;
    private Label _hint;
}
