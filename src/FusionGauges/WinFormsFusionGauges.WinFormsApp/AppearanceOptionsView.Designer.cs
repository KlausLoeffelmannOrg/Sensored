namespace WinFormsFusionGauges.WinFormsApp;

partial class AppearanceOptionsView
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
        _iconLabel = new Label();
        _fontLabel = new Label();
        _icons = new ComboBox();
        _fonts = new ComboBox();
        _hint = new Label();
        _digitsLabel = new Label();
        _digits = new NumericUpDown();
        _rings = new WarpToolkit.WinForms.Sensors.DirectX.SpeedometerOptionsView();
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
        _layout.Controls.Add(_iconLabel, 0, 0);
        _layout.Controls.Add(_icons, 1, 0);
        _layout.Controls.Add(_fontLabel, 0, 1);
        _layout.Controls.Add(_fonts, 1, 1);
        _layout.Controls.Add(_hint, 0, 2);
        _layout.SetColumnSpan(_hint, 2);
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_digitsLabel, 0, 3);
        _layout.Controls.Add(_digits, 1, 3);
        _layout.Controls.Add(_rings, 0, 4);
        _layout.SetColumnSpan(_rings, 2);
        _digitsLabel.Text = "Acceleration decimals:";
        _digitsLabel.AutoSize = true;
        _digitsLabel.Anchor = AnchorStyles.Left;
        _digitsLabel.Margin = new Padding(8);
        _digits.Minimum = 0;
        _digits.Maximum = 5;
        _digits.Value = 2;
        _digits.Margin = new Padding(8);
        _digits.AccessibleName = "Acceleration decimal places from zero to five";
        _rings.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _rings.Size = new Size(620, 360);
        _rings.AccessibleName = "Speedometer LED ring settings";
        _iconLabel.Text = "Icon Size:";
        _iconLabel.AutoSize = true;
        _iconLabel.Anchor = AnchorStyles.Left;
        _iconLabel.Margin = new Padding(8, 16, 8, 16);
        _fontLabel.Text = "UI Font Size:";
        _fontLabel.AutoSize = true;
        _fontLabel.Anchor = AnchorStyles.Left;
        _fontLabel.Margin = new Padding(8, 16, 8, 16);
        _icons.DropDownStyle = ComboBoxStyle.DropDownList;
        _icons.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _icons.Margin = new Padding(8, 16, 8, 16);
        _icons.AccessibleName = "Toolbar icon size";
        _fonts.DropDownStyle = ComboBoxStyle.DropDownList;
        _fonts.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _fonts.Margin = new Padding(8, 16, 8, 16);
        _fonts.AccessibleName = "Base UI font size";
        _hint.Text = "Sizes are logical pixels at 96 DPI. Touch padding remains independent of icon size. Menu, toolbar and status fonts are assigned explicitly.";
        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(620, 0);
        _hint.Margin = new Padding(8, 24, 8, 8);
        AutoScaleMode = AutoScaleMode.Inherit;
        AutoScroll = true;
        Padding = new Padding(12);
        Size = new Size(680, 400);
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private Label _iconLabel;
    private Label _fontLabel;
    private ComboBox _icons;
    private ComboBox _fonts;
    private Label _hint;
    private Label _digitsLabel;
    private NumericUpDown _digits;
    private WarpToolkit.WinForms.Sensors.DirectX.SpeedometerOptionsView _rings;
}
