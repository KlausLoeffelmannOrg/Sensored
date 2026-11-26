using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using WarpToolkit.ComponentModel;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.Specialized;

namespace Sensored
{
    /// <summary>
    ///  Presents fused vehicle motion alongside raw geolocation, health, and classification streams.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This form is intentionally a thin async <see cref="ISensorService"/> subscriber. It performs
    ///   no calibration or fusion math; a full-rate portable fusion session owns those operations.
    ///  </para>
    ///  <para>
    ///   Raw acquisition and recording remain unchanged. Vehicle-frame estimates reach presentation
    ///   only after fusion and full-rate tracing, independently of UI throttling.
    ///  </para>
    /// </remarks>
    public partial class MainForm : Form
    {
        private static readonly TimeSpan UiUpdateInterval = TimeSpan.FromMilliseconds(50);
        private static readonly TimeSpan ConsumerStopTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan RecordingStopTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TransitionStopTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ShutdownReportInterval = TimeSpan.FromSeconds(1);
        private const double DashboardFrameRate = 30d;
        private const int SubscriptionCapacity = 512;
        private const double SubscriptionMaximumRateHertz = 60d;
        private const string WindowPlacementSettingsKey = "MainWindow.Placement";
        private const string ToolbarSizeSettingsKey = "MainToolbar.Size";
        private const string MapZoomOffsetSettingsKey = "Map.ZoomOffset";
        private const string MapDebugSettingsKey = "Map.ShowDebugInformation";
        private const string AutoRecordingSettingsKey = "Recording.AutoStart";
        private const string MapEnabledSettingsKey = "Map.Enabled";
        private const string MapRowSpanSettingsKey = "Map.RowSpan";
        private const string MapColumnSpanSettingsKey = "Map.ColumnSpan";
        private const string ReplaySequenceSettingsKey = "Replay.DetectSequenceFiles";
        private const string ReplayHideControlsSettingsKey = "Replay.HideControls";
        private const string ReplaySpeedSettingsKey = "Replay.SpeedPercent";

        private readonly ISensorService _sensorService;
        private readonly DemoSensorSourceFactory? _sensorSourceFactory;
        private readonly IUserSettingsService? _userSettings;
        private readonly DemoLog _log = new();
        private readonly Queue<GeolocationPayload> _pausedGpsPoints = new();
        private readonly Queue<RoadSectionClassificationResult> _pausedClassifications = new();
        private readonly SensorRateTracker _sensorRates = new();
        private readonly ExceptionHistory _exceptionHistory = ExceptionHistory.Shared;
        private readonly FullScreenController _fullScreenController;
        private DemoOptions _options = DemoOptions.Default;
        private DemoOptions? _runningSourceOptions;
        private ISensorServiceSessionLease? _lease;
        private ISensorSubscription? _subscription;
        private ISensorRecordingSession? _recording;
        private Task? _recordingStopTask;
        private SensorReplayPlayer? _replayPlayer;
        private CancellationTokenSource? _consumerCancellation;
        private Task? _consumerTask;
        private RawImuPayload? _lastRawImu;
        private DateTimeOffset _lastRawImuReceivedAt;
        private SensorHealthPayload? _lastHealth;
        private long _gapCount;
        private bool _paused;
        private bool _transitioning;
        private bool _shutdownStarted;
        private bool _shutdownCompleted;
        private int _cleanupStageCount;
        private long _dashboardEnvelopeCount;
        private Exception? _receiverRestoreWarning;
        private string _shutdownStage = "Waiting for the current sensor operation.";
        private CleanupStageProgress? _shutdownProgress;
        private bool _windowPlacementInitialized;
        private Font? _ownedBaseFont;
        private Font? _ownedStatusFont;
        private Font? _ownedTelemetryFont;
        private Exception? _observedSourceException;
        private Exception? _observedProcessingException;
        private Exception? _observedRecordingException;
        private string? _appliedRoutePath;
        private bool? _appliedFollowCurrentPosition;
        private double _sectionOdometerKilometers;
        private bool _suppressZoomOffsetUpdate;
        private bool _mapEnabled = true;
        private int _mapRowSpan = 1;
        private int _mapColumnSpan = 1;
        private bool _hasCompletedReplayPresentation;
        private bool _gpsClockAheadWarningActive;

        private bool IsReportingCleanup => _cleanupStageCount > 0;

        /// <summary>
        ///  Initializes a new instance of the <see cref="MainForm"/> class for the application host.
        /// </summary>
        public MainForm()
            : this(
                ResolveSensorService(),
                ResolveUserSettingsService(),
                ResolveSensorSourceFactory())
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        /// <param name="sensorService">The shared sensor service.</param>
        public MainForm(ISensorService sensorService)
            : this(sensorService, userSettings: null, sensorSourceFactory: null)
        {
        }

        /// <summary>
        ///  Initializes a new instance of the <see cref="MainForm"/> class.
        /// </summary>
        /// <param name="sensorService">The shared sensor service.</param>
        /// <param name="userSettings">The per-user settings service.</param>
        public MainForm(
            ISensorService sensorService,
            IUserSettingsService? userSettings,
            DemoSensorSourceFactory? sensorSourceFactory = null)
        {
            ArgumentNullException.ThrowIfNull(sensorService);

            _sensorService = sensorService;
            _sensorSourceFactory = sensorSourceFactory;
            _userSettings = userSettings;
            InitializeComponent();
            _statusStrip.Renderer = new DashboardStatusRenderer();
            ConfigureDashboardRendering();
            _fullScreenController = new FullScreenController(this);
            _fullScreenController.FullScreenChanged += OnFullScreenChanged;
            _userSettings?.ApplyFormPlacement(this, WindowPlacementSettingsKey);
            _windowPlacementInitialized = true;
            DashboardToolbarSize toolbarSize = _userSettings?.Get(
                ToolbarSizeSettingsKey,
                DashboardToolbarSize.Medium)
                ?? DashboardToolbarSize.Medium;
            _options = _options with
            {
                ToolbarSize = Enum.IsDefined(toolbarSize)
                    ? toolbarSize
                    : DashboardToolbarSize.Medium,
                MapZoomOffset = Math.Clamp(
                    _userSettings?.Get(MapZoomOffsetSettingsKey, 0) ?? 0,
                    SensorRouteMapControl.MinimumZoomOffset,
                    SensorRouteMapControl.MaximumZoomOffset),
                ShowMapDebugInformation = _userSettings?.Get(MapDebugSettingsKey, false) ?? false,
                AutoRecording = _userSettings?.Get(AutoRecordingSettingsKey, false) ?? false,
                DetectReplaySequenceFiles =
                    _userSettings?.Get(ReplaySequenceSettingsKey, true) ?? true,
                HideReplayControls =
                    _userSettings?.Get(ReplayHideControlsSettingsKey, false) ?? false,
                ReplaySpeedPercent = LoadReplaySpeed()
            };
            _options = DemoSettings.Load(_userSettings, _options);
            _mapEnabled = _userSettings?.Get(MapEnabledSettingsKey, true) ?? true;
            _mapRowSpan = Math.Clamp(_userSettings?.Get(MapRowSpanSettingsKey, 1) ?? 1, 1, 2);
            _mapColumnSpan = Math.Clamp(
                _userSettings?.Get(MapColumnSpanSettingsKey, 1) ?? 1,
                1,
                2);
            SetupCommands();
            SetupObd();
            InitializeSensorSourcePresentation();
            _toolStrip.CarMode = true;
            SetupReplayStatus();
            SetupBottomArea();
            _lastExceptionButton.Click += OnLastExceptionCommand;
            _statusStrip.SizeChanged += OnStatusStripSizeChanged;
            Disposed += OnMainFormDisposed;
            ApplyDisplayOptions();
            UpdateTabletFrameStatus();
            UpdateCommandStates();
            UpdateTelemetryStatus();

            _readingsToolTip.SetToolTip(
                _accelerationGauge,
                "Calibrated vehicle acceleration with gravity removed: forward, right, down; displayed in g.");
            _readingsToolTip.SetToolTip(
                _mapZoomTrackBar,
                "Zooms relative to the automatic route fit: two ticks out, four ticks in.");

            _log.Information("WARP sensors dashboard ready.");
        }

        /// <summary>
        ///  Moves snapshot-safe gauges to the process-wide render thread and makes each host
        ///  consume only the newest requested frame at no more than 30 Hz.
        /// </summary>
        private void ConfigureDashboardRendering()
        {
            D2DGaugeControl[] gauges =
            [
                _speedometer,
                _artificialHorizon,
                _accelerationGauge
            ];

            foreach (D2DGaugeControl gauge in gauges)
            {
                gauge.TargetFrameRate = DashboardFrameRate;
                gauge.VSyncEnabled = false;
                gauge.RenderBackgroundFramesOnDemand = true;
                gauge.RenderMode = RenderMode.D2DSharedRenderThread;
            }
        }

        /// <summary>
        ///  Builds the tab-only telemetry area and initializes map interaction.
        /// </summary>
        private void SetupBottomArea()
        {
            _bottomTabControl.AddTab("Telemetry", _telemetryTabHost);
            _bottomTabControl.AddTab("Raw Data", _gpsMotionTabHost);
            _bottomTabControl.AddTab("Route sections", _routeSectionTabHost);
            _bottomTabControl.AddTab("Raw OBD2", _rawObdTabHost);
            _bottomTabControl.AddTab("Debug", _debugTabHost);
            _rawObdTabHost.RecordRequested += OnRawObdRecordRequested;
            _rawObdTabHost.StopRequested += OnRawObdStopRequested;
            _bottomTabControl.TabChanged += (_, _) => PresentRawObd();

            _debugTabHost.Attach(_log);
            InitializeInstrumentModes();
            ContextMenuStrip speedMenu = new(components);
            ToolStripMenuItem compareGps = new("Cycle speed source and units")
            {
                ToolTipText = "Same as double-tapping the speedometer; a single tap shows the current mode."
            };
            compareGps.Click += OnSpeedModeChangeRequested;
            speedMenu.Items.Add(compareGps);
            _speedometer.ContextMenuStrip = speedMenu;

            _mapZoomTrackBar.Value = _options.MapZoomOffset;
            _routeMap.ZoomOffset = _options.MapZoomOffset;
            _routeMap.ZoomOffsetChanged += OnRouteMapZoomOffsetChanged;
            _routeMap.Error += OnRouteMapError;
            _routeMap.MapControl.MouseDoubleClick += OnMapNotificationDoubleClick;
            _routeMap.MapControl.SizeChanged += OnNotificationMapBoundsChanged;

            ApplyMapLayout();
        }

        private void OnMapZoomChanged(object? sender, EventArgs e)
        {
            if (_suppressZoomOffsetUpdate)
            {
                return;
            }

            _routeMap.ZoomOffset = _mapZoomTrackBar.Value;
        }

        private void OnRouteMapZoomOffsetChanged(object? sender, EventArgs e)
        {
            _options = _options with { MapZoomOffset = _routeMap.ZoomOffset };
            _userSettings?.Set(MapZoomOffsetSettingsKey, _options.MapZoomOffset);

            if (_mapZoomTrackBar.Value == _routeMap.ZoomOffset)
            {
                return;
            }

            _suppressZoomOffsetUpdate = true;

            try
            {
                _mapZoomTrackBar.Value = _routeMap.ZoomOffset;
            }
            finally
            {
                _suppressZoomOffsetUpdate = false;
            }
        }

        private void OnRouteMapError(object? sender, SensorRouteMapErrorEventArgs e)
            => _log.Warning(e.Message);

        private void PersistMapLayout()
        {
            _userSettings?.Set(MapEnabledSettingsKey, _mapEnabled);
            _userSettings?.Set(MapRowSpanSettingsKey, _mapRowSpan);
            _userSettings?.Set(MapColumnSpanSettingsKey, _mapColumnSpan);
        }

        private void ApplyMapLayout()
        {
            _dashboardLayout.SuspendLayout();

            try
            {
                _gaugeLayout.Visible = false;
                _bottomPanel.Visible = false;
                _mapLayout.Visible = false;

                _dashboardLayout.SetCellPosition(_gaugeLayout, new TableLayoutPanelCellPosition(0, 0));
                _dashboardLayout.SetRowSpan(_gaugeLayout, 2);
                _dashboardLayout.SetColumnSpan(_gaugeLayout, 1);

                _dashboardLayout.SetCellPosition(
                    _bottomPanel,
                    new TableLayoutPanelCellPosition(1, _mapEnabled ? 1 : 0));
                _dashboardLayout.SetRowSpan(_bottomPanel, _mapEnabled ? 1 : 2);
                _dashboardLayout.SetColumnSpan(_bottomPanel, 1);

                _dashboardLayout.SetCellPosition(
                    _mapLayout,
                    new TableLayoutPanelCellPosition(_mapColumnSpan == 2 ? 0 : 1, 0));
                _dashboardLayout.SetRowSpan(_mapLayout, _mapRowSpan);
                _dashboardLayout.SetColumnSpan(_mapLayout, _mapColumnSpan);

                _gaugeLayout.Visible = !_mapEnabled || _mapColumnSpan == 1;
                _bottomPanel.Visible = !_mapEnabled || _mapRowSpan == 1;
                _mapLayout.Visible = _mapEnabled;

                if (_mapEnabled)
                {
                    _mapLayout.BringToFront();
                }
                else
                {
                    _bottomPanel.BringToFront();
                }
            }
            finally
            {
                _dashboardLayout.ResumeLayout(performLayout: true);
            }
        }

        /// <summary>
        ///  Gets the default recording root used by the demo.
        /// </summary>
        public static string RecordingsRootFolder
        {
            get
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string root = string.IsNullOrWhiteSpace(localAppData)
                    ? Environment.CurrentDirectory
                    : localAppData;

                return Path.Combine(root, "WARP", "Sensored", "Recordings");
            }
        }

        /// <inheritdoc/>
        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyMapLayout();
            OpenFusionLearning();
            ShowLastStatusNotification();
            if (_options.UseObd)
            {
                StartObdDiscovery();
            }
            try
            {
                StartBluetoothSensorDiscovery();
                if (_sensorSourceFactory is { } factory)
                {
                    await factory.ConfigureAsync(_options.CreateWindowsSourceOptions());
                    await factory.StartPreviewAsync();
                }
            }
            catch (Exception exception)
            {
                if (_automation is { } automation)
                {
                    automation.ReportFailure("Could not start GPS preview", exception);
                    return;
                }

                ShowError("Could not start GPS preview", exception);
            }

            await StartAutomationAsync().ConfigureAwait(true);
        }

        /// <inheritdoc/>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_shutdownCompleted
                && (_transitioning
                    || _lease is not null
                    || _recording is not null
                    || _recordingStopTask is not null
                    || _fusionSession is not null
                    || _consumerTask is not null
                    || _subscription is not null
                    || _receiverRestoreWarning is not null
                    || _terminalSourceStopFailure is not null
                    || _obdSource is not null
                    || _bluetoothSensorSource is not null
                    || _sensorSourceFactory is not null))
            {
                e.Cancel = true;
                _statusNotification?.Dismiss();

                if (!_shutdownStarted)
                {
                    _shutdownStarted = true;
                    _statusTimer.Stop();
                    _ = CompleteShutdownAsync();
                }

                return;
            }

            _shutdownStarted = true;
            _statusNotification?.Dismiss();
            base.OnFormClosing(e);
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _automation?.Dispose();
            SetShutdownStage("settings", "Saving window placement and settings.");
            _fullScreenController.ExitFullScreen();
            PersistWindowPlacement();
            _userSettings?.Flush();
            SetShutdownStage("ui", "Closing auxiliary windows and releasing UI resources.");
            _statusTimer.Stop();
            CloseExceptionPopup(this, EventArgs.Empty);
            _fullScreenController.FullScreenChanged -= OnFullScreenChanged;
            _fullScreenController.Dispose();
            CrashGuard.Current?.SetStage("shutdown-form-closed");
            SetShutdownStage("closed", "Shutdown completed.");
            _log.Detach();
            base.OnFormClosed(e);
        }

        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            => _statusNotification?.ProcessQuestionKey(keyData) == true
                || HandleRecordingMarkerShortcut(keyData) || _fullScreenController.ProcessKey(keyData)
                || base.ProcessCmdKey(ref msg, keyData);

        /// <inheritdoc/>
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            _statusNotification?.Reposition();
            PersistWindowPlacement();
        }

        /// <inheritdoc/>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _statusNotification?.Reposition();
            PersistWindowPlacement();
        }

        private async Task CompleteShutdownAsync()
        {
            bool close = false;

            try
            {
                _shutdownProgress = new CleanupStageProgress(
                    totalStages: 7,
                    initialStage: "Waiting for the current sensor operation.");
                SetShutdownStage("transition", "Waiting for the current sensor operation.");
                bool cleanedUp = await CleanupProgressForm.RunWithRecoveryAsync(
                    this,
                    _shutdownProgress,
                    StopAfterCurrentTransitionAsync,
                    () => _terminalSourceStopFailure is null
                        && _receiverRestoreWarning is null
                        && _recording?.Status.LastFaultMessage is null,
                    exception =>
                    {
                        _log.Error(
                            "Sensor cleanup failed",
                            exception,
                            $"Stage: {_shutdownStage}; service: {_sensorService.State}; "
                                + $"leases: {_sensorService.ActiveSessionCount}");
                        CrashGuard.Current?.RecordCleanupFailure(exception);
                    }).ConfigureAwait(true);

                if (!cleanedUp)
                {
                    _log.Warning("Force Exit: sensor cleanup is incomplete.");
                    CrashGuard? guard = CrashGuard.Current;
                    guard?.MarkIncompleteShutdown();
                    guard?.Uninstall();
                    Environment.Exit(2);
                    return;
                }

                close = true;
            }
            catch (Exception exception)
            {
                ShowError("Could not cleanly stop the sensor session", exception);
                SetStatus("Cleanup failed. The dashboard remains open; retry Exit after reviewing the error.");
            }
            finally
            {
                _shutdownProgress = null;

                if (close)
                {
                    _shutdownCompleted = true;
                    CrashGuard.Current?.SetStage("shutdown-closing");
                    Close();
                }
                else
                {
                    _shutdownStarted = false;
                    if (!IsDisposed && !Disposing)
                    {
                        _statusTimer.Start();
                    }

                    UpdateCommandStates();
                }
            }
        }

        private void SetShutdownStage(string key, string message)
        {
            _statusCleanupTask = message;
            _shutdownStage = message;

            if (_shutdownStarted)
            {
                CrashGuard.Current?.SetStage($"shutdown-{key}");
            }

            ReportShutdown(message);
        }

        private void ReportShutdown(string message)
        {
            string operation = _shutdownStarted ? "Shutdown" : "Cleanup";
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Sensored {operation}: {message}");
            _shutdownProgress?.SetStage(message);
            SetStatus(message);
            _log.Information($"{operation}: {message}");
        }

        private void ReportShutdownProgress(long started, Func<string>? describeWork)
        {
            string details = describeWork?.Invoke() ?? string.Empty;
            string stages = _shutdownProgress is { } progress
                ? $" · {progress.Snapshot().CompletedStages}/{progress.TotalStages} stages complete"
                : string.Empty;
            ReportShutdown(
                $"{_shutdownStage} {details}{stages} · {Stopwatch.GetElapsedTime(started).TotalSeconds:F1} s");
        }

        private async Task RunShutdownStageAsync(
            string key,
            string message,
            Func<Task> operation,
            Func<string>? describeWork = null)
        {
            _cleanupStageCount++;
            _statusCleanupItem++;
            _tabletFrameLabel.Available = false;
            _imuRateLabel.Available = false;
            SetShutdownStage(key, message);
            long started = Stopwatch.GetTimestamp();

            try
            {
                Task pending = operation();

                if (!pending.IsCompleted)
                {
                    ReportShutdownProgress(started, describeWork);

                    while (!pending.IsCompleted)
                    {
                        using CancellationTokenSource reportCancellation = new();
                        await Task.WhenAny(
                            pending,
                            Task.Delay(ShutdownReportInterval, reportCancellation.Token)).ConfigureAwait(true);
                        await reportCancellation.CancelAsync().ConfigureAwait(true);

                        if (!pending.IsCompleted)
                        {
                            ReportShutdownProgress(started, describeWork);
                        }
                    }
                }

                await pending.ConfigureAwait(true);

                ReportShutdown(
                    $"Completed: {message} {describeWork?.Invoke()} "
                        + $"Waited {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms.");
            }
            catch (Exception exception)
            {
                ReportShutdown(
                    $"Failed: {_shutdownStage} {describeWork?.Invoke()} "
                        + $"Waited {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F0} ms. "
                        + exception.Message);

                throw;
            }
            finally
            {
                _cleanupStageCount--;

                if (!IsReportingCleanup)
                {
                    _tabletFrameLabel.Available = false;
                    _imuRateLabel.Available = false;
                    _gpsRateLabel.Available = true;
                }
            }
        }

        private async Task StopAfterCurrentTransitionAsync()
        {
            await RunShutdownStageAsync(
                "transition",
                "Waiting for the current sensor operation.",
                WaitForCurrentTransitionAsync,
                () => _transitioning ? "A Start/Stop/Replay transition is still active." : "No transition pending.").ConfigureAwait(true);

            // An already-running Stop may have reported stages against this shutdown's tracker.
            _shutdownProgress?.Reset();

            _shutdownProgress?.CompleteStage();
            SetShutdownStage("stop", "Stopping the sensor session.");
            List<Exception> failures = [];
            foreach (Func<Task> cleanup in new Func<Task>[]
                {
                    StopSensorSessionAsync, StopObdAsync, StopBluetoothSensorsAsync, StopRawObdAsync,
                    () => _sensorSourceFactory?.DisposeAsync().AsTask() ?? Task.CompletedTask
                })
            {
                try
                {
                    await cleanup().ConfigureAwait(true);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
            if (failures.Count > 0)
            {
                throw new AggregateException("Application source shutdown was not clean.", failures);
            }
        }

        private async Task WaitForCurrentTransitionAsync()
        {
            long started = Stopwatch.GetTimestamp();

            while (_transitioning)
            {
                if (Stopwatch.GetElapsedTime(started) >= TransitionStopTimeout)
                {
                    throw new TimeoutException(
                        "The current sensor transition did not complete. Cleanup can be retried.");
                }

                await Task.Delay(25).ConfigureAwait(true);
            }

        }

        private async Task StartSensorSessionAsync(
            string? replayPath = null,
            FusionContinuity fusionContinuity = FusionContinuity.Reset,
            bool allowReceiverWrites = true)
        {
            if (_terminalSourceStopFailure is { } terminalFailure)
            {
                throw new InvalidOperationException(
                    "The previous sensor session ended with an unresolved source fault.",
                    terminalFailure);
            }

            if (_lease is not null)
            {
                return;
            }

            if (_consumerTask is not null
                || _subscription is not null
                || _recordingStopTask is { IsCompleted: false }
                || _fusionStopTask is { IsCompleted: false }
                || _gpsMotionSession is not null
                || _receiverRestoreWarning is not null)
            {
                throw new InvalidOperationException(
                    "The previous sensor session has not finished cleanup.");
            }

            if (_hasCompletedReplayPresentation)
            {
                ResetSessionPresentation();
            }

            _transitioning = true;
            _statusCleanupItem = 0;
            UpdateCommandStates();

            try
            {
                if (replayPath is null)
                {
                    _replaySequence = null;
                    _replayStoppedPosition = TimeSpan.Zero;
                    _replayStopStage = 0;
                    if (_sensorSourceFactory is { } factory)
                    {
                        await factory.ConfigureAsync(_options.CreateWindowsSourceOptions(),
                            allowReceiverWrites: allowReceiverWrites && _automation is null)
                            .ConfigureAwait(true);
                    }
                }

                _lease = await _sensorService
                    .AcquireSessionAsync(DemoOptions.CreateSensorServiceOptions())
                    .ConfigureAwait(true);
                _runningSourceOptions = replayPath is null ? _options : null;
                _replayPlayer = replayPath is null
                    ? null
                    : _sensorSourceFactory?.ActiveReplayPlayer
                        ?? throw new InvalidOperationException(
                            "The replay source did not create a replay player.");
                StartGpsMotionInput(fusionContinuity);
                await StartFusionInputAsync(replayPath, fusionContinuity).ConfigureAwait(true);
                _subscription = _sensorService.Subscribe(new SensorSubscriptionOptions
                {
                    Name = "Sensored dashboard",
                    Kinds = SensorDataKindFilter.Imu
                        | SensorDataKindFilter.Geolocation
                        | SensorDataKindFilter.Health
                        | SensorDataKindFilter.Gap
                        | SensorDataKindFilter.Classification,
                    Capacity = SubscriptionCapacity,
                    MaximumRateHz = SubscriptionMaximumRateHertz
                });
                _consumerCancellation = new CancellationTokenSource();
                Interlocked.Exchange(ref _dashboardEnvelopeCount, 0);
                _consumerTask = ConsumeAsync(_subscription, _consumerCancellation.Token);

                if (_replayPlayer is { } replayPlayer)
                {
                    _ = MonitorReplayCompletionAsync(replayPlayer, replayPath!);
                }
                else if (_options.AutoRecording)
                {
                    await StartRecordingAsync().ConfigureAwait(true);
                }

                if (replayPath is null)
                {
                    SetStatusAndLog(
                        "Running. Vehicle fusion calibrates automatically; "
                        + "acquisition, fusion and recording continue while visuals are paused.");
                }
                else
                {
                    SetStatusAndLog($"Replaying {Path.GetFileName(replayPath)} at {_options.ReplaySpeedPercent}% speed.");
                }
            }
            catch
            {
                await StopSensorSessionCoreAsync().ConfigureAwait(true);
                throw;
            }
            finally
            {
                _transitioning = false;
                UpdateCommandStates();
            }
        }

        private async Task RestartSensorSessionAsync()
        {
            await StopSensorSessionAsync().ConfigureAwait(true);
            await StartSensorSessionAsync().ConfigureAwait(true);
        }

        private async Task StopSensorSessionAsync()
        {
            _transitioning = true;
            _statusStopping = true;
            _statusCleanupItem = 0;
            UpdateCommandStates();

            try
            {
                await StopSensorSessionCoreAsync().ConfigureAwait(true);
                SetStatus("Sensor session stopped.");
            }
            finally
            {
                _statusStopping = false;
                _transitioning = false;
                UpdateCommandStates();
            }
        }

        private async Task StopSensorSessionCoreAsync(bool resetPresentation = true)
        {
            SensorReplayPlayer? replayPlayer = _replayPlayer;
            ISensorRecordingSession? recording = _recording;
            SensorFusionSession? fusionSession = _fusionSession;
            GpsMotionSession? gpsMotionSession = _gpsMotionSession;
            List<Exception> failures = [];

            try
            {
                await RunShutdownStageAsync(
                    "recording",
                    "Finalizing the recording.",
                    StopRecordingAsync,
                    () => recording is null
                        ? "No recording open."
                        : DescribeRecordingCleanup(recording.Status)).ConfigureAwait(true);
                _shutdownProgress?.CompleteStage();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            long sourceStopStarted = Stopwatch.GetTimestamp();

            try
            {
                await RunShutdownStageAsync(
                    "sources",
                    "Releasing sensor sources and restoring the GPS receiver.",
                    StopFusionInputAsync,
                    () => DescribeSourceCleanup(fusionSession)).ConfigureAwait(true);
                _shutdownProgress?.CompleteStage();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            finally
            {
                string sourceKind = replayPlayer is not null
                    ? "Replay"
                    : (_runningSourceOptions ?? _options).SourceKind.ToString();
                TimeSpan elapsed = Stopwatch.GetElapsedTime(sourceStopStarted);
                _log.Information(
                    $"Source stop: {sourceKind}; elapsed {elapsed}; "
                        + $"service {_sensorService.State}; leases {_sensorService.ActiveSessionCount}.");
                CrashGuard.Current?.RecordCleanupSourceStop(
                    sourceKind,
                    elapsed,
                    _sensorService.State.ToString(),
                    _sensorService.ActiveSessionCount);
            }

            try
            {
                await RunShutdownStageAsync(
                    "gps-motion",
                    "Draining independent GPS speed and direction calculations.",
                    StopGpsMotionInputAsync,
                    () => gpsMotionSession is null
                        ? "No GPS motion reader open."
                        : $"GPS motion: {gpsMotionSession.ProcessedCount:N0} fixes processed"
                            + $", {_gpsMotionTracker.Latest.PointCount} positions retained.").ConfigureAwait(true);
                _shutdownProgress?.CompleteStage();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            try
            {
                await RunShutdownStageAsync(
                    "fusion",
                    "Finishing vehicle fusion.",
                    () => CompleteFusionAsync(),
                    () => DescribeFusionCleanup(fusionSession)).ConfigureAwait(true);
                _shutdownProgress?.CompleteStage();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            try
            {
                await RunShutdownStageAsync(
                    "dashboard",
                    "Draining dashboard updates.",
                    () => StopDashboardConsumerAsync(replayPlayer, resetPresentation),
                    () => $"Dashboard: {Interlocked.Read(ref _dashboardEnvelopeCount):N0} envelopes consumed"
                        + (_consumerTask is { IsCompleted: false } ? "; reader still running." : "; reader stopped.")).ConfigureAwait(true);
                _shutdownProgress?.CompleteStage();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }

            SetShutdownStage("verify", "Verifying sensor cleanup and receiver restoration.");

            if (_terminalSourceStopFailure is { } terminalFailure
                && !failures.Contains(terminalFailure))
            {
                failures.Add(terminalFailure);
            }

            if (_sensorService.Faults.SourceException is { } sourceFault
                && IsUnverifiedReceiverRestore(sourceFault))
            {
                _receiverRestoreWarning = sourceFault;
                failures.Add(new InvalidOperationException(
                    "The original GPS receiver settings were not verified restored. "
                        + "Inspect the receiver before another session or confirm Force Exit.",
                    sourceFault));
            }

            if (failures.Count == 1)
            {
                ExceptionDispatchInfo.Capture(failures[0]).Throw();
            }

            if (failures.Count > 1)
            {
                throw new AggregateException("Sensor cleanup did not complete.", failures);
            }

            _replayPlayer = null;
            _shutdownProgress?.CompleteStage();
        }

        private static string DescribeRecordingCleanup(SensorRecordingStatus status)
            => $"Recording: {status.RowsWritten:N0} rows written, {status.FinalizedParts.Count:N0} parts finalized"
                + $", {status.RowsDropped:N0} rows dropped.";

        private string DescribeSourceCleanup(SensorFusionSession? fusionSession)
        {
            SensorServiceStatistics statistics = _sensorService.State == SensorServiceState.Stopped
                && _sensorService.LastCompletedSession is { } completed
                    ? completed.Statistics
                    : _sensorService.Statistics;

            return $"Sources: {_sensorService.State}, {_sensorService.ActiveSessionCount} leases"
                + $", {statistics.Processed:N0} samples processed. {DescribeFusionCleanup(fusionSession)}";
        }

        private static string DescribeFusionCleanup(SensorFusionSession? session)
            => session is null
                ? "No fusion session open."
                : $"Fusion: {session.InputCount:N0} inputs, {session.OutputCount:N0} estimates."
                    + (session.TracePath is null
                        ? string.Empty
                        : $" Trace: {session.TraceEntriesWritten:N0} entries written"
                            + $", {session.PendingTraceEntries:N0} queued (excludes current write).");

        /// <summary>Recognizes a receiver stop whose original configuration was not verified restored.</summary>
        /// <param name="exception">The source fault reported by the stopped session.</param>
        /// <returns>Whether volatile receiver restoration remains unverified.</returns>
        internal static bool IsUnverifiedReceiverRestore(Exception exception)
            => exception.ToString().Contains(
                "NOT VERIFIED RESTORED",
                StringComparison.OrdinalIgnoreCase)
                || exception.ToString().Contains(
                    "restoration is unverified",
                    StringComparison.OrdinalIgnoreCase);

        private async Task StopDashboardConsumerAsync(
            SensorReplayPlayer? replayPlayer,
            bool resetPresentation)
        {
            bool canDrain = _lease is null && _sensorService.ActiveSessionCount == 0;

            if (!canDrain && _consumerCancellation is { } cancellation)
            {
                SetShutdownStage("dashboard-cancel", "Canceling the dashboard reader.");
                await cancellation.CancelAsync().ConfigureAwait(true);
            }

            if (!canDrain && _subscription is { } subscription)
            {
                await subscription.DisposeAsync().ConfigureAwait(true);
                _subscription = null;
            }

            if (_consumerTask is { } consumerTask)
            {
                SetShutdownStage("dashboard-drain", "Waiting for dashboard updates to finish.");
                try
                {
                    await consumerTask.WaitAsync(ConsumerStopTimeout).ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                }

                _consumerTask = null;
            }

            if (_subscription is { } remainingSubscription)
            {
                SetShutdownStage("dashboard-detach", "Detaching the dashboard subscription.");
                await remainingSubscription.DisposeAsync().ConfigureAwait(true);
                _subscription = null;
            }

            _consumerCancellation?.Dispose();
            _consumerCancellation = null;

            if (resetPresentation && !_shutdownStarted && _sensorSourceFactory is { IsReplay: true } factory)
            {
                await factory.ConfigureAsync(_options.CreateWindowsSourceOptions()).ConfigureAwait(true);
            }

            if (resetPresentation)
            {
                SetShutdownStage("dashboard-reset", "Resetting gauges, telemetry, and the route map.");
                ResetSessionPresentation();
            }

            _runningSourceOptions = null;
        }

        private async Task StartReplayAsync(string path)
        {
            if (!ConfirmGpsApply(_options with { SerialGnssProfile = SerialGnssProfile.Passive }))
            {
                return;
            }
            if (_sensorSourceFactory is null)
            {
                throw new InvalidOperationException(
                    "The dashboard replay source factory is not available.");
            }

            SetStatus("Opening recording…");
            await Task.Yield();
            SetStatus("Scanning replay parts…");
            Progress<SensorReplayDiscoveryProgress> progress = new(report =>
            {
                SetStatus(
                    $"Scanning {Path.GetFileName(report.Path)} ({report.Index} of {report.Count})…");
            });
            SensorReplaySequence sequence = await SensorReplaySequenceDetector
                .DiscoverAsync(
                    path,
                    _options.DetectReplaySequenceFiles,
                    progress: progress)
                .ConfigureAwait(true);
            SetStatus("Starting replay…");
            _replaySequence = sequence;
            _replayPartIndex = sequence.SelectedIndex;
            _replayInitialPartIndex = sequence.SelectedIndex;
            _replayStoppedPosition = TimeSpan.Zero;
            _replayStopStage = 0;

            if (sequence.Warning is { } warning)
            {
                _log.Warning($"Replay sequence stopped during discovery: {warning}");
            }

            await StartReplayPartAsync(
                sequence.SelectedIndex,
                TimeSpan.Zero,
                pauseAfterStart: false,
                preservePresentation: false).ConfigureAwait(true);
        }

        private async Task MonitorReplayCompletionAsync(
            SensorReplayPlayer player,
            string path)
        {
            try
            {
                await player.Completion.ConfigureAwait(true);

                if (player.ReachedEndOfRecording
                    && ReferenceEquals(_replayPlayer, player)
                    && !_shutdownStarted)
                {
                    await CompleteReplayAsync(player, path).ConfigureAwait(true);
                }
            }
            catch (Exception exception)
            {
                if (!ReferenceEquals(_replayPlayer, player) || _shutdownStarted)
                {
                    return;
                }

                try
                {
                    await StopSensorSessionAsync().ConfigureAwait(true);
                }
                catch (Exception stopException)
                {
                    _log.Error("Could not stop the failed replay session", stopException);
                }

                if (_sensorSourceFactory is { } factory)
                {
                    await factory.ConfigureAsync(_options.CreateWindowsSourceOptions()).ConfigureAwait(true);
                }
                ResetSessionPresentation();
                ShowError($"Could not replay {Path.GetFileName(path)}", exception);
            }
        }

        private async Task CompleteReplayAsync(
            SensorReplayPlayer player,
            string path)
        {
            if (!ReferenceEquals(_replayPlayer, player))
            {
                return;
            }

            int? nextPartIndex = null;
            GpsMotionSession? gpsMotion = _gpsMotionSession;
            double replayRate = _options.ReplaySpeedPercent / 100d;
            bool finalGpsFresh = gpsMotion?.HasFreshFix(replayRate) == true;
            _transitioning = true;
            UpdateCommandStates();

            try
            {
                await StopFusionInputAsync().ConfigureAwait(true);
                await StopGpsMotionInputAsync().ConfigureAwait(true);
                finalGpsFresh |= gpsMotion?.HasFreshFix(replayRate) == true;

                if (_subscription is { } subscription)
                {
                    await subscription.DisposeAsync().ConfigureAwait(true);
                    _subscription = null;
                }

                if (_consumerTask is { } consumerTask)
                {
                    await consumerTask.WaitAsync(ConsumerStopTimeout).ConfigureAwait(true);
                    _consumerTask = null;
                }

                _consumerCancellation?.Dispose();
                _consumerCancellation = null;
                _paused = false;
                ResumeVisuals();
                _replayPlayer = null;
                _replayStoppedPosition = player.Duration;

                if (_options.DetectReplaySequenceFiles
                    && _replaySequence is { } sequence
                    && _replayPartIndex + 1 < sequence.Parts.Count)
                {
                    nextPartIndex = _replayPartIndex + 1;
                    SetStatusAndLog(
                        $"Replay part completed: {Path.GetFileName(path)}. Continuing with "
                        + $"{Path.GetFileName(sequence.Parts[nextPartIndex.Value].Path)}.");
                }
                else
                {
                    await CompleteFusionAsync(complete: true).ConfigureAwait(true);
                    _hasCompletedReplayPresentation = true;
                    _completedReplayGpsFresh = finalGpsFresh;
                    ApplyGpsMotionReadouts();
                    _replayStopStage = 2;
                    _replayStoppedPosition = TimeSpan.Zero;
                    SetStatusAndLog($"Replay completed: {Path.GetFileName(path)}");
                }
            }
            finally
            {
                _transitioning = false;
                UpdateCommandStates();
            }

            if (nextPartIndex is { } next)
            {
                await StartReplayPartAsync(
                    next,
                    TimeSpan.Zero,
                    pauseAfterStart: false,
                    preservePresentation: true,
                    fusionContinuity: FusionContinuity.Continue).ConfigureAwait(true);
            }
        }

        private async Task ConsumeAsync(
            ISensorSubscription subscription,
            CancellationToken cancellationToken)
        {
            try
            {
                await DashboardUpdateConsumer.RunAsync(
                    subscription.ReadAllAsync(cancellationToken),
                    TryApplyDashboardUpdateAsync,
                    envelope =>
                    {
                        _sensorRates.Record(envelope.Kind, DateTimeOffset.UtcNow);
                        Interlocked.Increment(ref _dashboardEnvelopeCount);
                    },
                    UiUpdateInterval,
                    DelayWithoutCancellation,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                await TryShowConsumerFailureAsync(exception).ConfigureAwait(false);
            }
        }

        private async Task<bool> TryApplyDashboardUpdateAsync(
            DashboardUpdateBatch batch,
            CancellationToken cancellationToken)
        {
            if (_shutdownStarted || IsDisposed || Disposing || !IsHandleCreated)
            {
                return false;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                await InvokeAsync(
                    () => ApplyDashboardUpdate(batch),
                    cancellationToken).ConfigureAwait(false);

                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
            {
                return false;
            }
        }

        private async Task TryShowConsumerFailureAsync(Exception exception)
        {
            if (_shutdownStarted || IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            try
            {
                await InvokeAsync(() =>
                {
                    SetStatus($"Dashboard updates stopped: {exception.Message}");
                    ShowError("Dashboard updates failed", exception);
                }).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        private void ApplyDashboardUpdate(DashboardUpdateBatch batch)
        {
            if (_shutdownStarted || IsDisposed || Disposing)
            {
                return;
            }

            if (batch.LatestImu is { } imu)
            {
                _lastRawImu = imu;
                _lastRawImuReceivedAt = DateTimeOffset.UtcNow;
                if (!_paused)
                {
                    _gpsMotionTabHost.ApplyRawImu(imu);
                }
            }

            List<SensorGeoPoint> routePoints = [with(batch.Geolocations.Count)];

            foreach (GeolocationPayload geolocation in batch.Geolocations)
            {
                if (_paused)
                {
                    _pausedGpsPoints.Enqueue(geolocation);
                }
                else
                {
                    routePoints.Add(ApplyGeolocation(geolocation));
                }
            }

            _routeMap.AddRoutePoints(routePoints);

            if (batch.LatestHealth is { } health)
            {
                _lastHealth = health;
            }

            _gapCount += batch.DroppedCount;

            foreach (RoadSectionClassificationResult classification in batch.Classifications)
            {
                if (_paused)
                {
                    _pausedClassifications.Enqueue(classification);
                }
                else
                {
                    AppendClassification(classification);
                }
            }

            if (!_paused && (batch.LatestHealth is not null || batch.DroppedCount != 0))
            {
                UpdateServiceStatus();
            }

            ApplyLatestFusionEstimate(batch.LatestImu is not null);
        }

        private SensorGeoPoint ApplyGeolocation(GeolocationPayload fix)
        {
            if (_replayPlayer is null
                && _sensorSourceFactory?.ActiveSerialSource is not null
                && _fusionEngine is IFusionTimingPolicy timing)
            {
                double aheadSeconds = (fix.CoordinateTimestamp - DateTimeOffset.UtcNow).TotalSeconds;
                bool clockAhead = aheadSeconds > timing.MaximumGpsFutureOffsetSeconds;

                if (clockAhead && !_gpsClockAheadWarningActive)
                {
                    _log.Warning(string.Create(
                        CultureInfo.CurrentCulture,
                        $"GPS measurement clock is {aheadSeconds:F2} s ahead of the PC clock. ")
                            + "Fusion rejects these fixes as gps-clock-ahead; check Windows time synchronization.");
                }
                else if (!clockAhead && _gpsClockAheadWarningActive)
                {
                    _log.Information("GPS measurement clock is back within the fusion pending-fix limit.");
                }

                _gpsClockAheadWarningActive = clockAhead;
            }

            double altitudeMeters = double.IsFinite(fix.AltitudeMeters)
                ? fix.AltitudeMeters
                : double.IsFinite(_speedometer.CurrentAltitudeMeters)
                    ? _speedometer.CurrentAltitudeMeters
                    : 0d;
            SensorGeoPoint point = new(
                fix.CoordinateTimestamp,
                fix.LatitudeDegrees,
                fix.LongitudeDegrees,
                altitudeMeters);

            _telemetryTabHost.ApplyGeolocation(fix);

            _speedometer.AddGpsPoint(point, deriveSpeed: false);
            _artificialHorizon.SetAltitudeTrace(_speedometer.CaptureAltitudeTrace());
            UpdateServiceStatus();

            return point;
        }

        private void ResumeVisuals()
        {
            _displayedFusionSequence = -1;
            _gpsMotionTabHost.ApplyRawImu(_lastRawImu);

            List<SensorGeoPoint> routePoints = [with(_pausedGpsPoints.Count)];

            while (_pausedGpsPoints.TryDequeue(out GeolocationPayload fix))
            {
                routePoints.Add(ApplyGeolocation(fix));
            }

            _routeMap.AddRoutePoints(routePoints);

            while (_pausedClassifications.TryDequeue(
                out RoadSectionClassificationResult classification))
            {
                AppendClassification(classification);
            }

            UpdateServiceStatus();
            ApplyLatestFusionEstimate();
        }

        private void ApplyDisplayOptions()
        {
            PresentObd();
            double damping = _options.DampingMilliseconds;
            _speedometer.DampingMilliseconds = damping;
            _speedometer.AccelerationDamping = _options.AccelerationDamping;
            _speedometer.Rings.Apply(_options.Speedometer);
            _artificialHorizon.DampingMilliseconds = damping;
            _accelerationGauge.DampingMilliseconds = damping;
            _compassGauge.DampingMilliseconds = damping;
            _accelerationGauge.VisualRangeG = _options.GScale;
            _telemetryTabHost.ApplyReadingFormats(_options.ReadingFormats);
            _gpsMotionTabHost.ApplyReadingFormats(_options.ReadingFormats);
            ApplyToolbarAppearance();

            // The debug overlay is independent of the route and the follow mode, so it must not sit
            // behind the route-reload guard below.
            if (_routeMap.ShowDebugInformation != _options.ShowMapDebugInformation)
            {
                _routeMap.ShowDebugInformation = _options.ShowMapDebugInformation;
                _userSettings?.Set(MapDebugSettingsKey, _options.ShowMapDebugInformation);
                _log.Information(_options.ShowMapDebugInformation
                    ? "Map rendering debug information enabled."
                    : "Map rendering debug information disabled.");
            }

            if (!string.Equals(
                    _appliedRoutePath,
                    _options.RouteFilePath,
                    StringComparison.OrdinalIgnoreCase)
                || _appliedFollowCurrentPosition != _options.FollowCurrentPosition)
            {
                ConfigureRouteMap();
                _appliedRoutePath = _options.RouteFilePath;
                _appliedFollowCurrentPosition = _options.FollowCurrentPosition;
            }

            if (_ownedBaseFont is null
                || Math.Abs(_ownedBaseFont.SizeInPoints - _options.BaseFontPointSize) > 0.01f)
            {
                ApplyBaseFontSize(_options.BaseFontPointSize);
            }
        }

        private void ApplyBaseFontSize(float pointSize)
        {
            Font newFont = new(Font.FontFamily, pointSize, Font.Style, GraphicsUnit.Point);
            Font? oldFont = _ownedBaseFont;
            _ownedBaseFont = newFont;
            Font = newFont;
            oldFont?.Dispose();

            Font newStatusFont = new(
                newFont.FontFamily,
                newFont.SizeInPoints + 2f,
                newFont.Style,
                GraphicsUnit.Point);
            Font? oldStatusFont = _ownedStatusFont;
            _ownedStatusFont = newStatusFont;
            _statusStrip.Font = newStatusFont;
            oldStatusFont?.Dispose();

            Font newTelemetryFont = SensorTelemetryAppearance.CreateFont(newFont);
            Font? oldTelemetryFont = _ownedTelemetryFont;
            _ownedTelemetryFont = newTelemetryFont;

            _telemetryTabHost.ApplyTelemetryFont(newTelemetryFont);
            _gpsMotionTabHost.Font = newTelemetryFont;
            oldTelemetryFont?.Dispose();
        }

        private void ResetDashboardPresentation()
        {
            ResetPerformanceTiming();
            _speedometer.Reset();
            _artificialHorizon.ResetPresentation();
            _accelerationGauge.ResetPresentation();
            _compassGauge.ResetPresentation();
            _routeMap.ResetRoute();
            _routeSectionTabHost.Clear();
            _sectionOdometerKilometers = 0d;
            _appliedRoutePath = null;
            _appliedFollowCurrentPosition = null;
            ApplyDisplayOptions();
        }

        /// <summary>
        ///  Appends one classified route section to the list.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   The classifier reports rows and timestamps but knows nothing about distance, so the
        ///   demo keeps its own odometer: each section starts where its predecessor ended and ends
        ///   at the speedometer's current trip distance.
        ///  </para>
        /// </remarks>
        /// <param name="classification">The classified section.</param>
        private void AppendClassification(RoadSectionClassificationResult classification)
        {
            double startKilometers = _sectionOdometerKilometers;
            double endKilometers = Math.Max(startKilometers, _speedometer.TripKilometers);
            _sectionOdometerKilometers = endKilometers;

            _routeSectionTabHost.Append(new RoadSectionClassificationEntry(
                classification.Classification,
                RoadSectionClassificationDescriptions.Get(classification.Classification),
                startKilometers,
                endKilometers,
                classification.StartTimestamp,
                classification.EndTimestamp,
                classification.Evidence.SampleCount));

            _log.Information(string.Create(
                CultureInfo.CurrentCulture,
                $"Route section {classification.Abbreviation}: {classification.Description}, "
                + $"{startKilometers:F2} → {endKilometers:F2} km, "
                + $"{classification.Evidence.SampleCount} fixes, "
                + $"confidence {classification.Confidence:P0}."));
        }

        private void ConfigureRouteMap()
        {
            _routeMap.FollowMarker = _options.FollowCurrentPosition;
            _routeMap.ShowDebugInformation = _options.ShowMapDebugInformation;

            if (string.IsNullOrWhiteSpace(_options.RouteFilePath))
            {
                return;
            }

            IReadOnlyList<SensorGeoPoint> points =
                SensorRouteFileReader.Read(_options.RouteFilePath);
            _routeMap.ResetRoute();
            _routeMap.AddRoutePoints(points);
            _routeMap.FitRoute();
        }

        private void ResetTelemetryValues()
            => _telemetryTabHost.ResetValues();

        private void ResetSessionPresentation()
        {
            _rawAuxiliaryFaulted = false;
            _rawAuxiliaryDiscovered = false;
            _rawMagnetometer = null;
            _rawAltimeter = null;
            _gpsClockAheadWarningActive = false;
            _paused = false;
            _pausedGpsPoints.Clear();
            _pausedClassifications.Clear();
            _lastFusionEstimate = null;
            _lastRawImu = null;
            lock (_horizonGate)
            {
                _horizonSources.Reset();
                _horizonInternalSample = null;
                _horizonInternalReceivedAt = default;
            }
            Volatile.Write(ref _presentedComparison, null);
            Interlocked.Exchange(ref _obdReplayTimeTicks, 0);
            _presentedHorizon = null;
            _gpsMotionTracker.Reset();
            _displayedGpsMotion = null;
            _displayedGpsFusion = null;
            _displayedGpsFresh = false;
            _completedReplayGpsFresh = false;
            _gpsMotionTabHost.ApplySnapshot(GpsMotionSnapshot.Empty, false, double.NaN, double.NaN);
            _gpsMotionTabHost.ApplyRawImu(null);
            _gpsMotionTabHost.ApplyAuxiliaryReadings(double.NaN, double.NaN, double.NaN, double.NaN);
            _displayedFusionSequence = -1;
            _lastHealth = null;
            _gapCount = 0;
            _sensorRates.Reset();
            ResetTelemetryValues();
            ResetDashboardPresentation();
            UpdateTelemetryStatus();
            _hasCompletedReplayPresentation = false;
        }

        private SensorRecordingRequest CreateRecordingRequest()
        {
            return new()
            {
                RootFolderPath = _options.RecordingRootFolder,
                Formats = SensorRecordingFormats.CompactJson
                    | (_options.RecordingFormats & SensorRecordingFormats.VerboseJsonLines),
                Kinds = SensorDataKindFilter.All,
                NamePrefix = "drive"
            };
        }

        private async Task StartRecordingAsync()
        {
            _recording = await _sensorService
                .StartRecordingAsync(CreateRecordingRequest())
                .ConfigureAwait(true);
            try
            {
                lock (_obdRecordingGate)
                {
                    _v2Recording = new(_recording, _options.RawSnapshotsPerSecond, gpsFallback: true);
                    foreach (FusionObservation observation in _recentV2Inputs)
                    {
                        _v2Recording.Publish(observation, seed: _recording.Coverage is not { } coverage
                            || observation.Input.Sequence < coverage.FirstSequence);
                    }
                }
            }
            catch
            {
                await _recording.StopAsync().ConfigureAwait(true);
                _recording = null;
                throw;
            }
            DemoRecordingRegistry.Set(_recording);
            using (File.Create(Path.Combine(_recording.FolderPath, ObdRecordingCompanion.FileName + ".pending")))
            {
            }

            try
            {
                BeginDrivingTaskRecording();
            }
            catch (IOException exception)
            {
                ShowError("Driving task marker failed; the independent journal is still recording", exception);
            }

            SetStatusAndLog($"Recording to {_recording.FolderPath}");
        }

        private async Task StopRecordingAsync()
        {
            if (_recording is not { } recording)
            {
                return;
            }

            List<Exception> failures = [];
            try
            {
                EndDrivingTaskRecording();
            }
            catch (IOException exception)
            {
                failures.Add(exception);
            }

            try
            {
                if (_v2Recording is { } pair)
                {
                    await pair.StopSnapshotsAsync().ConfigureAwait(true);
                }
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            Task stopTask = _recordingStopTask ??= recording.StopAsync();

            try
            {
                await stopTask.WaitAsync(RecordingStopTimeout).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            finally
            {
                if (stopTask.IsCompletedSuccessfully)
                {
                    _recordingStopTask = null;
                }
            }

            if (recording.Status.LastFaultMessage is { } fault)
            {
                failures.Add(new IOException($"The recording did not finalize cleanly: {fault}"));
            }

            try
            {
                await CompleteV2RecordingAsync(recording, journalVerified: failures.Count == 0).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            try
            {
                await CompleteObdRecordingAsync(recording).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            if (stopTask.IsCompleted)
            {
                _recording = null;
                DemoRecordingRegistry.Set(null);
            }
            if (failures.Count > 0)
            {
                throw new AggregateException("Recording finalization was incomplete; independent artifacts were retained.", failures);
            }
            SetStatusAndLog($"Recording finalized in {recording.FolderPath}");
        }

        private void OnMainFormDisposed(object? sender, EventArgs e)
        {
            Disposed -= OnMainFormDisposed;
            _exceptionPopup?.Dispose();
            _exceptionPopup = null;
            _statusNotification?.Dispose();
            _statusNotification = null;
            _ownedStatusFont?.Dispose();
            _ownedStatusFont = null;
            _ownedTelemetryFont?.Dispose();
            _ownedTelemetryFont = null;
            _ownedBaseFont?.Dispose();
            _ownedBaseFont = null;
            _fusionLearning?.Dispose();
            _fusionLearning = null;
        }

        private void UpdateServiceStatus()
        {
            if (PresentV2RecordingFault())
            {
                return;
            }
            if (_paused || IsReportingCleanup)
            {
                return;
            }

            SensorServiceStatistics statistics = _sensorService.Statistics;
            string health = _lastHealth is { } value
                ? $"IMU {value.ImuSampleCount:N0} · GPS {value.AcceptedFixCount:N0} · "
                    + $"GPS state {value.GpsAcquisitionStatus}"
                : "Waiting for sensor health";
            string recording = _recording is { } active
                ? $" · recording {active.Status.RowsWritten:N0} rows"
                : string.Empty;
            SetStatus(
                $"{health} · gaps {_gapCount:N0} · published {statistics.Sequence:N0}{recording}",
                notify: false);
        }

        private void SetStatus(string text, bool notify = true)
        {
            _statusDetail = text;
            RefreshStructuredStatus();
            if (notify)
            {
                NotifyStatusChange(text);
            }
        }

        /// <summary>
        ///  Waits without passing the token to <see cref="Task.Delay(int, CancellationToken)"/>,
        ///  so shutdown does not throw <see cref="TaskCanceledException"/>.
        /// </summary>
        private static async Task DelayWithoutCancellation(
            TimeSpan delay,
            CancellationToken cancellationToken)
        {
            long started = Stopwatch.GetTimestamp();

            while (Stopwatch.GetElapsedTime(started) < delay)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                TimeSpan remaining = delay - Stopwatch.GetElapsedTime(started);
                int sliceMilliseconds = (int)Math.Clamp(remaining.TotalMilliseconds, 1d, 15d);
                await Task.Delay(
                    sliceMilliseconds,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///  Sets the status text and records it in the debug log.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   The plain <see cref="SetStatus(string, bool)"/> is called on every status tick, so only
        ///   deliberate state changes use this overload; otherwise the log would drown in repeated
        ///   throughput lines.
        ///  </para>
        /// </remarks>
        /// <param name="text">The status text.</param>
        private void SetStatusAndLog(string text)
        {
            SetStatus(text);
            _log.Information(text);
        }

        private void ShowError(
            string operation,
            Exception exception,
            string? operationContext = null)
        {
            string diagnosticContext = BuildErrorDiagnosticContext(operationContext);
            _exceptionHistory.Add(operation, exception, diagnosticContext);
            _log.Error(operation, exception, diagnosticContext);
            UpdateTelemetryStatus();
            SetStatus($"{operation}: {exception.Message}");
            if (_automation is { } automation)
            {
                automation.RecordError(operation, exception);
                return;
            }

            MessageBox.Show(
                this,
                BuildErrorMessage(operation, exception, diagnosticContext),
                operation,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private string BuildErrorDiagnosticContext(string? operationContext)
        {
            string replayFile = CurrentReplayPart?.Path ?? "none";
            string sourceConfiguration = _sensorSourceFactory?.ConfigurationKey ?? "external";
            string suppliedContext = string.IsNullOrWhiteSpace(operationContext)
                ? "none"
                : operationContext;

            return
                $"""
                Occurred (UTC): {DateTimeOffset.UtcNow:O}
                Operation context: {suppliedContext}
                Sensor session active: {_lease is not null}
                Transition in progress: {_transitioning}
                Replay file: {replayFile}
                Replay part index: {_replayPartIndex}
                Replay position: {_replayPlayer?.Position ?? _replayStoppedPosition}
                Source configuration: {sourceConfiguration}
                """;
        }

        private static string BuildErrorMessage(
            string operation,
            Exception exception,
            string diagnosticContext)
            =>
                $"""
                Operation
                {operation}

                Reason
                {exception.Message}

                Exception
                {exception.GetType().FullName} (HRESULT 0x{exception.HResult:X8})

                Context
                {diagnosticContext}

                The full exception and stack trace were recorded in Last exception and the Debug log.
                """;

        private void OnStatusTimerTick(object? sender, EventArgs e)
        {
            if (_shutdownStarted || IsReportingCleanup || IsDisposed || Disposing)
            {
                return;
            }

            ApplyLatestFusionEstimate();
            ObserveServiceFaults();
            UpdateTabletFrameStatus();
            UpdateTelemetryStatus();

            if (_lease is not null)
            {
                UpdateServiceStatus();
            }

            UpdateReplayStatus();
            PresentRawObd();
        }

        private void ObserveServiceFaults()
        {
            SensorServiceFaults faults = _sensorService.Faults;
            ObserveServiceFault(
                "Sensor source",
                faults.SourceException,
                ref _observedSourceException);
            ObserveServiceFault(
                "Sensor processing thread",
                faults.ProcessingException,
                ref _observedProcessingException);
            ObserveServiceFault(
                "Sensor recording",
                faults.RecordingException,
                ref _observedRecordingException);
        }

        private void ObserveServiceFault(
            string context,
            Exception? exception,
            ref Exception? observedException)
        {
            if (exception is null || ReferenceEquals(exception, observedException))
            {
                return;
            }

            observedException = exception;
            _exceptionHistory.Add(context, exception);
        }

        private void UpdateTelemetryStatus()
        {
            RefreshStructuredStatus();

            ExceptionHistoryEntry? latest = _exceptionHistory.Latest;
            _lastExceptionButton.Enabled = true;
            _lastExceptionButton.AccessibleName = latest is null ? "No exceptions recorded" : "Show last exception";
            _lastExceptionButton.Text = latest is null ? string.Empty : Truncate(latest.Message, 48);
            _lastExceptionButton.ForeColor = StatusColor(latest is null
                ? WarpToolkit.Sensors.Fusion.TabletFrameQuality.Green
                : WarpToolkit.Sensors.Fusion.TabletFrameQuality.Red);
            _lastExceptionButton.Symbol = latest is null
                ? SensorDemoSymbols.StatusOk : SensorDemoSymbols.StatusException;
            _lastExceptionButton.ToolTipText = latest is null
                ? "No exception has been recorded."
                : $"{latest.Timestamp:G} · {latest.Context}: {latest.Message}";
            FitStatusFields();
        }

        private void OnLastExceptionCommand(object? sender, EventArgs e)
            => ShowLastExceptionPopup();

        private static string Truncate(string value, int maximumLength)
            => value.Length <= maximumLength
                ? value
                : $"{value[..(maximumLength - 1)]}…";

        private static ISensorService ResolveSensorService()
        {
            return Microsoft.Extensions.WinForms.WinFormsApplication.Services?
                .GetService(typeof(ISensorService)) as ISensorService
                ?? new SensorService(new WindowsSensorSourceFactory());
        }

        private static DemoSensorSourceFactory? ResolveSensorSourceFactory()
            => Microsoft.Extensions.WinForms.WinFormsApplication.Services?
                .GetService(typeof(DemoSensorSourceFactory)) as DemoSensorSourceFactory;

        private static IUserSettingsService ResolveUserSettingsService()
        {
            return Microsoft.Extensions.WinForms.WinFormsApplication.Services?
                .GetService(typeof(IUserSettingsService)) as IUserSettingsService
                ?? throw new InvalidOperationException(
                    "The user-settings service is not available.");
        }

        private void PersistWindowPlacement()
        {
            if (!_windowPlacementInitialized || _fullScreenController.IsFullScreen)
            {
                return;
            }

            _userSettings?.SaveFormPlacement(this, WindowPlacementSettingsKey);
        }
    }
}
