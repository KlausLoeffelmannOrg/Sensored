namespace WinFormsFusionGauges.WinFormsApp;

partial class MotionCalibrationDialog
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
        _heading = new Label();
        _instructions = new Label();
        _progress = new Label();
        _results = new TextBox();
        _buttons = new FlowLayoutPanel();
        _capture = new Button();
        _next = new Button();
        _restart = new Button();
        _cancel = new Button();
        _layout.SuspendLayout();
        _buttons.SuspendLayout();
        SuspendLayout();
        _layout.Dock = DockStyle.Fill;
        _layout.Padding = new Padding(12);
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.RowCount = 5;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(_heading, 0, 0);
        _layout.Controls.Add(_instructions, 0, 1);
        _layout.Controls.Add(_progress, 0, 2);
        _layout.Controls.Add(_results, 0, 3);
        _layout.Controls.Add(_buttons, 0, 4);
        _heading.AutoSize = true;
        _heading.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _heading.Margin = new Padding(6);
        _heading.Name = "_heading";
        _instructions.AutoSize = true;
        _instructions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _instructions.Margin = new Padding(6);
        _instructions.Name = "_instructions";
        _progress.AutoSize = true;
        _progress.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _progress.Margin = new Padding(6);
        _progress.Name = "_progress";
        _results.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _results.Multiline = true;
        _results.ReadOnly = true;
        _results.ScrollBars = ScrollBars.Vertical;
        _results.Margin = new Padding(6);
        _results.AccessibleName = "Measured landscape sensor axes";
        _results.Name = "_results";
        _buttons.AutoSize = true;
        _buttons.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _buttons.Controls.Add(_capture);
        _buttons.Controls.Add(_next);
        _buttons.Controls.Add(_restart);
        _buttons.Controls.Add(_cancel);
        _buttons.Name = "_buttons";
        _capture.AutoSize = true;
        _capture.MinimumSize = new Size(140, 48);
        _capture.Margin = new Padding(6);
        _capture.Click += OnCapture;
        _next.AutoSize = true;
        _next.MinimumSize = new Size(140, 48);
        _next.Margin = new Padding(6);
        _next.Click += OnNext;
        _restart.Text = "Start over";
        _restart.AutoSize = true;
        _restart.MinimumSize = new Size(120, 48);
        _restart.Margin = new Padding(6);
        _restart.Click += OnRestart;
        _cancel.Text = "Cancel";
        _cancel.AutoSize = true;
        _cancel.MinimumSize = new Size(120, 48);
        _cancel.Margin = new Padding(6);
        _cancel.DialogResult = DialogResult.Cancel;
        CancelButton = _cancel;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(800, 520);
        MinimumSize = new Size(600, 440);
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        MinimizeBox = false;
        Name = "MotionCalibrationDialog";
        Text = "Landscape motion calibration - while parked";
        Controls.Add(_layout);
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        _buttons.ResumeLayout(false);
        _buttons.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private Label _heading;
    private Label _instructions;
    private Label _progress;
    private TextBox _results;
    private FlowLayoutPanel _buttons;
    private Button _capture;
    private Button _next;
    private Button _restart;
    private Button _cancel;
}
