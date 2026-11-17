namespace Sensored.TabViews;

/// <summary>Displays a bounded raw OBD tail only when selected by the dashboard.</summary>
internal sealed partial class ObdRawTabView : UserControl
{
    private long _version = -1;

    internal ObdRawTabView()
    {
        InitializeComponent();
    }

    internal event EventHandler? RecordRequested;
    internal event EventHandler? StopRequested;

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        if (_toolbar is not null)
        {
            _toolbar.Font = Font;
        }
    }

    internal void Present(ObdRawTrafficSnapshot snapshot, bool canRecord, string? error = null)
    {
        _recordButton.Enabled = canRecord && !snapshot.Recording;
        _stopButton.Enabled = snapshot.Recording && error is null;
        _captureStatus.Text = error is not null ? $"Capture incomplete: {error}"
            : snapshot.Recording
            ? "Recording (automatic stop after 1 minute)"
            : snapshot.FilePath is { } path ? $"Saved: {path}" : "Live adapter TX/RX; latest 500 messages";
        _captureStatus.ToolTipText = _captureStatus.Text;
        if (snapshot.Version == _version)
        {
            return;
        }

        _version = snapshot.Version;
        _messages.Text = string.Join(Environment.NewLine, snapshot.Entries.Select(entry =>
            $"{entry.Timestamp:HH:mm:ss.fff} {entry.Direction} "
                + entry.Message.Replace("\r", "\\r").Replace("\n", "\\n")));
        _messages.SelectionStart = _messages.TextLength;
        _messages.ScrollToCaret();
    }

    internal void SetPending(bool pending)
    {
        _recordButton.Enabled = !pending && _recordButton.Enabled;
        _stopButton.Enabled = !pending && _stopButton.Enabled;
    }

    private void OnRecordClick(object? sender, EventArgs e)
        => RecordRequested?.Invoke(this, EventArgs.Empty);

    private void OnStopClick(object? sender, EventArgs e)
        => StopRequested?.Invoke(this, EventArgs.Empty);
}
