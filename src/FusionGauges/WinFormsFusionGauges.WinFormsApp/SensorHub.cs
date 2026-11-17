using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.Windows.Sensors;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>
///  Coordinates acquisition, full-rate fusion and recording without depending on a render callback.
/// </summary>
internal sealed class SensorHub : IAsyncDisposable
{
    private readonly Func<WindowsSensorSourceOptions, ISensorSourceFactory> _sourceFactory;
    private readonly Func<WindowsSensorSourceOptions, GpsAcquisitionSelection?> _gpsSelectionFactory;
    private readonly GpsAcquisitionCoordinator? _gpsOwner;
    private readonly GpsPreviewSession? _gpsPreview;
    private WindowsSensorSourceOptions? _gpsOptions;
    private readonly object _gpsGate = new();
    private long _gpsVersion;
    private readonly object _disposeGate = new();
    private Task? _disposal;
    private readonly object _fusionGate = new();
    private readonly Queue<FusionObservation> _recentInputs = [];
    private readonly List<SensorRecordingZipMirror> _mirrors = [];
    private SensorService? _service;
    private ISensorServiceSessionLease? _lease;
    private ISensorSubscription? _rawSubscription;
    private SensorFusionSession? _fusion;
    private ISensorRecordingSession? _recording;
    private SensorRecordingCompanions? _companions;
    private SensorV2Recording? _v2Recording;
    private SensorCausalInputJournal<ReplayControl>? _causalJournal;
    private Task _rawTask = Task.CompletedTask;
    private Task _mirrorMonitor = Task.CompletedTask;
    private CancellationTokenSource? _mirrorMonitorStop;
    private long _lastFusionInput = -1;
    private Exception? _fault;
    private volatile Exception? _mirrorFault;
    private Task? _recordingStop;
    private SensorSubscriptionStatistics _lastRawStatistics;
    private SensorObservationFrame? _stoppedFrame;
    private readonly GpsDashboardModel _gpsDashboard = new();
    private readonly ObdSpeedSource _obd;
    private int _useObdSpeed;
    private readonly object _speedGate = new();
    private long _obdGeneration;
    private bool _obdConnected;

    internal bool UseObdSpeed => Volatile.Read(ref _useObdSpeed) != 0;
    internal VehicleSpeedMeasurement? VehicleSpeed => UseObdSpeed ? _obd.Latest : null;
    internal string ObdStatus => _obd.Status;
    internal WarpToolkit.Windows.Bluetooth.ObdSpeedPollingStatistics? ObdStatistics => _obd.Statistics;
    internal void StartBluetooth() => _obd.Start();
    internal void StartBluetooth(WarpToolkit.Windows.Bluetooth.ObdAdapterKind adapter) => _obd.Start(adapter);
    internal ValueTask StopBluetoothAsync() => _obd.DisposeAsync();
    internal Task SetMercedesModeAsync(bool enabled)
        => _obd.SetMercedesModeAsync(enabled);
    internal event Action<WarpToolkit.Windows.Bluetooth.ObdDiagnosticEntry>? CanDiagnosticReceived
    {
        add => _obd.DiagnosticReceived += value;
        remove => _obd.DiagnosticReceived -= value;
    }
    internal event Action<string>? ShutdownProgress;
    internal event Action<string>? BluetoothStatusChanged
    {
        add => _obd.StatusChanged += value;
        remove => _obd.StatusChanged -= value;
    }

    internal void SelectObdSpeed(bool selected)
    {
        lock (_speedGate)
        {
            Volatile.Write(ref _useObdSpeed, selected ? 1 : 0);
            OnVehicleTelemetryChanged(_obd.LatestTelemetry);
        }
    }

    private void OnVehicleTelemetryChanged(VehicleTelemetryPayload telemetry)
    {
        DateTimeOffset received = DateTimeOffset.UtcNow;
        lock (_speedGate)
        {
            if (telemetry.Connected && !_obdConnected)
            {
                _obdGeneration++;
            }
            _obdConnected = telemetry.Connected;
            _service?.TryPublishObservation(new()
            {
                Sequence = -1,
                Kind = SensorDataKind.OBD2,
                SourceId = "OBD2",
                SourceGeneration = _obdGeneration,
                SourceTimestamp = received,
                ArrivalTimestamp = received,
                VehicleTelemetry = telemetry with { SelectedForSpeed = UseObdSpeed }
            });
        }
    }

    internal SensorHub(Func<WindowsSensorSourceOptions, ISensorSourceFactory>? sourceFactory = null,
        ObdSpeedSource? obd = null, GpsAcquisitionCoordinator? gpsOwner = null,
        Func<WindowsSensorSourceOptions, GpsAcquisitionSelection?>? gpsSelectionFactory = null)
    {
        _obd = obd ?? new ObdSpeedSource();
        if (sourceFactory is null || gpsOwner is not null)
        {
            _gpsOwner = gpsOwner ?? new();
            _gpsPreview = new(_gpsOwner.CreateSourceView());
        }
        _sourceFactory = sourceFactory ?? (options => new WindowsSensorSourceFactory(options, _gpsOwner!.CreateSourceView()));
        _gpsSelectionFactory = gpsSelectionFactory ?? (static options => new WindowsSensorSourceFactory(options).CreateGpsSelection());
        _obd.TelemetryChanged += OnVehicleTelemetryChanged;
    }

    internal event Action<SensorObservationFrame>? FrameReady;

    internal SensorObservationCore Core { get; } = new();
    internal GpsDashboardSnapshot GpsDashboard => !IsRunning && _gpsPreview is not null
        ? _gpsPreview.Dashboard : _gpsDashboard.Latest;
    internal bool HasGpsPreview => _gpsPreview is not null && _gpsOptions?.Sources is
        WindowsSensorSourceKind.ImuAndGps or WindowsSensorSourceKind.ImuAndSerialNmeaGps or WindowsSensorSourceKind.GpsOnly;
    internal Exception? GpsFault => _gpsOwner?.Fault;
    internal bool IsRunning => _lease is not null;
    internal bool IsRecording => _recording is not null;
    internal SensorRecordingStatus? RecordingStatus => _recording?.Status ?? LastRecording;
    internal SensorRecordingStatus? LastRecording { get; private set; }
    internal TabletFrameStatus FrameStatus => _fusion?.FrameStatus ?? TabletFrameStatus.Idle;
    internal Exception? Fault => _fault ?? _fusion?.Fault ?? _companions?.Fault ?? _v2Recording?.Fault ?? _causalJournal?.Fault
        ?? _rawTask.Exception?.GetBaseException() ?? _service?.Faults.LastFault;
    internal Exception? MirrorFault => _mirrorFault;
    internal string RecordingMessage { get; private set; } = "No recording";
    internal SensorObservationFrame Capture()
    {
        SensorObservationFrame frame = _stoppedFrame
            ?? Core.Capture(DateTimeOffset.UtcNow, _rawSubscription?.Statistics ?? _lastRawStatistics);
        if (_gpsPreview is null)
        {
            return frame;
        }
        GpsPreviewSnapshot preview = _gpsPreview.Capture();
        if (IsRunning)
        {
            return frame with
            {
                GpsFresh = frame.GpsFresh && preview.GpsFresh
                    && frame.Gps?.SourceGeneration == preview.Gps?.SourceGeneration
            };
        }
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return frame with
        {
            Gps = preview.Gps, GpsHz = preview.GpsHz, GpsFresh = preview.GpsFresh,
            ImuFresh = false, FusionFresh = false, ImuHz = 0,
            Health = new()
            {
                Sequence = -1, Kind = SensorDataKind.Health, SourceTimestamp = now,
                ArrivalTimestamp = now, Health = preview.Health
            }
        };
    }

    internal async Task StartGpsPreviewAsync(FusionGaugesOptions options)
    {
        await ApplyGpsAsync(options);
        if (_gpsPreview is not null)
        {
            await _gpsPreview.StartAsync();
        }
    }

    internal Task ApplyGpsAsync(FusionGaugesOptions options, bool allowReceiverWrites = false,
        bool stationaryConfirmed = false)
    {
        if (_gpsOwner is null)
        {
            return Task.CompletedTask;
        }
        lock (_gpsGate)
        {
            WindowsSensorSourceOptions selected = options.CreateSourceOptions();
            if (!allowReceiverWrites)
            {
                selected = selected with { SerialGpsOptions = selected.SerialGpsOptions.WithoutReceiverCommands() };
            }
            GpsAcquisitionSelection? selection = _gpsSelectionFactory(selected);
            if (selection?.Key != (_gpsOptions is null ? null : _gpsSelectionFactory(_gpsOptions)?.Key))
            {
                bool writes = selected.Sources == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                    && selected.SerialGpsOptions is { GnssProfile: not SerialGnssProfile.Passive, AllowReceiverCommandsThisSession: true };
                bool restores = _gpsOptions is { Sources: WindowsSensorSourceKind.ImuAndSerialNmeaGps,
                    SerialGpsOptions: { GnssProfile: not SerialGnssProfile.Passive, AllowReceiverCommandsThisSession: true } };
                GpsPreviewSnapshot? preview = _gpsPreview?.Capture();
                double speed = _gpsPreview?.Dashboard.Motion.ReceiverSpeedMetersPerSecond ?? double.NaN;
                if ((writes || restores) && !stationaryConfirmed
                    && (preview?.GpsFresh != true || !double.IsFinite(speed) || speed > 0.5d))
                {
                    throw new InvalidOperationException("A fresh stationary GPS fix is required before receiver writes or restoration. "
                        + "An inactive drive is not proof that the vehicle is parked.");
                }
            }
            long version = ++_gpsVersion;
            return CompleteGpsConfigurationAsync(version, selected, _gpsOwner.ConfigureAsync(selection));
        }
    }

    private async Task CompleteGpsConfigurationAsync(long version, WindowsSensorSourceOptions selected, Task configuring)
    {
        await configuring;
        lock (_gpsGate)
        {
            if (version == _gpsVersion)
            {
                _gpsOptions = selected;
            }
        }
    }
    internal void RaiseFrameReady() => FrameReady?.Invoke(Capture());
    internal SensorRecordingZipMirrorStatus CaptureMirrorStatus()
        => new(_mirrors.Sum(static mirror => mirror.Status.Pending),
            _mirrors.Sum(static mirror => mirror.Status.Published),
            _mirrorFault?.Message ?? _mirrors.Select(static mirror => mirror.Status.LastError)
                .FirstOrDefault(static error => error is not null));

    internal async Task StartAsync(FusionGaugesOptions options, bool stationaryConfirmed = false)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("Sensors are already running.");
        }

        if (_fault is not null)
        {
            throw new InvalidOperationException("The preceding sensor session failed; restart the app.", _fault);
        }

        options.Validate();
        await ApplyGpsAsync(options, allowReceiverWrites: true, stationaryConfirmed);
        if (_gpsPreview is not null)
        {
            await _gpsPreview.StartAsync();
        }
        _stoppedFrame = null;
        Core.Reset();
        _gpsDashboard.Reset();
        _lastRawStatistics = default;
        lock (_fusionGate)
        {
            _recentInputs.Clear();
            _lastFusionInput = -1;
        }

        _service = new SensorService(_sourceFactory(options.CreateSourceOptions()));

        try
        {
            _lease = await _service.AcquireSessionAsync(SensorServiceOptions.Default);
            _rawSubscription = _service.Subscribe(new()
            {
                Kinds = SensorDataKindFilter.All,
                Capacity = 4096,
                Name = "FusionGauges raw history"
            });
            _rawTask = Task.Run(() => ConsumeRawAsync(_rawSubscription));
            IFusionEngine engine = options.VehicleMount is { } mount
                ? FusionEngineFactory.CreateDefault(mount)
                : FusionEngineFactory.CreateDefault();
            _causalJournal = new();
            _fusion = new SensorFusionSession(engine);
            _fusion.Processing += OnFusionProcessing;
            _fusion.Processed += OnFusionProcessed;
            _fusion.Attach(_service.Subscribe(new()
            {
                Kinds = SensorDataKindFilter.All,
                Capacity = 8192,
                Name = "FusionGauges full-rate fusion"
            }));

            if (options.AutoRecording)
            {
                await StartRecordingAsync(options);
            }
        }
        catch (Exception exception)
        {
            _fault = exception;

            try
            {
                await StopAsync();
            }
            catch (Exception cleanupFailure)
            {
                _fault = new AggregateException("Sensor startup and cleanup failed.", exception, cleanupFailure);
                throw _fault;
            }

            throw;
        }
    }

    private async Task ConsumeRawAsync(ISensorSubscription subscription)
    {
        await foreach (SensorEnvelope envelope in subscription.ReadAllAsync().ConfigureAwait(false))
        {
            _gpsDashboard.Observe(envelope);
            Core.Observe(envelope);
        }
    }

    internal async Task StartRecordingAsync(FusionGaugesOptions options, string? name = null)
    {
        if (_service is null || !IsRunning || IsRecording)
        {
            throw new InvalidOperationException("Start sensors before starting a new recording.");
        }

        if (Fault is { } fault)
        {
            throw new InvalidOperationException("Fusion is not healthy; recording was not started.", fault);
        }

        _recordingStop = null;
        _recording = await _service.StartRecordingAsync(options.CreateRecordingRequest(name));

        try
        {
            lock (_fusionGate)
            {
                _v2Recording = new(_recording, options.RawSnapshotsPerSecond);
                _companions = new SensorRecordingCompanions(_recording);
                foreach (FusionObservation observation in _recentInputs)
                {
                    _companions.Publish(observation);
                    _v2Recording.Publish(observation, seed: _recording.Coverage is not { } coverage
                        || observation.Input.Sequence < coverage.FirstSequence);
                }
            }
        }
        catch
        {
            await _recording.StopAsync();
            _recording = null;
            throw;
        }

        RecordingMessage = $"Recording: {_recording.FolderPath}";

        if (options.MirrorEnabled)
        {
            try
            {
                SensorRecordingZipMirror mirror = await Task.Run(() => new SensorRecordingZipMirror(
                    _recording.FolderPath, new()
                    {
                        DestinationFolder = options.MirrorFolder,
                        CompressionLevel = options.CompressionLevel
                    }));
                _mirrors.Add(mirror);
                _mirrorFolders.Add(mirror, _recording.FolderPath);
                _mirrorMonitorStop = new();
                ISensorRecordingSession recording = _recording;
                CancellationToken token = _mirrorMonitorStop.Token;
                _mirrorMonitor = Task.Run(() => MonitorMirrorAsync(recording, mirror, token));
            }
            catch (Exception exception)
            {
                _mirrorFault = exception;
            }
        }
    }

    internal Task StopRecordingAsync()
        => _recordingStop ??= StopRecordingCoreAsync();

    internal void AddMarker(string label)
    {
        lock (_fusionGate)
        {
            SensorRecordingCompanions companions = _companions
                ?? throw new InvalidOperationException("Start recording before adding a marker.");
            companions.AddMarker(new(label, DateTimeOffset.UtcNow, _lastFusionInput, _fusion?.Epoch ?? 0));
            _v2Recording?.AddMarker(new(label, DateTimeOffset.UtcNow, _lastFusionInput, _fusion?.Epoch ?? 0));
        }
    }

    internal async Task CalibrateAsync(bool accept)
    {
        SensorFusionSession fusion = _fusion
            ?? throw new InvalidOperationException("Start sensors before calibration.");
        TabletFrameCommand command = accept ? TabletFrameCommand.Accept() : TabletFrameCommand.BeginCapture();
        fusion.PostTabletFrame(command);

        if (!await command.Completion.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("The current capture cannot be accepted; check capture quality.");
        }
    }

    internal async Task<MotionAxisCapture> CaptureMotionAxisAsync(CancellationToken cancellationToken)
    {
        SensorService service = _service
            ?? throw new InvalidOperationException("Start sensors before checking motion axes.");

        if (!IsRunning || IsRecording)
        {
            throw new InvalidOperationException("Motion calibration requires a running, non-recording sensor session.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        await using ISensorSubscription subscription = service.Subscribe(new()
        {
            Kinds = SensorDataKindFilter.Imu,
            Capacity = 4096,
            Name = "FusionGauges landscape motion calibration"
        });
        List<SensorEnvelope> samples = [];
        using CancellationTokenSource stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        stop.CancelAfter(MotionAxisCapture.CaptureDuration);

        try
        {
            await foreach (SensorEnvelope sample in subscription.ReadAllAsync(stop.Token).ConfigureAwait(false))
            {
                samples.Add(sample);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // The six-second observation window has ended.
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (!IsRunning)
        {
            throw new InvalidOperationException("The sensor session ended during motion calibration.");
        }

        if (subscription.Statistics.Dropped != 0)
        {
            throw new InvalidDataException("Motion calibration lost sensor samples. Repeat this step.");
        }

        if (Fault is { } fault)
        {
            throw new InvalidOperationException("The sensor session failed during motion calibration.", fault);
        }

        return MotionAxisCapture.Analyze(samples);
    }

    internal async Task ReplaceEngineAsync(IFusionEngine? engine)
    {
        SensorFusionSession fusion = _fusion
            ?? throw new InvalidOperationException("Start sensors before changing engines.");
        await fusion.ReplaceEngineAsync(engine).WaitAsync(TimeSpan.FromSeconds(5));
        Core.AdvanceFusionEpoch(fusion.Epoch);
    }

    internal async Task RetryMirrorsAsync(FusionGaugesOptions options)
    {
        string[] journals = await Task.Run(() => Directory.GetFiles(options.RecordingFolder,
            "zip-mirror.journal.json", SearchOption.AllDirectories));

        foreach (string journal in journals)
        {
            string folder = Path.GetDirectoryName(journal)!;

            if (!_mirrors.Any(mirror => _mirrorFolders.TryGetValue(mirror, out string? value) && value == folder))
            {
                SensorRecordingZipMirror mirror = await Task.Run(() => new SensorRecordingZipMirror(folder,
                    new() { DestinationFolder = options.MirrorFolder, CompressionLevel = options.CompressionLevel }));
                _mirrors.Add(mirror);
                _mirrorFolders.Add(mirror, folder);
            }
        }

        foreach (SensorRecordingZipMirror mirror in _mirrors)
        {
            await mirror.RetryAsync();
        }

        _mirrorFault = null;
    }

    private readonly Dictionary<SensorRecordingZipMirror, string> _mirrorFolders = [];

    internal async Task StopAsync()
    {
        List<Exception> failures = [];
        await ShutdownStepAsync("Finalize recording and queue closed ZIP artifacts", StopRecordingAsync, failures);

        // Hardware teardown time must not age the final acquisition history.
        DateTimeOffset stoppedAt = DateTimeOffset.UtcNow;
        ISensorServiceSessionLease? lease = _lease;
        _lease = null;

        await ShutdownStepAsync("Release acquisition lease and stop sensor sources", async () =>
        {
            if (lease is not null)
            {
                await lease.DisposeAsync();
            }
        }, failures);

        await ShutdownStepAsync("Drain raw observation history", () => _rawTask, failures);

        await ShutdownStepAsync("Drain fusion input and verify terminal acquisition counters", async () =>
        {
            if (_fusion is not null)
            {
                await _fusion.FinishInputAsync(lease?.CompletedSession);
                await _fusion.CompleteAsync();
                ReportShutdown($"Fusion drained: {_fusion.InputCount} inputs, {_fusion.OutputCount} outputs.");
                if (_fusion.InputCount > 0 && _fusion.OutputCount == 0)
                {
                    ReportShutdown("WARNING: No fusion estimates were produced. Check IMU availability; raw inputs are preserved.");
                }
            }
        }, failures);

        await ShutdownStepAsync("Release raw history subscription", async () =>
        {
            if (_rawSubscription is not null)
            {
                _lastRawStatistics = _rawSubscription.Statistics;
                await _rawSubscription.DisposeAsync();
                ReportShutdown((_lastRawStatistics.Dropped > 0 ? "WARNING: " : string.Empty)
                    + $"Raw subscription: {_lastRawStatistics.Dropped} dropped observations.");
            }
        }, failures);

        await ShutdownStepAsync("Release fusion owner", async () =>
        {
            if (_fusion is not null)
            {
                _fusion.Processing -= OnFusionProcessing;
                _fusion.Processed -= OnFusionProcessed;
                await _fusion.DisposeAsync();
            }
        }, failures);

        await ShutdownStepAsync("Flush and release temporary causal input journal", async () =>
        {
            if (_causalJournal is not null)
            {
                await _causalJournal.DisposeAsync();
                _causalJournal = null;
            }
        }, failures);

        await ShutdownStepAsync("Dispose acquisition service", async () =>
        {
            if (_service is not null)
            {
                await _service.DisposeAsync();
            }
        }, failures);
        _rawSubscription = null;
        _fusion = null;
        _service = null;
        _stoppedFrame ??= Core.Capture(stoppedAt, _lastRawStatistics) with
        {
            ImuHz = 0d,
            GpsHz = 0d,
            ImuFresh = false,
            GpsFresh = false,
            FusionFresh = false
        };

        if (failures.Count > 0)
        {
            _fault = new AggregateException("Sensor shutdown was not clean.", failures);
            throw _fault;
        }
    }

    public ValueTask DisposeAsync()
    {
        lock (_disposeGate)
        {
            return new(_disposal ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        List<Exception> failures = [];
        await ShutdownStepAsync("Stop Bluetooth polling and CAN monitoring",
            () => StopBluetoothAsync().AsTask(), failures);
        await ShutdownStepAsync("Stop sensor session", StopAsync, failures);

        await ShutdownStepAsync("Release shared GPS owner and preview", async () =>
        {
            if (_gpsOwner is not null)
            {
                await _gpsOwner.DisposeAsync();
            }
        }, failures);

        foreach (SensorRecordingZipMirror mirror in _mirrors)
        {
            await ShutdownStepAsync("Stop ZIP worker and persist retry journal",
                () => mirror.DisposeAsync().AsTask(), failures);
            SensorRecordingZipMirrorStatus status = mirror.Status;
            ReportShutdown($"ZIP journal: {status.Published} published, {status.Pending} pending."
                + (status.LastError is { } error ? $" WARNING: {error}" : string.Empty));
            if (status.Pending > 0)
            {
                ReportShutdown("WARNING: Pending ZIPs are retained locally; use Retry ZIPs next time.");
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException(failures);
        }

        ReportShutdown("All owned resources released; shutdown complete.");
    }

    private void ReportShutdown(string message)
    {
        System.Diagnostics.Trace.TraceInformation(message);
        ShutdownProgress?.Invoke(message);
    }

    private async Task ShutdownStepAsync(string name, Func<Task> step, List<Exception> failures)
    {
        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        ReportShutdown($"BEGIN: {name}");
        try
        {
            await step();
            ReportShutdown($"OK: {name} ({System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms)");
        }
        catch (Exception exception)
        {
            failures.Add(exception);
            ReportShutdown($"ERROR: {name}: {exception}");
            System.Diagnostics.Trace.TraceError(exception.ToString());
        }
    }

    private void OnFusionProcessing(SensorEnvelope input)
    {
        lock (_speedGate)
        {
            VehicleSpeedMeasurement? speed = VehicleSpeed;
            IFusionEngine? engine = _fusion?.Engine;
            if (engine is IVehicleSpeedConsumer consumer)
            {
                consumer.SetVehicleSpeed(speed);
            }
            else if (speed is not null)
            {
                throw new InvalidOperationException("The selected engine cannot consume the selected vehicle speed.");
            }
            TabletFrameStatus? frame = (engine as ITabletFrameController)?.FrameStatus;
            _causalJournal?.Publish(input, new(_fusion?.Epoch ?? 0,
                _fusion?.Engine?.GetType().FullName ?? "disabled", speed, frame?.Lock, frame?.Phase));
        }
    }

    private void OnFusionProcessed(FusionObservation observation)
    {
        Core.ObserveFusion(observation);

        lock (_fusionGate)
        {
            _lastFusionInput = observation.Input.Sequence;
            _recentInputs.Enqueue(observation);

            while (_recentInputs.Count > 16384)
            {
                _recentInputs.Dequeue();
            }

            _companions?.Publish(observation);
            _v2Recording?.Publish(observation);
        }
    }

    private async Task StopRecordingCoreAsync()
    {
        if (_recording is not { } recording)
        {
            return;
        }

        List<Exception> failures = [];
        ReportShutdown($"Recording cutoff requested: {recording.FolderPath}");

        try
        {
            if (_v2Recording is { } pair)
            {
                await pair.StopSnapshotsAsync();
            }
            await recording.StopAsync();
            long drainStarted = System.Diagnostics.Stopwatch.GetTimestamp();

            while (recording.Coverage is { } coverage && Interlocked.Read(ref _lastFusionInput) < coverage.LastSequence
                && _fusion is { Fault: null, Completion.IsCompleted: false }
                && System.Diagnostics.Stopwatch.GetElapsedTime(drainStarted) < TimeSpan.FromSeconds(10))
            {
                await Task.Delay(10);
            }

            if (recording.Status.LastFaultMessage is { } error)
            {
                throw new IOException(error);
            }

            if (recording.Coverage is { } cutoff)
            {
                if (_fusion?.Fault is { } fusionFault)
                {
                    throw new InvalidDataException(
                        $"Fusion stopped before recording cutoff {cutoff.LastSequence}; last processed input {_lastFusionInput}.",
                        fusionFault);
                }
                ReportShutdown($"Exporting causal journal through input {cutoff.LastSequence}; processed {_lastFusionInput}.");
                SensorCausalInputJournal<ReplayControl> history = _causalJournal
                    ?? throw new InvalidOperationException("The drive has no causal input journal.");
                await history.ExportAsync(recording.FolderPath, cutoff.LastSequence);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        SensorRecordingCompanions? companions;
        SensorV2Recording? v2;

        lock (_fusionGate)
        {
            companions = _companions;
            _companions = null;
            v2 = _v2Recording;
            _v2Recording = null;
        }

        try
        {
            if (companions is not null)
            {
                await companions.StopAsync(inputsVerified: failures.Count == 0 && Fault is null
                    && recording.Coverage?.LastSequence <= _lastFusionInput);

                if (!companions.IsComplete)
                {
                    throw new IOException("Fusion companions are incomplete.", companions.Fault);
                }
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            if (v2 is not null)
            {
                await v2.StopAsync(inputsVerified: failures.Count == 0 && Fault is null
                    && recording.Coverage?.LastSequence <= _lastFusionInput
                    && _service?.Statistics.IngressDropped == 0);
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }

        try
        {
            if (_mirrorMonitorStop is { } stop)
            {
                try
                {
                    await stop.CancelAsync();
                    await _mirrorMonitor;
                }
                finally
                {
                    stop.Dispose();
                    _mirrorMonitorStop = null;
                }
            }
        }
        catch (Exception exception)
        {
            failures.Add(exception);
        }
        finally
        {
            LastRecording = recording.Status;
            _recording = null;
        }

        if (failures.Count > 0)
        {
            RecordingMessage = $"Recording incomplete: {failures[0].Message}";
            throw new AggregateException("The recording could not be finalized.", failures);
        }

        RecordingMessage = $"Finalized: {recording.FolderPath}";
        ReportShutdown(RecordingMessage);
    }

    private async Task MonitorMirrorAsync(
        ISensorRecordingSession recording, SensorRecordingZipMirror mirror, CancellationToken cancellationToken)
    {
        HashSet<int> queued = [];

        async Task QueuePartsAsync()
        {
            foreach (SensorRecordingPart part in recording.Status.FinalizedParts.OrderBy(static part => part.PartIndex))
            {
                if (!queued.Contains(part.PartIndex))
                {
                    await mirror.QueueArchiveAsync($"part-{part.PartIndex:D4}.zip", [.. part.Files, part.SidecarPath]);
                    queued.Add(part.PartIndex);
                }
            }
        }

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await QueuePartsAsync();
                await Task.Delay(1000, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _mirrorFault = exception;
        }
        finally
        {
            try
            {
                await QueuePartsAsync();
                string[] companions = Directory.GetFiles(recording.FolderPath)
                    .Where(path => !path.EndsWith(".writing", StringComparison.Ordinal)
                        && (Path.GetFileName(path).StartsWith("fusion-epoch-", StringComparison.Ordinal)
                        || path.EndsWith(".RawV2.csv", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".FuseV2.csv", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".MarkersV2.jsonl", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".RawV2.csv.incomplete", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".FuseV2.csv.incomplete", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".MarkersV2.jsonl.incomplete", StringComparison.OrdinalIgnoreCase))).ToArray();
                string[] metadata = Directory.GetFiles(recording.FolderPath)
                    .Where(path => Path.GetFileName(path) is "manifest.json" or "recording.manifest.json"
                        or "companions.manifest.json" or "markers.jsonl"
                        or "markers.jsonl.incomplete" or "v2.manifest.json"
                        or "replay-inputs.compact.json" or "replay-controls.jsonl"
                        or "replay-inputs.manifest.json").ToArray();

                if (companions.Length + metadata.Length > 0)
                {
                    await mirror.QueueArchiveAsync("companions-and-manifests.zip", [.. companions, .. metadata]);
                }

            }
            catch (Exception exception)
            {
                _mirrorFault = exception;
            }
        }
    }

    /// <summary>Records the exact owner-boundary speed policy and tablet control state.</summary>
    private sealed record ReplayControl(
        int Epoch, string Engine, VehicleSpeedMeasurement? Speed, SessionDownLock? Mount, TabletFramePhase? Phase);
}
