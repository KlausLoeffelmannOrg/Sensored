using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Observes independent devices in the background and journals optional owner-applied feedback.</summary>
public partial class MainForm
{
    private FusionPlausibilityMonitor? _plausibilityMonitor;
    private FusionPlausibilityFeedback? _currentPlausibilityFeedback;
    private Exception? _observedPlausibilityFault;

    private void StartPlausibilityMonitoring()
    {
        _observedPlausibilityFault = null;
        if (!_obdReplayActive && _fusionEngine is IFusionPlausibilityController)
        {
            _plausibilityMonitor = new([new LongitudinalPlausibilityService()]);
            _log.Information($"Background plausibility: maximum 0.25 Hz; conditional moving renewal: "
                + LongitudinalPlausibilityService.Policy
                + ". Device inclinometers remain comparison-only; measured mounting is required.");
        }
    }

    private void ObservePlausibility(FusionObservation observation)
    {
        if (_plausibilityMonitor is not { Fault: null } monitor
            || _fusionEngine is not IFusionPlausibilityController controller
            || observation.Input.Kind != SensorDataKind.Imu || observation.Estimate is not { } estimate)
        {
            return;
        }

        monitor.Observe(() =>
        {
            BluetoothSensorSnapshot? bluetooth = _bluetoothSensorSource?.Latest;
            PlausibilitySensorReading? comparison = bluetooth?.IsFreshAt(observation.Input.ArrivalTimestamp) == true
                && bluetooth.Reading is { } reading
                    ? new(reading.Timestamp, new(reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ),
                        new(reading.AngularRateX, reading.AngularRateY, reading.AngularRateZ),
                        reading.Roll, reading.Pitch, reading.Yaw)
                    : null;

            return controller.CapturePlausibility(observation.Input, estimate, comparison) with
            {
                RecordingPath = _recording?.FolderPath,
                Epoch = observation.Epoch
            };
        });
    }

    private void ConsumePlausibilityResults(SensorEnvelope input, IFusionEngine engine)
    {
        _currentPlausibilityFeedback = null;
        if (_obdReplayActive || input.Kind != SensorDataKind.Imu
            || _plausibilityMonitor is not { Fault: null } monitor || monitor.TakeResults() is not { } results)
        {
            return;
        }

        _learningAssessments = results;
        foreach (FusionPlausibilityAssessment result in results)
        {
            _log.Information($"{result.Timestamp:O}: {result.Reason}; odometer a={result.OdometerAcceleration:0.000}, "
                + $"fusion a={result.FusionAcceleration:0.000}, residual={result.Residual:0.000} m/s2; "
                + $"internal device pitch={result.InternalPitch:0.00}, Bluetooth device pitch={result.ComparisonPitch:0.00} deg.");
            if (result.Feedback is { } feedback)
            {
                if (_currentPlausibilityFeedback is not null)
                {
                    throw new InvalidOperationException("Multiple plausibility services proposed incompatible gravity corrections.");
                }

                if (engine is not IFusionPlausibilityController controller)
                {
                    throw new InvalidOperationException("The fusion engine cannot consume plausibility feedback.");
                }

                controller.PostPlausibility(feedback);
                _currentPlausibilityFeedback = feedback;
            }
        }
    }

    private void PresentPlausibilityFault()
    {
        if (_plausibilityMonitor?.Fault is { } fault && !ReferenceEquals(fault, _observedPlausibilityFault))
        {
            _observedPlausibilityFault = fault;
            ShowError("Background plausibility stopped; no further moving corrections will be applied", fault);
        }
    }

    private async Task StopPlausibilityMonitoringAsync()
    {
        if (_plausibilityMonitor is { } monitor)
        {
            _plausibilityMonitor = null;
            await monitor.DisposeAsync().ConfigureAwait(true);
        }
    }
}
