using System.Globalization;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.ComponentModel;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Specialized;

namespace WarpSensorsDemo
{

    /// <summary>
    ///  Presents the sensor service's raw IMU, geolocation, health, gap, and classification streams.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   This form is intentionally a thin async <see cref="ISensorService"/> subscriber. It performs
    ///   no calibration, coordinate normalization, fusion, or duplicate smoothing.
    ///  </para>
    ///  <para>
    ///   The acceleration gauge's “forward” and “lateral” values are caller-selected raw axes:
    ///   raw device X is shown as forward and raw device Y is shown as lateral. No axis correction is
    ///   applied by the demo.
    ///  </para>
    /// </remarks>
    public partial class MainForm : Form
    {
        private static readonly TimeSpan UiUpdateInterval = TimeSpan.FromMilliseconds(50);
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
        private ISensorServiceSessionLease? _lease;
        private ISensorSubscription? _subscription;
        private ISensorRecordingSession? _recording;
        private CancellationTokenSource? _consumerCancellation;
        private Task? _consumerTask;
        private RawImuPayload? _lastRawImu;
        private SensorHealthPayload? _lastHealth;
        private long _gapCount;
        private bool _paused;
        private bool _transitioning;
        private bool _shutdownStarted;
        private bool _shutdownCompleted;
        private bool _windowPlacementInitialized;
        private Font? _ownedBaseFont;
        private Font? _ownedStatusFont;
        private Font? _ownedTelemetryFont;
        private ExceptionHistoryForm? _exceptionHistoryForm;
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
                AutoRecording = _userSettings?.Get(AutoRecordingSettingsKey, false) ?? false
            };
            _mapEnabled = _userSettings?.Get(MapEnabledSettingsKey, true) ?? true;
            _mapRowSpan = Math.Clamp(_userSettings?.Get(MapRowSpanSettingsKey, 1) ?? 1, 1, 2);
            _mapColumnSpan = Math.Clamp(
                _userSettings?.Get(MapColumnSpanSettingsKey, 1) ?? 1,
                1,
                2);
            SetupCommands();
            SetupBottomArea();
            _lastExceptionButton.Click += OnLastExceptionCommand;
            Disposed += OnMainFormDisposed;
            ApplyDisplayOptions();
            UpdateCommandStates();
            UpdateTelemetryStatus();

            _readingsToolTip.SetToolTip(
                _accelerationGauge,
                "Caller-selected raw axes: device X = forward, device Y = lateral, device Z = vertical.");
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
                _accelerationGauge,
                _compassGauge
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
            _bottomTabControl.AddTab("Route sections", _routeSectionTabHost);
            _bottomTabControl.AddTab("Debug", _debugTabHost);

            _debugTabHost.Attach(_log);

            _mapZoomTrackBar.Value = _options.MapZoomOffset;
            _routeMap.ZoomOffset = _options.MapZoomOffset;
            _routeMap.ZoomOffsetChanged += OnRouteMapZoomOffsetChanged;
            _routeMap.Error += OnRouteMapError;

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

                return Path.Combine(root, "WARP", "WarpSensorsDemo", "Recordings");
            }
        }

        /// <inheritdoc/>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            ApplyMapLayout();
        }

        /// <inheritdoc/>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_shutdownCompleted
                && (_transitioning || _lease is not null || _recording is not null))
            {
                e.Cancel = true;

                if (!_shutdownStarted)
                {
                    _shutdownStarted = true;
                    _ = CompleteShutdownAsync();
                }

                return;
            }

            base.OnFormClosing(e);
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _fullScreenController.ExitFullScreen();
            PersistWindowPlacement();
            _log.Detach();
            _userSettings?.Flush();
            _statusTimer.Stop();
            _exceptionHistoryForm?.Close();
            _exceptionHistoryForm = null;
            _fullScreenController.FullScreenChanged -= OnFullScreenChanged;
            _fullScreenController.Dispose();
            base.OnFormClosed(e);
        }

        /// <inheritdoc/>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
            => _fullScreenController.ProcessKey(keyData)
                || base.ProcessCmdKey(ref msg, keyData);

        /// <inheritdoc/>
        protected override void OnMove(EventArgs e)
        {
            base.OnMove(e);
            PersistWindowPlacement();
        }

        /// <inheritdoc/>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PersistWindowPlacement();
        }

        private async Task CompleteShutdownAsync()
        {
            try
            {
                await CleanupProgressForm.RunAsync(
                    this,
                    "Finalizing recordings and stopping sensor services...",
                    StopAfterCurrentTransitionAsync).ConfigureAwait(true);
            }
            catch (Exception exception)
            {
                ShowError("Could not cleanly stop the sensor session", exception);
            }
            finally
            {
                _shutdownCompleted = true;
                Close();
            }
        }

        private async Task StopAfterCurrentTransitionAsync()
        {
            while (_transitioning)
            {
                await Task.Delay(25).ConfigureAwait(true);
            }

            await StopSensorSessionAsync().ConfigureAwait(true);
        }

        private async Task StartSensorSessionAsync()
        {
            if (_lease is not null)
            {
                return;
            }

            _transitioning = true;
            UpdateCommandStates();

            try
            {
                _sensorSourceFactory?.Configure(_options.CreateWindowsSourceOptions());
                _lease = await _sensorService
                    .AcquireSessionAsync(DemoOptions.CreateSensorServiceOptions())
                    .ConfigureAwait(true);
                _subscription = _sensorService.Subscribe(new SensorSubscriptionOptions
                {
                    Name = "WarpSensorsDemo dashboard",
                    Kinds = SensorDataKindFilter.Imu
                        | SensorDataKindFilter.Geolocation
                        | SensorDataKindFilter.Health
                        | SensorDataKindFilter.Gap
                        | SensorDataKindFilter.Classification,
                    Capacity = SubscriptionCapacity,
                    MaximumRateHz = SubscriptionMaximumRateHertz
                });
                _consumerCancellation = new CancellationTokenSource();
                _consumerTask = ConsumeAsync(_subscription, _consumerCancellation.Token);

                if (_options.AutoRecording)
                {
                    await StartRecordingAsync().ConfigureAwait(true);
                }

                SetStatusAndLog(
                    "Running. Acceleration uses caller-selected raw axes: X = forward, Y = lateral; "
                    + "acquisition and recording continue while visuals are paused.");
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
            UpdateCommandStates();

            try
            {
                await StopSensorSessionCoreAsync().ConfigureAwait(true);
                SetStatus("Sensor session stopped.");
            }
            finally
            {
                _transitioning = false;
                UpdateCommandStates();
            }
        }

        private async Task StopSensorSessionCoreAsync()
        {
            try
            {
                await StopRecordingAsync().ConfigureAwait(true);
            }
            finally
            {
                if (_consumerCancellation is { } cancellation)
                {
                    await cancellation.CancelAsync().ConfigureAwait(true);
                }

                if (_subscription is { } subscription)
                {
                    _subscription = null;
                    await subscription.DisposeAsync().ConfigureAwait(true);
                }

                if (_consumerTask is { } consumerTask)
                {
                    _consumerTask = null;

                    try
                    {
                        await consumerTask.ConfigureAwait(true);
                    }
                    catch (OperationCanceledException)
                    {
                    }
                }

                _consumerCancellation?.Dispose();
                _consumerCancellation = null;

                if (_lease is { } lease)
                {
                    _lease = null;
                    await lease.DisposeAsync().ConfigureAwait(true);
                }

                _paused = false;
                _pausedGpsPoints.Clear();
                _pausedClassifications.Clear();
                _lastRawImu = null;
                _lastHealth = null;
                _gapCount = 0;
                _sensorRates.Reset();
                ResetTelemetryValues();
                ResetDashboardPresentation();
                UpdateTelemetryStatus();
            }
        }

        private async Task ConsumeAsync(
            ISensorSubscription subscription,
            CancellationToken cancellationToken)
        {
            DashboardUpdateBuffer updateBuffer = new();

            try
            {
                await using IAsyncEnumerator<SensorEnvelope> enumerator = subscription
                    .ReadAllAsync(cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
                Task<bool> moveNextTask = enumerator.MoveNextAsync().AsTask();
                Task delayTask = Task.Delay(UiUpdateInterval, cancellationToken);

                while (true)
                {
                    Task completedTask = await Task
                        .WhenAny(delayTask, moveNextTask)
                        .ConfigureAwait(false);

                    if (ReferenceEquals(completedTask, delayTask))
                    {
                        await delayTask.ConfigureAwait(false);

                        if (updateBuffer.HasPending)
                        {
                            DashboardUpdateBatch batch = updateBuffer.Drain();
                            bool applied = await TryApplyDashboardUpdateAsync(
                                batch,
                                cancellationToken).ConfigureAwait(false);

                            if (!applied)
                            {
                                return;
                            }
                        }

                        // Start the quiet interval after the UI work completes. If rendering is
                        // slow, queued sensor data is coalesced rather than immediately reinvoked.
                        delayTask = Task.Delay(UiUpdateInterval, cancellationToken);
                        continue;
                    }

                    if (!await moveNextTask.ConfigureAwait(false))
                    {
                        break;
                    }

                    SensorEnvelope envelope = enumerator.Current;
                    _sensorRates.Record(envelope.Kind, DateTimeOffset.UtcNow);
                    updateBuffer.Add(envelope);
                    moveNextTask = enumerator.MoveNextAsync().AsTask();
                }

                if (updateBuffer.HasPending)
                {
                    await TryApplyDashboardUpdateAsync(
                        updateBuffer.Drain(),
                        cancellationToken).ConfigureAwait(false);
                }
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
            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                return false;
            }

            try
            {
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
            if (IsDisposed || Disposing || !IsHandleCreated)
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
            if (IsDisposed || Disposing)
            {
                return;
            }

            if (batch.LatestImu is { } imu)
            {
                _lastRawImu = imu;

                if (!_paused)
                {
                    ApplyRawImu(imu);
                }
            }

            List<SensorGeoPoint> routePoints = new(batch.Geolocations.Count);

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
        }

        private void ApplyRawImu(RawImuPayload imu)
        {
            _accelerationGauge.SetAcceleration(
                imu.Acceleration.X,
                imu.Acceleration.Y,
                imu.Acceleration.Z);
            _artificialHorizon.SetAttitude(
                imu.AbsoluteRollDegrees,
                imu.AbsolutePitchDegrees);

            _telemetryTabHost.ApplyRawImu(imu);

            double heading = double.IsFinite(imu.MagneticHeadingDegrees)
                ? imu.MagneticHeadingDegrees
                : imu.AbsoluteYawDegrees;
            bool headingValid = double.IsFinite(heading);
            double confidence = double.IsFinite(imu.MagneticHeadingDegrees)
                ? imu.MagneticHeadingConfidence
                : headingValid ? 1d : 0d;
            _compassGauge.SetHeading(heading, headingValid, confidence);
        }

        private SensorGeoPoint ApplyGeolocation(GeolocationPayload fix)
        {
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

            bool providerSpeedValid = fix.SpeedValid
                && double.IsFinite(fix.SpeedMetersPerSecond)
                && fix.SpeedMetersPerSecond >= 0d;

            // Every point updates trip and altitude. Position-derived speed is used only when
            // the provider does not supply a valid speed.
            _speedometer.AddGpsPoint(point, deriveSpeed: !providerSpeedValid);

            if (providerSpeedValid)
            {
                _speedometer.SetSpeed(
                    fix.SpeedMetersPerSecond,
                    SpeedUnit.MetersPerSecond);
            }
            UpdateServiceStatus();

            return point;
        }

        private void ResumeVisuals()
        {
            if (_lastRawImu is { } imu)
            {
                ApplyRawImu(imu);
            }

            List<SensorGeoPoint> routePoints = new(_pausedGpsPoints.Count);

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
        }

        private void ApplyDisplayOptions()
        {
            double damping = _options.DampingMilliseconds;
            _speedometer.DampingMilliseconds = damping;
            _artificialHorizon.DampingMilliseconds = damping;
            _accelerationGauge.DampingMilliseconds = damping;
            _compassGauge.DampingMilliseconds = damping;
            _accelerationGauge.VisualRangeG = _options.GScale;
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

            Font newTelemetryFont = new(
                newFont.FontFamily,
                Math.Max(1f, newFont.SizeInPoints - 2f),
                newFont.Style,
                GraphicsUnit.Point);
            Font? oldTelemetryFont = _ownedTelemetryFont;
            _ownedTelemetryFont = newTelemetryFont;

            _telemetryTabHost.ApplyTelemetryFont(newTelemetryFont);
            oldTelemetryFont?.Dispose();
        }

        private void ResetDashboardPresentation()
        {
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

        private SensorRecordingRequest CreateRecordingRequest()
        {
            return new()
            {
                RootFolderPath = _options.RecordingRootFolder,
                Formats = _options.RecordingFormats,
                Kinds = SensorDataKindFilter.Imu
                        | SensorDataKindFilter.Geolocation
                        | SensorDataKindFilter.Health
                        | SensorDataKindFilter.Gap
                        | SensorDataKindFilter.Classification,
                NamePrefix = "drive"
            };
        }

        private async Task StartRecordingAsync()
        {
            _recording = await _sensorService
                .StartRecordingAsync(CreateRecordingRequest())
                .ConfigureAwait(true);
            DemoRecordingRegistry.Set(_recording);
            SetStatusAndLog($"Recording to {_recording.FolderPath}");
        }

        private async Task StopRecordingAsync()
        {
            if (_recording is not { } recording)
            {
                return;
            }

            _recording = null;
            DemoRecordingRegistry.Set(null);
            await recording.StopAsync().ConfigureAwait(true);
            SetStatusAndLog($"Recording finalized in {recording.FolderPath}");
        }

        private void OnMainFormDisposed(object? sender, EventArgs e)
        {
            Disposed -= OnMainFormDisposed;
            _ownedStatusFont?.Dispose();
            _ownedStatusFont = null;
            _ownedTelemetryFont?.Dispose();
            _ownedTelemetryFont = null;
            _ownedBaseFont?.Dispose();
            _ownedBaseFont = null;
        }

        private void UpdateServiceStatus()
        {
            if (_paused)
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
                $"{health} · gaps {_gapCount:N0} · published {statistics.Sequence:N0}{recording}");
        }

        private void SetStatus(string text)
        {
            _statusLabel.Text = text;
        }

        /// <summary>
        ///  Sets the status text and records it in the debug log.
        /// </summary>
        /// <remarks>
        ///  <para>
        ///   The plain <see cref="SetStatus(string)"/> is called on every status tick, so only
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

        private void ShowError(string operation, Exception exception)
        {
            _exceptionHistory.Add(operation, exception);
            _log.Error(operation, exception);
            UpdateTelemetryStatus();
            _statusLabel.Text = $"{operation}: {exception.Message}";
            MessageBox.Show(
                this,
                exception.Message,
                operation,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void OnStatusTimerTick(object? sender, EventArgs e)
        {
            ObserveServiceFaults();
            UpdateTelemetryStatus();

            if (_lease is not null)
            {
                UpdateServiceStatus();
            }
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
            SensorRateSnapshot rates = _sensorRates.GetSnapshot(DateTimeOffset.UtcNow);
            _imuRateLabel.Text = $"Sensors {rates.ImuHertz:F1} Hz";
            _gpsRateLabel.Text = $"GPS {rates.GpsHertz:F1} Hz";

            ExceptionHistoryEntry? latest = _exceptionHistory.Latest;
            _lastExceptionButton.Enabled = latest is not null;
            _lastExceptionButton.Text = latest is null
                ? "Last exception: none"
                : $"Last exception: {Truncate(latest.Message, 48)}";
            _lastExceptionButton.ToolTipText = latest is null
                ? "No exception has been recorded."
                : $"{latest.Timestamp:G} · {latest.Context}: {latest.Message}";
            _exceptionHistoryForm?.UpdateEntries();
        }

        private void OnLastExceptionCommand(object? sender, EventArgs e)
        {
            if (_exceptionHistoryForm is null || _exceptionHistoryForm.IsDisposed)
            {
                _exceptionHistoryForm = new ExceptionHistoryForm(
                    _exceptionHistory,
                    _options.ToolbarSize);
                _exceptionHistoryForm.FormClosed += OnExceptionHistoryClosed;
                _exceptionHistoryForm.Show(this);
                return;
            }

            _exceptionHistoryForm.ApplyToolbarSize(_options.ToolbarSize);
            _exceptionHistoryForm.UpdateEntries();
            _exceptionHistoryForm.Activate();
        }

        private void OnExceptionHistoryClosed(object? sender, FormClosedEventArgs e)
            => _exceptionHistoryForm = null;

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
