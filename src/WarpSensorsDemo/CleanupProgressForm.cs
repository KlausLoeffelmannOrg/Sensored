namespace WarpSensorsDemo;

/// <summary>
///  Shows delayed, non-interactive progress while a sensor window completes asynchronous cleanup.
/// </summary>
internal sealed partial class CleanupProgressForm : Form
{
    private const int DisplayDelayMilliseconds = 250;

    /// <summary>
    ///  Initializes a new cleanup progress window.
    /// </summary>
    /// <param name="message">The cleanup operation shown to the user.</param>
    internal CleanupProgressForm(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        InitializeComponent();
        _messageLabel.Text = message;
    }

    /// <summary>
    ///  Runs cleanup and displays progress only when the operation lasts beyond the delay threshold.
    /// </summary>
    /// <param name="owner">The window that owns the cleanup progress UI.</param>
    /// <param name="message">The cleanup operation shown to the user.</param>
    /// <param name="cleanup">The asynchronous cleanup operation.</param>
    internal static async Task RunAsync(
        Form owner,
        string message,
        Func<Task> cleanup)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(cleanup);

        Task cleanupTask = cleanup();
        Task delayTask = Task.Delay(DisplayDelayMilliseconds);

        if (await Task.WhenAny(cleanupTask, delayTask).ConfigureAwait(true) == cleanupTask)
        {
            await cleanupTask.ConfigureAwait(true);

            return;
        }

        using CleanupProgressForm dialog = new(message);
        dialog.Show(owner);

        try
        {
            await cleanupTask.ConfigureAwait(true);
        }
        finally
        {
            dialog.Close();
        }
    }
}
