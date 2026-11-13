#nullable disable

using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

partial class RawLogViewerDialog
{
    private System.ComponentModel.IContainer components;
    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _openFileButton;
    private SymbolToolStripButton _openFolderButton;
    private ToolStripSeparator _separator;
    private SymbolToolStripButton _closeButton;
    private DataGridView _grid;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _pathLabel;

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
        _closeButton = new SymbolToolStripButton();
        _grid = new DataGridView();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _pathLabel = new ToolStripStatusLabel();
        _toolStrip.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
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
        _grid.Size = new Size(1200, 590);
        _grid.TabIndex = 1;
        _grid.VirtualMode = true;
        _grid.CellValueNeeded += OnCellValueNeeded;
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
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
