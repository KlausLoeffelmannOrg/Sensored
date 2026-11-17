using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.ToolStrips;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.ComponentModel;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>
///  Edits the source, display, route, and recording settings relevant to the restored dashboard.
/// </summary>
internal sealed partial class OptionsDialog : Form
{
    private const string BoundsSettingsKey = "Options.Bounds";
    private readonly IUserSettingsService? _userSettings;
    private DemoOptions _options;
    private readonly string? _routeDirectory;
    private Font? _toolbarFont;
    private readonly Action<string>? _reportBluetoothDiscoveryStatus;

    /// <summary>
    ///  Initializes a new instance of the <see cref="OptionsDialog"/> class.
    /// </summary>
    /// <param name="options">The options to edit.</param>
    /// <param name="routeDirectory">The last directory used when choosing a route file.</param>
    /// <param name="userSettings">The settings store used to remember dialog bounds.</param>
    /// <param name="bluetoothAvailable">Whether Bluetooth can be newly selected. A stored selection stays visible.</param>
    /// <param name="readSensorReference">Reads the current uncorrected pose for one display source.</param>
    /// <param name="reportBluetoothDiscoveryStatus">Reports discovery progress to the owner status bar.</param>
    /// <param name="bluetoothScanner">The discovery provider, or null for Windows discovery.</param>
    internal OptionsDialog(
        DemoOptions options,
        string? routeDirectory = null,
        IUserSettingsService? userSettings = null,
        bool bluetoothAvailable = false,
        Func<SensorDisplaySource, SensorReferenceSample?>? readSensorReference = null,
        Action<string>? reportBluetoothDiscoveryStatus = null,
        IBluetoothDeviceScanner? bluetoothScanner = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _routeDirectory = routeDirectory;
        _userSettings = userSettings;
        _bluetoothAvailable = bluetoothAvailable;
        _readSensorReference = readSensorReference;
        _reportBluetoothDiscoveryStatus = reportBluetoothDiscoveryStatus;
        _bluetoothScanner = bluetoothScanner ?? new BluetoothDeviceScanner();
        _allowStoredBluetooth = options.SensorSource == SensorDisplaySource.Bluetooth;
        InitializeComponent();
        BuildCalibrationPage();
        BuildInteractionPage();
        ApplySectionToolbar();
        LoadOptions();
        UpDownHeightWorkaround.Attach(this);
    }

    /// <summary>
    ///  Gets the accepted options.
    /// </summary>
    internal DemoOptions Options => _options;

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        DialogBoundsPlacement.Restore(this, _userSettings, BoundsSettingsKey);
    }

    /// <inheritdoc/>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        DialogBoundsPlacement.Save(this, _userSettings, BoundsSettingsKey);
        _toolbarFont?.Dispose();
        _toolbarFont = null;
        base.OnFormClosed(e);
    }

    private void ApplySectionToolbar()
    {
        int symbolSize = (int)_options.ToolbarSize;
        SymbolToolStripAppearance.Apply(
            _sectionToolStrip,
            new Size(symbolSize, symbolSize));

        float toolbarFontSize = Math.Max(7f, Font.SizeInPoints * 0.85f);
        _toolbarFont = new Font(
            Font.FontFamily,
            toolbarFontSize,
            Font.Style,
            GraphicsUnit.Point);
        _sectionToolStrip.Font = _toolbarFont;

        _sourceSectionButton.Symbol = SensorDemoSymbols.OptionsSource;
        _sourceSectionButton.ToolTipText = "Sensor and GPS source options";
        _displaySectionButton.Symbol = SensorDemoSymbols.OptionsDisplay;
        _displaySectionButton.ToolTipText = "Dashboard formatting and sizing options";
        _mapSectionButton.Symbol = SensorDemoSymbols.OptionsMap;
        _mapSectionButton.ToolTipText = "Route and current-position options";
        _recordingSectionButton.Symbol = SensorDemoSymbols.OptionsRecording;
        _recordingSectionButton.ToolTipText = "Recording folder and file format options";
        _replaySectionButton.Symbol = SensorDemoSymbols.OptionsRecording;
        _replaySectionButton.ToolTipText = "Replay sequence and transport options";
        _sensorsSectionButton.Symbol = SensorDemoSymbols.OptionsSource;
        _sensorsSectionButton.ToolTipText = "Display sensor source and independent reference frames";
        _bluetoothSectionButton.Symbol = SensorDemoSymbols.OptionsSource;
        _bluetoothSectionButton.ToolTipText = "Discover paired and unpaired devices and choose OBD2 and sensor roles";
        UpdateSectionButtons();
    }

    private void OnSourceSectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _sourcePage;

    private void OnDisplaySectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _displayPage;

    private void OnMapSectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _mapPage;

    private void OnRecordingSectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _recordingPage;

    private void OnReplaySectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _replayPage;

    private void OnSelectedSectionChanged(object? sender, EventArgs e)
    {
        UpdateSectionButtons();
        if (_tabControl.SelectedTab == _bluetoothPage && !_bluetoothScanStarted)
        {
            OnScanBluetoothClick(sender, e);
        }
    }

    private void UpdateSectionButtons()
    {
        _sourceSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _sourcePage);
        _displaySectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _displayPage);
        _mapSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _mapPage);
        _recordingSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _recordingPage);
        _replaySectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _replayPage);
        _calibrationSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _calibrationPage);
        _sensorsSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _sensorsPage);
        _bluetoothSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _bluetoothPage);
    }

    private void LoadOptions()
    {
        _loadingOptions = true;
        try
        {
            LoadOptionsCore();
        }
        finally
        {
            _loadingOptions = false;
        }

        UpdateSensorSourceAvailability();
    }

    private void LoadOptionsCore()
    {
        _sourceComboBox.DataSource = new[]
        {
            new SourceChoice("Windows IMU + Windows GPS", WindowsSensorSourceKind.ImuAndGps),
            new SourceChoice("Windows IMU only", WindowsSensorSourceKind.ImuOnly),
            new SourceChoice("Windows IMU + serial NMEA GPS", WindowsSensorSourceKind.ImuAndSerialNmeaGps),
            new SourceChoice("No sources (health only)", WindowsSensorSourceKind.None)
        };
        _sourceComboBox.DisplayMember = nameof(SourceChoice.Caption);
        _sourceComboBox.SelectedItem = ((SourceChoice[])_sourceComboBox.DataSource)
            .First(choice => choice.Kind == _options.SourceKind);
        _serialPortComboBox.Text = _options.SerialPortName ?? string.Empty;
        _gnssProfileComboBox.DataSource = Enum.GetValues<SerialGnssProfile>()
            .Select(profile => new ProfileChoice(
                DemoOptions.DescribeGpsProfile(profile),
                profile))
            .ToArray();
        _gnssProfileComboBox.DisplayMember = nameof(ProfileChoice.Caption);
        _gnssProfileComboBox.SelectedItem = ((ProfileChoice[])_gnssProfileComboBox.DataSource)
            .First(choice => choice.Profile == _options.SerialGnssProfile);
        _allowReceiverCommandsCheckBox.Checked = _options.AllowReceiverCommandsThisSession;
        _gpsTargetRateComboBox.DataSource = DemoOptions.GpsTargetIntervals
            .Select(interval => new GpsRateChoice(interval == 0
                ? "Auto (serial fastest / internal 1 Hz)"
                : $"{1000d / interval:0.#} Hz", interval))
            .ToArray();
        _gpsTargetRateComboBox.DisplayMember = nameof(GpsRateChoice.Caption);
        _gpsTargetRateComboBox.SelectedItem = ((GpsRateChoice[])_gpsTargetRateComboBox.DataSource)
            .First(choice => choice.Interval == (_options.TargetGpsFixIntervalMilliseconds ?? 0));
        _obdSearchUpDown.Value = _options.ObdSearchMinutes;
        _obdAdapterComboBox.DataSource = Enum.GetValues<ObdAdapterKind>();
        _obdAdapterComboBox.SelectedItem = _options.ObdAdapter;
        LoadBluetoothOptions();
        _accelerationDampingComboBox.DataSource = Enum.GetValues<SpeedometerAccelerationDamping>();
        _accelerationDampingComboBox.SelectedItem = _options.AccelerationDamping;
        _exceptionPopupUpDown.Value = _options.ExceptionPopupSeconds;
        _sourceApplyHintLabel.Text =
            "GPS changes apply immediately to live acquisition. Motion warnings can be overridden if detection is incorrect. Choose OBD and sensor devices on the Bluetooth page; pairing is not requested. AD20 requires ELM-compatible firmware.";

        _accelerationDigitsUpDown.Value = _options.ReadingFormats.AccelerationDigits;
        _speedometerOptions.Settings = _options.Speedometer;
        _gpsDigitsUpDown.Value = _options.ReadingFormats.GpsDigits;
        _speedDigitsUpDown.Value = _options.ReadingFormats.SpeedDigits;
        _otherDigitsUpDown.Value = _options.ReadingFormats.RestDigits;
        _dampingUpDown.Value = _options.DampingMilliseconds;
        _gScaleUpDown.Value = (decimal)_options.GScale;
        _baseFontUpDown.Value = (decimal)_options.BaseFontPointSize;
        _toolbarSizeComboBox.DataSource = Enum.GetValues<DashboardToolbarSize>();
        _toolbarSizeComboBox.SelectedItem = _options.ToolbarSize;
        _forceObdGaugesCheckBox.Checked = _options.ForceObdGaugesOn;
        _sphericalHorizonCheckBox.Checked = _options.UseSphericalHorizon;
        _alwaysOnCheckBox.Checked = _options.AlwaysOn;
        _sensorSourceComboBox.DataSource = new[]
        {
            new SensorSourceChoice("Internal sensors", SensorDisplaySource.Internal),
            new SensorSourceChoice("Bluetooth sensors", SensorDisplaySource.Bluetooth)
        };
        _sensorSourceComboBox.DisplayMember = nameof(SensorSourceChoice.Caption);
        _sensorSourceComboBox.SelectedItem = _sensorSourceComboBox.Items
            .OfType<SensorSourceChoice>()
            .First(choice => choice.Source == _options.SensorSource);
        _bluetoothSearchUpDown.Value = _options.BluetoothSensorSearchMinutes;
        _internalFrame = _options.InternalSensorFrame;
        _bluetoothFrame = _options.BluetoothSensorFrame;
        UpdateSensorFrameLabels();

        _routePathTextBox.Text = _options.RouteFilePath ?? string.Empty;
        _followPositionCheckBox.Checked = _options.FollowCurrentPosition;
        _mapDebugCheckBox.Checked = _options.ShowMapDebugInformation;
        _onlineTilesCheckBox.Checked = _options.AllowOnlineMapTiles;
        _recordingFolderTextBox.Text = _options.RecordingRootFolder;
        _jsonLinesCheckBox.Checked =
            (_options.RecordingFormats & SensorRecordingFormats.VerboseJsonLines) != 0;
        _compactJsonCheckBox.Checked = true;
        _csvCheckBox.Checked = true;
        _rawSnapshotRate.Value = _options.RawSnapshotsPerSecond;
        _recordingMarkerLabel.Text = _options.RecordingMarkerLabel;
        _autoRecordingCheckBox.Checked = _options.AutoRecording;
        _detectReplaySequenceCheckBox.Checked = _options.DetectReplaySequenceFiles;
        _hideReplayControlsCheckBox.Checked = _options.HideReplayControls;
        _sessionLockRadio.Checked = _options.FrameMode == TabletFrameMode.SessionStartLock;
        _continuousRadio.Checked = !_sessionLockRadio.Checked;
        _staticMountCheckBox.Checked = _options.AssumeStaticMountedDevice;

        UpdateSerialPortState();
        UpdateNumberPreview();
    }

    private readonly TabPage _calibrationPage = new() { Text = "Calibration", Padding = new Padding(12) };
    private readonly bool _bluetoothAvailable;
    private readonly Func<SensorDisplaySource, SensorReferenceSample?>? _readSensorReference;
    private bool _allowStoredBluetooth;
    private bool _loadingOptions;
    private SensorReferenceFrame _internalFrame;
    private SensorReferenceFrame _bluetoothFrame;

    private void OnSensorsSectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _sensorsPage;

    private void OnEditInternalReferenceClick(object? sender, EventArgs e)
        => EditSensorFrame(internalSensors: true);

    private void OnEditBluetoothReferenceClick(object? sender, EventArgs e)
        => EditSensorFrame(internalSensors: false);

    private void OnResetInternalReferenceClick(object? sender, EventArgs e)
        => ResetSensorFrame(internalSensors: true);

    private void OnResetBluetoothReferenceClick(object? sender, EventArgs e)
        => ResetSensorFrame(internalSensors: false);

    private void OnSensorSourceChanged(object? sender, EventArgs e)
    {
        if (_loadingOptions)
        {
            return;
        }

        if (_sensorSourceComboBox.SelectedItem is SensorSourceChoice { Source: SensorDisplaySource.Bluetooth }
            && !_bluetoothAvailable
            && !_allowStoredBluetooth)
        {
            _loadingOptions = true;
            try
            {
                SelectSensorSource(SensorDisplaySource.Internal);
            }
            finally
            {
                _loadingOptions = false;
            }
        }
        else if (_sensorSourceComboBox.SelectedItem is SensorSourceChoice { Source: not SensorDisplaySource.Bluetooth })
        {
            _allowStoredBluetooth = false;
        }

        UpdateSensorSourceAvailability();
    }

    private void EditSensorFrame(bool internalSensors)
    {
        SensorReferenceFrame current = internalSensors ? _internalFrame : _bluetoothFrame;
        string caption = internalSensors
            ? "Internal sensor reference"
            : "Bluetooth sensor reference";
        Func<SensorReferenceSample?>? readSample = _readSensorReference is null
            ? null
            : () => _readSensorReference(internalSensors
                ? SensorDisplaySource.Internal
                : SensorDisplaySource.Bluetooth);
        using SensorReferenceFrameDialog dialog = new(current, caption, readSample, Font,
            resetFrame: internalSensors ? default : SensorReferenceFrame.BluetoothDefault,
            allowAxisSwap: !internalSensors);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        if (internalSensors)
        {
            _internalFrame = dialog.Frame;
        }
        else
        {
            _bluetoothFrame = dialog.Frame;
        }

        UpdateSensorFrameLabels();
    }

    private void SelectSensorSource(SensorDisplaySource source)
    {
        _sensorSourceComboBox.SelectedItem = _sensorSourceComboBox.Items
            .OfType<SensorSourceChoice>()
            .First(choice => choice.Source == source);
    }

    private SensorDisplaySource SelectedSensorSource()
    {
        if (_sensorSourceComboBox.SelectedItem is not SensorSourceChoice sensorSource)
        {
            throw new InvalidOperationException("Select a display sensor source before accepting options.");
        }

        if (sensorSource.Source == SensorDisplaySource.Bluetooth
            && !_bluetoothAvailable
            && _options.SensorSource != SensorDisplaySource.Bluetooth)
        {
            throw new InvalidOperationException("Bluetooth sensors are unavailable and cannot be newly selected.");
        }

        return sensorSource.Source;
    }

    private void UpdateSensorSourceAvailability()
    {
        _sensorSourceHintLabel.Text = _bluetoothAvailable
            ? "Bluetooth can be selected when a fresh reading is available. Choose the device on the Bluetooth page."
            : _allowStoredBluetooth
                ? "Bluetooth is unavailable. The stored selection remains visible, but it cannot be newly chosen after you switch away."
                : "Bluetooth is unavailable and cannot be newly selected.";
    }

    private void ResetSensorFrame(bool internalSensors)
    {
        if (internalSensors)
        {
            _internalFrame = default;
        }
        else
        {
            _bluetoothFrame = SensorReferenceFrame.BluetoothDefault;
        }

        UpdateSensorFrameLabels();
    }

    private void UpdateSensorFrameLabels()
    {
        _internalFrameLabel.Text = FormatFrame("Internal reference", _internalFrame);
        _bluetoothFrameLabel.Text = FormatFrame("Bluetooth reference", _bluetoothFrame);
    }

    private static string FormatFrame(string name, SensorReferenceFrame frame)
        => $"{name}: yaw {frame.YawDegrees:0.0}°, pitch {frame.PitchDegrees:0.0}°, roll {frame.RollDegrees:0.0}°"
            + (frame.SwapPitchAndRoll ? " (pitch/roll swapped)" : string.Empty);

    /// <summary>Pairs a display-source caption with the saved selection.</summary>
    private sealed record SensorSourceChoice(string Caption, SensorDisplaySource Source);

    private void OnObdAdapterFormat(object? sender, ListControlConvertEventArgs e)
        => e.Value = ObdAdapters.GetName((ObdAdapterKind)e.ListItem!);
    private readonly CheckBox _confirmGpsChanges = new()
    {
        Text = "Confirm temporary GPS receiver changes when selecting a source, profile or rate",
        AutoSize = true
    };
    private readonly CheckBox _confirmSensorStart = new()
    {
        Text = "Confirm that the vehicle is parked before starting receiver commands",
        AutoSize = true
    };
    private readonly NumericUpDown _toolbarCommandTimeout = new() { Minimum = 1, Maximum = 300 };
    private readonly NumericUpDown _toolbarIdleTimeout = new() { Minimum = 1, Maximum = 300 };

    private void BuildInteractionPage()
    {
        TabPage page = new() { Text = "Interaction", Padding = new Padding(12) };
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        panel.Controls.Add(_confirmGpsChanges);
        panel.Controls.Add(_confirmSensorStart);
        panel.Controls.Add(new Label
        {
            Text = "Return toolbar to root after a command (seconds)",
            AutoSize = true
        });
        panel.Controls.Add(_toolbarCommandTimeout);
        panel.Controls.Add(new Label
        {
            Text = "Return toolbar to root when browsing without a command (seconds)",
            AutoSize = true
        });
        panel.Controls.Add(_toolbarIdleTimeout);
        panel.Controls.Add(new Label
        {
            Text = "Disabling confirmations authorizes temporary receiver commands, not unsafe live changes. "
                + "Keep the receiver connected for restoration. Motion warnings always allow an explicit user override.",
            AutoSize = true,
            MaximumSize = new Size(640, 0)
        });
        page.Controls.Add(panel);
        _tabControl.TabPages.Add(page);
        SymbolToolStripButton button = new("Interaction") { Symbol = SensorDemoSymbols.OptionsSource };
        button.Click += (_, _) => _tabControl.SelectedTab = page;
        _sectionToolStrip.Items.Add(button);
        _tabControl.SelectedIndexChanged += (_, _) =>
            button.Checked = ReferenceEquals(_tabControl.SelectedTab, page);
        _confirmGpsChanges.Checked = _options.ConfirmGpsReceiverChanges;
        _confirmSensorStart.Checked = _options.ConfirmSensorStart;
        _toolbarCommandTimeout.Value = _options.ToolbarCommandTimeoutSeconds;
        _toolbarIdleTimeout.Value = _options.ToolbarIdleTimeoutSeconds;
    }

    private readonly SymbolToolStripButton _calibrationSectionButton = new() { Text = "Calibration" };
    private readonly RadioButton _sessionLockRadio = new()
    {
        Text = "Legacy down-only capture (not a complete vehicle installation)",
        Enabled = false,
        AutoSize = true,
        Checked = true
    };
    private readonly RadioButton _continuousRadio = new()
    {
        Text = "Legacy motion learning (current uncertainty gates abstain)",
        Enabled = false,
        AutoSize = true
    };
    private readonly CheckBox _staticMountCheckBox = new()
    {
        Text = "Assume static mounted device and reuse the last accepted calibration",
        Enabled = false,
        AutoSize = true
    };

    private void BuildCalibrationPage()
    {
        FlowLayoutPanel panel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };
        Label hint = new()
        {
            AutoSize = true,
            MaximumSize = new Size(640, 0),
            Text = "These legacy preferences are retained for compatibility but do not configure the current pattern engine. "
                + "Use the main toolbar's Fusion setup command to inspect support and capture the current tablet pose "
                + "under an explicit level-vehicle assumption, or enter independently measured axes. "
                + "Gravity alone cannot determine vehicle forward; select the forward direction. "
                + "Changes are saved for the next live session without interrupting acquisition or recording."
        };
        panel.Controls.Add(_sessionLockRadio);
        panel.Controls.Add(_continuousRadio);
        panel.Controls.Add(_staticMountCheckBox);
        panel.Controls.Add(hint);
        _calibrationPage.Controls.Add(panel);
        _tabControl.TabPages.Add(_calibrationPage);
        _calibrationSectionButton.Click += (_, _) => _tabControl.SelectedTab = _calibrationPage;
        _sectionToolStrip.Items.Add(_calibrationSectionButton);
    }

    private string ResolveRouteDirectory()
    {
        string? fromText = string.IsNullOrWhiteSpace(_routePathTextBox.Text)
            ? null
            : Path.GetDirectoryName(_routePathTextBox.Text);

        if (!string.IsNullOrWhiteSpace(fromText) && Directory.Exists(fromText))
        {
            return fromText;
        }

        if (!string.IsNullOrWhiteSpace(_routeDirectory) && Directory.Exists(_routeDirectory))
        {
            return _routeDirectory;
        }

        return Environment.CurrentDirectory;
    }

    private void OnSourceChanged(object? sender, EventArgs e)
        => UpdateSerialPortState();

    private void OnNumberFormatChanged(object? sender, EventArgs e)
        => UpdateNumberPreview();

    private void OnBrowseRouteClick(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            CheckFileExists = true,
            Filter = "Route files (*.gpx;*.csv)|*.gpx;*.csv|All files (*.*)|*.*",
            InitialDirectory = ResolveRouteDirectory(),
            Title = "Select route file"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _routePathTextBox.Text = dialog.FileName;
        }
    }

    private void OnBrowseRecordingFolderClick(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the root folder for sensor recordings",
            InitialDirectory = _recordingFolderTextBox.Text,
            ShowNewFolderButton = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _recordingFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void OnAcceptClick(object? sender, EventArgs e)
    {
        SensorRecordingFormats formats = SensorRecordingFormats.CompactJson;

        if (_jsonLinesCheckBox.Checked)
        {
            formats |= SensorRecordingFormats.VerboseJsonLines;
        }

        if (string.IsNullOrWhiteSpace(_recordingFolderTextBox.Text))
        {
            MessageBox.Show(
                this,
                "Select a recording root folder.",
                "WARP sensor demo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        WindowsSensorSourceKind sourceKind = _sourceComboBox.SelectedItem is SourceChoice choice
            ? choice.Kind
            : WindowsSensorSourceKind.ImuAndGps;
        SerialGnssProfile profile = _gnssProfileComboBox.SelectedItem is ProfileChoice selected
            ? selected.Profile
            : SerialGnssProfile.Passive;
        int? targetInterval = _gpsTargetRateComboBox.SelectedItem is GpsRateChoice { Interval: > 0 } selectedRate
            ? selectedRate.Interval : null;
        if (profile == SerialGnssProfile.Passive
            && targetInterval != _options.TargetGpsFixIntervalMilliseconds)
        {
            profile = SerialGnssProfile.Current;
        }

        string? portName = string.IsNullOrWhiteSpace(_serialPortComboBox.Text)
            ? null : _serialPortComboBox.Text.Trim();
        bool authorize = sourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
            && profile != SerialGnssProfile.Passive && _allowReceiverCommandsCheckBox.Checked;
        bool receiverChanged = sourceKind != _options.SourceKind
            || !string.Equals(portName, _options.SerialPortName, StringComparison.OrdinalIgnoreCase)
            || profile != _options.SerialGnssProfile || targetInterval != _options.TargetGpsFixIntervalMilliseconds;
        if (authorize && (!_options.AllowReceiverCommandsThisSession || receiverChanged)
            && _confirmGpsChanges.Checked
            && !WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(this,
                new SerialNmeaGpsSourceOptions
                {
                    GnssProfile = profile,
                    AllowReceiverCommandsThisSession = false
                }))
        {
            return;
        }

        _options = _options with
        {
            SourceKind = sourceKind,
            Speedometer = _speedometerOptions.Settings,
            SerialPortName = portName,
            SerialGnssProfile = profile,
            TargetGpsFixIntervalMilliseconds = targetInterval,
            WindowsGpsReportIntervalMilliseconds = _gpsTargetRateComboBox.SelectedItem is GpsRateChoice { Interval: > 0 } windowsRate
                ? (uint)windowsRate.Interval : 1000u,
            ObdSearchMinutes = (int)_obdSearchUpDown.Value,
            ObdAdapter = (ObdAdapterKind)_obdAdapterComboBox.SelectedItem!,
            ObdDevice = _selectedObdDevice,
            BluetoothSensorDevice = _selectedSensorDevice,
            ExceptionPopupSeconds = (int)_exceptionPopupUpDown.Value,
            ForceObdGaugesOn = _forceObdGaugesCheckBox.Checked,
            UseSphericalHorizon = _sphericalHorizonCheckBox.Checked,
            AlwaysOn = _alwaysOnCheckBox.Checked,
            SensorSource = SelectedSensorSource(),
            BluetoothSensorSearchMinutes = (int)_bluetoothSearchUpDown.Value,
            InternalSensorFrame = _internalFrame,
            BluetoothSensorFrame = _bluetoothFrame,
            ConfirmGpsReceiverChanges = _confirmGpsChanges.Checked,
            ConfirmSensorStart = _confirmSensorStart.Checked,
            ToolbarCommandTimeoutSeconds = (int)_toolbarCommandTimeout.Value,
            ToolbarIdleTimeoutSeconds = (int)_toolbarIdleTimeout.Value,
            AllowReceiverCommandsThisSession = authorize,
            ReadingFormats = new ReadingFormats(
                (int)_accelerationDigitsUpDown.Value,
                (int)_gpsDigitsUpDown.Value,
                (int)_speedDigitsUpDown.Value,
                (int)_otherDigitsUpDown.Value).Normalized(),
            DampingMilliseconds = (int)_dampingUpDown.Value,
            AccelerationDamping = (SpeedometerAccelerationDamping)_accelerationDampingComboBox.SelectedItem!,
            GScale = (double)_gScaleUpDown.Value,
            BaseFontPointSize = (float)_baseFontUpDown.Value,
            ToolbarSize = _toolbarSizeComboBox.SelectedItem is DashboardToolbarSize toolbarSize
                ? toolbarSize
                : DashboardToolbarSize.Medium,
            RouteFilePath = string.IsNullOrWhiteSpace(_routePathTextBox.Text)
                ? null
                : _routePathTextBox.Text.Trim(),
            FollowCurrentPosition = _followPositionCheckBox.Checked,
            ShowMapDebugInformation = _mapDebugCheckBox.Checked,
            AllowOnlineMapTiles = _onlineTilesCheckBox.Checked,
            RecordingRootFolder = Path.GetFullPath(_recordingFolderTextBox.Text.Trim()),
            RecordingFormats = formats,
            RawSnapshotsPerSecond = (int)_rawSnapshotRate.Value,
            RecordingMarkerLabel = string.IsNullOrWhiteSpace(_recordingMarkerLabel.Text)
                ? "Marker" : _recordingMarkerLabel.Text.Trim(),
            AutoRecording = _autoRecordingCheckBox.Checked,
            DetectReplaySequenceFiles = _detectReplaySequenceCheckBox.Checked,
            HideReplayControls = _hideReplayControlsCheckBox.Checked,
            FrameMode = _sessionLockRadio.Checked
                ? TabletFrameMode.SessionStartLock
                : TabletFrameMode.ContinuousMotion,
            AssumeStaticMountedDevice = _staticMountCheckBox.Checked
        };

        DialogResult = DialogResult.OK;
    }

    private void UpdateSerialPortState()
    {
        bool serialSelected = _sourceComboBox.SelectedItem is SourceChoice
        {
            Kind: WindowsSensorSourceKind.ImuAndSerialNmeaGps
        };

        _serialPortLabel.Enabled = serialSelected;
        _serialPortComboBox.Enabled = serialSelected;
        _gnssProfileLabel.Enabled = serialSelected;
        _gnssProfileComboBox.Enabled = serialSelected;
        _allowReceiverCommandsCheckBox.Enabled = serialSelected
            && _gnssProfileComboBox.SelectedItem is ProfileChoice
            {
                Profile: not SerialGnssProfile.Passive
            };
        _receiverWarningLabel.Enabled = serialSelected;
    }

    private void OnGnssProfileChanged(object? sender, EventArgs e)
        => UpdateSerialPortState();

    /// <summary>Pairs a requested GPS interval with its human-readable rate.</summary>
    private sealed record GpsRateChoice(string Caption, int Interval);

    private void UpdateNumberPreview()
    {
        ReadingFormats formats = new(
            (int)_accelerationDigitsUpDown.Value,
            (int)_gpsDigitsUpDown.Value,
            (int)_speedDigitsUpDown.Value,
            (int)_otherDigitsUpDown.Value);

        _numberPreviewLabel.Text =
            $"Acceleration {ReadingFormats.Preview(formats.Acceleration, 1.234567d)} g    "
            + $"GPS {ReadingFormats.Preview(formats.Gps, 48.137154d)}    "
            + $"Speed {ReadingFormats.Preview(formats.Speed, 123.456789d)} km/h    "
            + $"Other {ReadingFormats.Preview(formats.Rest, 12.345678d)}";
    }

    /// <summary>
    ///  Associates a source caption with its service source kind.
    /// </summary>
    /// <param name="Caption">The displayed caption.</param>
    /// <param name="Kind">The service source kind.</param>
    private sealed record SourceChoice(string Caption, WindowsSensorSourceKind Kind);

    /// <summary>Associates a readable receiver profile caption with the backend selection.</summary>
    private sealed record ProfileChoice(string Caption, SerialGnssProfile Profile);
}
