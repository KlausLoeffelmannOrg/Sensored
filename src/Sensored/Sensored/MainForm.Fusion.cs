using System.Runtime.ExceptionServices;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <summary>
///  Connects full-rate vehicle fusion to the existing dashboard without coupling it to rendering.
/// </summary>
public partial class MainForm
{
    private SensorFusionSession? _fusionSession;
    private IFusionEngine? _fusionEngine;
    private ISensorSubscription? _fusionSubscription;
    private ISensorServiceSessionLease? _fusionStopLease;
    private Task? _fusionStopTask;
    private Exception? _terminalSourceStopFailure;
    private FusionEstimate? _lastFusionEstimate;
    private Exception? _observedFusionFault;
    private long _displayedFusionSequence = -1;
    private SensorGaugeDashboard? _gaugeDashboard;

    private SensorGaugeDashboard GaugeDashboard
        => _gaugeDashboard ??= new(_speedometer, _artificialHorizon, _accelerationGauge, _compassGauge);

    /// <summary>Specifies estimator continuity independently of visual preservation.</summary>
    private enum FusionContinuity
    {
        /// <summary>Starts an unrelated drive, device or source with no retained calibration.</summary>
        Reset,

        /// <summary>Continues one timeline across contiguous parts, preserving every estimate.</summary>
        Continue,

        /// <summary>
        ///  Restarts navigation after seeking inside the same recording, retaining the
        ///  installation calibration that seeking cannot invalidate.
        /// </summary>
        Reseek
    }

    private async Task StartFusionInputAsync(string? replayPath, FusionContinuity continuity)
    {
        bool wasReplay = _obdReplayActive;
        _obdReplayActive = replayPath is not null;
        if ((_obdReplayActive && continuity == FusionContinuity.Reset) || (wasReplay && !_obdReplayActive))
        {
            _horizonMode = _obdReplayActive ? HorizonSourceMode.FusionGps : PreferredLiveHorizonMode;
            ResetHorizonSourcePresentation();
        }

        if (_obdReplayActive)
        {
            await StopObdAsync().ConfigureAwait(true);
            await StopBluetoothSensorsAsync().ConfigureAwait(true);
        }
        else if (_options.UseObd && _obdSource is null)
        {
            StartObdDiscovery();
        }
        if (!_obdReplayActive)
        {
            StartBluetoothSensorDiscovery();
        }

        if (continuity != FusionContinuity.Continue && _fusionSession is not null)
        {
            await CompleteFusionAsync().ConfigureAwait(true);
        }

        if (continuity != FusionContinuity.Continue)
        {
            Volatile.Write(ref _presentedComparison, null);
            Interlocked.Exchange(ref _obdReplayTimeTicks, 0);
            _obdReplay?.Dispose();
            _obdReplay = replayPath is null ? null
                : ObdReplayCursor.Open(Path.GetDirectoryName(replayPath)!);
            _obdReplayMinimumSequence = 0;
            _obdReplayNeedsWarmup = _obdReplay is not null;
            if (replayPath is not null && _obdReplay is not null)
            {
                _obdReplayMinimumSequence = await ObdReplayCursor.FindMinimumSequenceAsync(
                    SensorRecordingReader.ReadPartAsync(replayPath), _replayStoppedPosition);
            }
        }

        if (_fusionSession is null)
        {
            IFusionEngine engine;

            if (continuity == FusionContinuity.Reseek && wasReplay && _obdReplay is null
                && _fusionEngine is { } retained && retained is not RecordedFusionEngine)
            {
                // Seeking restarts navigation on the same recording, not its installation identity.
                engine = retained;
                if (engine is IFusionNavigationReset navigation)
                {
                    navigation.ResetNavigation();
                    _log.Information("Replay seek: navigation restarted with retained mounting calibration.");
                }
                else
                {
                    throw new InvalidOperationException("The current engine cannot seek while preserving mounting calibration.");
                }
            }
            else
            {
                engine = replayPath is null
                    ? _options.CreateFusionEngine(isReplay: false)
                    : RecordedFusionEngine.Open(Path.GetDirectoryName(replayPath)!)
                        ?? _options.CreateFusionEngine(isReplay: true);
                _activeVehicleMount = replayPath is null ? _options.VehicleMount : null;
                _fusionEngine = engine;
                if (engine is RecordedFusionEngine)
                {
                    _log.Information("Replay: restoring recorded fusion configuration epochs, including the physical installation.");
                }
            }

            string? tracePath = null;

            if (replayPath is not null)
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "WARP",
                    "Sensored",
                    "FusionTraces",
                    $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
                Directory.CreateDirectory(directory);
                tracePath = Path.Combine(directory, "golden.trace.csv");
                _log.Information($"Full-rate fusion trace: {tracePath}");
            }

            _fusionSession = new SensorFusionSession(engine, tracePath);
            _fusionSession.PrepareInput = PrepareObdInput;
            _fusionSession.Processing += OnObdFusionInput;
            _fusionSession.Processed += OnVehicleFusionProcessed;
            _vehicleTrip = new VehicleTripAccumulator();
            _referenceSpeed = new SensorReferenceSpeed();
            _lastFusionEstimate = null;
            _displayedFusionSequence = -1;
            _observedFusionFault = null;
            StartPlausibilityMonitoring();
            PrepareSessionFrame(replayPath);
            if (!_obdReplayActive)
            {
                BeginFusionLearning();
                _obdRecording = new ObdRecordingCompanion();
            }
        }

        lock (_obdRecordingGate)
        {
            _recentV2Inputs.Clear();
        }
        _fusionSubscription = _sensorService.Subscribe(new SensorSubscriptionOptions
        {
            Name = "Golden-reference fusion",
            Kinds = SensorDataKindFilter.All,
            Capacity = 8192
        });
        _fusionSession.Attach(_fusionSubscription);
    }

    private Task StopFusionInputAsync()
    {
        if (_lease is not { } lease)
        {
            return _fusionStopTask is { IsCompleted: false } pending
                ? pending
                : Task.CompletedTask;
        }

        if (ReferenceEquals(_fusionStopLease, lease))
        {
            return _fusionStopTask!;
        }

        if (_fusionStopTask is { IsCompleted: false })
        {
            throw new InvalidOperationException(
                "The previous sensor lease is still stopping; a new session cannot be stopped concurrently.");
        }

        _fusionStopLease = lease;
        _fusionStopTask = StopFusionInputCoreAsync(lease);

        return _fusionStopTask;
    }

    private async Task StopFusionInputCoreAsync(ISensorServiceSessionLease lease)
    {
        bool shared = _sensorService.ActiveSessionCount > 1;
        List<Exception> failures = [];

        if (shared && _fusionSubscription is { } cutoff)
        {
            try
            {
                await cutoff.DisposeAsync().ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        try
        {
            SetShutdownStage("sources-release", "Stopping acquisition and restoring the GPS receiver.");
            await lease.DisposeAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        finally
        {
            // A completed release consumes this one-shot lease even when teardown faults.
            if (ReferenceEquals(_lease, lease))
            {
                _lease = null;
            }
        }

        try
        {
            SensorCompletedSessionDiagnostics? terminal = shared ? null : lease.CompletedSession;

            if (_fusionSession is { } session)
            {
                try
                {
                    SetShutdownStage("fusion-input", "Draining the remaining vehicle-fusion input.");
                    // Live dashboards do not publish reference traces. An acquisition fault
                    // is diagnostic data, not evidence that a successfully released lease leaked.
                    if (failures.Count == 0 && session.TracePath is null
                        && terminal?.Faults.SourceException is { } acquisitionFault
                        && !IsUnverifiedReceiverRestore(acquisitionFault))
                    {
                        ObserveServiceFaults();
                        _log.Warning($"Acquisition ended with a source fault; sensor release completed: {acquisitionFault}");
                        terminal = terminal with
                        {
                            Faults = terminal.Faults with { SourceException = null }
                        };
                    }

                    await session.FinishInputAsync(terminal).ConfigureAwait(true);
                }
                finally
                {
                    _fusionSubscription = null;
                    _lastFusionEstimate = session.Latest;
                }
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        await SaveFusionLearningAsync().ConfigureAwait(true);

        if (failures.Count > 0)
        {
            Exception failure = failures.Count == 1
                ? failures[0]
                : new AggregateException(
                    "Sensor lease release and fusion input drain did not complete cleanly.",
                    failures);
            _terminalSourceStopFailure ??= failure;
            ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private async Task CompleteFusionAsync(bool complete = false)
    {
        if (_fusionSession is not { } session)
        {
            await SaveFusionLearningAsync().ConfigureAwait(true);
            return;
        }

        _fusionSession = null;
        _lastFusionEstimate = session.Latest;
        PresentPerformanceTiming();

        try
        {
            SetShutdownStage("fusion-trace", "Flushing vehicle-fusion traces and writing the final manifest.");
            await session.CompleteAsync(complete).ConfigureAwait(true);

            if (session.TracePath is { } path)
            {
                _log.Information(complete
                    ? $"Completed fusion trace: {path}"
                    : $"Partial fusion trace retained: {path}.writing");
            }
        }
        finally
        {
            SetShutdownStage("fusion-dispose", "Releasing vehicle-fusion resources.");
            try
            {
                await session.DisposeAsync().ConfigureAwait(true);
            }
            finally
            {
                try
                {
                    _obdReplay?.Dispose();
                    _obdReplay = null;
                    if (_obdRecording is { } journal)
                    {
                        _obdRecording = null;
                        await journal.DisposeAsync().ConfigureAwait(true);
                    }
                }
                finally
                {
                    try
                    {
                        await StopPlausibilityMonitoringAsync().ConfigureAwait(true);
                    }
                    finally
                    {
                        await SaveFusionLearningAsync().ConfigureAwait(true);
                    }
                }
            }
        }
    }

    private void ApplyLatestFusionEstimate(bool rawImuChanged = false)
    {
        ApplyGpsMotionReadouts();
        PresentObd();
        PresentPlausibilityFault();

        if (_fusionSession?.Fault is { } fault && !ReferenceEquals(fault, _observedFusionFault))
        {
            _observedFusionFault = fault;
            ShowError("Full-rate fusion stopped; its trace is incomplete", fault);
        }

        FusionEstimate? estimate = _fusionSession?.Latest ?? _lastFusionEstimate;

        if (_paused
            || (estimate is null && _lastRawImu is null)
            || (!rawImuChanged && estimate?.Sequence == _displayedFusionSequence))
        {
            ApplySelectedHorizon();
            return;
        }

        if (estimate is not null)
        {
            _lastFusionEstimate = estimate;
            _displayedFusionSequence = estimate.Sequence;
        }

        RawImuPayload? rawImu = _lastRawImu;
        // Gauge animations use the render clock: a historical replay timestamp would instantly
        // expire the shock indicator. Measurement time remains unchanged in the engine and trace.
        DateTimeOffset presentationTime = DateTimeOffset.UtcNow;
        VehicleGaugeReadings gaugeReadings = GaugeDashboard.ApplyFusion(estimate, rawImu, presentationTime,
            applySpeed: estimate?.HasAuthoritativeSpeed != true, applyAttitude: false);
        if (estimate?.HasAuthoritativeSpeed == true && _obdReplayActive)
        {
            _speedometer.SetVehicleSpeed(estimate.Speed,
                estimate.SpeedSelection?.Reading?.MeasurementTime ?? estimate.Timestamp);
        }

        PresentObd();
        if (estimate is not null)
        {
            _telemetryTabHost.ApplyFusion(
                estimate,
                gaugeReadings with
                {
                    ForwardG = _accelerationGauge.IsAccelerationValid ? _accelerationGauge.ForwardG : double.NaN,
                    LateralG = _accelerationGauge.IsAccelerationValid ? _accelerationGauge.LateralG : double.NaN,
                    VerticalG = _accelerationGauge.IsAccelerationValid ? _accelerationGauge.VerticalG : double.NaN
                });
        }

        if (rawImu is { } rawFallback)
        {
            _telemetryTabHost.ApplyRawImuFallback(rawFallback, estimate);
        }

        // The dashboard's acceleration/speed policy stays fusion-owned; only attitude display is selectable.
        ApplySelectedHorizon();
    }
}
