using WarpToolkit.Windows.Sensors;
using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

/// <summary>
///  Edits the source, display, route, and recording settings relevant to the restored dashboard.
/// </summary>
internal partial class OptionsDialog : Form
{
    private DemoOptions _options;
    private Font? _toolbarFont;

    /// <summary>
    ///  Initializes a new instance of the <see cref="OptionsDialog"/> class.
    /// </summary>
    /// <param name="options">The options to edit.</param>
    internal OptionsDialog(DemoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        InitializeComponent();
        ApplySectionToolbar();
        LoadOptions();
    }

    /// <summary>
    ///  Gets the accepted options.
    /// </summary>
    internal DemoOptions Options => _options;

    /// <inheritdoc/>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
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

    private void OnSelectedSectionChanged(object? sender, EventArgs e)
        => UpdateSectionButtons();

    private void UpdateSectionButtons()
    {
        _sourceSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _sourcePage);
        _displaySectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _displayPage);
        _mapSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _mapPage);
        _recordingSectionButton.Checked = ReferenceEquals(_tabControl.SelectedTab, _recordingPage);
    }

    private void LoadOptions()
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

        _accelerationDigitsUpDown.Value = _options.ReadingFormats.AccelerationDigits;
        _gpsDigitsUpDown.Value = _options.ReadingFormats.GpsDigits;
        _speedDigitsUpDown.Value = _options.ReadingFormats.SpeedDigits;
        _otherDigitsUpDown.Value = _options.ReadingFormats.RestDigits;
        _dampingUpDown.Value = _options.DampingMilliseconds;
        _gScaleUpDown.Value = (decimal)_options.GScale;
        _baseFontUpDown.Value = (decimal)_options.BaseFontPointSize;
        _toolbarSizeComboBox.DataSource = Enum.GetValues<DashboardToolbarSize>();
        _toolbarSizeComboBox.SelectedItem = _options.ToolbarSize;

        _routePathTextBox.Text = _options.RouteFilePath ?? string.Empty;
        _followPositionCheckBox.Checked = _options.FollowCurrentPosition;
        _mapDebugCheckBox.Checked = _options.ShowMapDebugInformation;
        _recordingFolderTextBox.Text = _options.RecordingRootFolder;
        _jsonLinesCheckBox.Checked =
            (_options.RecordingFormats & SensorRecordingFormats.VerboseJsonLines) != 0;
        _compactJsonCheckBox.Checked =
            (_options.RecordingFormats & SensorRecordingFormats.CompactJson) != 0;
        _csvCheckBox.Checked =
            (_options.RecordingFormats & SensorRecordingFormats.SemicolonCsv) != 0;
        _autoRecordingCheckBox.Checked = _options.AutoRecording;

        UpdateSerialPortState();
        UpdateNumberPreview();
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
        SensorRecordingFormats formats = SensorRecordingFormats.None;

        if (_jsonLinesCheckBox.Checked)
        {
            formats |= SensorRecordingFormats.VerboseJsonLines;
        }

        if (_compactJsonCheckBox.Checked)
        {
            formats |= SensorRecordingFormats.CompactJson;
        }

        if (_csvCheckBox.Checked)
        {
            formats |= SensorRecordingFormats.SemicolonCsv;
        }

        if (formats == SensorRecordingFormats.None)
        {
            MessageBox.Show(
                this,
                "Select at least one recording format.",
                "WARP sensor demo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
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

        _options = _options with
        {
            SourceKind = sourceKind,
            SerialPortName = string.IsNullOrWhiteSpace(_serialPortComboBox.Text)
                ? null
                : _serialPortComboBox.Text.Trim(),
            ReadingFormats = new ReadingFormats(
                (int)_accelerationDigitsUpDown.Value,
                (int)_gpsDigitsUpDown.Value,
                (int)_speedDigitsUpDown.Value,
                (int)_otherDigitsUpDown.Value).Normalized(),
            DampingMilliseconds = (int)_dampingUpDown.Value,
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
            RecordingRootFolder = Path.GetFullPath(_recordingFolderTextBox.Text.Trim()),
            RecordingFormats = formats,
            AutoRecording = _autoRecordingCheckBox.Checked
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
    }

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
}
