using System.Diagnostics;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <summary>
///  Actions available when graceful sensor cleanup cannot finish promptly.
/// </summary>
internal enum CleanupDecision
{
    Retry,
    ReturnToApp,
    ForceExit
}

/// <summary>
///  Tracks explicitly completed cleanup steps independently of the progress window.
/// </summary>
internal sealed class CleanupStageProgress
{
    private readonly Lock _sync = new();
    private readonly string _initialStage;
    private string _stage;
    private int _completedStages;

    internal CleanupStageProgress(int totalStages, string initialStage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalStages);
        ArgumentException.ThrowIfNullOrWhiteSpace(initialStage);

        TotalStages = totalStages;
        _initialStage = initialStage;
        _stage = initialStage;
    }

    internal int TotalStages { get; }

    internal (string Stage, int CompletedStages) Snapshot()
    {
        lock (_sync)
        {
            return (_stage, _completedStages);
        }
    }

    internal void SetStage(string stage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);

        lock (_sync)
        {
            _stage = stage;
        }
    }

    internal void CompleteStage()
    {
        lock (_sync)
        {
            if (_completedStages >= TotalStages)
            {
                throw new InvalidOperationException("All cleanup stages are already complete.");
            }

            _completedStages++;
        }
    }

    internal void Reset()
    {
        lock (_sync)
        {
            _stage = _initialStage;
            _completedStages = 0;
        }
    }
}

/// <summary>
///  Shows delayed progress while a sensor window completes asynchronous cleanup.
/// </summary>
internal sealed partial class CleanupProgressForm : Form
{
    private const int DisplayDelayMilliseconds = 250;
    private const int MinimumMessageWrapWidth = 400;
    private const int MessageWrapFontHeights = 14;
    private const int WorkingAreaHorizontalPadding = 48;
    private const int MaximumFailureSummaryLength = 120;
    private static readonly TimeSpan RecoveryPromptDelay = TimeSpan.FromSeconds(10);
    private TaskCompletionSource<CleanupDecision> _decision =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool _centeringOverOwner;
    private bool _buttonsInitialized;

    /// <summary>
    ///  Initializes a new cleanup progress window.
    /// </summary>
    /// <param name="message">The cleanup operation shown to the user.</param>
    /// <param name="dialogFont">The owner's font, applied before sizing the content.</param>
    internal CleanupProgressForm(string message, Font? dialogFont = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        InitializeComponent();
        SuspendLayout();
        Font = dialogFont ?? SystemFonts.MessageBoxFont!;
        _messageLabel.Text = message;
        _buttonsInitialized = true;
        UpdateWrapWidth();
        ResumeLayout(performLayout: true);
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_buttonsInitialized)
        {
            UpdateWrapWidth();
            CenterOverOwner();
        }
    }

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        UpdateWrapWidth();
        CenterOverOwner();
    }

    /// <inheritdoc/>
    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);

        if (Visible)
        {
            CenterOverOwner();
        }
    }

    private void CenterOverOwner()
    {
        if (_centeringOverOwner || Owner is not { } owner)
        {
            return;
        }

        _centeringOverOwner = true;

        try
        {
            Rectangle workingArea = Screen.FromControl(owner).WorkingArea;
            int x = Math.Clamp(
                owner.Left + (owner.Width - Width) / 2,
                workingArea.Left,
                Math.Max(workingArea.Left, workingArea.Right - Width));
            int y = Math.Clamp(
                owner.Top + (owner.Height - Height) / 2,
                workingArea.Top,
                Math.Max(workingArea.Top, workingArea.Bottom - Height));
            Location = new Point(x, y);
        }
        finally
        {
            _centeringOverOwner = false;
        }
    }

    private void UpdateWrapWidth()
    {
        Rectangle workingArea = Screen.FromControl(Owner ?? this).WorkingArea;
        int wrapWidth = Math.Min(
            Math.Max(1, workingArea.Width - WorkingAreaHorizontalPadding),
            Math.Max(MinimumMessageWrapWidth, (int)Math.Ceiling(Font.GetHeight() * MessageWrapFontHeights)));
        Size wrap = new(wrapWidth, 0);
        if (_messageLabel.MaximumSize != wrap || _stageLabel.MaximumSize != wrap)
        {
            SuspendLayout();
            _messageLabel.MaximumSize = wrap;
            _stageLabel.MaximumSize = wrap;
            ResumeLayout(performLayout: true);
        }
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

        Stopwatch elapsed = Stopwatch.StartNew();
        Task cleanupTask = cleanup();
        Task delayTask = Task.Delay(DisplayDelayMilliseconds);

        if (await Task.WhenAny(cleanupTask, delayTask).ConfigureAwait(true) == cleanupTask)
        {
            await cleanupTask.ConfigureAwait(true);

            return;
        }

        using CleanupProgressForm dialog = new(message, owner.Font);
        dialog.Show(owner);
        using System.Windows.Forms.Timer refresh = new() { Interval = 500 };
        refresh.Tick += (_, _) => dialog.UpdateProgress(message, 0, elapsed.Elapsed);
        refresh.Start();

        try
        {
            await cleanupTask.ConfigureAwait(true);
            dialog.UpdateProgress(message, completedStages: 1, elapsed.Elapsed);
        }
        finally
        {
            refresh.Stop();
            dialog.Close();
        }
    }

    /// <summary>
    ///  Keeps cleanup visible and recoverable without claiming a timed-out operation succeeded.
    /// </summary>
    /// <param name="owner">The sensor dashboard.</param>
    /// <param name="currentStage">The current coordinate-free shutdown stage.</param>
    /// <param name="cleanup">The cleanup operation; retried only after a previous attempt has failed.</param>
    /// <returns>True when cleanup completed; false when Force Exit was explicitly confirmed.</returns>
    internal static async Task<bool> RunWithRecoveryAsync(
        Form owner,
        Func<string> currentStage,
        Func<Task> cleanup)
    {
        ArgumentNullException.ThrowIfNull(currentStage);

        return await RunWithRecoveryCoreAsync(
            owner,
            () => (currentStage(), 0),
            totalStages: 1,
            cleanup,
            resetStages: null,
            canRetry: null,
            onFailure: null).ConfigureAwait(true);
    }

    /// <summary>
    ///  Displays only stages explicitly reported complete by the cleanup operation.
    /// </summary>
    /// <param name="owner">The sensor dashboard.</param>
    /// <param name="stages">The stage tracker updated by the shutdown operation.</param>
    /// <param name="cleanup">The cleanup operation, retried only after failure.</param>
    /// <param name="canRetry">Checks whether retry is safe after a failure.</param>
    /// <param name="onFailure">Reports a cleanup failure to the dashboard.</param>
    /// <returns>False only after a confirmed Force Exit.</returns>
    internal static Task<bool> RunWithRecoveryAsync(
        Form owner,
        CleanupStageProgress stages,
        Func<Task> cleanup,
        Func<bool>? canRetry = null,
        Action<Exception>? onFailure = null)
    {
        ArgumentNullException.ThrowIfNull(stages);

        return RunWithRecoveryCoreAsync(
            owner,
            stages.Snapshot,
            stages.TotalStages,
            cleanup,
            stages.Reset,
            canRetry,
            onFailure);
    }

    private static async Task<bool> RunWithRecoveryCoreAsync(
        Form owner,
        Func<(string Stage, int CompletedStages)> currentProgress,
        int totalStages,
        Func<Task> cleanup,
        Action? resetStages,
        Func<bool>? canRetry,
        Action<Exception>? onFailure)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(currentProgress);
        ArgumentNullException.ThrowIfNull(cleanup);

        Stopwatch waiting = Stopwatch.StartNew();
        Task operation = cleanup();
        using CancellationTokenSource displayDelayCancellation = new();
        Task displayDelay = Task.Delay(
            DisplayDelayMilliseconds, displayDelayCancellation.Token);

        if (await Task.WhenAny(
            operation,
            displayDelay).ConfigureAwait(true) == operation
            && operation.IsCompletedSuccessfully)
        {
            await displayDelayCancellation.CancelAsync().ConfigureAwait(true);

            return true;
        }

        await displayDelayCancellation.CancelAsync().ConfigureAwait(true);
        using CleanupProgressForm dialog = new("Finishing sensor cleanup. Please wait.", owner.Font);
        dialog._progressBar.Maximum = totalStages;
        var (initialStage, initialCompletedStages) = currentProgress();
        dialog.UpdateProgress(initialStage, initialCompletedStages, waiting.Elapsed);
        dialog.Show(owner);
        Stopwatch recoveryPrompt = Stopwatch.StartNew();

        try
        {
            while (true)
            {
                if (operation.IsCompleted)
                {
                    var (lastStage, lastCompletedStages) = currentProgress();
                    dialog.UpdateProgress(lastStage, lastCompletedStages, waiting.Elapsed);

                    try
                    {
                        await operation.ConfigureAwait(true);

                        return true;
                    }
                    catch (Exception exception)
                    {
                        onFailure?.Invoke(exception);
                        dialog.ShowFailure(exception, canRetry?.Invoke() ?? true);
                        CleanupDecision failedAction = await dialog.NextDecisionAsync().ConfigureAwait(true);
                        dialog.ResetDecision();

                        switch (failedAction)
                        {
                            case CleanupDecision.Retry when canRetry?.Invoke() ?? true:
                                resetStages?.Invoke();
                                operation = cleanup();
                                waiting.Restart();
                                recoveryPrompt.Restart();
                                dialog.HideActions();
                                continue;
                            case CleanupDecision.ForceExit when ConfirmForceExit(dialog, pending: false):
                                CrashGuard.Current?.RecordCleanupDecision("force-exit-after-failure");
                                return false;
                            case CleanupDecision.ReturnToApp:
                                CrashGuard.Current?.RecordCleanupDecision("return-to-app");
                                throw;
                            default:
                                dialog.ShowFailure(exception, canRetry?.Invoke() ?? true);
                                continue;
                        }
                    }
                }

                var (stage, completedStages) = currentProgress();
                dialog.UpdateProgress(stage, completedStages, waiting.Elapsed);

                if (recoveryPrompt.Elapsed >= RecoveryPromptDelay)
                {
                    dialog.ShowActions(running: true);
                }

                Task<CleanupDecision> decisionTask = dialog.NextDecisionAsync();
                using CancellationTokenSource pollCancellation = new();
                Task completed = await Task.WhenAny(
                    operation,
                    decisionTask,
                    Task.Delay(500, pollCancellation.Token)).ConfigureAwait(true);
                await pollCancellation.CancelAsync().ConfigureAwait(true);

                if (ReferenceEquals(completed, decisionTask))
                {
                    CleanupDecision decision = await decisionTask.ConfigureAwait(true);
                    dialog.ResetDecision();

                    if (decision == CleanupDecision.ForceExit)
                    {
                        if (operation.IsCompletedSuccessfully)
                        {
                            CrashGuard.Current?.RecordCleanupDecision("completed-before-force-confirmation");
                            return true;
                        }

                        if (ConfirmForceExit(dialog, pending: !operation.IsCompleted))
                        {
                            if (operation.IsCompletedSuccessfully)
                            {
                                CrashGuard.Current?.RecordCleanupDecision("completed-during-force-confirmation");
                                return true;
                            }

                            if (operation.Exception is { } failure)
                            {
                                onFailure?.Invoke(failure);
                            }

                            CrashGuard.Current?.RecordCleanupDecision(
                                operation.IsCompleted
                                    ? "force-exit-after-failure"
                                    : "force-exit-pending");
                            return false;
                        }
                    }

                    recoveryPrompt.Restart();
                    dialog.HideActions();
                }
            }
        }
        finally
        {
            dialog.Close();
        }
    }

    private static bool ConfirmForceExit(Form owner, bool pending)
        => MessageBox.Show(
            owner,
            pending
                ? "Sensor cleanup is still running. Force Exit may leave the current recording "
                    + "unfinished and prevent restoration of temporary GPS receiver settings. Exit anyway?"
                : "Sensor cleanup ended with a fault. The recording or receiver restoration may be "
                    + "incomplete. Exit anyway?",
            pending ? "Force Exit during cleanup" : "Exit after incomplete cleanup",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;

    private Task<CleanupDecision> NextDecisionAsync() => _decision.Task;

    private void ResetDecision()
        => _decision = new TaskCompletionSource<CleanupDecision>(
            TaskCreationOptions.RunContinuationsAsynchronously);

    private void ShowFailure(Exception exception, bool canRetry)
    {
        string reason = exception is AggregateException aggregate
            ? string.Join("; ", aggregate.Flatten().InnerExceptions.Select(inner => inner.Message))
            : exception.GetBaseException().Message;
        _messageLabel.Text = reason.Length <= MaximumFailureSummaryLength
            ? $"Cleanup failed: {reason}"
            : $"Cleanup failed: {reason[..MaximumFailureSummaryLength]}… (see diagnostics for full error).";
        _stageLabel.Text = canRetry
            ? "Cleanup incomplete — retry or return to the app."
            : "Cleanup incomplete — this stop cannot be retried safely.";
        ShowActions(running: false, canRetry);
    }

    private void UpdateProgress(string stage, int completedStages, TimeSpan elapsed)
    {
        if (!string.Equals(_stageLabel.Text, stage, StringComparison.Ordinal))
        {
            _stageLabel.Text = stage;
        }

        string elapsedText = $"Elapsed: {elapsed:hh\\:mm\\:ss}";

        if (!string.Equals(_elapsedLabel.Text, elapsedText, StringComparison.Ordinal))
        {
            _elapsedLabel.Text = elapsedText;
        }

        int progress = Math.Clamp(completedStages, _progressBar.Minimum, _progressBar.Maximum);

        if (_progressBar.Value != progress)
        {
            _progressBar.Value = progress;
        }
    }

    private void ShowActions(bool running, bool canRetry = true)
    {
        _actionLayout.SuspendLayout();
        _retryButton.Text = DialogButtonText.WrapCaption(running ? "Keep waiting" : "Retry cleanup");
        _retryButton.Visible = running || canRetry;
        _returnButton.Visible = !running;
        _forceExitButton.Text = DialogButtonText.WrapCaption(running ? "Force Exit..." : "Exit anyway...");
        Button[] actions = running
            ? [_retryButton, _forceExitButton]
            : canRetry
                ? [_retryButton, _returnButton, _forceExitButton]
                : [_returnButton, _forceExitButton];

        if (_actionLayout.ColumnCount != actions.Length)
        {
            _actionLayout.ColumnCount = actions.Length;
            _actionLayout.ColumnStyles.Clear();

            for (int index = 0; index < actions.Length; index++)
            {
                _actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / actions.Length));
            }
        }

        for (int index = 0; index < actions.Length; index++)
        {
            if (_actionLayout.GetColumn(actions[index]) != index)
            {
                _actionLayout.SetColumn(actions[index], index);
            }
        }

        _actionLayout.Visible = true;
        _actionLayout.ResumeLayout(performLayout: true);
        CenterOverOwner();
    }

    private void HideActions()
    {
        _messageLabel.Text = "Finishing sensor cleanup. Please wait.";
        _actionLayout.Visible = false;
    }

    private void OnRetryClick(object? sender, EventArgs e)
        => _decision.TrySetResult(CleanupDecision.Retry);

    private void OnReturnClick(object? sender, EventArgs e)
        => _decision.TrySetResult(CleanupDecision.ReturnToApp);

    private void OnForceExitClick(object? sender, EventArgs e)
        => _decision.TrySetResult(CleanupDecision.ForceExit);
}
