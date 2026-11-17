namespace WinFormsFusionGauges.WinFormsApp;

partial class SessionNameDialog
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
        _name = new TextBox();
        _hint = new Label();
        _ok = new Button();
        _cancel = new Button();
        _buttons = new FlowLayoutPanel();
        _layout.SuspendLayout();
        _buttons.SuspendLayout();
        SuspendLayout();
        _layout.Dock = DockStyle.Fill;
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 3;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_hint, 0, 0);
        _layout.Controls.Add(_name, 0, 1);
        _layout.Controls.Add(_buttons, 0, 2);
        _hint.Text = "Session prefix - enter only while parked";
        _hint.AutoSize = true;
        _hint.Margin = new Padding(12);
        _name.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _name.Margin = new Padding(12);
        _name.AccessibleName = "Recording session prefix";
        _buttons.AutoSize = true;
        _buttons.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
        _buttons.Controls.Add(_ok);
        _buttons.Controls.Add(_cancel);
        _ok.Text = "Record";
        _ok.AutoSize = true;
        _ok.MinimumSize = new Size(140, 48);
        _ok.Margin = new Padding(8);
        _ok.Click += OnAccept;
        _cancel.Text = "Cancel";
        _cancel.AutoSize = true;
        _cancel.MinimumSize = new Size(140, 48);
        _cancel.Margin = new Padding(8);
        _cancel.DialogResult = DialogResult.Cancel;
        AcceptButton = _ok;
        CancelButton = _cancel;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(520, 220);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Text = "New recording";
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        _buttons.ResumeLayout(false);
        _buttons.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private TextBox _name;
    private Label _hint;
    private Button _ok;
    private Button _cancel;
    private FlowLayoutPanel _buttons;
}
