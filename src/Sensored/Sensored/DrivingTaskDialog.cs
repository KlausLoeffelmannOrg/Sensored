namespace Sensored;

/// <summary>Lets a parked user or passenger opt into guidance for one upcoming road condition.</summary>
internal sealed partial class DrivingTaskDialog : Form
{
    /// <summary>Creates a task editor without starting sensors or recording.</summary>
    internal DrivingTaskDialog(DrivingTaskSelection? selection, bool isRecording, Font dialogFont)
    {
        InitializeComponent();
        Font = dialogFont;
        _tasks.DisplayMember = nameof(DrivingCaptureTask.Title);
        _tasks.DataSource = DrivingCaptureTask.All;
        _tasks.SelectedItem = selection?.Task ?? DrivingCaptureTask.All[0];
        _notes.Text = selection?.Notes ?? string.Empty;
        _recordingHint.Text = isRecording
            ? "Recording is already running. Use task adds an intent boundary now; switching or turning Off ends the previous label. Recording continues."
            : "No live recording is running. Use task arms a label for your next recording; it does not start sensors or recording.";
        OnTaskChanged(this, EventArgs.Empty);
    }

    /// <summary>Gets the current choice, to be applied only when the dialog is accepted.</summary>
    internal DrivingTaskSelection? Selection
        => _tasks.SelectedItem is DrivingCaptureTask { Id: not "off" } task
            ? new(task, _notes.Text.Trim())
            : null;

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Rectangle area = Screen.FromControl(this).WorkingArea;
        MinimumSize = new Size(Math.Min(MinimumSize.Width, area.Width), Math.Min(MinimumSize.Height, area.Height));
        Size = new Size(Math.Min(Width, area.Width), Math.Min(Height, area.Height));
    }

    private void OnTaskChanged(object? sender, EventArgs e)
    {
        if (_tasks.SelectedItem is not DrivingCaptureTask task)
        {
            return;
        }

        _guidance.Text = task.Guidance;
        _notes.Enabled = task.Id != "off";
        _apply.Text = task.Id == "off" ? "Turn task off" : "Use task";
    }
}
