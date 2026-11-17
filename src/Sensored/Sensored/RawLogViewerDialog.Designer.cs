#nullable disable

using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

partial class RawLogViewerDialog
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
        _toolStrip = new SymbolToolStrip();
        _openFileButton = new SymbolToolStripButton();
        _openFolderButton = new SymbolToolStripButton();
        _separator = new ToolStripSeparator();
        _rawButton = new ToolStripButton();
        _detectButton = new ToolStripButton();
        _saveCopyButton = new ToolStripButton();
        _cancelButton = new ToolStripButton();
        _closeButton = new SymbolToolStripButton();
        _grid = new DataGridView();
        _rawGrid = new DataGridView();
        _editorPanel = new Panel();
        _rangeList = new CheckedListBox();
        _editButtons = new FlowLayoutPanel();
        _selectRangeButton = new Button();
        _addRangeButton = new Button();
        _removeRangeButton = new Button();
        _startPicker = new DateTimePicker();
        _endPicker = new DateTimePicker();
        _startLabel = new Label();
        _endLabel = new Label();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _pathLabel = new ToolStripStatusLabel();
        _toolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_rawGrid).BeginInit();
        _editorPanel.SuspendLayout();
        _editButtons.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // _toolStrip
        //
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.ImageScalingSize = new Size(64, 64);
        _toolStrip.Items.AddRange(new ToolStripItem[]
        {
            _openFileButton,
            _openFolderButton,
            _separator,
            _rawButton,
            _detectButton,
            _saveCopyButton,
            _cancelButton,
            _closeButton
        });
        _toolStrip.Location = new Point(0, 0);
        _toolStrip.Name = "_toolStrip";
        _toolStrip.Padding = new Padding(6, 3, 6, 3);
        _toolStrip.Size = new Size(1200, 88);
        _toolStrip.TabIndex = 0;
        //
        // _openFileButton
        //
        _openFileButton.Name = "_openFileButton";
        _openFileButton.Size = new Size(78, 82);
        _openFileButton.Text = "Open file...";
        _openFileButton.Click += OnOpenFileClick;
        //
        // _openFolderButton
        //
        _openFolderButton.Name = "_openFolderButton";
        _openFolderButton.Size = new Size(92, 82);
        _openFolderButton.Text = "Open folder...";
        _openFolderButton.Click += OnOpenFolderClick;
        //
        // _separator
        //
        _separator.Name = "_separator";
        _separator.Size = new Size(6, 85);
        //
        // _rawButton
        //
        _rawButton.Name = "_rawButton";
        _rawButton.Text = "Raw selected row";
        _rawButton.Enabled = false;
        _rawButton.Click += OnRawClick;
        //
        // _detectButton
        //
        _detectButton.Name = "_detectButton";
        _detectButton.Text = "Find stationary ranges";
        _detectButton.Enabled = false;
        _detectButton.Click += OnDetectClick;
        //
        // _saveCopyButton
        //
        _saveCopyButton.Name = "_saveCopyButton";
        _saveCopyButton.Text = "Save As copy...";
        _saveCopyButton.Enabled = false;
        _saveCopyButton.Click += OnSaveCopyClick;
        //
        // _cancelButton
        //
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Text = "Cancel operation";
        _cancelButton.Enabled = false;
        _cancelButton.Click += OnCancelClick;
        //
        // _closeButton
        //
        _closeButton.Name = "_closeButton";
        _closeButton.Size = new Size(40, 82);
        _closeButton.Text = "Close";
        _closeButton.Click += OnCloseClick;
        //
        // _grid
        //
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToOrderColumns = true;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _grid.Dock = DockStyle.Fill;
        _grid.EditMode = DataGridViewEditMode.EditProgrammatically;
        _grid.Location = new Point(0, 88);
        _grid.Name = "_grid";
        _grid.ReadOnly = true;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Size = new Size(1200, 420);
        _grid.TabIndex = 1;
        _grid.VirtualMode = true;
        _grid.CellValueNeeded += OnCellValueNeeded;
        _grid.CellFormatting += OnCellFormatting;
        _grid.RowPostPaint += OnRowPostPaint;
        //
        // _rawGrid
        //
        _rawGrid.AllowUserToAddRows = false;
        _rawGrid.AllowUserToDeleteRows = false;
        _rawGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _rawGrid.Dock = DockStyle.Right;
        _rawGrid.Name = "_rawGrid";
        _rawGrid.ReadOnly = true;
        _rawGrid.RowHeadersVisible = false;
        _rawGrid.Size = new Size(360, 420);
        _rawGrid.TabIndex = 2;
        _rawGrid.Visible = false;
        //
        // _editorPanel
        //
        _editorPanel.Controls.Add(_rangeList);
        _editorPanel.Controls.Add(_editButtons);
        _editorPanel.Dock = DockStyle.Bottom;
        _editorPanel.Location = new Point(0, 508);
        _editorPanel.Name = "_editorPanel";
        _editorPanel.Size = new Size(1200, 170);
        _editorPanel.TabIndex = 2;
        //
        // _rangeList
        //
        _rangeList.CheckOnClick = true;
        _rangeList.Dock = DockStyle.Fill;
        _rangeList.FormattingEnabled = true;
        _rangeList.Name = "_rangeList";
        _rangeList.Size = new Size(1200, 128);
        _rangeList.TabIndex = 1;
        _rangeList.ItemCheck += OnRangeItemCheck;
        //
        // _editButtons
        //
        _editButtons.Controls.Add(_startLabel);
        _editButtons.Controls.Add(_startPicker);
        _editButtons.Controls.Add(_endLabel);
        _editButtons.Controls.Add(_endPicker);
        _editButtons.Controls.Add(_selectRangeButton);
        _editButtons.Controls.Add(_addRangeButton);
        _editButtons.Controls.Add(_removeRangeButton);
        _editButtons.Dock = DockStyle.Top;
        _editButtons.Name = "_editButtons";
        _editButtons.Size = new Size(1200, 42);
        _editButtons.TabIndex = 0;
        _editButtons.WrapContents = false;
        //
        // _startLabel
        //
        _startLabel.AutoSize = true;
        _startLabel.Margin = new Padding(8, 12, 3, 3);
        _startLabel.Name = "_startLabel";
        _startLabel.Text = "Start (local)";
        //
        // _startPicker
        //
        _startPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss.fff";
        _startPicker.Format = DateTimePickerFormat.Custom;
        _startPicker.Name = "_startPicker";
        _startPicker.ShowUpDown = true;
        _startPicker.Width = 195;
        //
        // _endLabel
        //
        _endLabel.AutoSize = true;
        _endLabel.Margin = new Padding(8, 12, 3, 3);
        _endLabel.Name = "_endLabel";
        _endLabel.Text = "End (local)";
        //
        // _endPicker
        //
        _endPicker.CustomFormat = "yyyy-MM-dd HH:mm:ss.fff";
        _endPicker.Format = DateTimePickerFormat.Custom;
        _endPicker.Name = "_endPicker";
        _endPicker.ShowUpDown = true;
        _endPicker.Width = 195;
        //
        // _selectRangeButton
        //
        _selectRangeButton.AutoSize = true;
        _selectRangeButton.Name = "_selectRangeButton";
        _selectRangeButton.Text = "Use selected rows";
        _selectRangeButton.Click += OnSelectRangeClick;
        //
        // _addRangeButton
        //
        _addRangeButton.AutoSize = true;
        _addRangeButton.Name = "_addRangeButton";
        _addRangeButton.Text = "Add manual range";
        _addRangeButton.Click += OnAddRangeClick;
        //
        // _removeRangeButton
        //
        _removeRangeButton.AutoSize = true;
        _removeRangeButton.Name = "_removeRangeButton";
        _removeRangeButton.Text = "Remove range";
        _removeRangeButton.Click += OnRemoveRangeClick;
        //
        // _statusStrip
        //
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _pathLabel });
        _statusStrip.Location = new Point(0, 678);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Size = new Size(1200, 22);
        _statusStrip.TabIndex = 2;
        //
        // _statusLabel
        //
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(997, 17);
        _statusLabel.Spring = true;
        _statusLabel.Text = "Open a current service-owned recording file or folder.";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _pathLabel
        //
        _pathLabel.Name = "_pathLabel";
        _pathLabel.Size = new Size(188, 17);
        _pathLabel.Text = "--";
        //
        // RawLogViewerDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 700);
        Controls.Add(_grid);
        Controls.Add(_rawGrid);
        Controls.Add(_editorPanel);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);
        MinimizeBox = false;
        MinimumSize = new Size(700, 450);
        Name = "RawLogViewerDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Raw sensor recording viewer";
        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_rawGrid).EndInit();
        _editorPanel.ResumeLayout(false);
        _editButtons.ResumeLayout(false);
        _editButtons.PerformLayout();
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _openFileButton;
    private SymbolToolStripButton _openFolderButton;
    private ToolStripSeparator _separator;
    private ToolStripButton _rawButton;
    private ToolStripButton _detectButton;
    private ToolStripButton _saveCopyButton;
    private ToolStripButton _cancelButton;
    private SymbolToolStripButton _closeButton;
    private DataGridView _grid;
    private DataGridView _rawGrid;
    private Panel _editorPanel;
    private CheckedListBox _rangeList;
    private FlowLayoutPanel _editButtons;
    private Button _selectRangeButton;
    private Button _addRangeButton;
    private Button _removeRangeButton;
    private DateTimePicker _startPicker;
    private DateTimePicker _endPicker;
    private Label _startLabel;
    private Label _endLabel;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _pathLabel;
}
