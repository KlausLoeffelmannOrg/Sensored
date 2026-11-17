using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarpToolkit.ComponentModel;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.Symbols;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Hosts touch-oriented driving diagnostics; processing never depends on painting.</summary>
public partial class MainForm : Form, IServiceProvider
{
    private const string OptionsKey = "FusionGauges.Options";
    private const string BoundsKey = "FusionGauges.RestoreBounds";
    private readonly SensorHub _hub;
    private IServiceProvider? _serviceProvider;
    private IUserSettingsService? _settings;
    private FusionGaugesOptions _options = new();
    private Font? _uiFont;
    private bool _busy;
    private bool _stopping;
    private bool _closing;
    private bool _closedCleanly;
    private bool _shutdownFinished;
    private bool _shutdownHadIssue;
    private int _markerNumber;
    private long _nextFrameTick;
    private Exception? _uiFault;
    private MotionAxisCalibration? _motionCalibration;
    private readonly CancellationTokenSource _formLifetime = new();

    /// <summary>Initializes the Designer-compatible shell without starting hardware.</summary>
    public MainForm() : this(new SensorHub())
    {
    }

    /// <summary>Creates the shell with an acquisition owner, without starting hardware.</summary>
    internal MainForm(SensorHub hub)
    {
        ArgumentNullException.ThrowIfNull(hub);
        _hub = hub;
        InitializeComponent();
        RefreshGpsConfiguration();
        _toolbar.CarMode = true;
        _tabs.AddTab("Instruments", _instruments);
        _tabs.AddTab("Diagnostics", _diagnostics);
        _tabs.AddTab("Recording", _recordingView);
        _tabs.BindNavigationButton(_instrumentsTabButton, _instruments);
        _tabs.BindNavigationButton(_diagnosticsTabButton, _diagnostics);
        _tabs.BindNavigationButton(_recordingTabButton, _recordingView);
        _hub.FrameReady += PresentFrame;
        _hub.BluetoothStatusChanged += OnBluetoothStatusChanged;
        _hub.CanDiagnosticReceived += OnCanDiagnostic;
        _hub.ShutdownProgress += OnShutdownProgress;
        _toolbar.CarModeCommandTimeoutSeconds = 5;
        _toolbar.CarModeIdleTimeoutSeconds = 15;
        Disposed += OnDisposed;
    }

    /// <summary>Initializes the shell with hosted settings and diagnostics services.</summary>
    public MainForm(IServiceProvider serviceProvider) : this()
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _serviceProvider = serviceProvider;
        _settings = serviceProvider.GetRequiredService<IUserSettingsService>();
    }

    object? IServiceProvider.GetService(Type serviceType)
        => _serviceProvider?.GetService(serviceType);

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        if (_settings is not null)
        {
            try
            {
                FusionGaugesOptions options = _settings.Get(OptionsKey, new FusionGaugesOptions());
                options.Validate();
                _options = options;
                Rectangle bounds = _settings.Get(BoundsKey, Bounds);
                Rectangle area = Screen.FromRectangle(bounds).WorkingArea;
                bounds.Size = new(Math.Clamp(bounds.Width, 1, area.Width),
                    Math.Clamp(bounds.Height, 1, area.Height));
                bounds.Location = new(Math.Clamp(bounds.X, area.Left, area.Right - bounds.Width),
                    Math.Clamp(bounds.Y, area.Top, area.Bottom - bounds.Height));
                Bounds = bounds;
            }
            catch (Exception exception)
            {
                _uiFault = new InvalidDataException("Saved settings could not be applied. Review Tools / Options.",
                    exception);
                System.Diagnostics.Trace.TraceError(_uiFault.ToString());
            }
        }

        ApplyAppearance();
        _hub.SelectObdSpeed(_options.UseObdSpeed);
        RefreshGpsConfiguration();
        UpdateCommands();
    }

    /// <inheritdoc/>
    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _hub.StartBluetooth(_options.ObdAdapter);
        try
        {
            await _hub.StartGpsPreviewAsync(_options);
        }
        catch (Exception exception)
        {
            await ShowFailureAsync("GPS preview could not start.", exception);
        }
        RefreshPresentation();
    }

    /// <inheritdoc/>
    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        ApplyAppearance();
        RefreshGpsConfiguration();
    }

    /// <inheritdoc/>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.Shift | Keys.S)
            && !_hub.IsRunning && _sensorsButton.Enabled)
        {
            _sensorsButton.PerformClick();

            return true;
        }

        if (keyData == (Keys.Control | Keys.Shift | Keys.X)
            && _hub.IsRunning && _sensorsButton.Enabled)
        {
            _sensorsButton.PerformClick();

            return true;
        }

        ToolStripItem? command = keyData switch
        {
            (Keys.Control | Keys.R) => _recordButton,
            (Keys.Control | Keys.M) => _markerButton,
            (Keys.Control | Keys.K) => _calibrateButton,
            _ => null
        };

        if (command is not null && command.Enabled)
        {
            command.PerformClick();

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <inheritdoc/>
    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);

        if (_closedCleanly)
        {
            return;
        }

        if (_shutdownFinished)
        {
            _closedCleanly = true;

            return;
        }

        e.Cancel = true;

        if (_closing)
        {
            return;
        }

        _closing = true;
        _frameTimer.Stop();
        _kiosk.FullScreen = false;
        UpdateCommands();
        ShowShutdownTerminal();
        AppendShutdown("Exit requested. Acquisition commands are disabled; this log stays selectable.");

        try
        {
            _settings?.Set(OptionsKey, _options);
            _settings?.Set(BoundsKey, WindowState == FormWindowState.Normal && !_kiosk.FullScreen
                ? Bounds : RestoreBounds);
            _settings?.Flush();
            AppendShutdown("OK: Saved options and window bounds.");
        }
        catch (Exception exception)
        {
            _shutdownHadIssue = true;
            AppendShutdown("ERROR: Saved settings were not flushed: " + exception.Message);
        }

        try
        {
            await _hub.DisposeAsync();
        }
        catch (Exception exception)
        {
            _shutdownHadIssue = true;
            AppendShutdown("ERROR: Sensor shutdown was not clean: " + exception.Message);
        }

        _shutdownFinished = true;
        _kiosk.AlwaysOn = false;
        _kiosk.FullScreen = false;
        UpdateCommands();

        if (_shutdownHadIssue)
        {
            AppendShutdown("Shutdown finished with warnings or errors. Review this log, then choose Exit to close.");

            return;
        }

        AppendShutdown("Shutdown finished without warnings. This log stays visible for review, then closes.");
        await Task.Delay(TimeSpan.FromSeconds(4));
        if (!IsDisposed)
        {
            _closedCleanly = true;
            Close();
        }
    }

    private async void OnSensors(object? sender, EventArgs e)
    {
        if (_hub.IsRunning)
        {
            await RunCommandAsync(StopSessionAsync);

            return;
        }

        await RunCommandAsync(StartSensorsAsync);
    }

    private async Task StartSensorsAsync()
    {
            bool stationaryConfirmed = false;
            if (_options.SourceKind == WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && _options.GnssProfile != WarpToolkit.Sensors.SerialGnssProfile.Passive)
            {
                if (!WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(
                    this, _options.CreateSourceOptions().SerialGpsOptions))
                {
                    return;
                }

                _options = _options with { AllowReceiverCommandsThisSession = true };
                if (MessageBox.Show(this,
                    "Temporary receiver commands may be sent. Is the vehicle parked?",
                    "Confirm stationary vehicle", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                {
                    return;
                }
                stationaryConfirmed = true;
            }

            _motionCalibration = null;
            _instruments.ResetSession();
            await _hub.StartAsync(_options, stationaryConfirmed);
    }

    private async Task StopSessionAsync()
    {
        _stopping = true;
        _frameTimer.Stop();
        _statusLabel.Text = "Stopping sensors - finalizing recording and releasing sources...";

        try
        {
            await _hub.StopAsync();
        }
        finally
        {
            _stopping = false;
        }
    }

    private async void OnRecord(object? sender, EventArgs e)
    {
        await RunCommandAsync(async () =>
        {
            if (_hub.IsRecording)
            {
                await _hub.StopRecordingAsync();

                return;
            }

            string? name = null;

            if (!_options.AutomaticNaming)
            {
                using SessionNameDialog dialog = new(_options.NamePrefix, _uiFont!);

                if (await dialog.ShowDialogAsync(this) != DialogResult.OK)
                {
                    return;
                }

                name = dialog.SessionName;
            }

            await _hub.StartRecordingAsync(_options, name);
        });
    }

    private async void OnMarker(object? sender, EventArgs e)
        => await RunCommandAsync(() =>
        {
            _hub.AddMarker($"{_options.MarkerLabel} {++_markerNumber}");

            return Task.CompletedTask;
        });

    private async void OnCalibrate(object? sender, EventArgs e)
    {
        await RunCommandAsync(() =>
        {
            using VehicleFusionSetupDialog dialog = new(ReadFusionReadiness, _options.VehicleMount,
                canEdit: !_hub.IsRunning && !_hub.IsRecording, dialogFont: _uiFont ?? Font);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return Task.CompletedTask;
            }

            FusionGaugesOptions changed = _options with { VehicleMount = dialog.Mount };
            changed.Validate();
            _settings?.Set(OptionsKey, changed);
            _settings?.Flush();
            _options = changed;
            _instruments.ApplyOptions(_options);
            _statusLabel.Text = dialog.Mount is { } mount
                ? $"Saved measured mount {mount.Id}, revision {mount.Version}; applies to the next live session."
                : "Measured mount disabled; horizon and vehicle acceleration remain unavailable.";
            System.Diagnostics.Trace.TraceInformation(_statusLabel.Text);
            RefreshPresentation();

            return Task.CompletedTask;
        });
    }

    private FusionReadinessSnapshot ReadFusionReadiness()
    {
        SensorObservationFrame frame = _hub.Capture();

        return FusionReadinessSnapshot.Create(frame.Fusion?.Estimate, _options.VehicleMount,
            _hub.IsRunning, frame.FusionFresh, _hub.Fault?.Message);
    }

    private void OnResetDisplay(object? sender, EventArgs e)
    {
        _instruments.ResetSession();
        _markerNumber = 0;
        _statusLabel.Text = "Display reset. Acquisition and recording were not changed.";
    }

    private async void OnMercSensors(object? sender, EventArgs e)
    {
        bool enabled = _mercSensorsItem.Checked;
        await RunCommandAsync(async () =>
        {
            try
            {
                await _hub.SetMercedesModeAsync(enabled);
                _instruments.SetMercedesLayout(enabled);
            }
            catch
            {
                _mercSensorsItem.Checked = _instruments.IsMercedesLayout;
                throw;
            }
        });
    }

    private async void OnMotionCalibration(object? sender, EventArgs e)
    {
        await RunCommandAsync(async () =>
        {
            using MotionCalibrationDialog dialog = new(_hub, _uiFont!);

            if (await dialog.ShowDialogAsync(this) == DialogResult.OK)
            {
                _motionCalibration = dialog.Result
                    ?? throw new InvalidOperationException("Motion calibration closed without a verified result.");
            }
        });
    }

    private void OnFullscreen(object? sender, EventArgs e)
        => _kiosk.ToggleFullScreen();

    private void OnFullscreenChanged(object? sender, EventArgs e)
    {
        _menu.Visible = !_kiosk.FullScreen;
        _fullscreenButton.Checked = _kiosk.FullScreen;
        _fullscreenButton.Text = _kiosk.FullScreen ? "Windowed" : "Full screen";
        _fullscreenButton.AccessibleName = _fullscreenButton.Text;
    }

    private void OnExit(object? sender, EventArgs e) => Close();

    private async void OnOptions(object? sender, EventArgs e)
    {
        await RunCommandAsync(async () =>
        {
            using OptionsDialog dialog = new(_options, _uiFont!);

            if (await dialog.ShowDialogAsync(this) == DialogResult.OK)
            {
                FusionGaugesOptions updated = dialog.Options;
                if (new WarpToolkit.Windows.Sensors.WindowsSensorSourceFactory(updated.CreateSourceOptions()).CreateGpsSelection()?.Key
                    != new WarpToolkit.Windows.Sensors.WindowsSensorSourceFactory(_options.CreateSourceOptions()).CreateGpsSelection()?.Key)
                {
                    await _hub.ApplyGpsAsync(updated, allowReceiverWrites: true);
                }
                bool adapterChanged = updated.ObdAdapter != _options.ObdAdapter;
                _options = updated;
                _hub.SelectObdSpeed(updated.UseObdSpeed);
                _uiFault = null;
                _settings?.Set(OptionsKey, _options);
                _settings?.Flush();
                ApplyAppearance();
                RefreshGpsConfiguration();
                _statusLabel.Text = adapterChanged
                    ? "Options saved. The OBD adapter choice applies after restarting the app; the current connection and recording were not changed."
                    : "Options saved; GPS selection applied without restarting acquisition or recording.";
            }
        });
    }

    private async void OnRetry(object? sender, EventArgs e)
        => await RunCommandAsync(() => _hub.RetryMirrorsAsync(_options));

    private async Task RunCommandAsync(Func<Task> command)
    {
        if (_busy || _closing)
        {
            return;
        }

        _busy = true;
        UpdateCommands();

        try
        {
            await command();
        }
        catch (Exception exception)
        {
            await ShowFailureAsync("The operation did not complete.", exception);
        }
        finally
        {
            _busy = false;
            UpdateCommands();
            RefreshPresentation();
        }
    }

    private void OnFrameTick(object? sender, EventArgs e)
    {
        if (_closing || _stopping || !_frameTimer.Enabled)
        {
            return;
        }

        long now = System.Diagnostics.Stopwatch.GetTimestamp();

        if (!AdvancePresentationClock(now))
        {
            return;
        }

        RefreshPresentation();
    }

    private void RefreshPresentation()
    {
        if (_closing || _stopping || IsDisposed)
        {
            return;
        }

        try
        {
            _hub.RaiseFrameReady();
            UpdatePresentationTimer();
        }
        catch (Exception exception)
        {
            _uiFault = exception;
            _frameTimer.Stop();
            _statusLabel.Text = $"PRESENTATION FAULT (acquisition continues): {exception.Message}";
            System.Diagnostics.Trace.TraceError(exception.ToString());
        }
    }

    private void UpdatePresentationTimer()
    {
        if (_uiFault is not null || (!_hub.IsRunning && !_hub.UseObdSpeed && !_hub.HasGpsPreview
            && _hub.CaptureMirrorStatus().Pending == 0))
        {
            _frameTimer.Stop();

            return;
        }

        int interval = _hub.IsRunning || _hub.UseObdSpeed ? 16 : _hub.HasGpsPreview ? 200 : 1000;

        if (!_frameTimer.Enabled || _frameTimer.Interval != interval)
        {
            _nextFrameTick = 0;
            _frameTimer.Interval = interval;
            _frameTimer.Start();
        }
    }

    private bool AdvancePresentationClock(long now)
    {
        if (now < _nextFrameTick)
        {
            return false;
        }

        long period = System.Diagnostics.Stopwatch.Frequency / 30;
        _nextFrameTick = _nextFrameTick == 0 || now - _nextFrameTick > period
            ? now + period
            : _nextFrameTick + period;

        return true;
    }

    private void PresentFrame(SensorObservationFrame frame)
    {
        if (!_hub.IsRunning)
        {
            frame = frame with
            {
                ImuHz = 0d,
                ImuFresh = false,
                FusionFresh = false
            };
        }

        FusionReadinessSnapshot readiness = FusionReadinessSnapshot.Create(frame.Fusion?.Estimate,
            _options.VehicleMount, _hub.IsRunning, frame.FusionFresh, _hub.Fault?.Message);
        _instruments.Present(frame, _hub.FrameStatus, _hub.GpsDashboard, _hub.VehicleSpeed, readiness);
        _instruments.PresentVehicle(ReadVehicleTelemetry());
        _diagnostics.Present(frame, _hub.FrameStatus, _hub.Core, _motionCalibration, _hub.IsRunning,
            _hub.HasGpsPreview, readiness);
        SensorRecordingZipMirrorStatus mirror = _hub.CaptureMirrorStatus();
        SensorRecordingStatus? recording = _hub.RecordingStatus;
        _recordingView.Present(_hub.RecordingMessage, recording, mirror);
        _statusLabel.Text = (_uiFault ?? _hub.Fault ?? _hub.GpsFault) is { } fault
            ? $"FAULT: {fault.Message}"
            : recording?.LastFaultMessage is { } recordingFault
                ? $"RECORDING FAULT: {recordingFault}"
                : $"{(_hub.IsRunning ? "Running" : "Stopped")} | IMU {frame.ImuHz:F1} Hz | GPS {frame.GpsHz:F1} Hz"
                + $" ({frame.Health?.Health?.GpsAcquisitionStatus}; {(frame.GpsFresh ? "fresh fix" : "NO FRESH FIX")})"
                + $" | drops {frame.SubscriptionDropped} / ingress {frame.IngressGaps}"
                + $" | {(_hub.IsRecording ? "RECORDING" : "Not recording")}"
                + (mirror.LastError is null ? "" : $" | ZIP FAULT: {mirror.LastError}");
        _statusLabel.Text += BluetoothStatusSuffix();
        _statusLabel.ToolTipText = readiness.Details;
        UpdateCommands();
    }

    private void UpdateCommands()
    {
        bool available = !_busy && !_closing;
        bool healthy = _hub.Fault is null && _hub.GpsFault is null && _uiFault is null;
        _sensorsButton.Enabled = available && healthy;
        _sensorsButton.Text = _hub.IsRunning ? "Stop sensors" : "Start sensors";
        _sensorsButton.AccessibleName = _sensorsButton.Text;
        _recordButton.Text = _hub.IsRecording ? "Stop recording" : "Start recording";
        _recordButton.AccessibleName = _recordButton.Text;
        _recordButton.Enabled = available && _hub.IsRunning
            && (_hub.IsRecording || !_options.AutoRecording);
        _markerButton.Enabled = available && _hub.IsRecording;
        _calibrateButton.Enabled = available;
        _calibrateButton.Text = "Fusion setup";
        _calibrateButton.ToolTipText = "Per-output support and reasons; edit the measured physical mount only between sessions";
        _motionCalibrationMenu.Enabled = available && _hub.IsRunning && !_hub.IsRecording;
        _optionsButton.Enabled = _optionsMenu.Enabled = available;
        _retryButton.Enabled = available && _options.MirrorEnabled;
        _gpsConfigurationButton.Enabled = available;
        _fullscreenButton.Enabled = available;
        _specialButton.Enabled = available;
        _clipboardButton.Enabled = !_closedCleanly;
        _exitButton.Enabled = _exitMenu.Enabled = !_busy && (!_closing || _shutdownFinished);
    }

    private void ApplyAppearance()
    {
        _toolbar.CarModeCommandTimeoutSeconds = 5;
        _toolbar.CarModeIdleTimeoutSeconds = 15;
        _toolbar.GoToCarModeRoot();
        Font? previous = null;

        if (_uiFont is null || _uiFont.SizeInPoints != _options.FontSize)
        {
            previous = _uiFont;
            _uiFont = new Font("Segoe UI", _options.FontSize, FontStyle.Regular, GraphicsUnit.Point);
        }

        Font = _uiFont;
        _instruments.ApplyOptions(_options);
        _menu.Font = _toolbar.Font = _status.Font = _uiFont;
        _fileMenu.DropDown.Font = _toolsMenu.DropDown.Font = _uiFont;
        int iconSize = LogicalToDeviceUnits(_options.IconSize);
        WarpToolkit.WinForms.ToolStrips.SymbolToolStripAppearance.Apply(_toolbar, new Size(iconSize, iconSize));
        ConfigureIcon(_sensorsButton, _hub.IsRunning
            ? FluentSymbols.AllSymbols.Stop : FluentSymbols.AllSymbols.Play);
        ConfigureIcon(_recordButton, FluentSymbols.AllSymbols.Record);
        ConfigureIcon(_markerButton, FluentSymbols.AllSymbols.Flag);
        ConfigureIcon(_calibrateButton, FluentSymbols.AllSymbols.MapCompassTop);
        ConfigureIcon(_fullscreenButton, FluentSymbols.AllSymbols.FullScreen);
        ConfigureIcon(_optionsButton, FluentSymbols.AllSymbols.Settings);
        ConfigureIcon(_retryButton, FluentSymbols.AllSymbols.Sync);
        ConfigureIcon(_instrumentsTabButton, FluentSymbols.AllSymbols.ViewDashboard);
        ConfigureIcon(_diagnosticsTabButton, FluentSymbols.AllSymbols.DataSense);
        ConfigureIcon(_recordingTabButton, FluentSymbols.AllSymbols.Record2);
        ConfigureIcon(_specialButton, FluentSymbols.AllSymbols.More);
        ConfigureIcon(_resetDisplayItem, FluentSymbols.AllSymbols.ResetDevice);
        ConfigureIcon(_mercSensorsItem, FluentSymbols.AllSymbols.Car);
        ConfigureIcon(_clipboardButton, FluentSymbols.AllSymbols.CopyTo);
        ConfigureIcon(_copyDiagnosticsItem, FluentSymbols.AllSymbols.DataSense);
        ConfigureIcon(_copyLogItem, FluentSymbols.AllSymbols.Record2);
        ConfigureIcon(_copyCanDiagnosticsItem, FluentSymbols.AllSymbols.Car);
        ConfigureIcon(_copyAllItem, FluentSymbols.AllSymbols.CopyTo);
        ConfigureIcon(_exitButton, FluentSymbols.AllSymbols.PowerButton);

        foreach (ToolStripItem item in _toolbar.Items)
        {
            if (item is ToolStripSeparator)
            {
                continue;
            }

            item.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            item.TextImageRelation = TextImageRelation.ImageAboveText;
            item.Padding = new Padding(10, 6, 10, 6);
            item.AccessibleName ??= item.Text;
        }

        previous?.Dispose();
    }

    private static void ConfigureIcon(ToolStripItem item, FluentSymbols.AllSymbols symbol)
    {
        Image? previous = item.Image;
        item.ConfigureItem(symbol, tooltipText: item.Text);

        if (!ReferenceEquals(previous, item.Image))
        {
            previous?.Dispose();
        }
    }

    /// <summary>Gets a labeled snapshot of all three text panels, including hidden pages.</summary>
    internal string AllDiagnosticText
        => $"Diagnostics{Environment.NewLine}{_diagnostics.DiagnosticText}{Environment.NewLine}{Environment.NewLine}"
            + $"Log{Environment.NewLine}{_recordingView.DiagnosticText}{Environment.NewLine}{Environment.NewLine}"
            + $"CAN Diagnostics{Environment.NewLine}{_instruments.CanDiagnosticText}";

    private async void OnCopyDiagnostics(object? sender, EventArgs e)
        => await CopyDiagnosticTextAsync(_diagnostics.DiagnosticText, "Diagnostics");

    private async void OnCopyLog(object? sender, EventArgs e)
        => await CopyDiagnosticTextAsync(_recordingView.DiagnosticText, "Log");

    private async void OnCopyCanDiagnostics(object? sender, EventArgs e)
        => await CopyDiagnosticTextAsync(_instruments.CanDiagnosticText, "CAN Diagnostics");

    private async void OnCopyAllDiagnostics(object? sender, EventArgs e)
        => await CopyDiagnosticTextAsync(AllDiagnosticText, "All diagnostics");

    private async Task CopyDiagnosticTextAsync(string text, string source)
    {
        try
        {
            if (string.IsNullOrEmpty(text))
            {
                _statusLabel.Text = $"{source} has no text to copy.";

                return;
            }

            Clipboard.SetText(text);
            _statusLabel.Text = $"{source} copied to the clipboard.";
        }
        catch (System.Runtime.InteropServices.ExternalException exception)
        {
            await ShowFailureAsync("The clipboard is unavailable. Try copying again.", exception);
        }
    }

    private async Task ShowFailureAsync(string heading, Exception exception)
    {
        _statusLabel.Text = exception.Message;
        System.Diagnostics.Trace.TraceError(exception.ToString());
        await TaskDialog.ShowDialogAsync(this, new TaskDialogPage
        {
            Heading = heading,
            Text = exception.Message,
            Icon = TaskDialogIcon.Error,
            Buttons = { TaskDialogButton.OK }
        });
    }

    private void OnDisposed(object? sender, EventArgs e)
    {
        Disposed -= OnDisposed;
        _hub.BluetoothStatusChanged -= OnBluetoothStatusChanged;
        _hub.CanDiagnosticReceived -= OnCanDiagnostic;
        _hub.ShutdownProgress -= OnShutdownProgress;
        _formLifetime.Cancel();
        _ = Task.Run(ObserveOwnerShutdownAsync);
        _hub.FrameReady -= PresentFrame;
        _uiFont?.Dispose();
        _shutdownFont?.Dispose();

        foreach (ToolStripItem item in _toolbar.Items)
        {
            if (item is not WarpToolkit.WinForms.ToolStrips.ISymbolToolStripItem)
            {
                item.Image?.Dispose();
            }
        }
    }

    private string BluetoothStatusSuffix()
        => $" | {_hub.ObdStatus}" + (_hub.ObdStatistics is { } obd
            ? $" | OBD2 {obd.SamplesPerSecond:F1} Hz / {obd.LastRequestLatency.TotalMilliseconds:F1} ms"
                + (obd.Elapsed >= TimeSpan.FromSeconds(1) && obd.SamplesPerSecond < 10
                    ? " WARNING: below 10 Hz" : "")
            : "");

    private async void OnBluetoothStatusChanged(string status)
    {
        try
        {
            await InvokeAsync(() =>
            {
                if (!_closing && !_stopping)
                {
                    string text = _statusLabel.Text ?? string.Empty;
                    int suffix = text.IndexOf(" | OBD2", StringComparison.Ordinal);
                    _statusLabel.Text = (suffix >= 0 ? text[..suffix] : text) + BluetoothStatusSuffix();
                }
            }, _formLifetime.Token);
        }
        catch (OperationCanceledException) when (_formLifetime.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (IsDisposed)
        {
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Bluetooth status presentation failed: {exception}");
        }
    }

    private async Task ObserveOwnerShutdownAsync()
    {
        try
        {
            await _hub.DisposeAsync();
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError($"Acquisition owner shutdown failed: {exception}");
        }
        finally
        {
            _formLifetime.Dispose();
        }
    }
}
