namespace Sensored.TabViews;

partial class GpsMotionTabView
{
    private System.ComponentModel.IContainer components = null;

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
        _layout = new TableLayoutPanel();
        _motionGrid = new DataGridView();
        _methodColumn = new DataGridViewTextBoxColumn();
        _speedColumn = new DataGridViewTextBoxColumn();
        _courseColumn = new DataGridViewTextBoxColumn();
        _durationColumn = new DataGridViewTextBoxColumn();
        _countColumn = new DataGridViewTextBoxColumn();
        _motionStatus = new Label();
        _explanation = new Label();
        _rawGrid = new DataGridView();
        _rawCaptionColumn = new DataGridViewTextBoxColumn();
        _rawValueColumn = new DataGridViewTextBoxColumn();
        _axisValueColumn = new DataGridViewTextBoxColumn();
        components = new System.ComponentModel.Container();
        SuspendLayout();
        _layout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_motionGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_rawGrid).BeginInit();
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_explanation, 0, 0);
        _layout.Controls.Add(_motionGrid, 0, 1);
        _layout.Controls.Add(_motionStatus, 0, 2);
        _layout.Controls.Add(_rawGrid, 0, 3);
        _layout.AutoScroll = true;
        _layout.Dock = DockStyle.Fill;
        _layout.Name = "_layout";
        _layout.RowCount = 4;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _motionGrid.AccessibleName = "GPS motion speed and direction comparison";
        _motionGrid.AllowUserToAddRows = false;
        _motionGrid.AllowUserToDeleteRows = false;
        _motionGrid.AllowUserToOrderColumns = false;
        _motionGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _motionGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _motionGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _motionGrid.Columns.AddRange(new DataGridViewColumn[] {
            _methodColumn, _speedColumn, _courseColumn, _durationColumn, _countColumn });
        _motionGrid.Dock = DockStyle.Fill;
        _motionGrid.Name = "_motionGrid";
        _motionGrid.ReadOnly = true;
        _motionGrid.RowHeadersVisible = false;
        _motionGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _motionGrid.TabIndex = 1;
        _methodColumn.HeaderText = "Source/window";
        _methodColumn.Name = "_methodColumn";
        _methodColumn.MinimumWidth = 120;
        _methodColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _speedColumn.HeaderText = "km/h";
        _speedColumn.ToolTipText = "Speed in kilometres per hour. Coordinate windows report interval averages.";
        _speedColumn.Name = "_speedColumn";
        _speedColumn.MinimumWidth = 85;
        _speedColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _courseColumn.HeaderText = "Course";
        _courseColumn.ToolTipText = "Course clockwise from true north in degrees. Coordinate course requires at least 5 m of usable motion.";
        _courseColumn.Name = "_courseColumn";
        _courseColumn.MinimumWidth = 100;
        _courseColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _durationColumn.HeaderText = "Span s";
        _durationColumn.Name = "_durationColumn";
        _durationColumn.MinimumWidth = 70;
        _durationColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _countColumn.HeaderText = "Fixes";
        _countColumn.Name = "_countColumn";
        _countColumn.MinimumWidth = 50;
        _countColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _motionStatus.AutoSize = true;
        _motionStatus.Dock = DockStyle.Fill;
        _motionStatus.Name = "_motionStatus";
        _motionStatus.Padding = new Padding(4);
        _motionStatus.Text = "GPS unavailable";
        _motionStatus.TabIndex = 2;
        _explanation.AutoSize = true;
        _explanation.Dock = DockStyle.Fill;
        _explanation.Name = "_explanation";
        _explanation.Padding = new Padding(4);
        _explanation.Text = "GPS speed / course";
        _explanation.AccessibleDescription = "Coordinate windows average track distance over measurement time. Longer windows lag; position jitter can inflate speed. Course requires 5 m of motion.";
        _explanation.TabIndex = 0;
        _rawGrid.AccessibleName = "Internal and Bluetooth raw inclinometer, accelerometer, gyro and magnetometer readings";
        _rawGrid.AllowUserToAddRows = false;
        _rawGrid.AllowUserToDeleteRows = false;
        _rawGrid.AllowUserToOrderColumns = false;
        _rawGrid.AllowUserToResizeRows = false;
        _rawGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        _rawGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _rawGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        _rawGrid.Columns.AddRange(new DataGridViewColumn[] {
            _rawCaptionColumn, _rawValueColumn, _axisValueColumn });
        _rawGrid.Dock = DockStyle.Fill;
        _rawGrid.Margin = new Padding(3, 24, 3, 3);
        _rawGrid.Name = "_rawGrid";
        _rawGrid.ReadOnly = true;
        _rawGrid.RowHeadersVisible = false;
        _rawGrid.TabIndex = 3;
        _rawCaptionColumn.Name = "_rawCaptionColumn";
        _rawCaptionColumn.HeaderText = "Sensor";
        _rawCaptionColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _rawValueColumn.Name = "_rawValueColumn";
        _rawValueColumn.HeaderText = "Internal Sensors";
        _rawValueColumn.MinimumWidth = 100;
        _rawValueColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _axisValueColumn.Name = "_axisValueColumn";
        _axisValueColumn.HeaderText = "Bluetooth Sensors";
        _axisValueColumn.MinimumWidth = 100;
        _axisValueColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _axisValueColumn.Visible = false;
        AutoScaleMode = AutoScaleMode.Inherit;
        Controls.Add(_layout);
        Name = "GpsMotionTabView";
        AccessibleName = "Raw Data";
        Size = new Size(700, 320);
        ((System.ComponentModel.ISupportInitialize)_motionGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)_rawGrid).EndInit();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private DataGridView _motionGrid;
    private DataGridViewTextBoxColumn _methodColumn;
    private DataGridViewTextBoxColumn _speedColumn;
    private DataGridViewTextBoxColumn _courseColumn;
    private DataGridViewTextBoxColumn _durationColumn;
    private DataGridViewTextBoxColumn _countColumn;
    private Label _motionStatus;
    private Label _explanation;
    private DataGridView _rawGrid;
    private DataGridViewTextBoxColumn _rawCaptionColumn;
    private DataGridViewTextBoxColumn _rawValueColumn;
    private DataGridViewTextBoxColumn _axisValueColumn;
}
