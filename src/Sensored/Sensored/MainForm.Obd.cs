using System.Diagnostics;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Coordinates independent OBD speed, live telemetry, and recording/replay owner boundaries.</summary>
public partial class MainForm
{
    private ObdTelemetrySource? _obdSource;
    private ObdTelemetrySnapshot _presentedObd = ObdTelemetrySnapshot.Disabled;
    private readonly object _obdRecordingGate = new();
    private ObdRecordingCompanion? _obdRecording;
    private ObdReplayCursor? _obdReplay;
    private long _obdReplayMinimumSequence;
    private long _obdReplayTimeTicks;
    private bool _obdReplayNeedsWarmup;
    private ObdRecordedInput? _currentObdReplayInput;
    private volatile bool _obdReplayActive;
    private Exception? _observedObdFault;
    private bool _obdTogglePending;
    private Exception? _terminalObdFailure;
    private bool _obdSpeedWasEffective;
    private VehicleTripAccumulator _vehicleTrip = new();
    private long _obdGeneration;
    private bool _obdConnected;
    private SensorReferenceSpeed _referenceSpeed = new();
    private SensorComparisonReference? _presentedComparison;

    private void OnVehicleFusionProcessed(FusionObservation observation)
    {
        ObserveV2Recording(observation);
        ObservePerformanceTiming(observation);
        ObservePlausibility(observation);
        ObserveFusionLearning(observation);
        if (observation.Estimate is { } estimate)
        {
            _vehicleTrip.Observe(estimate.Timestamp,
                estimate.IsSpeedSupported
                    ? estimate.Speed : double.NaN);
        }
    }

    private void SetupObd()
    {
        _obdButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.SpeedMedium,
            tooltipText: "Prefer selected OBD2 speed; GPS remains active for direction and fallback",
            size: (int)_options.ToolbarSize);
        _obdButton.Checked = _options.UseObd;
    }

    private async void OnObdCommand(object? sender, EventArgs e)
    {
        if (_obdTogglePending || _obdReplayActive || _shutdownStarted)
        {
            return;
        }

        _obdTogglePending = true;
        try
        {
            bool enable = !_options.UseObd;
            if (enable)
            {
                if (_terminalObdFailure is { } failure)
                {
                    throw new InvalidOperationException("OBD2 release failed terminally; restart the app before reconnecting.", failure);
                }

                StartObdDiscovery();
            }
            else
            {
                await StopObdAsync().ConfigureAwait(true);
            }

            _options = _options with { UseObd = enable };
            OnObdTelemetryReceived(_obdSource?.Latest ?? ObdTelemetrySnapshot.Disabled);
            DemoSettings.Save(_userSettings, _options);
            _userSettings?.Flush();
        }
        catch (Exception exception)
        {
            ShowError("OBD2 source transition failed", exception);
        }
        finally
        {
            _obdTogglePending = false;
            PresentObd();
        }
    }

    private void StartObdDiscovery()
    {
        if (_obdSource is null)
        {
            if (_terminalObdFailure is { } failure)
            {
                throw new InvalidOperationException("OBD2 release failed terminally; restart before reconnecting.", failure);
            }

            _obdSource = new ObdTelemetrySource(searchDuration: TimeSpan.FromMinutes(_options.ObdSearchMinutes),
                adapter: _options.ObdAdapter, rawTraffic: RawObd.Publish, selectedDevice: _options.ObdDevice);
            _obdSource.ReadingChanged += OnObdTelemetryReceived;
        }
    }

    private async Task ApplyObdDeviceOptionsAsync(DemoOptions previous)
    {
        if (previous.ObdDevice == _options.ObdDevice && previous.ObdAdapter == _options.ObdAdapter
            && previous.ObdSearchMinutes == _options.ObdSearchMinutes)
        {
            return;
        }

        await StopObdAsync().ConfigureAwait(true);
        if (_options.UseObd && !_obdReplayActive && !_shutdownStarted)
        {
            StartObdDiscovery();
        }
    }

    private void OnObdTelemetryReceived(ObdTelemetrySnapshot snapshot)
    {
        if (_obdReplayActive)
        {
            return;
        }

        DateTimeOffset received = DateTimeOffset.UtcNow;
        lock (_obdRecordingGate)
        {
            if (snapshot.Connected && !_obdConnected)
            {
                _obdGeneration++;
            }
            _obdConnected = snapshot.Connected;
            _sensorService.TryPublishObservation(new()
            {
                Sequence = -1,
                Kind = SensorDataKind.OBD2,
                SourceId = snapshot.DeviceName ?? "OBD2",
                SourceGeneration = _obdGeneration,
                SourceTimestamp = received,
                ArrivalTimestamp = received,
                VehicleTelemetry = snapshot.Readings.ToSensorTelemetry(snapshot.Connected, _options.UseObd)
            });
        }
    }

    private async Task StopObdAsync()
    {
        if (_obdSource is { } source)
        {
            try
            {
                await source.DisposeAsync().ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                _terminalObdFailure ??= exception;
                throw;
            }

            _obdSource = null;
            OnObdTelemetryReceived(source.Latest with { Connected = false });
        }
    }

    private void OnObdRpmScaleChanged(int maximum)
    {
        _options = _options with { MaximumRpm = maximum };
        DemoSettings.Save(_userSettings, _options);
        _userSettings?.Flush();
        PresentObd();
    }

    private SensorEnvelope PrepareObdInput(SensorEnvelope input)
    {
        _currentObdReplayInput = null;
        if (_obdReplay is { IncludesCanonicalInputs: false }
            && input.Kind is not (SensorDataKind.Imu or SensorDataKind.Geolocation or SensorDataKind.Gap))
        {
            return input;
        }
        if (_obdReplayActive && _obdReplay is { } replay)
        {
            IFusionEngine engine = _fusionEngine
                ?? throw new InvalidOperationException("OBD replay requires an active fusion engine.");
            bool resume = _obdReplayNeedsWarmup && _replayPlayer is { IsPaused: false };
            if (resume)
            {
                _replayPlayer!.Pause();
            }

            try
            {
                _currentObdReplayInput = replay.Resolve(input, _obdReplayMinimumSequence, engine,
                    estimate => _vehicleTrip.Observe(estimate.Timestamp,
                        estimate.IsSpeedSupported
                            ? estimate.Speed : double.NaN),
                    prepareInput: engine is RecordedFusionEngine recorded ? recorded.PrepareInput : null);
                _obdReplayNeedsWarmup = false;

                input = _currentObdReplayInput.Input;
            }
            finally
            {
                if (resume)
                {
                    _replayPlayer?.Resume();
                }
            }
        }

        if (_fusionEngine is RecordedFusionEngine recordedEngine)
        {
            recordedEngine.PrepareInput(input);
        }

        return input;
    }

    private void OnObdFusionInput(SensorEnvelope input)
    {
        ObserveHorizonInput(input);
        if (_obdReplayActive && _obdReplay is not { IncludesCanonicalInputs: true }
            && input.Kind is not (SensorDataKind.Imu or SensorDataKind.Geolocation or SensorDataKind.Gap))
        {
            return;
        }
        IFusionEngine engine = _fusionEngine
            ?? throw new InvalidOperationException("OBD input requires an active fusion engine.");
        ObdTelemetrySnapshot snapshot = _obdReplayActive
            ? _currentObdReplayInput?.State.Telemetry ?? ObdTelemetrySnapshot.Disabled with
            {
                Status = "Replay: OBD2 companion unavailable"
            }
            : _obdSource?.Latest ?? ObdTelemetrySnapshot.Disabled;
        if (_currentObdReplayInput is { } recorded)
        {
            recorded.State.Apply(engine);
        }

        if (engine is IVehicleSpeedConsumer consumer)
        {
            consumer.SetVehicleSpeed(snapshot.Speed);
        }
        else if (snapshot.Speed is not null)
        {
            throw new InvalidOperationException("The current engine cannot consume the recorded vehicle speed.");
        }
        ConsumePlausibilityResults(input, engine);
        bool effective = snapshot.Speed?.IsFreshAt(input.ArrivalTimestamp) == true;
        if (effective != _obdSpeedWasEffective)
        {
            _obdSpeedWasEffective = effective;
            _log.Information($"{input.ArrivalTimestamp:O}: speed source "
                + (effective ? "OBD2, confidence 1.0; GPS direction retained." : "GPS/inertial fallback; OBD2 unavailable or stale."));
        }

        Interlocked.Exchange(ref _obdReplayTimeTicks, input.ArrivalTimestamp.UtcTicks);
        Volatile.Write(ref _presentedObd, snapshot);
        Volatile.Write(ref _presentedComparison, _obdReplayActive ? _currentObdReplayInput?.State.Comparison : null);
        lock (_obdRecordingGate)
        {
            TabletFrameStatus? frame = (engine as ITabletFrameController)?.FrameStatus;
            DemoOptions comparisonOptions = _options;
            double? acceleration = _referenceSpeed.Observe(snapshot.Speed, input.ArrivalTimestamp);
            BluetoothSensorSnapshot? bluetooth = _bluetoothSensorSource?.Latest;
            if (bluetooth is not null && !bluetooth.IsFreshAt(input.ArrivalTimestamp))
            {
                bluetooth = bluetooth with { Reading = null };
            }

            _obdRecording?.Publish(input, new(snapshot, frame?.Lock, frame?.Phase)
            {
                Plausibility = _currentPlausibilityFeedback,
                Comparison = _obdReplayActive ? _currentObdReplayInput?.State.Comparison
                    : new(comparisonOptions.SensorSource, bluetooth,
                        comparisonOptions.InternalSensorFrame, comparisonOptions.BluetoothSensorFrame,
                        snapshot.Speed?.IsFreshAt(input.ArrivalTimestamp) == true ? snapshot.Speed.MetersPerSecond : null,
                        snapshot.Speed?.Timestamp, acceleration)
            });
        }
    }

    private void PresentObd()
    {
        ObdTelemetrySnapshot snapshot = _obdReplayActive
            ? Volatile.Read(ref _presentedObd)
            : _obdSource?.Latest ?? ObdTelemetrySnapshot.Disabled;
        if (_obdSource?.Fault is { } fault && !ReferenceEquals(fault, _observedObdFault))
        {
            _observedObdFault = fault;
            ShowError("OBD2 acquisition or cleanup failed", fault);
        }

        DateTimeOffset now = _obdReplayActive
            ? new DateTimeOffset(Interlocked.Read(ref _obdReplayTimeTicks), TimeSpan.Zero)
            : DateTimeOffset.UtcNow;
        _obdView.Present(snapshot, now, _options.MaximumRpm);
        if (_speedometer.TripKilometers != _vehicleTrip.Kilometers)
        {
            _speedometer.SetTripKilometers(_vehicleTrip.Kilometers);
        }

        bool visible = _options.ForceObdGaugesOn || snapshot.ModeEstablished;
        float instrumentColumnWidth = 50F;
        if (_gaugeLayout.ColumnStyles[1].Width != instrumentColumnWidth)
        {
            _gaugeLayout.SuspendLayout();
            _gaugeLayout.ColumnStyles[0].Width = 100F - instrumentColumnWidth;
            _gaugeLayout.ColumnStyles[1].Width = instrumentColumnWidth;
            _gaugeLayout.ResumeLayout(performLayout: true);
        }

        ApplySourceDisplayLayout(visible);
        bool fresh = snapshot.Speed?.IsFreshAt(now) == true;
        bool displayObd = fresh && !PreferGpsInstrument;
        UpdateSpeedModeCaption(displayObd);
        _obdButton.Checked = _options.UseObd;
        _obdButton.Enabled = !_shutdownStarted && !_obdTogglePending && !_obdReplayActive;
        string source = fresh ? "OBD2 speed 100%; GPS direction"
            : snapshot.Enabled ? "GPS speed fallback" : "GPS speed";
        _obdButton.ToolTipText = $"{snapshot.Status}. Active: {source}.";
        _obdButton.AccessibleDescription = _obdButton.ToolTipText;
        if (!_paused && _lease is not null)
        {
            if (displayObd)
            {
                _speedometer.SetVehicleSpeed(snapshot.Speed!.MetersPerSecond, snapshot.Speed.Timestamp);
            }
            else
            {
                _speedometer.SetReceiverSpeed(
                    _displayedGpsFresh ? _displayedGpsMotion?.ReceiverSpeedMetersPerSecond ?? double.NaN : double.NaN,
                    _displayedGpsMotion?.Timestamp ?? now);
            }
        }

        PresentPerformanceTiming();
    }

    private async Task CompleteObdRecordingAsync(ISensorRecordingSession recording)
    {
        ObdRecordingCompanion journal;
        lock (_obdRecordingGate)
        {
            journal = _obdRecording
                ?? throw new InvalidOperationException("The fusion input journal is unavailable.");
        }

        long lastRequired = await Task.Run(async () =>
        {
            foreach (SensorRecordingPart part in recording.Status.FinalizedParts.OrderByDescending(part => part.PartIndex))
            {
                long last = -1;
                if (part.Files.Count > 0)
                {
                    string path = part.Files[0];
                    await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadPartAsync(path))
                    {
                        last = Math.Max(last, row.Envelope.Sequence);
                    }
                }

                if (last >= 0)
                {
                    return last;
                }
            }

            return -1L;
        }).ConfigureAwait(true);
        long started = Stopwatch.GetTimestamp();
        while (journal.LastSequence < lastRequired)
        {
            if (journal.Fault is { } fault)
            {
                throw new IOException("OBD recording lost its fusion input.", fault);
            }

            if (Stopwatch.GetElapsedTime(started) > RecordingStopTimeout)
            {
                throw new TimeoutException("Fusion did not reach the raw recording boundary; OBD companion remains incomplete.");
            }

            await Task.Delay(10).ConfigureAwait(true);
        }

        await journal.ExportAsync(recording.FolderPath).ConfigureAwait(true);
    }
}
