namespace WinFormsFusionGauges.WinFormsApp;

partial class CanDiagnosticResultsDialog
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
        _description = new Label();
        _results = new DataGridView();
        _commandColumn = new DataGridViewTextBoxColumn();
        _readingColumn = new DataGridViewTextBoxColumn();
        _resultColumn = new DataGridViewTextBoxColumn();
        _attemptsColumn = new DataGridViewTextBoxColumn();
        _samplesColumn = new DataGridViewTextBoxColumn();
        _responseColumn = new DataGridViewTextBoxColumn();
        _rawColumn = new DataGridViewTextBoxColumn();
        _valueColumn = new DataGridViewTextBoxColumn();
        _unitColumn = new DataGridViewTextBoxColumn();
        _timeColumn = new DataGridViewTextBoxColumn();
        _close = new Button();
        SuspendLayout();
        _layout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_results).BeginInit();
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 3;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_description, 0, 0);
        _layout.Controls.Add(_results, 0, 1);
        _layout.Controls.Add(_close, 0, 2);
        _description.AutoSize = true;
        _description.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _description.Text = "Snapshot of this investigation; latest readings, not live polling. OK acknowledges adapter setup, not vehicle data. CAN values are UNVERIFIED; missing frames do not establish lack of support. Select cells and press Ctrl+C to copy.";
        _description.Margin = new Padding(3, 3, 3, 12);
        _results.Name = "_results";
        _results.AccessibleName = "CAN investigative command and signal results";
        _results.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _results.ReadOnly = true;
        _results.AllowUserToAddRows = false;
        _results.AllowUserToDeleteRows = false;
        _results.AllowUserToOrderColumns = true;
        _results.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _results.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _results.RowHeadersVisible = false;
        _results.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText;
        _results.Columns.AddRange(new DataGridViewColumn[] {
            _commandColumn, _readingColumn, _resultColumn, _attemptsColumn, _samplesColumn,
            _responseColumn, _rawColumn, _valueColumn, _unitColumn, _timeColumn });
        _commandColumn.HeaderText = "Command / CAN ID";
        _readingColumn.HeaderText = "Reading";
        _resultColumn.HeaderText = "Result";
        _attemptsColumn.HeaderText = "Attempts";
        _samplesColumn.HeaderText = "Frames";
        _responseColumn.HeaderText = "Latest raw response";
        _rawColumn.HeaderText = "Raw signal";
        _valueColumn.HeaderText = "Decoded value";
        _unitColumn.HeaderText = "Unit";
        _timeColumn.HeaderText = "Last received";
        _close.Text = "Close";
        _close.AutoSize = true;
        _close.Anchor = AnchorStyles.Right;
        _close.DialogResult = DialogResult.Cancel;
        AcceptButton = _close;
        CancelButton = _close;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 650);
        MinimumSize = new Size(700, 400);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Text = "CAN Diagnostics - investigative results";
        Controls.Add(_layout);
        ((System.ComponentModel.ISupportInitialize)_results).EndInit();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private Label _description;
    private DataGridView _results;
    private DataGridViewTextBoxColumn _commandColumn;
    private DataGridViewTextBoxColumn _readingColumn;
    private DataGridViewTextBoxColumn _resultColumn;
    private DataGridViewTextBoxColumn _attemptsColumn;
    private DataGridViewTextBoxColumn _samplesColumn;
    private DataGridViewTextBoxColumn _responseColumn;
    private DataGridViewTextBoxColumn _rawColumn;
    private DataGridViewTextBoxColumn _valueColumn;
    private DataGridViewTextBoxColumn _unitColumn;
    private DataGridViewTextBoxColumn _timeColumn;
    private Button _close;
}
