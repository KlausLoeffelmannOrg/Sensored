#nullable disable

namespace Sensored;

partial class CleanupProgressForm
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
        _layout = new TableLayoutPanel();
        _messageLabel = new Label();
        _stageLabel = new Label();
        _elapsedLabel = new Label();
        _progressBar = new ProgressBar();
        _actionLayout = new TableLayoutPanel();
        _retryButton = new Button();
        _returnButton = new Button();
        _forceExitButton = new Button();
        _layout.SuspendLayout();
        _actionLayout.SuspendLayout();
        SuspendLayout();
        //
        // _layout
        //
        _layout.AutoSize = true;
        _layout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_messageLabel, 0, 0);
        _layout.Controls.Add(_stageLabel, 0, 1);
        _layout.Controls.Add(_elapsedLabel, 0, 2);
        _layout.Controls.Add(_progressBar, 0, 3);
        _layout.Controls.Add(_actionLayout, 0, 4);
        _layout.Dock = DockStyle.Fill;
        _layout.Location = new Point(0, 0);
        _layout.Name = "_layout";
        _layout.Padding = new Padding(12);
        _layout.RowCount = 5;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Size = new Size(400, 191);
        _layout.TabIndex = 0;
        //
        // _messageLabel
        //
        _messageLabel.AutoSize = true;
        _messageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _messageLabel.Location = new Point(15, 12);
        _messageLabel.Margin = new Padding(3, 0, 3, 0);
        _messageLabel.MaximumSize = new Size(520, 0);
        _messageLabel.Name = "_messageLabel";
        _messageLabel.Size = new Size(370, 42);
        _messageLabel.TabIndex = 0;
        _messageLabel.Text = "Finishing sensor cleanup. Please wait.";
        _messageLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _stageLabel
        //
        _stageLabel.AutoSize = true;
        _stageLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _stageLabel.Location = new Point(15, 54);
        _stageLabel.Name = "_stageLabel";
        _stageLabel.MaximumSize = new Size(520, 0);
        _stageLabel.Size = new Size(370, 24);
        _stageLabel.TabIndex = 1;
        _stageLabel.Text = "Waiting for cleanup.";
        _stageLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _elapsedLabel
        //
        _elapsedLabel.AutoSize = true;
        _elapsedLabel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _elapsedLabel.Location = new Point(15, 84);
        _elapsedLabel.Name = "_elapsedLabel";
        _elapsedLabel.Size = new Size(370, 19);
        _elapsedLabel.TabIndex = 2;
        _elapsedLabel.Text = "Elapsed: 00:00:00";
        _elapsedLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _progressBar
        //
        _progressBar.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _progressBar.Location = new Point(15, 112);
        _progressBar.Name = "_progressBar";
        _progressBar.MinimumSize = new Size(340, 0);
        _progressBar.Size = new Size(370, 24);
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.TabIndex = 3;
        //
        // _actionLayout
        //
        _actionLayout.AutoSize = true;
        _actionLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _actionLayout.ColumnCount = 3;
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33333F));
        _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33334F));
        _actionLayout.Controls.Add(_retryButton, 0, 0);
        _actionLayout.Controls.Add(_returnButton, 1, 0);
        _actionLayout.Controls.Add(_forceExitButton, 2, 0);
        _actionLayout.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _actionLayout.Location = new Point(15, 139);
        _actionLayout.Margin = new Padding(0, 4, 0, 0);
        _actionLayout.Name = "_actionLayout";
        _actionLayout.RowCount = 1;
        _actionLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _actionLayout.Size = new Size(370, 40);
        _actionLayout.TabIndex = 4;
        _actionLayout.Visible = false;
        //
        // _retryButton
        //
        _retryButton.AutoSize = true;
        _retryButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _retryButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _retryButton.Margin = new Padding(4);
        _retryButton.Name = "_retryButton";
        _retryButton.TabIndex = 0;
        _retryButton.Text = "Keep waiting";
        _retryButton.Click += OnRetryClick;
        //
        // _returnButton
        //
        _returnButton.AutoSize = true;
        _returnButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _returnButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _returnButton.Margin = new Padding(4);
        _returnButton.Name = "_returnButton";
        _returnButton.TabIndex = 1;
        _returnButton.Text = "Return to app";
        _returnButton.Click += OnReturnClick;
        //
        // _forceExitButton
        //
        _forceExitButton.AutoSize = true;
        _forceExitButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _forceExitButton.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _forceExitButton.Margin = new Padding(4);
        _forceExitButton.Name = "_forceExitButton";
        _forceExitButton.TabIndex = 2;
        _forceExitButton.Text = "Force Exit...";
        _forceExitButton.Click += OnForceExitClick;
        //
        // CleanupProgressForm
        //
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ClientSize = new Size(400, 191);
        ControlBox = false;
        Controls.Add(_layout);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "CleanupProgressForm";
        ShowIcon = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Sensored";
        TopMost = true;
        _actionLayout.ResumeLayout(false);
        _actionLayout.PerformLayout();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private TableLayoutPanel _layout;
    private Label _messageLabel;
    private Label _stageLabel;
    private Label _elapsedLabel;
    private ProgressBar _progressBar;
    private TableLayoutPanel _actionLayout;
    private Button _retryButton;
    private Button _returnButton;
    private Button _forceExitButton;
}
