using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Attaches optional user-declared task intervals to the existing recording markers.</summary>
public partial class MainForm
{
    private DrivingTaskSelection? _selectedDrivingTask;
    private DrivingTaskSelection? _recordedDrivingTask;

    private void ApplyDrivingTaskSelection(DrivingTaskSelection? selection)
    {
        if (selection == _selectedDrivingTask)
        {
            return;
        }

        lock (_obdRecordingGate)
        {
            if (_v2Recording is { } pair && _recording is { Status.IsActive: true }
                && _recordingStopTask is null && _replayPlayer is null)
            {
                if (_recordedDrivingTask is { } previous)
                {
                    AddDrivingTaskMarker(pair, previous, "intent-end (selection changed)");
                }

                _recordedDrivingTask = null;
                if (selection is not null)
                {
                    AddDrivingTaskMarker(pair, selection, "intent-start (selection changed)");
                    _recordedDrivingTask = selection;
                }
            }

            _selectedDrivingTask = selection;
        }

        SetStatusAndLog(selection is null ? "Driving task off; recording is unchanged."
            : $"Driving task selected: {selection.Task.Title}. Expected conditions only; recording is unchanged.");
    }

    private void BeginDrivingTaskRecording()
    {
        lock (_obdRecordingGate)
        {
            _recordedDrivingTask = null;
            if (_selectedDrivingTask is { } selection && _v2Recording is { } pair)
            {
                AddDrivingTaskMarker(pair, selection, "intent-start (recording started)");
                _recordedDrivingTask = selection;
            }
        }
    }

    private void EndDrivingTaskRecording()
    {
        lock (_obdRecordingGate)
        {
            if (_recordedDrivingTask is { } selection && _v2Recording is { } pair)
            {
                AddDrivingTaskMarker(pair, selection, "intent-end (recording stopping; not task completion)");
            }

            _recordedDrivingTask = null;
        }
    }

    private void AddDrivingTaskMarker(SensorV2Recording pair, DrivingTaskSelection selection, string boundary)
        => pair.AddMarker(new(selection.MarkerLabel(boundary),
            DateTimeOffset.UtcNow, pair.LastInputSequence, _fusionSession?.Epoch ?? 0));
}
