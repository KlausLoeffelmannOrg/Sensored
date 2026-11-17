#nullable disable

using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored.TabViews;

partial class RouteSectionTabView
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
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        _routeSectionGroupBox = new GroupBox();
        _routeSectionGrid = new RoadSectionClassificationGrid();
        components = new System.ComponentModel.Container();
        _routeSectionGroupBox.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_routeSectionGrid).BeginInit();
        SuspendLayout();
        // 
        // _routeSectionGroupBox
        // 
        _routeSectionGroupBox.Controls.Add(_routeSectionGrid);
        _routeSectionGroupBox.Dock = DockStyle.Fill;
        _routeSectionGroupBox.FlatStyle = FlatStyle.Popup;
        _routeSectionGroupBox.Location = new Point(0, 0);
        _routeSectionGroupBox.Margin = new Padding(4, 5, 4, 5);
        _routeSectionGroupBox.Name = "_routeSectionGroupBox";
        _routeSectionGroupBox.Padding = new Padding(9, 10, 9, 10);
        _routeSectionGroupBox.Size = new Size(974, 650);
        _routeSectionGroupBox.TabIndex = 0;
        _routeSectionGroupBox.TabStop = false;
        _routeSectionGroupBox.Text = "Route section classification";
        _routeSectionGroupBox.VisualStylesMode = VisualStylesMode.Net11;
        // 
        // _routeSectionGrid
        // 
        _routeSectionGrid.AccessibleName = "Route section classifications";
        _routeSectionGrid.AllowUserToAddRows = false;
        _routeSectionGrid.AllowUserToDeleteRows = false;
        _routeSectionGrid.AllowUserToResizeColumns = false;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(245, 245, 245);
        dataGridViewCellStyle1.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle1.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle1.SelectionForeColor = SystemColors.HighlightText;
        _routeSectionGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        _routeSectionGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
        _routeSectionGrid.BackgroundColor = SystemColors.Window;
        dataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle2.BackColor = SystemColors.Control;
        dataGridViewCellStyle2.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle2.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.SelectionBackColor = SystemColors.Control;
        dataGridViewCellStyle2.SelectionForeColor = SystemColors.ControlText;
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        _routeSectionGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
        _routeSectionGrid.ColumnHeadersHeight = 34;
        _routeSectionGrid.ColumnHeadersVisible = false;
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = SystemColors.Window;
        dataGridViewCellStyle3.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle3.ForeColor = SystemColors.WindowText;
        dataGridViewCellStyle3.SelectionBackColor = SystemColors.Highlight;
        dataGridViewCellStyle3.SelectionForeColor = SystemColors.HighlightText;
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        _routeSectionGrid.DefaultCellStyle = dataGridViewCellStyle3;
        _routeSectionGrid.Dock = DockStyle.Fill;
        _routeSectionGrid.EnableHeadersVisualStyles = false;
        _routeSectionGrid.GridColor = SystemColors.ControlDark;
        _routeSectionGrid.Location = new Point(13, 88);
        _routeSectionGrid.Margin = new Padding(4, 5, 4, 5);
        _routeSectionGrid.MultiSelect = false;
        _routeSectionGrid.Name = "_routeSectionGrid";
        _routeSectionGrid.ReadOnly = true;
        dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle4.BackColor = SystemColors.Control;
        dataGridViewCellStyle4.Font = new Font("Segoe UI", 9F);
        dataGridViewCellStyle4.ForeColor = SystemColors.ControlText;
        dataGridViewCellStyle4.SelectionBackColor = SystemColors.Control;
        dataGridViewCellStyle4.SelectionForeColor = SystemColors.ControlText;
        dataGridViewCellStyle4.WrapMode = DataGridViewTriState.True;
        _routeSectionGrid.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
        _routeSectionGrid.RowHeadersVisible = false;
        _routeSectionGrid.RowHeadersWidth = 62;
        _routeSectionGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _routeSectionGrid.Size = new Size(948, 548);
        _routeSectionGrid.TabIndex = 0;
        // 
        // RouteSectionTabView
        // 
        AccessibleName = "Route sections";
        AutoScaleMode = AutoScaleMode.Inherit;
        Controls.Add(_routeSectionGroupBox);
        Margin = new Padding(4, 5, 4, 5);
        Name = "RouteSectionTabView";
        Size = new Size(974, 650);
        _routeSectionGroupBox.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)_routeSectionGrid).EndInit();
        ResumeLayout(false);
    }

    private System.ComponentModel.IContainer components;
    private GroupBox _routeSectionGroupBox;
    private RoadSectionClassificationGrid _routeSectionGrid;
}
