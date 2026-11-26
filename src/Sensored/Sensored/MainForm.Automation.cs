namespace Sensored;

public partial class MainForm
{
    private AppAutomation? _automation;

    internal void ConfigureAutomation(string directory)
    {
        if (_automation is not null) { throw new InvalidOperationException("Automation is already configured."); }
        _automation = new(directory, ExecuteAutomationCommandAsync, GetAutomationState,
            () => _roadQuizDialog is { IsDisposed: false } quiz ? [this, quiz] : [this],
            (message, exception) => _log.Error(message, exception));
        _options = _options with { AutoRecording = false };
        Text += " - AUTOMATION (isolated quiz labels)";
        Disposed += (_, _) => _automation.Dispose();
    }

    private async Task StartAutomationAsync()
    {
        if (_automation is not { } automation) { return; }
        try
        {
            await ExecuteAutomationCommandAsync(new(Guid.NewGuid(), "start-session")).ConfigureAwait(true);
            await ExecuteAutomationCommandAsync(new(Guid.NewGuid(), "start-quiz")).ConfigureAwait(true);
            automation.Start();
        }
        catch (Exception exception)
        {
            automation.ReportFailure("Automation startup failed", exception);
        }
    }

    internal async Task ExecuteAutomationCommandAsync(AutomationCommand command)
    {
        if (_automation is null) { throw new InvalidOperationException("Launch with --automation to enable commands."); }
        if (_shutdownStarted || _transitioning) { throw new InvalidOperationException("The app is transitioning or closing."); }
        switch (command.Action)
        {
            case "start-session":
                await StartSensorSessionAsync(allowReceiverWrites: false).ConfigureAwait(true);
                break;
            case "start-quiz":
                if (!CanRunRoadLearning) { throw new InvalidOperationException("A healthy live sensor session is required."); }
                StartRoadLearning(confirmPassenger: false);
                break;
            case "answer":
                if (_roadQuizDialog is not { IsDisposed: false } quiz
                    || command.QuestionId is not { } questionId || command.Level is not { } level)
                {
                    throw new InvalidOperationException("Answer requires an open quiz, QuestionId and Level.");
                }

                await quiz.AnswerAsync(questionId, level).ConfigureAwait(true);
                break;
            case "end-quiz":
                if (_roadQuizDialog is not { IsDisposed: false } activeQuiz)
                {
                    throw new InvalidOperationException("No quiz is open.");
                }

                await activeQuiz.FinishAsync().ConfigureAwait(true);
                break;
            case "stop-session":
                await StopSensorSessionAsync().ConfigureAwait(true);
                break;
            case "close":
                Close();
                break;
            default:
                throw new ArgumentException($"Unknown automation action: {command.Action}");
        }
    }

    private object GetAutomationState() => new
    {
        SessionActive = _lease is not null,
        FusionFault = _fusionSession?.Fault?.Message,
        ShutdownStarted = _shutdownStarted,
        Status = _statusDetail,
        Gps = _sensorSourceFactory?.GpsDiagnostic,
        Quiz = _roadQuizDialog is { IsDisposed: false } quiz ? quiz.GetAutomationState() : null
    };
}
