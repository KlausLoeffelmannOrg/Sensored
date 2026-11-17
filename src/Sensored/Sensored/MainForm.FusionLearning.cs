using System.Text.Json;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;
using WarpToolkit.Windows.Sensors;

namespace Sensored;

/// <summary>Displays support progress and retains retrospective feedback without training on unverified labels.</summary>
public partial class MainForm
{
    private readonly SymbolToolStripButton _fusionFeedbackButton = new()
    {
        Name = "_fusionFeedbackButton",
        Text = "Fusion feedback"
    };

    private FusionLearningStore? _fusionLearning;
    private FusionPlausibilityAssessment[]? _learningAssessments;
    private Task? _learningSaveTask;
    private DateTimeOffset _lastLearningSave;
    private bool _learningOpenFailed;
    private bool _feedbackDialogOpen;
    private string? _learningSaveError;
    private string? _learningGpsSelectionKey;

    private void ConfigureFusionLearningCommand(int symbolSize)
        => _fusionFeedbackButton.ConfigureItem(_toolStrip, FluentSymbols.AllSymbols.Ruler,
            (OnFusionFeedbackCommand, true),
            "Review saved fusion episodes while parked; feedback survives frame resets and restarts", symbolSize);

    private void BeginFusionLearning()
        => BeginFusionLearningProfile(new WindowsSensorSourceFactory(_options.CreateWindowsSourceOptions())
            .CreateGpsSelection()?.Key);

    private void BeginFusionLearningProfile(string? gpsSelectionKey)
    {
        if (_learningOpenFailed)
        {
            return;
        }

        try
        {
            OpenFusionLearning();
            if (_fusionLearning is null)
            {
                return;
            }

            _fusionLearning.BeginSession(_activeVehicleMount, JsonSerializer.Serialize(new
            {
                GpsSelection = gpsSelectionKey,
                Engine = (_fusionEngine as IFusionConfigurationSource)?.Configuration
            }));
            _learningGpsSelectionKey = gpsSelectionKey;
            _learningAssessments = null;
            _lastLearningSave = DateTimeOffset.MinValue;
            _log.Information("Fusion evidence retained across sessions; no historical gravity or unverified road label is applied as calibration.");
        }
        catch (Exception exception)
        {
            _learningOpenFailed = true;
            ShowError("Fusion evidence could not be opened; existing evidence was not replaced", exception);
        }
    }

    private void OpenFusionLearning()
    {
        if (_learningOpenFailed || _fusionLearning is not null)
        {
            return;
        }

        try
        {
            _fusionLearning = FusionLearningStore.Open(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WARP", "Sensored", "FusionLearning", "evidence-v1.json"));
            if (_fusionLearning.HasLegacySequenceRanges)
            {
                _log.Warning("Retained legacy fusion episodes have incomparable OBD/IMU sequence endpoints. "
                    + "Use their timestamps and source identity, not an ordered input range.");
            }
        }
        catch (Exception exception)
        {
            _learningOpenFailed = true;
            ShowError("Fusion evidence could not be opened; existing evidence was not replaced", exception);
        }
    }

    private void ObserveFusionLearning(FusionObservation observation)
    {
        if (_obdReplayActive || _fusionLearning is not { } learning)
        {
            return;
        }

        if (observation.Input.SourceTransition is { SourceId: "GPS", State: "probing" or "disabled" } transition)
        {
            string? selectionKey = transition.State == "probing" ? transition.Reason : null;
            if (selectionKey != _learningGpsSelectionKey)
            {
                BeginFusionLearningProfile(selectionKey);
            }
        }

        string? recordingPath = _recording?.FolderPath;
        learning.Observe(observation, recordingPath);
        if (_learningAssessments is { } assessments && observation.Input.Kind == SensorDataKind.Imu)
        {
            _learningAssessments = null;
            foreach (FusionPlausibilityAssessment assessment in assessments)
            {
                learning.ObserveAssessment(observation, assessment, recordingPath);
            }
        }
    }

    private string PresentFusionLearning(bool currentBodySupported)
    {
        if (_learningSaveTask is { IsCompleted: true } saved)
        {
            _learningSaveTask = null;
            if (saved.Exception is { } fault)
            {
                _learningSaveError = fault.GetBaseException().Message;
                ShowError("Fusion evidence checkpoint failed; evidence remains pending in memory", fault.GetBaseException());
            }
            else
            {
                _learningSaveError = null;
            }
        }

        int pending = _fusionLearning?.PendingEpisodes.Length ?? 0;
        _fusionFeedbackButton.Enabled = _fusionLearning is not null
            && !_transitioning && !_shutdownStarted && !_feedbackDialogOpen
            && _replayPlayer is null && CanReviewFusionFeedback();
        _fusionFeedbackButton.Text = $"Fusion feedback ({pending})";
        if (!_shutdownStarted && !_obdReplayActive && _fusionLearning is { } learning
            && _learningSaveTask is null && DateTimeOffset.UtcNow - _lastLearningSave >= TimeSpan.FromSeconds(30))
        {
            _lastLearningSave = DateTimeOffset.UtcNow;
            _learningSaveTask = learning.SaveAsync();
        }

        FusionEstimate? estimate = _fusionSession?.Latest ?? _lastFusionEstimate;
        string budget = !_obdReplayActive && currentBodySupported
            && estimate?.PatternDiagnostics?.TiltErrorBoundDegrees is { } bound
                ? $"Gravity budget: {Math.Max(0, 5 - bound):0.0} of 5 degrees remaining (not accuracy)."
                : _obdReplayActive ? "Replay uses recorded support; no learning evidence is added."
                : "Gravity budget unavailable; no historical gravity is reused.";
        IReadOnlyList<FusionPlausibilityAssessment>? assessments = _plausibilityMonitor?.Latest;
        string assessment = (assessments is { Count: > 0 } ? assessments[0].Reason : null)
            ?? (_obdReplayActive ? "Replay does not add learning evidence."
                : "Moving renewal needs matched OBD/IMU evidence; labels alone cannot renew it.");
        string progress = _fusionLearning?.Progress
            ?? (_learningOpenFailed ? "Persistent evidence unavailable; see the reported error."
                : "Evidence collection begins with the next live session.");

        return $"{budget}\r\n{assessment}\r\n{progress}"
            + (_learningSaveError is { } error ? $"\r\nNOT SAVED: {error}" : "");
    }

    private bool CanReviewFusionFeedback()
    {
        if (_replayPlayer is not null || _obdReplayActive || _transitioning || _shutdownStarted)
        {
            return false;
        }

        if (_lease is null)
        {
            return true;
        }

        FusionEstimate? estimate = _fusionSession?.Latest;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        return !_paused && estimate is { RejectedSample: false, PatternDiagnostics: { } pattern }
            && now >= estimate.Timestamp && now - estimate.Timestamp <= TimeSpan.FromSeconds(2)
            && (pattern.QuietReason.StartsWith("conditional-static-body-tilt;", StringComparison.Ordinal)
                || pattern.QuietReason.StartsWith("stopped-gravity;", StringComparison.Ordinal));
    }

    private async void OnFusionFeedbackCommand(object? sender, EventArgs e)
    {
        if (_feedbackDialogOpen || !CanReviewFusionFeedback() || _fusionLearning is not { } learning)
        {
            return;
        }

        _feedbackDialogOpen = true;
        try
        {
            using FusionFeedbackDialog dialog = new(learning.PendingEpisodes, learning.Progress,
                CanReviewFusionFeedback, Font);
            if (await dialog.ShowDialogAsync(this).ConfigureAwait(true) != DialogResult.OK
                || dialog.SelectedEpisode is not { } episode)
            {
                return;
            }

            if (!CanReviewFusionFeedback())
            {
                throw new InvalidOperationException("Feedback deferred: stop safely before reviewing an episode.");
            }

            learning.Answer(episode.Id, dialog.CreateFeedback());
            await learning.SaveAsync().ConfigureAwait(true);
            _learningSaveError = null;
            SetStatusAndLog($"Saved user-declared fusion feedback for {episode.End:O}; calibration and current frame are unchanged.");
        }
        catch (Exception exception)
        {
            _learningSaveError = exception.Message;
            ShowError("Fusion feedback was not saved; pending evidence is retained", exception);
        }
        finally
        {
            _feedbackDialogOpen = false;
            UpdateTabletFrameStatus();
        }
    }

    private async Task SaveFusionLearningAsync()
    {
        if (_fusionLearning is not { } learning)
        {
            return;
        }

        try
        {
            if (_learningSaveTask is { } pending)
            {
                _learningSaveTask = null;
                try
                {
                    await pending.ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    _log.Warning($"Earlier fusion evidence checkpoint failed; retrying at session end: {exception}");
                }
            }

            await learning.SaveAsync().ConfigureAwait(true);
            _learningSaveError = null;
        }
        catch (Exception exception)
        {
            _learningSaveError = exception.Message;
            ShowError("Fusion evidence was not saved at session end; evidence remains in memory", exception);
        }
    }
}
