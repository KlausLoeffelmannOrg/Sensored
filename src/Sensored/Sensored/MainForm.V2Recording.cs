using System.Diagnostics;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Owns the V2 pair without coupling recording cadence to dashboard updates.</summary>
public partial class MainForm
{
    private SensorV2Recording? _v2Recording;
    private readonly Queue<FusionObservation> _recentV2Inputs = [];
    private int _recordingMarkerNumber;
    private Exception? _observedV2Fault;

    private bool PresentV2RecordingFault()
    {
        if (_v2Recording?.Fault is not { } fault)
        {
            return false;
        }
        if (!ReferenceEquals(_observedV2Fault, fault))
        {
            _observedV2Fault = fault;
            ShowError("V2 CSV pair failed; independent journal continues", fault);
        }
        SetStatus($"V2 pair incomplete: {fault.Message}; full-rate journal remains independent.");
        return true;
    }

    private void ObserveV2Recording(FusionObservation observation)
    {
        lock (_obdRecordingGate)
        {
            _recentV2Inputs.Enqueue(observation);
            while (_recentV2Inputs.Count > 8192)
            {
                _recentV2Inputs.Dequeue();
            }
            _v2Recording?.Publish(observation);
        }
    }

    private async Task CompleteV2RecordingAsync(ISensorRecordingSession recording, bool journalVerified)
    {
        SensorV2Recording? pair = _v2Recording;
        if (pair is null)
        {
            return;
        }
        long started = Stopwatch.GetTimestamp();
        while (recording.Coverage is { } coverage && pair.LastInputSequence < coverage.LastSequence
            && pair.Fault is null && _fusionSession?.Fault is null
            && Stopwatch.GetElapsedTime(started) < RecordingStopTimeout)
        {
            await Task.Delay(10).ConfigureAwait(true);
        }
        lock (_obdRecordingGate)
        {
            _v2Recording = null;
            _recordedDrivingTask = null;
        }
        await pair.StopAsync(journalVerified && _fusionSession?.Fault is null
            && _sensorService.Statistics.IngressDropped == 0
            && (_fusionSubscription?.Statistics.Dropped ?? 0) == 0).ConfigureAwait(true);
    }

    private bool HandleRecordingMarkerShortcut(Keys keyData)
    {
        if (keyData != (Keys.Control | Keys.M) || _v2Recording is null)
        {
            return false;
        }
        try
        {
            lock (_obdRecordingGate)
            {
                if (_v2Recording is { } pair)
                {
                    pair.AddMarker(new($"{_options.RecordingMarkerLabel} {++_recordingMarkerNumber}",
                        DateTimeOffset.UtcNow, pair.LastInputSequence, _fusionSession?.Epoch ?? 0));
                }
            }
            SetStatusAndLog("Recording marker added.");
        }
        catch (Exception exception)
        {
            ShowError("Recording marker failed", exception);
        }
        return true;
    }
}
