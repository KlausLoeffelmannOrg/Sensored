using System.Text.Json;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Sensors;

namespace Sensored;

/// <summary>Retains legacy evidence independently of the opt-in road quiz, without reusing labels as calibration.</summary>
public partial class MainForm
{
    private FusionLearningStore? _fusionLearning;
    private FusionPlausibilityAssessment[]? _learningAssessments;
    private Task? _learningSaveTask;
    private DateTimeOffset _lastLearningSave;
    private bool _learningOpenFailed;
    private string? _learningGpsSelectionKey;

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
            _fusionLearning = FusionLearningStore.Open(_automation is { } automation
                ? Path.Combine(automation.DirectoryPath, "fusion-evidence.json")
                : Path.Combine(
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

    private void CheckpointFusionLearning()
    {
        if (_learningSaveTask is { IsCompleted: true } saved)
        {
            _learningSaveTask = null;
            if (saved.Exception is { } fault)
            {
                ShowError("Fusion evidence checkpoint failed; evidence remains pending in memory", fault.GetBaseException());
            }
        }

        if (!_shutdownStarted && !_obdReplayActive && _fusionLearning is { } learning
            && _learningSaveTask is null && DateTimeOffset.UtcNow - _lastLearningSave >= TimeSpan.FromSeconds(30))
        {
            _lastLearningSave = DateTimeOffset.UtcNow;
            _learningSaveTask = learning.SaveAsync();
        }
    }

    private async Task SaveFusionLearningAsync()
    {
        if (_roadQuizDialog is { IsDisposed: false } quiz)
        {
            await quiz.FinishAsync().ConfigureAwait(true);
        }

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
        }
        catch (Exception exception)
        {
            ShowError("Fusion evidence was not saved at session end; evidence remains in memory", exception);
        }
    }
}
