using System.Globalization;
using System.IO.Ports;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.Sensors.Payloads;

namespace Sensored;

/// <summary>
///  Contains the restored dashboard commands and their enabled and checked state.
/// </summary>
public partial class MainForm
{
    private const string ReceiverRestoreWarningText =
        "Receiver restore unverified; inspect/reboot receiver or Force Exit";
    private const string TerminalCleanupWarningText =
        "Sensor release ended with a fault. Review diagnostics; only confirmed Exit anyway is safe.";
    private static readonly int[] s_dampingChoices = [0, 50, 100, 200, 300, 400, 500];
    private static readonly double[] s_gScaleChoices = [0.75d, 1d, 1.5d, 2d, 3d];
    private static readonly float[] s_baseFontChoices = [10f, 12f, 14f, 18f, 24f];
    private static readonly int[] s_replaySpeedChoices = [25, 50, 100, 150, 200, 300];
    private readonly CancellationTokenSource _tileStatusUpdateCancellation = new();
    private int _commandUiThreadId;
    private SymbolToolStripMenuItem[] _serialPortItems = [];
    private SymbolToolStripMenuItem[] _serialProfileItems = [];
    private SymbolToolStripMenuItem[] _gpsTargetRateItems = [];

    private bool HasUnresolvedSourceCleanup
        => _receiverRestoreWarning is not null
            || _terminalSourceStopFailure is not null
            || (_lease is null
                && (_consumerTask is not null
                    || _subscription is not null
                    || _recordingStopTask is { IsCompleted: false }
                    || _fusionStopTask is { IsCompleted: false }));

    private string CleanupWarningText
        => _receiverRestoreWarning is not null
            ? ReceiverRestoreWarningText
            : _terminalSourceStopFailure is not null
                ? TerminalCleanupWarningText
                : "Previous session cleanup is incomplete. Retry cleanup or Force Exit.";

    private readonly SymbolToolStripMenuItem _onlineMapTilesMenuItem = new()
    {
        Text = "Allow ONLINE OpenStreetMap tiles (shares viewed area)",
        ToolTipText = "Uses an internet connection and sends the viewed area to OpenStreetMap. "
            + "Route and marker work without online tiles.",
        CheckOnClick = true
    };
    private readonly SymbolToolStripMenuItem _retryMapTilesMenuItem = new()
    {
        Text = "Retry online tiles"
    };
    private readonly SymbolToolStripMenuItem _mapTileStatusMenuItem = new()
    {
        Enabled = false
    };

    private void SetupCommands()
    {
        _commandUiThreadId = Environment.CurrentManagedThreadId;
        EnsureCalibrationUi();
        _toolStrip.Items.Insert(_toolStrip.Items.IndexOf(_calibrateButton) + 1, _fusionFeedbackButton);
        _toolStrip.Items.Insert(_toolStrip.Items.IndexOf(_recordingButton), _drivingTasksButton);
        ConfigureCommandImages();
        _replayOpenMenuItem.Symbol = SensorDemoSymbols.OpenRecordingFolder;
        _replayOpenMenuItem.AccessibleName = "Open recording";
        _replayOpenMenuItem.ToolTipText = "Open a WARP sensor CSV recording";
        ToolStripMenuItem[] replaySpeedItems =
        [
            _replaySpeed25MenuItem,
            _replaySpeed50MenuItem,
            _replaySpeed100MenuItem,
            _replaySpeed150MenuItem,
            _replaySpeed200MenuItem,
            _replaySpeed300MenuItem
        ];

        for (int index = 0; index < replaySpeedItems.Length; index++)
        {
            replaySpeedItems[index].Tag = s_replaySpeedChoices[index];
            replaySpeedItems[index].CheckOnClick = true;
            ((SymbolToolStripMenuItem)replaySpeedItems[index]).Symbol =
                SensorDemoSymbols.CreateAdjustmentSymbol(index, replaySpeedItems.Length);
            replaySpeedItems[index].Click += OnReplaySpeedCommand;
        }

        _windowsGpsMenuItem.Tag = WindowsSensorSourceKind.ImuAndGps;
        _windowsGpsMenuItem.CheckOnClick = true;
        _serialGpsMenuItem.Tag = WindowsSensorSourceKind.ImuAndSerialNmeaGps;
        _windowsGpsMenuItem.Symbol = SensorDemoSymbols.GpsWindows;
        _serialGpsMenuItem.Symbol = SensorDemoSymbols.GpsSerial;
        _windowsGpsMenuItem.Click += OnGpsSourceCommand;
        _serialGpsMenuItem.Click += OnGpsSourceCommand;
        SetupSerialGpsCommands();
        SetupGpsTargetRateCommands();
        _obdButton.CheckOnClick = true;
        _mapEnabledMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapRowSpanMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapColumnSpanMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _mapEnabledMenuItem.Click += OnMapEnabledCommand;
        _mapRowSpanMenuItem.Click += OnMapRowSpanCommand;
        _mapColumnSpanMenuItem.Click += OnMapColumnSpanCommand;
        _onlineMapTilesMenuItem.Click += OnOnlineMapTilesCommand;
        _onlineMapTilesMenuItem.Symbol = SensorDemoSymbols.OptionsMap;
        _retryMapTilesMenuItem.Symbol = SensorDemoSymbols.GpsWindows;
        _mapTileStatusMenuItem.Symbol = SensorDemoSymbols.GpsSerial;
        _retryMapTilesMenuItem.Click += OnRetryMapTilesCommand;
        _mapButton.DropDownItems.Add(new ToolStripSeparator());
        _mapButton.DropDownItems.Add(_onlineMapTilesMenuItem);
        _mapButton.DropDownItems.Add(_retryMapTilesMenuItem);
        _mapButton.DropDownItems.Add(_mapTileStatusMenuItem);
        _routeMap.TileStatusChanged += OnMapTileStatusChanged;
        Disposed += OnCommandsDisposed;
        _routeMap.AllowTileNetwork = _options.AllowOnlineMapTiles;
        _statusTimer.Tick += OnGpsHealthTick;

        SymbolToolStripMenuItem[] dampingItems =
        [
            _dampingOffMenuItem,
            _damping50MenuItem,
            _damping100MenuItem,
            _damping200MenuItem,
            _damping300MenuItem,
            _damping400MenuItem,
            _damping500MenuItem
        ];

        for (int index = 0; index < dampingItems.Length; index++)
        {
            dampingItems[index].Symbol = SensorDemoSymbols.CreateAdjustmentSymbol(index, dampingItems.Length);
            dampingItems[index].Tag = s_dampingChoices[index];
            dampingItems[index].CheckOnClick = true;
            dampingItems[index].Click += OnDampingCommand;
        }

        SymbolToolStripMenuItem[] scaleItems =
        [
            _gScale075MenuItem,
            _gScale100MenuItem,
            _gScale150MenuItem,
            _gScale200MenuItem,
            _gScale300MenuItem
        ];

        for (int index = 0; index < scaleItems.Length; index++)
        {
            scaleItems[index].Symbol = SensorDemoSymbols.CreateAdjustmentSymbol(index, scaleItems.Length);
            scaleItems[index].Tag = s_gScaleChoices[index];
            scaleItems[index].CheckOnClick = true;
            scaleItems[index].Click += OnGScaleCommand;
        }

        SymbolToolStripMenuItem[] fontItems =
        [
            _baseFont10MenuItem,
            _baseFont12MenuItem,
            _baseFont14MenuItem,
            _baseFont18MenuItem,
            _baseFont24MenuItem
        ];

        for (int index = 0; index < fontItems.Length; index++)
        {
            fontItems[index].Symbol = SensorDemoSymbols.CreateAdjustmentSymbol(index, fontItems.Length);
            fontItems[index].Tag = s_baseFontChoices[index];
            fontItems[index].CheckOnClick = true;
            fontItems[index].Click += OnBaseFontCommand;
        }
    }

    private void ConfigureCommandImages()
    {
        int symbolSize = (int)_options.ToolbarSize;
        ConfigureDrivingTasksCommand(symbolSize);
        ConfigureFusionLearningCommand(symbolSize);
        _startButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Play,
            (OnStartCommand, true),
            "Start sensor acquisition",
            symbolSize);
        _restartButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Refresh,
            (OnRestartCommand, true),
            "Restart acquisition with the selected source",
            symbolSize);
        _pauseButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Pause,
            (OnPauseCommand, true),
            "Freeze visuals while acquisition and recording continue",
            symbolSize);
        _stopButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Stop,
            (OnStopCommand, true),
            "Stop sensor acquisition",
            symbolSize);
        _sourcesButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Location,
            tooltipText: "GPS, OBD2, and display sensor sources",
            size: symbolSize);
        _gpsSourceButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Location,
            tooltipText: "Choose GPS source, serial port and receiver profile; Restart while parked to apply",
            size: symbolSize);
        _obdButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.SpeedMedium,
            tooltipText: "Prefer selected OBD2 speed; GPS supplies direction and fallback",
            size: symbolSize);
        _sensorsMenuItem.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Ruler,
            tooltipText: "Select the display reference. Fusion continues to use the internal IMU.",
            size: symbolSize);
        _internalSensorsMenuItem.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Ruler,
            tooltipText: "Display the internal sensors. This does not change the fusion measured mount.",
            size: symbolSize);
        _bluetoothSensorsMenuItem.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Ruler,
            tooltipText: "Display the selected Bluetooth reference when a fresh reading is available.",
            size: symbolSize);
        _optionsButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Settings,
            (OnOptionsCommand, true),
            "Source, sensors, display, route, and recording options",
            symbolSize);
        _mapButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Location,
            tooltipText: "Configure the map layout",
            size: symbolSize);
        _dampingButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.SpeedMedium,
            tooltipText: "Select display damping",
            size: symbolSize);
        _gScaleButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Ruler,
            tooltipText: "Select the acceleration scale",
            size: symbolSize);
        _rawLogButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.OpenFile,
            (OnRawLogCommand, true),
            "Open a current service-owned recording",
            symbolSize);
        _replayButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Replay,
            tooltipText: "Open a recording or select its playback speed",
            size: symbolSize);
        _baseFontButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.FontSize,
            tooltipText: "Select the dashboard font size",
            size: symbolSize);
        _recordingButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Record,
            (OnRecordingCommand, true),
            "Start or stop service-owned recording",
            symbolSize);
        _fullScreenButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.FullScreen,
            (OnFullScreenCommand, true),
            "Toggle full-screen mode (F11)",
            symbolSize);
        _exitButton.ConfigureItem(
            _toolStrip,
            FluentSymbols.AllSymbols.Cancel,
            (OnExitCommand, true),
            "Exit the dashboard",
            symbolSize);
    }

    private void ApplyToolbarAppearance()
    {
        _toolStrip.CarModeCommandTimeoutSeconds = _options.ToolbarCommandTimeoutSeconds;
        _toolStrip.CarModeIdleTimeoutSeconds = _options.ToolbarIdleTimeoutSeconds;
        Size imageSize = GetToolbarImageSize(_options.ToolbarSize);
        SymbolToolStripAppearance.Apply(_toolStrip, imageSize);
        ConfigureCommandImages();
    }

    private static Size GetToolbarImageSize(DashboardToolbarSize toolbarSize)
    {
        int size = (int)toolbarSize;
        return new Size(size, size);
    }

    private void UpdateCommandStates()
    {
        SynchronizeRecordingState();

        bool running = _lease is not null;
        bool replaying = _replayPlayer is not null;
        bool available = !_transitioning && !_shutdownStarted;
        bool recordingFinalizing = _recordingStopTask is { IsCompleted: false };
        string? recordingFault = _recording?.Status.LastFaultMessage;
        bool recordingIncomplete = _recording is { } recording
            && (!recording.Status.IsActive || recordingFault is not null
                || _recordingStopTask is { IsCompleted: true });
        bool recordingBlocksSource = _recordingStopTask is not null
            || recordingIncomplete || (!running && _recording is not null);
        bool cleanupPending = HasUnresolvedSourceCleanup;

        _startButton.Enabled = available && !running && !recordingBlocksSource
            && !cleanupPending;
        _startButton.ToolTipText = cleanupPending
            ? CleanupWarningText
            : "Start sensor acquisition";
        _restartButton.Enabled = available && running && !replaying
            && !recordingBlocksSource && !cleanupPending;
        _pauseButton.Enabled = available && running;
        _pauseButton.Checked = _paused;
        _stopButton.Enabled = available && running;
        _sourcesButton.Enabled = available;
        _gpsSourceButton.Enabled = available && !recordingFinalizing
            && !cleanupPending;
        _sensorsMenuItem.Enabled = available;
        UpdateSensorSourcesUi(BluetoothSensorsAvailable, BluetoothSensorsStatus);
        _optionsButton.Enabled = available;
        _calibrateButton.Enabled = available;
        UpdateDrivingTasksCommand();
        _mapButton.Enabled = available;
        _mapButton.Text = _mapEnabled ? "Map" : "Map off";
        _mapEnabledMenuItem.Checked = _mapEnabled;
        _mapRowSpanMenuItem.Checked = _mapRowSpan == 2;
        _mapColumnSpanMenuItem.Checked = _mapColumnSpan == 2;
        _mapRowSpanMenuItem.Enabled = _mapEnabled;
        _mapColumnSpanMenuItem.Enabled = _mapEnabled;
        _onlineMapTilesMenuItem.Enabled = available;
        _onlineMapTilesMenuItem.Checked = _options.AllowOnlineMapTiles;
        _retryMapTilesMenuItem.Enabled = available && _options.AllowOnlineMapTiles
            && _routeMap.TileStatus == SensorMapTileStatus.Unavailable;
        _mapTileStatusMenuItem.Text = _routeMap.TileStatus switch
        {
            SensorMapTileStatus.Online => "Online tiles available",
            SensorMapTileStatus.Loading => "Online tiles loading",
            SensorMapTileStatus.Unavailable => "Some online tiles unavailable · retry available",
            _ => "Offline tiles unavailable · route-only"
        };
        _dampingButton.Enabled = available;
        _gScaleButton.Enabled = available;
        _rawLogButton.Enabled = available;
        _replayButton.Enabled = available && !recordingBlocksSource && !cleanupPending
            && _sensorSourceFactory is not null;
        _replayButton.Text = $"Replay: {_options.ReplaySpeedPercent}%";

        foreach (ToolStripItem item in _replayButton.DropDownItems)
        {
            if (item is ToolStripMenuItem { Tag: int speed } speedItem)
            {
                speedItem.Checked = speed == _options.ReplaySpeedPercent;
            }
        }

        _baseFontButton.Enabled = available;
        _recordingButton.Enabled = available && !replaying && !recordingFinalizing
            && (running || recordingIncomplete);
        _recordingButton.Checked = _recording is { Status.IsActive: true }
            && !recordingFinalizing && recordingFault is null;
        _recordingButton.Text = recordingFinalizing
            ? "Finalizing recording…"
            : recordingIncomplete
                ? "Recording incomplete"
                : _recording is null ? "Record" : "Stop recording";
        _recordingButton.ToolTipText = recordingFault is not null
            ? $"Recording incomplete: {recordingFault}. Click to retry finalization."
            : recordingFinalizing
                ? "Recording finalization is in progress. Wait before starting or applying a source."
                : recordingIncomplete
                    ? "Recording was not confirmed as finalized. Click to retry finalization."
                    : "Start or stop service-owned recording";
        UpdateRecordingWarning(recordingFinalizing, recordingIncomplete, cleanupPending);
        _fullScreenButton.Enabled = available;
        _fullScreenButton.Checked = _fullScreenController.IsFullScreen;
        _fullScreenButton.Text = _fullScreenController.IsFullScreen
            ? "Exit full screen"
            : "Full screen";
        _exitButton.Enabled = !_shutdownStarted;

        _dampingButton.Text = _options.DampingMilliseconds == 0
            ? "Damping: off"
            : $"Damping: {_options.DampingMilliseconds} ms";
        _gScaleButton.Text =
            $"Scale: {_options.GScale.ToString("0.00", CultureInfo.CurrentCulture)} g";
        _baseFontButton.Text =
            $"Font: {_options.BaseFontPointSize.ToString("0", CultureInfo.CurrentCulture)} pt";
        DemoOptions? activeGps = _lease is not null && _replayPlayer is null
            ? _runningSourceOptions
            : null;
        bool sourcePending = activeGps is not null
            && (activeGps.SourceKind != _options.SourceKind
                || !string.Equals(
                    activeGps.SerialPortName,
                    _options.SerialPortName,
                    StringComparison.OrdinalIgnoreCase)
                || activeGps.SerialGnssProfile != _options.SerialGnssProfile
                || activeGps.TargetGpsFixIntervalMilliseconds != _options.TargetGpsFixIntervalMilliseconds
                || activeGps.WindowsGpsReportIntervalMilliseconds != _options.WindowsGpsReportIntervalMilliseconds
                || activeGps.AllowReceiverCommandsThisSession
                    != _options.AllowReceiverCommandsThisSession);
        _gpsSourceButton.Text = replaying ? "GPS: replay" : _options.SourceKind switch
        {
            WindowsSensorSourceKind.ImuAndSerialNmeaGps => "GPS: serial",
            WindowsSensorSourceKind.ImuAndGps => "GPS: Windows",
            _ => "GPS: off"
        };
        if (sourcePending)
        {
            _gpsSourceButton.Text += " (pending)";
        }

        UpdateGpsHealthStatus();

        _windowsGpsMenuItem.Checked =
            _options.SourceKind == WindowsSensorSourceKind.ImuAndGps;
        _serialGpsMenuItem.Checked =
            _options.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps;
        foreach (SymbolToolStripMenuItem item in _serialPortItems)
        {
            item.Checked = string.Equals(
                item.Tag as string,
                _options.SerialPortName ?? string.Empty,
                StringComparison.OrdinalIgnoreCase);
        }

        foreach (SymbolToolStripMenuItem item in _serialProfileItems)
        {
            item.Checked = item.Tag is SerialGnssProfile profile
                && profile == _options.SerialGnssProfile;
        }

        foreach (SymbolToolStripMenuItem item in _gpsTargetRateItems)
        {
            item.Checked = item.Tag is int interval
                && interval == (_options.TargetGpsFixIntervalMilliseconds ?? 0);
        }

        SymbolToolStripMenuItem[] dampingItems =
        [
            _dampingOffMenuItem,
            _damping50MenuItem,
            _damping100MenuItem,
            _damping200MenuItem,
            _damping300MenuItem,
            _damping400MenuItem,
            _damping500MenuItem
        ];

        for (int index = 0; index < dampingItems.Length; index++)
        {
            dampingItems[index].Checked =
                s_dampingChoices[index] == _options.DampingMilliseconds;
        }

        SymbolToolStripMenuItem[] scaleItems =
        [
            _gScale075MenuItem,
            _gScale100MenuItem,
            _gScale150MenuItem,
            _gScale200MenuItem,
            _gScale300MenuItem
        ];

        for (int index = 0; index < scaleItems.Length; index++)
        {
            scaleItems[index].Checked =
                Math.Abs(s_gScaleChoices[index] - _options.GScale) < 0.001d;
        }

        SymbolToolStripMenuItem[] fontItems =
        [
            _baseFont10MenuItem,
            _baseFont12MenuItem,
            _baseFont14MenuItem,
            _baseFont18MenuItem,
            _baseFont24MenuItem
        ];

        for (int index = 0; index < fontItems.Length; index++)
        {
            fontItems[index].Checked =
                Math.Abs(s_baseFontChoices[index] - _options.BaseFontPointSize) < 0.01f;
        }

        RefreshStructuredStatus();
    }

    private async void OnStartCommand(object? sender, EventArgs e)
    {
        if (HasUnresolvedSourceCleanup)
        {
            SetStatus(CleanupWarningText);

            return;
        }

        if (_recordingStopTask is not null || _recording is not null)
        {
            SetStatus("Finish or retry incomplete recording finalization before starting sensors.");

            return;
        }

        if (!ConfirmReceiverCommandsBeforeStart(_options))
        {
            return;
        }

        try
        {
            await StartSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not start sensors", exception);
        }
    }

    private async void OnRestartCommand(object? sender, EventArgs e)
    {
        if (HasUnresolvedSourceCleanup)
        {
            SetStatus(CleanupWarningText);

            return;
        }

        if (_recordingStopTask is not null
            || _recording is { Status.IsActive: false }
            || _recording?.Status.LastFaultMessage is not null)
        {
            SetStatus("Finish or retry incomplete recording finalization before restarting sensors.");

            return;
        }

        if (!ConfirmGpsApply(_options))
        {
            return;
        }

        if (!ConfirmReceiverCommandsBeforeStart(_options))
        {
            return;
        }

        try
        {
            await RestartSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not restart sensors", exception);
        }
    }

    private void OnPauseCommand(object? sender, EventArgs e)
    {
        _paused = _pauseButton.Checked;

        if (_paused)
        {
            SetStatus(_replayPlayer is null
                ? "Visuals paused. Acquisition and recording continue."
                : "Replay visuals paused. Playback continues.");
        }
        else
        {
            ResumeVisuals();
            SetStatus("Visuals resumed.");
        }

        UpdateCommandStates();
    }

    private async void OnStopCommand(object? sender, EventArgs e)
    {
        try
        {
            await StopSensorSessionAsync().ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError("Could not stop sensors", exception);
        }
    }

    private async void OnRecordingCommand(object? sender, EventArgs e)
    {
        if (_recordingStopTask is { IsCompleted: false })
        {
            SetStatus("Recording finalization is already in progress.");

            return;
        }

        _recordingButton.Enabled = false;

        try
        {
            if (_recording is null)
            {
                await StartRecordingAsync().ConfigureAwait(true);
            }
            else
            {
                await StopRecordingAsync().ConfigureAwait(true);
            }
        }
        catch (Exception exception)
        {
            ShowError("Recording operation failed", exception);
        }
        finally
        {
            UpdateCommandStates();
        }
    }

    private async void OnGpsSourceCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: WindowsSensorSourceKind sourceKind })
        {
            return;
        }

        await SaveGpsSelectionAsync(_options with
        {
            SourceKind = sourceKind,
            AllowReceiverCommandsThisSession = sourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && _options.AllowReceiverCommandsThisSession
        });
    }

    private void SetupSerialGpsCommands()
    {
        SymbolToolStripMenuItem portMenu = new("COM port")
        {
            Symbol = SensorDemoSymbols.GpsSerial
        };
        SymbolToolStripMenuItem profileMenu = new("GPS type")
        {
            Symbol = SensorDemoSymbols.GpsWindows
        };
        string[] ports = SerialPort.GetPortNames()
            .Append(_options.SerialPortName ?? string.Empty)
            .Where(port => !string.IsNullOrWhiteSpace(port))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .Prepend(string.Empty)
            .ToArray();
        _serialPortItems = ports.Select(port =>
        {
            SymbolToolStripMenuItem item = new(port.Length == 0 ? "Auto-detect" : port)
            {
                Tag = port,
                CheckOnClick = true,
                Symbol = SensorDemoSymbols.GpsSerial
            };
            item.Click += OnSerialGpsOptionCommand;

            return item;
        }).ToArray();
        _serialProfileItems = Enum.GetValues<SerialGnssProfile>().Select(profile =>
        {
            SymbolToolStripMenuItem item = new(DemoOptions.DescribeGpsProfile(profile))
            {
                Tag = profile,
                CheckOnClick = true,
                Symbol = SensorDemoSymbols.GpsWindows
            };
            item.Click += OnSerialGpsOptionCommand;

            return item;
        }).ToArray();
        portMenu.Name = "_serialPortMenu";
        profileMenu.Name = "_serialProfileMenu";
        portMenu.DropDownItems.AddRange(_serialPortItems);
        profileMenu.DropDownItems.AddRange(_serialProfileItems);
        // Serial receiver stays a selectable leaf. Nesting these groups under it would make
        // Sources > GPS > Serial NMEA receiver > COM port > Auto-detect a fifth CarMode level.
        _gpsSourceButton.DropDownItems.AddRange([portMenu, profileMenu]);
    }

    private void SetupGpsTargetRateCommands()
    {
        SymbolToolStripMenuItem rateMenu = new("Target GPS rate")
        {
            Name = "_gpsTargetRateMenu",
            Symbol = SensorDemoSymbols.GpsWindows
        };
        _gpsTargetRateItems = DemoOptions.GpsTargetIntervals.Select(interval =>
        {
            SymbolToolStripMenuItem item = new(interval == 0
                ? "Auto (serial fastest / internal 1 Hz)"
                : $"{1000d / interval:0.#} Hz")
            {
                Tag = interval,
                CheckOnClick = true,
                Symbol = SensorDemoSymbols.GpsWindows
            };
            item.Click += OnGpsTargetRateCommand;

            return item;
        }).ToArray();
        rateMenu.DropDownItems.AddRange(_gpsTargetRateItems);
        _gpsSourceButton.DropDownItems.Add(rateMenu);
    }

    private async void OnGpsTargetRateCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: int interval })
        {
            throw new InvalidOperationException("Unknown GPS target rate.");
        }

        await SaveGpsSelectionAsync(_options with
        {
            TargetGpsFixIntervalMilliseconds = interval == 0 ? null : interval,
            WindowsGpsReportIntervalMilliseconds = interval == 0 ? 1000u : (uint)interval,
            SerialGnssProfile = _options.SerialGnssProfile == SerialGnssProfile.Passive
                ? SerialGnssProfile.Current : _options.SerialGnssProfile
        });
    }

    private async void OnSerialGpsOptionCommand(object? sender, EventArgs e)
    {
        DemoOptions selected = sender switch
        {
            ToolStripMenuItem { Tag: string port } => _options with
            {
                SerialPortName = port.Length == 0 ? null : port
            },
            ToolStripMenuItem { Tag: SerialGnssProfile profile } => _options with
            {
                SerialGnssProfile = profile,
                AllowReceiverCommandsThisSession = profile == _options.SerialGnssProfile
                    && _options.AllowReceiverCommandsThisSession
            },
            _ => throw new InvalidOperationException("Unknown serial GPS toolbar option.")
        };
        await SaveGpsSelectionAsync(selected with { SourceKind = WindowsSensorSourceKind.ImuAndSerialNmeaGps });
    }

    private async Task SaveGpsSelectionAsync(DemoOptions selected)
    {
        try
        {
            if (!ConfirmGpsApply(selected))
            {
                UpdateCommandStates();

                return;
            }
            if (selected.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && selected.SerialGnssProfile != SerialGnssProfile.Passive)
            {
                if (selected.ConfirmGpsReceiverChanges
                    && !WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(
                    this, selected.CreateWindowsSourceOptions().SerialGpsOptions))
                {
                    UpdateCommandStates();

                    return;
                }
                selected = selected with { AllowReceiverCommandsThisSession = true };
            }

            _options = selected;
            DemoSettings.Save(_userSettings, _options);
            UpdateCommandStates();
            if (_sensorSourceFactory is { IsReplay: false } factory)
            {
                await factory.ConfigureAsync(selected.CreateWindowsSourceOptions(), allowReceiverWrites: true);
                if (_lease is not null)
                {
                    _runningSourceOptions = _options with { VehicleMount = _activeVehicleMount };
                }
            }
            SetStatus(_sensorSourceFactory is { IsReplay: true }
                ? "GPS selection saved for live acquisition after replay."
                : "GPS selection applied; acquisition and recording continue.");
        }
        catch (Exception exception)
        {
            ShowError("Could not apply GPS selection", exception);
        }
    }

    private static string FormatFixAge(TimeSpan age)
    {
        TimeSpan positive = age < TimeSpan.Zero ? TimeSpan.Zero : age;

        return positive.TotalHours >= 1
            ? $"{(int)positive.TotalHours}h {positive.Minutes:D2}m"
            : positive.TotalMinutes >= 1
                ? $"{(int)positive.TotalMinutes}m {positive.Seconds:D2}s"
                : $"{positive.TotalSeconds:F1}s";
    }

    private bool ConfirmGpsApply(DemoOptions proposed, Func<string, bool>? confirmMotion = null)
    {
        if (HasUnresolvedSourceCleanup)
        {
            SetStatusAndLog(CleanupWarningText);

            return false;
        }

        if (_recordingStopTask is not null
            || _recording is { Status.IsActive: false }
            || _recording?.Status.LastFaultMessage is not null)
        {
            SetStatusAndLog("Finish or retry incomplete recording finalization before applying GPS settings.");

            return false;
        }

        bool proposedWrites = proposed.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
            && proposed.SerialGnssProfile != SerialGnssProfile.Passive;
        bool restorationWrites = _sensorSourceFactory?.LiveOptions is
        {
            Sources: WindowsSensorSourceKind.ImuAndSerialNmeaGps,
            SerialGpsOptions: { GnssProfile: not SerialGnssProfile.Passive, AllowReceiverCommandsThisSession: true }
        };
        if (_sensorSourceFactory is { IsReplay: true } || (!proposedWrites && !restorationWrites))
        {
            return true;
        }

        WarpToolkit.Sensors.Presentation.GpsPreviewSession? preview = _sensorSourceFactory?.Preview;
        double speed = preview?.Dashboard.Motion.ReceiverSpeedMetersPerSecond ?? double.NaN;
        if (preview?.Capture().GpsFresh != true || !double.IsFinite(speed) || speed > 0.5d)
        {
            string warning = "GPS cannot confirm a stationary vehicle: "
                + (double.IsFinite(speed) ? $"reported speed is {speed * 3.6:F1} km/h, or the fix is stale. "
                    : "no usable speed fix is available. ")
                + "This detection may be incorrect. Temporary receiver commands or restoration may be sent. "
                + "You can override the detection and apply your settings. Proceed anyway?";
            bool accepted = confirmMotion?.Invoke(warning) ?? MessageBox.Show(
                this, warning, "Override GPS motion detection",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
            _log.Warning(accepted
                ? $"User overrode GPS motion detection for this settings change. {warning}"
                : "GPS settings change cancelled by user at the motion warning.");

            return accepted;
        }

        return true;
    }

    private bool ConfirmReceiverCommandsBeforeStart(DemoOptions proposed)
    {
        if (proposed.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
            && proposed.SerialGnssProfile != SerialGnssProfile.Passive)
        {
            if (proposed.ConfirmGpsReceiverChanges
                && !WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(
                this, proposed.CreateWindowsSourceOptions().SerialGpsOptions))
            {
                return false;
            }

            _options = proposed = proposed with { AllowReceiverCommandsThisSession = true };
        }

        return !proposed.ConfirmSensorStart
            || proposed.SourceKind != WindowsSensorSourceKind.ImuAndSerialNmeaGps
            || proposed.SerialGnssProfile == SerialGnssProfile.Passive
            || !proposed.AllowReceiverCommandsThisSession
            || MessageBox.Show(
                this,
                "Temporary receiver commands may be sent. Start only while the vehicle "
                    + "is stationary and keep the receiver connected so it can be restored. "
                    + "Is the vehicle parked?",
                "Confirm stationary vehicle",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }

    private void OnGpsHealthTick(object? sender, EventArgs e)
    {
        if (_shutdownStarted || IsReportingCleanup || IsDisposed || Disposing)
        {
            return;
        }

        if (_recordingStopTask is { IsCompleted: true }
            && !_recordingButton.Enabled && !_transitioning && !_shutdownStarted)
        {
            UpdateCommandStates();
        }

        UpdateGpsHealthStatus();
        if (_lease is null && _sensorSourceFactory is { IsReplay: false })
        {
            ApplyGpsMotionReadouts();
        }
        PresentObd();
        UpdateRecordingWarning(
            _recordingStopTask is { IsCompleted: false },
            _recording is { } recording
                && (!recording.Status.IsActive
                    || recording.Status.LastFaultMessage is not null
                    || _recordingStopTask is { IsCompleted: true }),
            HasUnresolvedSourceCleanup);
    }

    private void UpdateRecordingWarning(bool finalizing, bool incomplete, bool cleanupPending)
    {
        string? warning = _receiverRestoreWarning is not null
            ? ReceiverRestoreWarningText
            : finalizing
            ? "RECORDING FINALIZING"
            : incomplete ? "RECORDING INCOMPLETE"
            : cleanupPending ? "CLEANUP INCOMPLETE" : null;

        if (_statusWarning != warning)
        {
            _statusWarning = warning;
            NotifyStatusChange(warning ?? "Recording and source cleanup warnings cleared.");
        }
        RefreshStructuredStatus();
    }

    private void UpdateGpsHealthStatus()
    {
        SensorRateSnapshot rates = _sensorRates.GetSnapshot(DateTimeOffset.UtcNow);

        if (_receiverRestoreWarning is not null)
        {
            _gpsHealthSummary = "GPS · receiver restore UNVERIFIED";
            _gpsHealthDetails = ReceiverRestoreWarningText;
            _gpsSourceButton.ToolTipText = ReceiverRestoreWarningText;

            return;
        }

        if (_terminalSourceStopFailure is not null)
        {
            _gpsHealthSummary = "GPS · sensor cleanup INCOMPLETE";
            _gpsHealthDetails = TerminalCleanupWarningText;
            _gpsSourceButton.ToolTipText = TerminalCleanupWarningText;

            return;
        }

        if (_sensorSourceFactory?.GpsFault is { } gpsFault)
        {
            _gpsHealthSummary = "GPS · release FAILED; replacement opens blocked";
            _gpsHealthDetails = gpsFault.Message;
            _gpsSourceButton.ToolTipText = gpsFault.ToString();
            return;
        }

        if (_replayPlayer is not null || _sensorSourceFactory is { IsReplay: true })
        {
            _gpsHealthSummary = $"GPS replay · {rates.GpsHertz:F1} Hz playback"
                + " · historical fix age unavailable";
            _gpsHealthDetails =
                "Playback cadence is measured now; recorded arrival timestamps are historical. "
                + "Receiver configuration and fix age are unavailable for older recordings. "
                + "This is not live GPS link or fix health.";
            _gpsSourceButton.ToolTipText = "Replay source active. Next live source selected: "
                + DemoOptions.DescribeGpsSource(_options)
                + ". Receiver diagnostics during replay are not live.";

            return;
        }

        if (_lease is null && _sensorSourceFactory is null)
        {
            _gpsHealthSummary = "GPS · no live session";
            _gpsHealthDetails = "No GPS link or fix age is being measured. "
                + $"Selected for next session: {DemoOptions.DescribeGpsSource(_options)}.";
            _gpsSourceButton.ToolTipText =
                $"Selected (not running): {DemoOptions.DescribeGpsSource(_options)}.";

            return;
        }

        DemoOptions source = _lease is not null && _replayPlayer is null
            ? _runningSourceOptions ?? _options
            : _options;

        string sourceName = DemoOptions.DescribeGpsSource(source);

        string shortSourceName = source.SourceKind switch
        {
            WindowsSensorSourceKind.ImuAndSerialNmeaGps
                => $"Serial {DemoOptions.DescribeGpsProfile(source.SerialGnssProfile)}",
            WindowsSensorSourceKind.ImuAndGps => "Windows",
            _ => "off"
        };

        SensorHealthPayload? health = _sensorSourceFactory is { IsReplay: false } factory
            ? factory.Preview.Health : _lastHealth;

        GpsFixDiagnostics gps = health?.GpsFixDiagnostics ?? default;

        string age = gps.LastValidFixArrivalUtc is { } arrival
            ? FormatFixAge(DateTimeOffset.UtcNow - arrival)
            : "none";

        string freshness = health is null
            ? "fix unknown"
            : gps.LastValidFixArrivalUtc is null
                ? health.Value.AcceptedFixCount > 0
                    ? "fix age not reported"
                    : "NO FIX"
                : gps.IsFixStale(DateTimeOffset.UtcNow)
                    ? $"STALE (>{gps.GetStaleThreshold().TotalSeconds:F1}s)"
                    : "fresh";

        _gpsHealthSummary =
            $"GPS {shortSourceName} · {gps.MeasuredFixRateHz ?? rates.GpsHertz:F1} Hz · fix {age} {freshness}";

        _gpsHealthDetails = $"Configured: {sourceName}. "
            + $"Link: {health?.GpsAcquisitionStatus.ToString() ?? "not running"}; "
            + $"transport: {gps.Connection}; fix age: {age} ({freshness}). "
            + $"Measured: {rates.GpsHertz:F1} Hz; receiver requested/applied: "
            + (gps.ReceiverConfiguration == GpsReceiverConfigurationStatus.Applied
                && gps.RequestedFixRateHz is { } requested ? $"{requested:F1} Hz" : "not verified")
            + $"; rejected fixes: {health?.InvalidFixCount.ToString("N0", CultureInfo.CurrentCulture) ?? "unknown"}."
            + (health?.FaultMessage is { } faultMessage ? $" Recovery: {faultMessage}" : string.Empty)
            + (source.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && gps.LastInvalidFixReason == GpsInvalidFixReason.ReceiverNoFix
                && gps.LastReportedFixQuality == 0
                    ? " Last reported fix was unavailable (GGA quality 0). "
                        + "Check clear sky and antenna while parked."
                    : string.Empty);

        _gpsSourceButton.ToolTipText = $"Selected: {DemoOptions.DescribeGpsSource(_options)}. "
            + $"Running: {sourceName} ({(_lease is null ? "preview" : "drive + preview")}). "
            + "Profile selection is not proof of satellite use; inspect live fix health before trusting a position.";
    }

    private void OnOnlineMapTilesCommand(object? sender, EventArgs e)
    {
        _options = _options with { AllowOnlineMapTiles = _onlineMapTilesMenuItem.Checked };
        DemoSettings.Save(_userSettings, _options);
        _userSettings?.Flush();

        _routeMap.AllowTileNetwork = _options.AllowOnlineMapTiles;
        UpdateCommandStates();
    }

    private void OnRetryMapTilesCommand(object? sender, EventArgs e)
        => _routeMap.RetryTiles();

    private async void OnMapTileStatusChanged(object? sender, EventArgs e)
    {
        try
        {
            if (_shutdownStarted
                || _tileStatusUpdateCancellation.IsCancellationRequested
                || IsDisposed
                || Disposing)
            {
                return;
            }

            if (Environment.CurrentManagedThreadId == _commandUiThreadId)
            {
                UpdateMapTileStatusOnUiThread();
            }
            else if (IsHandleCreated)
            {
                await InvokeAsync(
                    (Action)UpdateMapTileStatusOnUiThread,
                    _tileStatusUpdateCancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (_tileStatusUpdateCancellation.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (IsDisposed || Disposing)
        {
        }
        catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
        {
        }
        catch (Exception exception)
        {
            _log.Error("Could not update map tile status on the UI thread.", exception);
        }
    }

    private void UpdateMapTileStatusOnUiThread()
    {
        if (!_shutdownStarted
            && !_tileStatusUpdateCancellation.IsCancellationRequested
            && !IsDisposed
            && !Disposing)
        {
            UpdateCommandStates();
        }
    }

    private void OnCommandsDisposed(object? sender, EventArgs e)
    {
        _routeMap.TileStatusChanged -= OnMapTileStatusChanged;
        _statusTimer.Tick -= OnGpsHealthTick;
        Disposed -= OnCommandsDisposed;
        _tileStatusUpdateCancellation.Cancel();
        _tileStatusUpdateCancellation.Dispose();
    }

    private void OnDampingCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: int milliseconds })
        {
            return;
        }

        _options = _options with
        {
            DampingMilliseconds = milliseconds
        };
        DemoSettings.Save(_userSettings, _options);
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private void OnGScaleCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: double scale })
        {
            return;
        }

        _options = _options with
        {
            GScale = scale
        };
        DemoSettings.Save(_userSettings, _options);
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private void OnBaseFontCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: float pointSize })
        {
            return;
        }

        _options = _options with
        {
            BaseFontPointSize = pointSize
        };
        DemoSettings.Save(_userSettings, _options);
        ApplyDisplayOptions();
        UpdateCommandStates();
    }

    private int LoadReplaySpeed()
    {
        int speed = _userSettings?.Get(ReplaySpeedSettingsKey, 100) ?? 100;

        if (Array.IndexOf(s_replaySpeedChoices, speed) >= 0)
        {
            return speed;
        }

        _log.Warning($"Invalid saved replay speed {speed}%; resetting it to 100%.");
        _userSettings?.Set(ReplaySpeedSettingsKey, 100);

        return 100;
    }

    private void OnReplaySpeedCommand(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: int speed }
            || Array.IndexOf(s_replaySpeedChoices, speed) < 0)
        {
            return;
        }

        try
        {
            _userSettings?.Set(ReplaySpeedSettingsKey, speed);

            if (_replayPlayer is { } player)
            {
                player.PlaybackSpeed = speed / 100d;
            }

            _options = _options with { ReplaySpeedPercent = speed };
            UpdateCommandStates();
            UpdateReplayStatus();
            SetStatusAndLog($"Replay speed: {speed}%.");
        }
        catch (Exception exception)
        {
            ShowError("Could not change replay speed", exception);
        }
    }

    private async void OnOptionsCommand(object? sender, EventArgs e)
    {
        using OptionsDialog dialog = new(
            _options,
            DemoSettings.ResolveInitialDirectory(
                _userSettings,
                DemoSettings.RouteDirectoryKey,
                RecordingsRootFolder),
            _userSettings,
            BluetoothSensorsAvailable,
            ReadSensorReferenceSample,
            ReportBluetoothDiscoveryStatus);

        DialogResult result = dialog.ShowDialog(this);
        _bluetoothDiscoveryStatus = null;
        RefreshStructuredStatus();
        if (result != DialogResult.OK)
        {
            return;
        }

        DemoOptions previousOptions = _options;
        _options = dialog.Options;
        bool sourceChanged = new WindowsSensorSourceFactory(previousOptions.CreateWindowsSourceOptions())
            .CreateGpsSelection()?.Key != new WindowsSensorSourceFactory(_options.CreateWindowsSourceOptions())
            .CreateGpsSelection()?.Key;
        try
        {
            if (sourceChanged && !ConfirmGpsApply(_options))
            {
                _options = previousOptions;
                UpdateCommandStates();

                return;
            }
            if (sourceChanged && _sensorSourceFactory is { IsReplay: false } factory)
            {
                await factory.ConfigureAsync(_options.CreateWindowsSourceOptions(), allowReceiverWrites: sourceChanged);
                if (_lease is not null)
                {
                    _runningSourceOptions = _options with { VehicleMount = _activeVehicleMount };
                }
            }
            _userSettings?.Set(ToolbarSizeSettingsKey, _options.ToolbarSize);
            _userSettings?.Set(AutoRecordingSettingsKey, _options.AutoRecording);
            _userSettings?.Set(ReplaySequenceSettingsKey, _options.DetectReplaySequenceFiles);
            _userSettings?.Set(ReplayHideControlsSettingsKey, _options.HideReplayControls);
            DemoSettings.RememberPath(_userSettings, DemoSettings.RouteDirectoryKey, _options.RouteFilePath);
            DemoSettings.RememberPath(_userSettings, DemoSettings.RecordingFolderDirectoryKey,
                _options.RecordingRootFolder);
            await ApplySensorSourceOptionsAsync(previousOptions).ConfigureAwait(true);
            await ApplyObdDeviceOptionsAsync(previousOptions).ConfigureAwait(true);
            ApplyDisplayOptions();
            _routeMap.AllowTileNetwork = _options.AllowOnlineMapTiles;
            DemoSettings.Save(_userSettings, _options);
            _userSettings?.Flush();
            UpdateReplayStatus();
            UpdateCommandStates();
        }
        catch (Exception exception)
        {
            DemoOptions failedOptions = _options;
            _options = previousOptions;
            _routeMap.AllowTileNetwork = previousOptions.AllowOnlineMapTiles;
            try
            {
                await ApplySensorSourceOptionsAsync(failedOptions).ConfigureAwait(true);
                await ApplyObdDeviceOptionsAsync(failedOptions).ConfigureAwait(true);
            }
            catch (Exception rollback)
            {
                _log.Warning($"Sensor source rollback failed: {rollback.Message}");
            }

            ApplyDisplayOptions();
            UpdateCommandStates();
            ShowError("Could not apply dashboard options", exception);

            return;
        }

        SetStatus(previousOptions.ObdAdapter != _options.ObdAdapter || previousOptions.ObdDevice != _options.ObdDevice
            || previousOptions.BluetoothSensorDevice != _options.BluetoothSensorDevice
            ? "Bluetooth choices saved. Enabled live sources are reconnecting to the selected devices; no pairing was requested."
            : sourceChanged && _lease is not null
            ? "Options updated. GPS selection applied without restarting the drive or recording."
            : _options.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
                ? _options.SerialGnssProfile == SerialGnssProfile.Passive
                    ? "Serial GPS: passive NMEA; receiver unchanged."
                    : _options.AllowReceiverCommandsThisSession
                        ? "Serial GPS: temporary profile authorized for this session; actual rate depends on receiver ACK/readback."
                        : "Serial GPS: profile selected but commands NOT authorized; receiver remains unchanged."
                : "Options updated.");
    }

    private void OnRawLogCommand(object? sender, EventArgs e)
    {
        try
        {
            string? initialPath = _recording?.FolderPath;
            using RawLogViewerDialog dialog = new(
                _options.ReadingFormats,
                _options.ToolbarSize,
                initialPath,
                DemoSettings.ResolveInitialDirectory(
                    _userSettings,
                    DemoSettings.RawLogDirectoryKey,
                    RecordingsRootFolder));
            dialog.ShowDialog(this);
            DemoSettings.RememberPath(
                _userSettings,
                DemoSettings.RawLogDirectoryKey,
                dialog.LastOpenedPath);
        }
        catch (Exception exception)
        {
            ShowError("Raw recording viewer failed", exception);
        }
    }

    private async void OnReplayCommand(object? sender, EventArgs e)
    {
        if (HasUnresolvedSourceCleanup)
        {
            SetStatus(CleanupWarningText);

            return;
        }

        using OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            Filter = "WARP sensor journals (*.compact.json;*.jsonl;*.csv)|*.compact.json;*.jsonl;*.csv",
            InitialDirectory = DemoSettings.ResolveInitialDirectory(
                _userSettings,
                DemoSettings.ReplayDirectoryKey,
                RecordingsRootFolder),
            Title = "Replay WARP sensor recording"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        DemoSettings.RememberPath(
            _userSettings,
            DemoSettings.ReplayDirectoryKey,
            dialog.FileName);

        try
        {
            await StartReplayAsync(dialog.FileName).ConfigureAwait(true);
        }
        catch (Exception exception)
        {
            ShowError(
                "Could not start sensor replay",
                exception,
                $"Selected replay file: {Path.GetFullPath(dialog.FileName)}{Environment.NewLine}"
                + $"Sequence detection: {_options.DetectReplaySequenceFiles}");
        }
    }

    private void OnExitCommand(object? sender, EventArgs e)
        => Close();

    private void OnFullScreenCommand(object? sender, EventArgs e)
        => _fullScreenController.Toggle();

    private void OnFullScreenChanged(object? sender, EventArgs e)
        => UpdateCommandStates();

    private void OnMapEnabledCommand(object? sender, EventArgs e)
    {
        _mapEnabled = _mapEnabledMenuItem.Checked;
        PersistMapLayout();
        ApplyMapLayout();
        PresentObd();
        UpdateCommandStates();
    }

    private void OnMapRowSpanCommand(object? sender, EventArgs e)
    {
        _mapRowSpan = _mapRowSpanMenuItem.Checked ? 2 : 1;
        PersistMapLayout();
        ApplyMapLayout();
        PresentObd();
        UpdateCommandStates();
    }

    private void OnMapColumnSpanCommand(object? sender, EventArgs e)
    {
        _mapColumnSpan = _mapColumnSpanMenuItem.Checked ? 2 : 1;
        PersistMapLayout();
        ApplyMapLayout();
        PresentObd();
        UpdateCommandStates();
    }

    private void SynchronizeRecordingState()
    {
        if (_recording is { } recording
            && !recording.Status.IsActive
            && recording.Status.LastFaultMessage is null
            && _recordingStopTask is null)
        {
            _recording = null;
        }

        if (_recording is null && DemoRecordingRegistry.Active is { } active)
        {
            _recording = active.Status.IsActive
                || active.Status.LastFaultMessage is not null
                || _recordingStopTask is not null
                    ? active
                    : null;
        }

        if (_recording is null && DemoRecordingRegistry.Active is not null)
        {
            DemoRecordingRegistry.Set(null);
        }
    }
}
