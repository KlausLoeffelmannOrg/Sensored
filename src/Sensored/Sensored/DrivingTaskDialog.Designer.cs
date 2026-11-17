#nullable disable

namespace Sensored;

partial class DrivingTaskDialog
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
        _layout = new TableLayoutPanel();
        _content = new Panel();
        _contentLayout = new TableLayoutPanel();
        _safety = new Label();
        _taskLabel = new Label();
        _tasks = new ComboBox();
        _guidance = new TextBox();
        _notesLabel = new Label();
        _notes = new TextBox();
        _recordingHint = new Label();
        _buttons = new TableLayoutPanel();
        _apply = new Button();
        _cancel = new Button();
        components = new System.ComponentModel.Container();
        _layout.SuspendLayout();
        _content.SuspendLayout();
        _contentLayout.SuspendLayout();
        _buttons.SuspendLayout();
        SuspendLayout();
        //
        // _layout
        //
        _layout.ColumnCount = 1;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _layout.Controls.Add(_content, 0, 0);
        _layout.Controls.Add(_buttons, 0, 1);
        _layout.Dock = DockStyle.Fill;
        _layout.Name = "_layout";
        _layout.Padding = new Padding(12);
        _layout.RowCount = 2;
        _layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.TabIndex = 0;
        //
        // _content
        //
        _content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _content.AutoScroll = true;
        _content.Controls.Add(_contentLayout);
        _content.Name = "_content";
        _content.TabIndex = 0;
        //
        // _contentLayout
        //
        _contentLayout.AutoSize = true;
        _contentLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _contentLayout.ColumnCount = 1;
        _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _contentLayout.Controls.Add(_safety, 0, 0);
        _contentLayout.Controls.Add(_taskLabel, 0, 1);
        _contentLayout.Controls.Add(_tasks, 0, 2);
        _contentLayout.Controls.Add(_guidance, 0, 3);
        _contentLayout.Controls.Add(_notesLabel, 0, 4);
        _contentLayout.Controls.Add(_notes, 0, 5);
        _contentLayout.Controls.Add(_recordingHint, 0, 6);
        _contentLayout.Dock = DockStyle.Top;
        _contentLayout.Name = "_contentLayout";
        _contentLayout.RowCount = 7;
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _contentLayout.TabIndex = 0;
        //
        // _safety
        //
        _safety.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _safety.AutoSize = true;
        _safety.Name = "_safety";
        _safety.Text = "Choose while parked, or let a passenger operate the app. Follow your normal route and traffic rules; skip any unsuitable task. No timed maneuvers, targets or automatic completion. Labels describe your intent, not proven road conditions or exact event times.";
        //
        // _taskLabel
        //
        _taskLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _taskLabel.AutoSize = true;
        _taskLabel.Name = "_taskLabel";
        _taskLabel.Text = "Upcoming condition (one optional task at a time)";
        //
        // _tasks
        //
        _tasks.AccessibleName = "Optional driving task";
        _tasks.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _tasks.DropDownStyle = ComboBoxStyle.DropDownList;
        _tasks.Name = "_tasks";
        _tasks.TabIndex = 0;
        _tasks.SelectedIndexChanged += OnTaskChanged;
        //
        // _guidance
        //
        _guidance.AccessibleName = "Opportunity, collection guidance and purpose";
        _guidance.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _guidance.Multiline = true;
        _guidance.MinimumSize = new Size(0, 160);
        _guidance.Name = "_guidance";
        _guidance.ReadOnly = true;
        _guidance.ScrollBars = ScrollBars.Vertical;
        _guidance.TabIndex = 1;
        //
        // _notesLabel
        //
        _notesLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _notesLabel.AutoSize = true;
        _notesLabel.Name = "_notesLabel";
        _notesLabel.Text = "Optional notes: direction, surface, known references or uncertainty";
        //
        // _notes
        //
        _notes.AccessibleName = "User-declared driving task notes";
        _notes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _notes.MaxLength = 2000;
        _notes.Multiline = true;
        _notes.MinimumSize = new Size(0, 60);
        _notes.Name = "_notes";
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.TabIndex = 2;
        //
        // _recordingHint
        //
        _recordingHint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _recordingHint.AutoSize = true;
        _recordingHint.Name = "_recordingHint";
        _recordingHint.Text = "Use task does not start sensors or recording.";
        //
        // _buttons
        //
        _buttons.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _buttons.AutoSize = true;
        _buttons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttons.ColumnCount = 2;
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _buttons.Controls.Add(_apply, 0, 0);
        _buttons.Controls.Add(_cancel, 1, 0);
        _buttons.Name = "_buttons";
        _buttons.RowCount = 1;
        _buttons.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _buttons.TabIndex = 3;
        //
        // _apply
        //
        _apply.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _apply.AutoSize = true;
        _apply.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _apply.DialogResult = DialogResult.OK;
        _apply.Name = "_apply";
        _apply.TabIndex = 0;
        _apply.Text = "Use task";
        _apply.UseVisualStyleBackColor = true;
        //
        // _cancel
        //
        _cancel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _cancel.AutoSize = true;
        _cancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cancel.DialogResult = DialogResult.Cancel;
        _cancel.Name = "_cancel";
        _cancel.TabIndex = 1;
        _cancel.Text = "Cancel";
        _cancel.UseVisualStyleBackColor = true;
        //
        // DrivingTaskDialog
        //
        AcceptButton = _apply;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _cancel;
        ClientSize = new Size(740, 650);
        Controls.Add(_layout);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(480, 420);
        Name = "DrivingTaskDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Driving tasks - optional data collection guide";
        _buttons.ResumeLayout(false);
        _buttons.PerformLayout();
        _contentLayout.ResumeLayout(false);
        _contentLayout.PerformLayout();
        _content.ResumeLayout(false);
        _content.PerformLayout();
        _layout.ResumeLayout(false);
        _layout.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _layout;
    private Panel _content;
    private TableLayoutPanel _contentLayout;
    private Label _safety;
    private Label _taskLabel;
    private ComboBox _tasks;
    private TextBox _guidance;
    private Label _notesLabel;
    private TextBox _notes;
    private Label _recordingHint;
    private TableLayoutPanel _buttons;
    private Button _apply;
    private Button _cancel;
}
