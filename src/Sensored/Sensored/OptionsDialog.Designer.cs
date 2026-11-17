#nullable disable

using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

partial class OptionsDialog
{
    private System.ComponentModel.IContainer components;
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OptionsDialog));
        _rootLayout = new TableLayoutPanel();
        _sectionToolStrip = new SymbolToolStrip();
        _sourceSectionButton = new SymbolToolStripButton();
        _sensorsSectionButton = new SymbolToolStripButton();
        _displaySectionButton = new SymbolToolStripButton();
        _mapSectionButton = new SymbolToolStripButton();
        _recordingSectionButton = new SymbolToolStripButton();
        _replaySectionButton = new SymbolToolStripButton();
        _tabControl = new TabControl();
        _sourcePage = new TabPage();
        _sourceLayout = new TableLayoutPanel();
        _sourceLabel = new Label();
        _sourceComboBox = new ComboBox();
        _serialPortLabel = new Label();
        _serialPortComboBox = new ComboBox();
        _gnssProfileLabel = new Label();
        _gnssProfileComboBox = new ComboBox();
        _allowReceiverCommandsCheckBox = new CheckBox();
        _receiverWarningLabel = new Label();
        _sourceApplyHintLabel = new Label();
        _gpsTargetRateLabel = new Label();
        _gpsTargetRateComboBox = new ComboBox();
        _obdSearchLabel = new Label();
        _obdSearchUpDown = new NumericUpDown();
        _obdAdapterLabel = new Label();
        _obdAdapterComboBox = new ComboBox();
        _accelerationDampingLabel = new Label();
        _accelerationDampingComboBox = new ComboBox();
        _accelerationDampingHintLabel = new Label();
        _sensorsPage = new TabPage();
        _sensorsLayout = new TableLayoutPanel();
        _sensorSourceLabel = new Label();
        _sensorSourceComboBox = new ComboBox();
        _bluetoothSearchLabel = new Label();
        _bluetoothSearchUpDown = new NumericUpDown();
        _sensorSourceHintLabel = new Label();
        _internalFrameLabel = new Label();
        _internalFrameButtons = new FlowLayoutPanel();
        _editInternalReferenceButton = new Button();
        _resetInternalReferenceButton = new Button();
        _bluetoothFrameLabel = new Label();
        _bluetoothFrameButtons = new FlowLayoutPanel();
        _editBluetoothReferenceButton = new Button();
        _resetBluetoothReferenceButton = new Button();
        _sensorNoteLabel = new Label();
        _displayPage = new TabPage();
        _displayLayout = new TableLayoutPanel();
        _accelerationDigitsLabel = new Label();
        _accelerationDigitsUpDown = new NumericUpDown();
        _gpsDigitsLabel = new Label();
        _gpsDigitsUpDown = new NumericUpDown();
        _speedDigitsLabel = new Label();
        _speedDigitsUpDown = new NumericUpDown();
        _otherDigitsLabel = new Label();
        _otherDigitsUpDown = new NumericUpDown();
        _dampingLabel = new Label();
        _dampingUpDown = new NumericUpDown();
        _gScaleLabel = new Label();
        _gScaleUpDown = new NumericUpDown();
        _baseFontLabel = new Label();
        _baseFontUpDown = new NumericUpDown();
        _toolbarSizeLabel = new Label();
        _toolbarSizeComboBox = new ComboBox();
        _numberPreviewLabel = new Label();
        _speedometerOptions = new WarpToolkit.WinForms.Sensors.DirectX.SpeedometerOptionsView();
        _exceptionPopupLabel = new Label();
        _exceptionPopupUpDown = new NumericUpDown();
        _forceObdGaugesCheckBox = new CheckBox();
        _sphericalHorizonCheckBox = new CheckBox();
        _alwaysOnCheckBox = new CheckBox();
        _mapPage = new TabPage();
        _mapLayout = new TableLayoutPanel();
        _routePathLabel = new Label();
        _routePathTextBox = new TextBox();
        _browseRouteButton = new Button();
        _followPositionCheckBox = new CheckBox();
        _mapDebugCheckBox = new CheckBox();
        _onlineTilesCheckBox = new CheckBox();
        _offlineTilesHintLabel = new Label();
        _recordingPage = new TabPage();
        _recordingLayout = new TableLayoutPanel();
        _recordingFolderLabel = new Label();
        _recordingFolderTextBox = new TextBox();
        _browseRecordingFolderButton = new Button();
        _recordingFormatsGroupBox = new GroupBox();
        _recordingFormatsPanel = new FlowLayoutPanel();
        _jsonLinesCheckBox = new CheckBox();
        _compactJsonCheckBox = new CheckBox();
        _csvCheckBox = new CheckBox();
        _autoRecordingCheckBox = new CheckBox();
        _rawSnapshotRateLabel = new Label();
        _rawSnapshotRate = new NumericUpDown();
        _recordingMarkerHint = new Label();
        _recordingMarkerLabel = new TextBox();
        _replayPage = new TabPage();
        _replayLayout = new FlowLayoutPanel();
        _detectReplaySequenceCheckBox = new CheckBox();
        _hideReplayControlsCheckBox = new CheckBox();
        _buttonPanel = new FlowLayoutPanel();
        _okButton = new Button();
        _cancelButton = new Button();
        _bluetoothSectionButton = new SymbolToolStripButton();
        _bluetoothPage = new TabPage();
        _bluetoothLayout = new TableLayoutPanel();
        _bluetoothHint = new Label();
        _bluetoothActions = new FlowLayoutPanel();
        _scanBluetoothButton = new Button();
        _stopBluetoothScanButton = new Button();
        _bluetoothDevices = new ListView();
        _deviceNameColumn = new ColumnHeader();
        _deviceAddressColumn = new ColumnHeader();
        _deviceKindColumn = new ColumnHeader();
        _devicePairingColumn = new ColumnHeader();
        _deviceSignalColumn = new ColumnHeader();
        _obdDeviceLabel = new Label();
        _obdDeviceComboBox = new ComboBox();
        _sensorDeviceLabel = new Label();
        _sensorDeviceComboBox = new ComboBox();
        _bluetoothStatusStrip = new StatusStrip();
        _bluetoothScanStatus = new ToolStripStatusLabel();
        _bluetoothScanProgress = new ToolStripProgressBar();
        components = new System.ComponentModel.Container();
        _bluetoothScanTimer = new System.Windows.Forms.Timer(components);
        _rootLayout.SuspendLayout();
        _sectionToolStrip.SuspendLayout();
        _tabControl.SuspendLayout();
        _sourcePage.SuspendLayout();
        _sourceLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_obdSearchUpDown).BeginInit();
        _sensorsPage.SuspendLayout();
        _sensorsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_bluetoothSearchUpDown).BeginInit();
        _internalFrameButtons.SuspendLayout();
        _bluetoothFrameButtons.SuspendLayout();
        _displayPage.SuspendLayout();
        _displayLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_accelerationDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gpsDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_speedDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_otherDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dampingUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gScaleUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_baseFontUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_exceptionPopupUpDown).BeginInit();
        _mapPage.SuspendLayout();
        _mapLayout.SuspendLayout();
        _recordingPage.SuspendLayout();
        _recordingLayout.SuspendLayout();
        _recordingFormatsGroupBox.SuspendLayout();
        _recordingFormatsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_rawSnapshotRate).BeginInit();
        _replayPage.SuspendLayout();
        _replayLayout.SuspendLayout();
        _buttonPanel.SuspendLayout();
        _bluetoothPage.SuspendLayout();
        _bluetoothLayout.SuspendLayout();
        _bluetoothActions.SuspendLayout();
        _bluetoothStatusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // _rootLayout
        // 
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_sectionToolStrip, 0, 0);
        _rootLayout.Controls.Add(_tabControl, 0, 1);
        _rootLayout.Controls.Add(_bluetoothStatusStrip, 0, 2);
        _rootLayout.Controls.Add(_buttonPanel, 0, 3);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Margin = new Padding(7, 7, 7, 7);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(17, 20, 17, 20);
        _rootLayout.RowCount = 4;
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.RowStyles.Add(new RowStyle());
        _rootLayout.Size = new Size(1543, 1241);
        _rootLayout.TabIndex = 0;
        // 
        // _sectionToolStrip
        // 
        _sectionToolStrip.Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
        _sectionToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _sectionToolStrip.ImageScalingSize = new Size(64, 64);
        _sectionToolStrip.Items.AddRange(new ToolStripItem[] { _sourceSectionButton, _bluetoothSectionButton, _sensorsSectionButton, _displaySectionButton, _mapSectionButton, _recordingSectionButton, _replaySectionButton });
        _sectionToolStrip.Location = new Point(17, 20);
        _sectionToolStrip.Name = "_sectionToolStrip";
        _sectionToolStrip.Padding = new Padding(7, 3, 7, 3);
        _sectionToolStrip.Size = new Size(1509, 53);
        _sectionToolStrip.TabIndex = 0;
        // 
        // _sourceSectionButton
        // 
        _sourceSectionButton.CheckOnClick = true;
        _sourceSectionButton.Image = null;
        _sourceSectionButton.Name = "_sourceSectionButton";
        _sourceSectionButton.Size = new Size(101, 41);
        _sourceSectionButton.Text = "Source";
        _sourceSectionButton.Click += OnSourceSectionClick;
        // 
        // _sensorsSectionButton
        // 
        _sensorsSectionButton.CheckOnClick = true;
        _sensorsSectionButton.Image = null;
        _sensorsSectionButton.Name = "_sensorsSectionButton";
        _sensorsSectionButton.Size = new Size(111, 41);
        _sensorsSectionButton.Text = "Sensors";
        _sensorsSectionButton.Click += OnSensorsSectionClick;
        // 
        // _displaySectionButton
        // 
        _displaySectionButton.CheckOnClick = true;
        _displaySectionButton.Image = null;
        _displaySectionButton.Name = "_displaySectionButton";
        _displaySectionButton.Size = new Size(108, 41);
        _displaySectionButton.Text = "Display";
        _displaySectionButton.Click += OnDisplaySectionClick;
        // 
        // _mapSectionButton
        // 
        _mapSectionButton.CheckOnClick = true;
        _mapSectionButton.Image = null;
        _mapSectionButton.Name = "_mapSectionButton";
        _mapSectionButton.Size = new Size(163, 41);
        _mapSectionButton.Text = "Map / route";
        _mapSectionButton.Click += OnMapSectionClick;
        // 
        // _recordingSectionButton
        // 
        _recordingSectionButton.CheckOnClick = true;
        _recordingSectionButton.Image = null;
        _recordingSectionButton.Name = "_recordingSectionButton";
        _recordingSectionButton.Size = new Size(141, 41);
        _recordingSectionButton.Text = "Recording";
        _recordingSectionButton.Click += OnRecordingSectionClick;
        // 
        // _replaySectionButton
        // 
        _replaySectionButton.CheckOnClick = true;
        _replaySectionButton.Image = null;
        _replaySectionButton.Name = "_replaySectionButton";
        _replaySectionButton.Size = new Size(100, 41);
        _replaySectionButton.Text = "Replay";
        _replaySectionButton.Click += OnReplaySectionClick;
        // 
        // _tabControl
        // 
        _tabControl.Controls.Add(_sourcePage);
        _tabControl.Controls.Add(_bluetoothPage);
        _tabControl.Controls.Add(_sensorsPage);
        _tabControl.Controls.Add(_displayPage);
        _tabControl.Controls.Add(_mapPage);
        _tabControl.Controls.Add(_recordingPage);
        _tabControl.Controls.Add(_replayPage);
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Location = new Point(24, 80);
        _tabControl.Margin = new Padding(7, 7, 7, 7);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new Size(1495, 1033);
        _tabControl.TabIndex = 1;
        _tabControl.SelectedIndexChanged += OnSelectedSectionChanged;
        // 
        // _sourcePage
        // 
        _sourcePage.AutoScroll = true;
        _sourcePage.Controls.Add(_sourceLayout);
        _sourcePage.Location = new Point(8, 51);
        _sourcePage.Margin = new Padding(7, 7, 7, 7);
        _sourcePage.Name = "_sourcePage";
        _sourcePage.Padding = new Padding(17, 20, 17, 20);
        _sourcePage.Size = new Size(1479, 974);
        _sourcePage.TabIndex = 0;
        _sourcePage.Text = "Source";
        _sourcePage.UseVisualStyleBackColor = true;
        // 
        // _sourceLayout
        // 
        _sourceLayout.AutoSize = true;
        _sourceLayout.ColumnCount = 2;
        _sourceLayout.ColumnStyles.Add(new ColumnStyle());
        _sourceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _sourceLayout.Controls.Add(_sourceLabel, 0, 0);
        _sourceLayout.Controls.Add(_sourceComboBox, 1, 0);
        _sourceLayout.Controls.Add(_serialPortLabel, 0, 1);
        _sourceLayout.Controls.Add(_serialPortComboBox, 1, 1);
        _sourceLayout.Controls.Add(_gnssProfileLabel, 0, 2);
        _sourceLayout.Controls.Add(_gnssProfileComboBox, 1, 2);
        _sourceLayout.Controls.Add(_allowReceiverCommandsCheckBox, 1, 3);
        _sourceLayout.Controls.Add(_sourceApplyHintLabel, 1, 5);
        _sourceLayout.Controls.Add(_gpsTargetRateLabel, 0, 6);
        _sourceLayout.Controls.Add(_gpsTargetRateComboBox, 1, 6);
        _sourceLayout.Controls.Add(_accelerationDampingLabel, 0, 9);
        _sourceLayout.Controls.Add(_accelerationDampingComboBox, 1, 9);
        _sourceLayout.Controls.Add(_accelerationDampingHintLabel, 0, 10);
        _sourceLayout.Controls.Add(_receiverWarningLabel, 1, 4);
        _sourceLayout.Dock = DockStyle.Top;
        _sourceLayout.Location = new Point(17, 20);
        _sourceLayout.Margin = new Padding(7, 7, 7, 7);
        _sourceLayout.Name = "_sourceLayout";
        _sourceLayout.RowCount = 11;
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle());
        _sourceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
        _sourceLayout.Size = new Size(1445, 671);
        _sourceLayout.TabIndex = 0;
        // 
        // _sourceLabel
        // 
        _sourceLabel.Anchor = AnchorStyles.Left;
        _sourceLabel.AutoSize = true;
        _sourceLabel.Location = new Point(7, 10);
        _sourceLabel.Margin = new Padding(7, 0, 7, 0);
        _sourceLabel.Name = "_sourceLabel";
        _sourceLabel.Size = new Size(186, 37);
        _sourceLabel.TabIndex = 0;
        _sourceLabel.Text = "Sensor source:";
        // 
        // _sourceComboBox
        // 
        _sourceComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sourceComboBox.FormattingEnabled = true;
        _sourceComboBox.Location = new Point(466, 6);
        _sourceComboBox.Margin = new Padding(6, 6, 6, 6);
        _sourceComboBox.Name = "_sourceComboBox";
        _sourceComboBox.Size = new Size(973, 45);
        _sourceComboBox.TabIndex = 1;
        _sourceComboBox.SelectedIndexChanged += OnSourceChanged;
        // 
        // _serialPortLabel
        // 
        _serialPortLabel.Anchor = AnchorStyles.Left;
        _serialPortLabel.AutoSize = true;
        _serialPortLabel.Location = new Point(7, 67);
        _serialPortLabel.Margin = new Padding(7, 0, 7, 0);
        _serialPortLabel.Name = "_serialPortLabel";
        _serialPortLabel.Size = new Size(145, 37);
        _serialPortLabel.TabIndex = 2;
        _serialPortLabel.Text = "Serial port:";
        // 
        // _serialPortComboBox
        // 
        _serialPortComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _serialPortComboBox.FormattingEnabled = true;
        _serialPortComboBox.Location = new Point(466, 63);
        _serialPortComboBox.Margin = new Padding(6, 6, 6, 6);
        _serialPortComboBox.Name = "_serialPortComboBox";
        _serialPortComboBox.Size = new Size(973, 45);
        _serialPortComboBox.TabIndex = 3;
        // 
        // _gnssProfileLabel
        // 
        _gnssProfileLabel.Anchor = AnchorStyles.Left;
        _gnssProfileLabel.AutoSize = true;
        _gnssProfileLabel.Location = new Point(7, 124);
        _gnssProfileLabel.Margin = new Padding(7, 0, 7, 0);
        _gnssProfileLabel.Name = "_gnssProfileLabel";
        _gnssProfileLabel.Size = new Size(174, 37);
        _gnssProfileLabel.TabIndex = 4;
        _gnssProfileLabel.Text = "GNSS profile:";
        // 
        // _gnssProfileComboBox
        // 
        _gnssProfileComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _gnssProfileComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _gnssProfileComboBox.Location = new Point(466, 120);
        _gnssProfileComboBox.Margin = new Padding(6, 6, 6, 6);
        _gnssProfileComboBox.Name = "_gnssProfileComboBox";
        _gnssProfileComboBox.Size = new Size(973, 45);
        _gnssProfileComboBox.TabIndex = 5;
        _gnssProfileComboBox.SelectedIndexChanged += OnGnssProfileChanged;
        // 
        // _allowReceiverCommandsCheckBox
        // 
        _allowReceiverCommandsCheckBox.AutoSize = true;
        _allowReceiverCommandsCheckBox.Location = new Point(466, 177);
        _allowReceiverCommandsCheckBox.Margin = new Padding(6, 6, 6, 6);
        _allowReceiverCommandsCheckBox.Name = "_allowReceiverCommandsCheckBox";
        _allowReceiverCommandsCheckBox.Size = new Size(808, 41);
        _allowReceiverCommandsCheckBox.TabIndex = 6;
        _allowReceiverCommandsCheckBox.Text = "Authorize temporary receiver commands this session (not saved)";
        _allowReceiverCommandsCheckBox.UseVisualStyleBackColor = true;
        // 
        // _receiverWarningLabel
        // 
        _receiverWarningLabel.AutoSize = true;
        _receiverWarningLabel.Location = new Point(466, 230);
        _receiverWarningLabel.Margin = new Padding(6, 6, 6, 6);
        _receiverWarningLabel.Name = "_receiverWarningLabel";
        _receiverWarningLabel.Size = new Size(915, 74);
        _receiverWarningLabel.TabIndex = 7;
        _receiverWarningLabel.Text = "Passive by default. Only identified u-blox devices can be configured; restore requires a connected receiver.";
        // 
        // _sourceApplyHintLabel
        // 
        _sourceApplyHintLabel.AutoSize = true;
        _sourceApplyHintLabel.Location = new Point(466, 316);
        _sourceApplyHintLabel.Margin = new Padding(6, 6, 6, 6);
        _sourceApplyHintLabel.MaximumSize = new Size(1154, 0);
        _sourceApplyHintLabel.Name = "_sourceApplyHintLabel";
        _sourceApplyHintLabel.Size = new Size(912, 37);
        _sourceApplyHintLabel.TabIndex = 8;
        _sourceApplyHintLabel.Text = "Source changes apply on the next Start or Restart. Restart only while parked.";
        // 
        // _gpsTargetRateLabel
        // 
        _gpsTargetRateLabel.Anchor = AnchorStyles.Left;
        _gpsTargetRateLabel.AutoSize = true;
        _gpsTargetRateLabel.Location = new Point(7, 369);
        _gpsTargetRateLabel.Margin = new Padding(7, 0, 7, 0);
        _gpsTargetRateLabel.Name = "_gpsTargetRateLabel";
        _gpsTargetRateLabel.Size = new Size(291, 37);
        _gpsTargetRateLabel.TabIndex = 9;
        _gpsTargetRateLabel.Text = "Target GPS refresh rate:";
        // 
        // _gpsTargetRateComboBox
        // 
        _gpsTargetRateComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _gpsTargetRateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _gpsTargetRateComboBox.Location = new Point(466, 365);
        _gpsTargetRateComboBox.Margin = new Padding(6, 6, 6, 6);
        _gpsTargetRateComboBox.Name = "_gpsTargetRateComboBox";
        _gpsTargetRateComboBox.Size = new Size(973, 45);
        _gpsTargetRateComboBox.TabIndex = 9;
        // 
        // _obdSearchLabel
        // 
        _obdSearchLabel.Anchor = AnchorStyles.Left;
        _obdSearchLabel.AutoSize = true;
        _obdSearchLabel.Location = new Point(7, 425);
        _obdSearchLabel.Margin = new Padding(7, 0, 7, 0);
        _obdSearchLabel.Name = "_obdSearchLabel";
        _obdSearchLabel.Size = new Size(392, 37);
        _obdSearchLabel.TabIndex = 10;
        _obdSearchLabel.Text = "OBD2 search window (minutes):";
        // 
        // _obdSearchUpDown
        // 
        _obdSearchUpDown.Location = new Point(466, 422);
        _obdSearchUpDown.Margin = new Padding(6, 6, 6, 6);
        _obdSearchUpDown.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
        _obdSearchUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _obdSearchUpDown.Name = "_obdSearchUpDown";
        _obdSearchUpDown.Size = new Size(257, 43);
        _obdSearchUpDown.TabIndex = 10;
        _obdSearchUpDown.Value = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // _obdAdapterLabel
        // 
        _obdAdapterLabel.Anchor = AnchorStyles.Left;
        _obdAdapterLabel.AutoSize = true;
        _obdAdapterLabel.Location = new Point(7, 481);
        _obdAdapterLabel.Margin = new Padding(7, 7, 7, 7);
        _obdAdapterLabel.Name = "_obdAdapterLabel";
        _obdAdapterLabel.Size = new Size(272, 37);
        _obdAdapterLabel.TabIndex = 11;
        _obdAdapterLabel.Text = "Paired OBD2 adapter:";
        // 
        // _obdAdapterComboBox
        // 
        _obdAdapterComboBox.AccessibleName = "Paired OBD2 adapter (applies at the next OBD connection)";
        _obdAdapterComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _obdAdapterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _obdAdapterComboBox.FormattingEnabled = true;
        _obdAdapterComboBox.Location = new Point(466, 477);
        _obdAdapterComboBox.Margin = new Padding(6, 6, 6, 6);
        _obdAdapterComboBox.Name = "_obdAdapterComboBox";
        _obdAdapterComboBox.Size = new Size(973, 45);
        _obdAdapterComboBox.TabIndex = 11;
        _obdAdapterComboBox.Format += OnObdAdapterFormat;
        // 
        // _accelerationDampingLabel
        // 
        _accelerationDampingLabel.Anchor = AnchorStyles.Left;
        _accelerationDampingLabel.AutoSize = true;
        _accelerationDampingLabel.Location = new Point(7, 538);
        _accelerationDampingLabel.Margin = new Padding(7, 7, 7, 7);
        _accelerationDampingLabel.Name = "_accelerationDampingLabel";
        _accelerationDampingLabel.Size = new Size(446, 37);
        _accelerationDampingLabel.TabIndex = 12;
        _accelerationDampingLabel.Text = "Speedometer acceleration damping:";
        // 
        // _accelerationDampingComboBox
        // 
        _accelerationDampingComboBox.AccessibleName = "Speedometer forward acceleration damping";
        _accelerationDampingComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _accelerationDampingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _accelerationDampingComboBox.FormattingEnabled = true;
        _accelerationDampingComboBox.Location = new Point(466, 534);
        _accelerationDampingComboBox.Margin = new Padding(6, 6, 6, 6);
        _accelerationDampingComboBox.Name = "_accelerationDampingComboBox";
        _accelerationDampingComboBox.Size = new Size(973, 45);
        _accelerationDampingComboBox.TabIndex = 13;
        // 
        // _accelerationDampingHintLabel
        // 
        _accelerationDampingHintLabel.AutoSize = true;
        _sourceLayout.SetColumnSpan(_accelerationDampingHintLabel, 2);
        _accelerationDampingHintLabel.Dock = DockStyle.Fill;
        _accelerationDampingHintLabel.Location = new Point(6, 591);
        _accelerationDampingHintLabel.Margin = new Padding(6, 6, 6, 6);
        _accelerationDampingHintLabel.Name = "_accelerationDampingHintLabel";
        _accelerationDampingHintLabel.Size = new Size(1433, 74);
        _accelerationDampingHintLabel.TabIndex = 14;
        _accelerationDampingHintLabel.Text = resources.GetString("_accelerationDampingHintLabel.Text");
        // 
        // _sensorsPage
        // 
        _sensorsPage.AutoScroll = true;
        _sensorsPage.Controls.Add(_sensorsLayout);
        _sensorsPage.Location = new Point(8, 51);
        _sensorsPage.Margin = new Padding(7, 7, 7, 7);
        _sensorsPage.Name = "_sensorsPage";
        _sensorsPage.Padding = new Padding(17, 20, 17, 20);
        _sensorsPage.Size = new Size(1479, 974);
        _sensorsPage.TabIndex = 5;
        _sensorsPage.Text = "Sensors";
        _sensorsPage.UseVisualStyleBackColor = true;
        // 
        // _sensorsLayout
        // 
        _sensorsLayout.AutoSize = true;
        _sensorsLayout.ColumnCount = 2;
        _sensorsLayout.ColumnStyles.Add(new ColumnStyle());
        _sensorsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _sensorsLayout.Controls.Add(_sensorSourceLabel, 0, 0);
        _sensorsLayout.Controls.Add(_sensorSourceComboBox, 1, 0);
        _sensorsLayout.Controls.Add(_sensorSourceHintLabel, 0, 2);
        _sensorsLayout.Controls.Add(_internalFrameLabel, 0, 3);
        _sensorsLayout.Controls.Add(_internalFrameButtons, 1, 4);
        _sensorsLayout.Controls.Add(_bluetoothFrameLabel, 0, 5);
        _sensorsLayout.Controls.Add(_bluetoothFrameButtons, 1, 6);
        _sensorsLayout.Controls.Add(_sensorNoteLabel, 0, 7);
        _sensorsLayout.Dock = DockStyle.Top;
        _sensorsLayout.Location = new Point(17, 20);
        _sensorsLayout.Name = "_sensorsLayout";
        _sensorsLayout.RowCount = 8;
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.RowStyles.Add(new RowStyle());
        _sensorsLayout.Size = new Size(1445, 584);
        _sensorsLayout.TabIndex = 0;
        // 
        // _sensorSourceLabel
        // 
        _sensorSourceLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sensorSourceLabel.AutoSize = true;
        _sensorSourceLabel.Location = new Point(3, 9);
        _sensorSourceLabel.Margin = new Padding(3);
        _sensorSourceLabel.Name = "_sensorSourceLabel";
        _sensorSourceLabel.Size = new Size(338, 37);
        _sensorSourceLabel.TabIndex = 0;
        _sensorSourceLabel.Text = "Display source:";
        // 
        // _sensorSourceComboBox
        // 
        _sensorSourceComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sensorSourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sensorSourceComboBox.FormattingEnabled = true;
        _sensorSourceComboBox.Location = new Point(349, 5);
        _sensorSourceComboBox.Margin = new Padding(5);
        _sensorSourceComboBox.Name = "_sensorSourceComboBox";
        _sensorSourceComboBox.Size = new Size(1091, 45);
        _sensorSourceComboBox.TabIndex = 1;
        _sensorSourceComboBox.SelectedIndexChanged += OnSensorSourceChanged;
        // 
        // _bluetoothSearchLabel
        // 
        _bluetoothSearchLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothSearchLabel.AutoSize = true;
        _bluetoothSearchLabel.Location = new Point(3, 63);
        _bluetoothSearchLabel.Margin = new Padding(3);
        _bluetoothSearchLabel.Name = "_bluetoothSearchLabel";
        _bluetoothSearchLabel.Size = new Size(338, 37);
        _bluetoothSearchLabel.TabIndex = 2;
        _bluetoothSearchLabel.Text = "Bluetooth search (minutes):";
        // 
        // _bluetoothSearchUpDown
        // 
        _bluetoothSearchUpDown.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothSearchUpDown.Location = new Point(349, 60);
        _bluetoothSearchUpDown.Margin = new Padding(5);
        _bluetoothSearchUpDown.Maximum = new decimal(new int[] { 15, 0, 0, 0 });
        _bluetoothSearchUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _bluetoothSearchUpDown.Name = "_bluetoothSearchUpDown";
        _bluetoothSearchUpDown.Size = new Size(1091, 43);
        _bluetoothSearchUpDown.TabIndex = 3;
        _bluetoothSearchUpDown.Value = new decimal(new int[] { 3, 0, 0, 0 });
        // 
        // _sensorSourceHintLabel
        // 
        _sensorSourceHintLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sensorSourceHintLabel.AutoSize = true;
        _sensorsLayout.SetColumnSpan(_sensorSourceHintLabel, 2);
        _sensorSourceHintLabel.Location = new Point(5, 113);
        _sensorSourceHintLabel.Margin = new Padding(5);
        _sensorSourceHintLabel.MaximumSize = new Size(831, 0);
        _sensorSourceHintLabel.Name = "_sensorSourceHintLabel";
        _sensorSourceHintLabel.Size = new Size(831, 37);
        _sensorSourceHintLabel.TabIndex = 4;
        _sensorSourceHintLabel.Text = "Bluetooth can be selected when a fresh reading is available. Choose the device on the Bluetooth page.";
        // 
        // _internalFrameLabel
        // 
        _internalFrameLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _internalFrameLabel.AutoSize = true;
        _sensorsLayout.SetColumnSpan(_internalFrameLabel, 2);
        _internalFrameLabel.Location = new Point(3, 169);
        _internalFrameLabel.Margin = new Padding(3, 14, 3, 3);
        _internalFrameLabel.Name = "_internalFrameLabel";
        _internalFrameLabel.Size = new Size(1439, 37);
        _internalFrameLabel.TabIndex = 5;
        _internalFrameLabel.Text = "Internal reference";
        // 
        // _internalFrameButtons
        // 
        _internalFrameButtons.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _internalFrameButtons.AutoSize = true;
        _internalFrameButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _internalFrameButtons.Controls.Add(_editInternalReferenceButton);
        _internalFrameButtons.Controls.Add(_resetInternalReferenceButton);
        _internalFrameButtons.Location = new Point(347, 212);
        _internalFrameButtons.Name = "_internalFrameButtons";
        _internalFrameButtons.Size = new Size(1095, 57);
        _internalFrameButtons.TabIndex = 6;
        _internalFrameButtons.WrapContents = false;
        // 
        // _editInternalReferenceButton
        // 
        _editInternalReferenceButton.AutoSize = true;
        _editInternalReferenceButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _editInternalReferenceButton.Location = new Point(5, 5);
        _editInternalReferenceButton.Margin = new Padding(5);
        _editInternalReferenceButton.Name = "_editInternalReferenceButton";
        _editInternalReferenceButton.Size = new Size(304, 47);
        _editInternalReferenceButton.TabIndex = 0;
        _editInternalReferenceButton.Text = "Edit internal reference...";
        _editInternalReferenceButton.UseVisualStyleBackColor = true;
        _editInternalReferenceButton.Click += OnEditInternalReferenceClick;
        // 
        // _resetInternalReferenceButton
        // 
        _resetInternalReferenceButton.AutoSize = true;
        _resetInternalReferenceButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _resetInternalReferenceButton.Location = new Point(319, 5);
        _resetInternalReferenceButton.Margin = new Padding(5);
        _resetInternalReferenceButton.Name = "_resetInternalReferenceButton";
        _resetInternalReferenceButton.Size = new Size(215, 47);
        _resetInternalReferenceButton.TabIndex = 1;
        _resetInternalReferenceButton.Text = "Reset to all zero";
        _resetInternalReferenceButton.UseVisualStyleBackColor = true;
        _resetInternalReferenceButton.Click += OnResetInternalReferenceClick;
        // 
        // _bluetoothFrameLabel
        // 
        _bluetoothFrameLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothFrameLabel.AutoSize = true;
        _sensorsLayout.SetColumnSpan(_bluetoothFrameLabel, 2);
        _bluetoothFrameLabel.Location = new Point(3, 286);
        _bluetoothFrameLabel.Margin = new Padding(3, 14, 3, 3);
        _bluetoothFrameLabel.Name = "_bluetoothFrameLabel";
        _bluetoothFrameLabel.Size = new Size(1439, 37);
        _bluetoothFrameLabel.TabIndex = 7;
        _bluetoothFrameLabel.Text = "Bluetooth reference";
        // 
        // _bluetoothFrameButtons
        // 
        _bluetoothFrameButtons.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothFrameButtons.AutoSize = true;
        _bluetoothFrameButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _bluetoothFrameButtons.Controls.Add(_editBluetoothReferenceButton);
        _bluetoothFrameButtons.Controls.Add(_resetBluetoothReferenceButton);
        _bluetoothFrameButtons.Location = new Point(347, 329);
        _bluetoothFrameButtons.Name = "_bluetoothFrameButtons";
        _bluetoothFrameButtons.Size = new Size(1095, 57);
        _bluetoothFrameButtons.TabIndex = 8;
        _bluetoothFrameButtons.WrapContents = false;
        // 
        // _editBluetoothReferenceButton
        // 
        _editBluetoothReferenceButton.AutoSize = true;
        _editBluetoothReferenceButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _editBluetoothReferenceButton.Location = new Point(5, 5);
        _editBluetoothReferenceButton.Margin = new Padding(5);
        _editBluetoothReferenceButton.Name = "_editBluetoothReferenceButton";
        _editBluetoothReferenceButton.Size = new Size(330, 47);
        _editBluetoothReferenceButton.TabIndex = 0;
        _editBluetoothReferenceButton.Text = "Edit Bluetooth reference...";
        _editBluetoothReferenceButton.UseVisualStyleBackColor = true;
        _editBluetoothReferenceButton.Click += OnEditBluetoothReferenceClick;
        // 
        // _resetBluetoothReferenceButton
        // 
        _resetBluetoothReferenceButton.AutoSize = true;
        _resetBluetoothReferenceButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _resetBluetoothReferenceButton.Location = new Point(345, 5);
        _resetBluetoothReferenceButton.Margin = new Padding(5);
        _resetBluetoothReferenceButton.Name = "_resetBluetoothReferenceButton";
        _resetBluetoothReferenceButton.Size = new Size(215, 47);
        _resetBluetoothReferenceButton.TabIndex = 1;
        _resetBluetoothReferenceButton.Text = "Reset to all zero";
        _resetBluetoothReferenceButton.UseVisualStyleBackColor = true;
        _resetBluetoothReferenceButton.Click += OnResetBluetoothReferenceClick;
        // 
        // _sensorNoteLabel
        // 
        _sensorNoteLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sensorNoteLabel.AutoSize = true;
        _sensorsLayout.SetColumnSpan(_sensorNoteLabel, 2);
        _sensorNoteLabel.Location = new Point(5, 394);
        _sensorNoteLabel.Margin = new Padding(5);
        _sensorNoteLabel.MaximumSize = new Size(831, 0);
        _sensorNoteLabel.Name = "_sensorNoteLabel";
        _sensorNoteLabel.Size = new Size(831, 185);
        _sensorNoteLabel.TabIndex = 9;
        _sensorNoteLabel.Text = resources.GetString("_sensorNoteLabel.Text");
        // 
        // _displayPage
        // 
        _displayPage.AutoScroll = true;
        _displayPage.Controls.Add(_displayLayout);
        _displayPage.Location = new Point(8, 46);
        _displayPage.Margin = new Padding(7, 7, 7, 7);
        _displayPage.Name = "_displayPage";
        _displayPage.Padding = new Padding(17, 20, 17, 20);
        _displayPage.Size = new Size(1478, 977);
        _displayPage.TabIndex = 1;
        _displayPage.Text = "Display";
        _displayPage.UseVisualStyleBackColor = true;
        // 
        // _displayLayout
        // 
        _displayLayout.AutoSize = true;
        _displayLayout.ColumnCount = 3;
        _displayLayout.ColumnStyles.Add(new ColumnStyle());
        _displayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _displayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _displayLayout.Controls.Add(_accelerationDigitsLabel, 0, 0);
        _displayLayout.Controls.Add(_accelerationDigitsUpDown, 1, 0);
        _displayLayout.Controls.Add(_gpsDigitsLabel, 0, 1);
        _displayLayout.Controls.Add(_gpsDigitsUpDown, 1, 1);
        _displayLayout.Controls.Add(_speedDigitsLabel, 0, 2);
        _displayLayout.Controls.Add(_speedDigitsUpDown, 1, 2);
        _displayLayout.Controls.Add(_otherDigitsLabel, 0, 3);
        _displayLayout.Controls.Add(_otherDigitsUpDown, 1, 3);
        _displayLayout.Controls.Add(_dampingLabel, 0, 4);
        _displayLayout.Controls.Add(_dampingUpDown, 1, 4);
        _displayLayout.Controls.Add(_gScaleLabel, 0, 5);
        _displayLayout.Controls.Add(_gScaleUpDown, 1, 5);
        _displayLayout.Controls.Add(_baseFontLabel, 0, 6);
        _displayLayout.Controls.Add(_baseFontUpDown, 1, 6);
        _displayLayout.Controls.Add(_toolbarSizeLabel, 0, 7);
        _displayLayout.Controls.Add(_toolbarSizeComboBox, 1, 7);
        _displayLayout.Controls.Add(_numberPreviewLabel, 0, 8);
        _displayLayout.Controls.Add(_speedometerOptions, 0, 9);
        _displayLayout.Controls.Add(_exceptionPopupLabel, 0, 10);
        _displayLayout.Controls.Add(_exceptionPopupUpDown, 1, 10);
        _displayLayout.Controls.Add(_forceObdGaugesCheckBox, 0, 11);
        _displayLayout.Controls.Add(_sphericalHorizonCheckBox, 0, 12);
        _displayLayout.Controls.Add(_alwaysOnCheckBox, 0, 13);
        _displayLayout.Dock = DockStyle.Top;
        _displayLayout.Location = new Point(17, 20);
        _displayLayout.Name = "_displayLayout";
        _displayLayout.RowCount = 14;
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.RowStyles.Add(new RowStyle());
        _displayLayout.Size = new Size(1410, 1176);
        _displayLayout.TabIndex = 0;
        // 
        // _accelerationDigitsLabel
        // 
        _accelerationDigitsLabel.Anchor = AnchorStyles.Left;
        _accelerationDigitsLabel.AutoSize = true;
        _accelerationDigitsLabel.Location = new Point(7, 10);
        _accelerationDigitsLabel.Margin = new Padding(7, 0, 7, 0);
        _accelerationDigitsLabel.Name = "_accelerationDigitsLabel";
        _accelerationDigitsLabel.Size = new Size(242, 37);
        _accelerationDigitsLabel.TabIndex = 0;
        _accelerationDigitsLabel.Text = "Acceleration digits:";
        // 
        // _accelerationDigitsUpDown
        // 
        _accelerationDigitsUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _accelerationDigitsUpDown.Location = new Point(463, 7);
        _accelerationDigitsUpDown.Margin = new Padding(7, 7, 7, 7);
        _accelerationDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _accelerationDigitsUpDown.Name = "_accelerationDigitsUpDown";
        _accelerationDigitsUpDown.Size = new Size(463, 43);
        _accelerationDigitsUpDown.TabIndex = 1;
        _accelerationDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        // 
        // _gpsDigitsLabel
        // 
        _gpsDigitsLabel.Anchor = AnchorStyles.Left;
        _gpsDigitsLabel.AutoSize = true;
        _gpsDigitsLabel.Location = new Point(7, 67);
        _gpsDigitsLabel.Margin = new Padding(7, 0, 7, 0);
        _gpsDigitsLabel.Name = "_gpsDigitsLabel";
        _gpsDigitsLabel.Size = new Size(279, 37);
        _gpsDigitsLabel.TabIndex = 2;
        _gpsDigitsLabel.Text = "GPS coordinate digits:";
        // 
        // _gpsDigitsUpDown
        // 
        _gpsDigitsUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _gpsDigitsUpDown.Location = new Point(463, 64);
        _gpsDigitsUpDown.Margin = new Padding(7, 7, 7, 7);
        _gpsDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _gpsDigitsUpDown.Name = "_gpsDigitsUpDown";
        _gpsDigitsUpDown.Size = new Size(463, 43);
        _gpsDigitsUpDown.TabIndex = 3;
        _gpsDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        // 
        // _speedDigitsLabel
        // 
        _speedDigitsLabel.Anchor = AnchorStyles.Left;
        _speedDigitsLabel.AutoSize = true;
        _speedDigitsLabel.Location = new Point(7, 124);
        _speedDigitsLabel.Margin = new Padding(7, 0, 7, 0);
        _speedDigitsLabel.Name = "_speedDigitsLabel";
        _speedDigitsLabel.Size = new Size(170, 37);
        _speedDigitsLabel.TabIndex = 4;
        _speedDigitsLabel.Text = "Speed digits:";
        // 
        // _speedDigitsUpDown
        // 
        _speedDigitsUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _speedDigitsUpDown.Location = new Point(463, 121);
        _speedDigitsUpDown.Margin = new Padding(7, 7, 7, 7);
        _speedDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _speedDigitsUpDown.Name = "_speedDigitsUpDown";
        _speedDigitsUpDown.Size = new Size(463, 43);
        _speedDigitsUpDown.TabIndex = 5;
        _speedDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        // 
        // _otherDigitsLabel
        // 
        _otherDigitsLabel.Anchor = AnchorStyles.Left;
        _otherDigitsLabel.AutoSize = true;
        _otherDigitsLabel.Location = new Point(7, 181);
        _otherDigitsLabel.Margin = new Padding(7, 0, 7, 0);
        _otherDigitsLabel.Name = "_otherDigitsLabel";
        _otherDigitsLabel.Size = new Size(163, 37);
        _otherDigitsLabel.TabIndex = 6;
        _otherDigitsLabel.Text = "Other digits:";
        // 
        // _otherDigitsUpDown
        // 
        _otherDigitsUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _otherDigitsUpDown.AutoSize = true;
        _otherDigitsUpDown.Location = new Point(463, 178);
        _otherDigitsUpDown.Margin = new Padding(7, 7, 7, 7);
        _otherDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _otherDigitsUpDown.Name = "_otherDigitsUpDown";
        _otherDigitsUpDown.Size = new Size(463, 43);
        _otherDigitsUpDown.TabIndex = 7;
        _otherDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        // 
        // _dampingLabel
        // 
        _dampingLabel.Anchor = AnchorStyles.Left;
        _dampingLabel.AutoSize = true;
        _dampingLabel.Location = new Point(7, 238);
        _dampingLabel.Margin = new Padding(7, 0, 7, 0);
        _dampingLabel.Name = "_dampingLabel";
        _dampingLabel.Size = new Size(283, 37);
        _dampingLabel.TabIndex = 8;
        _dampingLabel.Text = "Control damping (ms):";
        // 
        // _dampingUpDown
        // 
        _dampingUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _dampingUpDown.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        _dampingUpDown.Location = new Point(463, 235);
        _dampingUpDown.Margin = new Padding(7, 7, 7, 7);
        _dampingUpDown.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        _dampingUpDown.Name = "_dampingUpDown";
        _dampingUpDown.Size = new Size(463, 43);
        _dampingUpDown.TabIndex = 9;
        // 
        // _gScaleLabel
        // 
        _gScaleLabel.Anchor = AnchorStyles.Left;
        _gScaleLabel.AutoSize = true;
        _gScaleLabel.Location = new Point(7, 295);
        _gScaleLabel.Margin = new Padding(7, 0, 7, 0);
        _gScaleLabel.Name = "_gScaleLabel";
        _gScaleLabel.Size = new Size(190, 37);
        _gScaleLabel.TabIndex = 10;
        _gScaleLabel.Text = "G full scale (g):";
        // 
        // _gScaleUpDown
        // 
        _gScaleUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _gScaleUpDown.DecimalPlaces = 2;
        _gScaleUpDown.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        _gScaleUpDown.Location = new Point(463, 292);
        _gScaleUpDown.Margin = new Padding(7, 7, 7, 7);
        _gScaleUpDown.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        _gScaleUpDown.Minimum = new decimal(new int[] { 25, 0, 0, 131072 });
        _gScaleUpDown.Name = "_gScaleUpDown";
        _gScaleUpDown.Size = new Size(463, 43);
        _gScaleUpDown.TabIndex = 11;
        _gScaleUpDown.Value = new decimal(new int[] { 25, 0, 0, 131072 });
        // 
        // _baseFontLabel
        // 
        _baseFontLabel.Anchor = AnchorStyles.Left;
        _baseFontLabel.AutoSize = true;
        _baseFontLabel.Location = new Point(7, 352);
        _baseFontLabel.Margin = new Padding(7, 0, 7, 0);
        _baseFontLabel.Name = "_baseFontLabel";
        _baseFontLabel.Size = new Size(231, 37);
        _baseFontLabel.TabIndex = 12;
        _baseFontLabel.Text = "Base font size (pt):";
        // 
        // _baseFontUpDown
        // 
        _baseFontUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _baseFontUpDown.AutoSize = true;
        _baseFontUpDown.DecimalPlaces = 1;
        _baseFontUpDown.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _baseFontUpDown.Location = new Point(463, 349);
        _baseFontUpDown.Margin = new Padding(7, 7, 7, 7);
        _baseFontUpDown.Maximum = new decimal(new int[] { 36, 0, 0, 0 });
        _baseFontUpDown.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        _baseFontUpDown.Name = "_baseFontUpDown";
        _baseFontUpDown.Size = new Size(463, 43);
        _baseFontUpDown.TabIndex = 13;
        _baseFontUpDown.Value = new decimal(new int[] { 8, 0, 0, 0 });
        // 
        // _toolbarSizeLabel
        // 
        _toolbarSizeLabel.Anchor = AnchorStyles.Left;
        _toolbarSizeLabel.AutoSize = true;
        _toolbarSizeLabel.Location = new Point(7, 410);
        _toolbarSizeLabel.Margin = new Padding(7, 0, 7, 0);
        _toolbarSizeLabel.Name = "_toolbarSizeLabel";
        _toolbarSizeLabel.Size = new Size(220, 37);
        _toolbarSizeLabel.TabIndex = 14;
        _toolbarSizeLabel.Text = "Toolbar icon size:";
        // 
        // _toolbarSizeComboBox
        // 
        _toolbarSizeComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _toolbarSizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _toolbarSizeComboBox.FormattingEnabled = true;
        _toolbarSizeComboBox.Location = new Point(463, 406);
        _toolbarSizeComboBox.Margin = new Padding(7, 7, 7, 7);
        _toolbarSizeComboBox.Name = "_toolbarSizeComboBox";
        _toolbarSizeComboBox.Size = new Size(463, 45);
        _toolbarSizeComboBox.TabIndex = 15;
        // 
        // _numberPreviewLabel
        // 
        _numberPreviewLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _numberPreviewLabel.AutoSize = true;
        _displayLayout.SetColumnSpan(_numberPreviewLabel, 2);
        _numberPreviewLabel.Location = new Point(7, 488);
        _numberPreviewLabel.Margin = new Padding(7, 30, 7, 7);
        _numberPreviewLabel.Name = "_numberPreviewLabel";
        _numberPreviewLabel.Size = new Size(919, 37);
        _numberPreviewLabel.TabIndex = 16;
        _numberPreviewLabel.Text = "Number format preview";
        // 
        // _speedometerOptions
        // 
        _speedometerOptions.AccessibleName = "Speedometer LED ring settings";
        _speedometerOptions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _displayLayout.SetColumnSpan(_speedometerOptions, 3);
        _speedometerOptions.Location = new Point(3, 535);
        _speedometerOptions.Name = "_speedometerOptions";
        _speedometerOptions.Size = new Size(1404, 416);
        _speedometerOptions.TabIndex = 17;
        // 
        // _exceptionPopupLabel
        // 
        _exceptionPopupLabel.Anchor = AnchorStyles.Left;
        _exceptionPopupLabel.AutoSize = true;
        _exceptionPopupLabel.Location = new Point(7, 964);
        _exceptionPopupLabel.Margin = new Padding(7, 0, 7, 0);
        _exceptionPopupLabel.Name = "_exceptionPopupLabel";
        _exceptionPopupLabel.Size = new Size(442, 37);
        _exceptionPopupLabel.TabIndex = 18;
        _exceptionPopupLabel.Text = "Exception popup timeout (seconds):";
        // 
        // _exceptionPopupUpDown
        // 
        _exceptionPopupUpDown.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _exceptionPopupUpDown.Location = new Point(463, 961);
        _exceptionPopupUpDown.Margin = new Padding(7, 7, 7, 7);
        _exceptionPopupUpDown.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        _exceptionPopupUpDown.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _exceptionPopupUpDown.Name = "_exceptionPopupUpDown";
        _exceptionPopupUpDown.Size = new Size(463, 43);
        _exceptionPopupUpDown.TabIndex = 20;
        _exceptionPopupUpDown.Value = new decimal(new int[] { 15, 0, 0, 0 });
        // 
        // _forceObdGaugesCheckBox
        // 
        _forceObdGaugesCheckBox.AccessibleDescription = "Keep OBD gauges visible even when no OBD adapter is available. Does not enable OBD acquisition.";
        _forceObdGaugesCheckBox.AutoSize = true;
        _displayLayout.SetColumnSpan(_forceObdGaugesCheckBox, 2);
        _forceObdGaugesCheckBox.Location = new Point(7, 1018);
        _forceObdGaugesCheckBox.Margin = new Padding(7, 7, 7, 7);
        _forceObdGaugesCheckBox.Name = "_forceObdGaugesCheckBox";
        _forceObdGaugesCheckBox.Size = new Size(308, 41);
        _forceObdGaugesCheckBox.TabIndex = 11;
        _forceObdGaugesCheckBox.Text = "Force OBD Gauges on";
        // 
        // _sphericalHorizonCheckBox
        // 
        _sphericalHorizonCheckBox.AccessibleDescription = "Show attitude and heading on one sphere, hide the compass, and give the map the full panel width.";
        _sphericalHorizonCheckBox.AutoSize = true;
        _displayLayout.SetColumnSpan(_sphericalHorizonCheckBox, 2);
        _sphericalHorizonCheckBox.Location = new Point(7, 1073);
        _sphericalHorizonCheckBox.Margin = new Padding(7, 7, 7, 7);
        _sphericalHorizonCheckBox.Name = "_sphericalHorizonCheckBox";
        _sphericalHorizonCheckBox.Size = new Size(254, 41);
        _sphericalHorizonCheckBox.TabIndex = 12;
        _sphericalHorizonCheckBox.Text = "Spherical horizon";
        // 
        // _alwaysOnCheckBox
        // 
        _alwaysOnCheckBox.AccessibleDescription = "Disable the screensaver while Sensored is open. Full screen remains owned by the existing full-screen command.";
        _alwaysOnCheckBox.AutoSize = true;
        _displayLayout.SetColumnSpan(_alwaysOnCheckBox, 2);
        _alwaysOnCheckBox.Location = new Point(7, 1128);
        _alwaysOnCheckBox.Margin = new Padding(7, 7, 7, 7);
        _alwaysOnCheckBox.Name = "_alwaysOnCheckBox";
        _alwaysOnCheckBox.Size = new Size(282, 41);
        _alwaysOnCheckBox.TabIndex = 13;
        _alwaysOnCheckBox.Text = "Keep display awake";
        // 
        // _mapPage
        // 
        _mapPage.Controls.Add(_mapLayout);
        _mapPage.Location = new Point(8, 46);
        _mapPage.Margin = new Padding(7, 7, 7, 7);
        _mapPage.Name = "_mapPage";
        _mapPage.Padding = new Padding(17, 20, 17, 20);
        _mapPage.Size = new Size(1478, 977);
        _mapPage.TabIndex = 2;
        _mapPage.Text = "Map / route";
        _mapPage.UseVisualStyleBackColor = true;
        // 
        // _mapLayout
        // 
        _mapLayout.ColumnCount = 3;
        _mapLayout.ColumnStyles.Add(new ColumnStyle());
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mapLayout.ColumnStyles.Add(new ColumnStyle());
        _mapLayout.Controls.Add(_routePathLabel, 0, 0);
        _mapLayout.Controls.Add(_routePathTextBox, 1, 0);
        _mapLayout.Controls.Add(_browseRouteButton, 2, 0);
        _mapLayout.Controls.Add(_followPositionCheckBox, 1, 1);
        _mapLayout.Controls.Add(_mapDebugCheckBox, 1, 2);
        _mapLayout.Controls.Add(_onlineTilesCheckBox, 1, 3);
        _mapLayout.Controls.Add(_offlineTilesHintLabel, 1, 4);
        _mapLayout.Dock = DockStyle.Top;
        _mapLayout.Location = new Point(17, 20);
        _mapLayout.Margin = new Padding(7, 7, 7, 7);
        _mapLayout.Name = "_mapLayout";
        _mapLayout.RowCount = 5;
        _mapLayout.RowStyles.Add(new RowStyle());
        _mapLayout.RowStyles.Add(new RowStyle());
        _mapLayout.RowStyles.Add(new RowStyle());
        _mapLayout.RowStyles.Add(new RowStyle());
        _mapLayout.RowStyles.Add(new RowStyle());
        _mapLayout.Size = new Size(1444, 383);
        _mapLayout.TabIndex = 0;
        // 
        // _routePathLabel
        // 
        _routePathLabel.Anchor = AnchorStyles.Left;
        _routePathLabel.AutoSize = true;
        _routePathLabel.Location = new Point(7, 40);
        _routePathLabel.Margin = new Padding(7, 0, 7, 0);
        _routePathLabel.Name = "_routePathLabel";
        _routePathLabel.Size = new Size(135, 37);
        _routePathLabel.TabIndex = 0;
        _routePathLabel.Text = "Route file:";
        // 
        // _routePathTextBox
        // 
        _routePathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _routePathTextBox.Location = new Point(156, 37);
        _routePathTextBox.Margin = new Padding(7, 7, 7, 7);
        _routePathTextBox.Name = "_routePathTextBox";
        _routePathTextBox.Size = new Size(1020, 43);
        _routePathTextBox.TabIndex = 1;
        // 
        // _browseRouteButton
        // 
        _browseRouteButton.AutoSize = true;
        _browseRouteButton.Location = new Point(1190, 7);
        _browseRouteButton.Margin = new Padding(7, 7, 7, 7);
        _browseRouteButton.Name = "_browseRouteButton";
        _browseRouteButton.Size = new Size(247, 104);
        _browseRouteButton.TabIndex = 2;
        _browseRouteButton.Text = "Browse...";
        _browseRouteButton.UseVisualStyleBackColor = true;
        _browseRouteButton.Click += OnBrowseRouteClick;
        // 
        // _followPositionCheckBox
        // 
        _followPositionCheckBox.AutoSize = true;
        _followPositionCheckBox.Location = new Point(156, 138);
        _followPositionCheckBox.Margin = new Padding(7, 20, 7, 7);
        _followPositionCheckBox.Name = "_followPositionCheckBox";
        _followPositionCheckBox.Size = new Size(343, 41);
        _followPositionCheckBox.TabIndex = 3;
        _followPositionCheckBox.Text = "Follow current GPS point";
        _followPositionCheckBox.UseVisualStyleBackColor = true;
        // 
        // _mapDebugCheckBox
        // 
        _mapDebugCheckBox.AutoSize = true;
        _mapDebugCheckBox.Location = new Point(156, 206);
        _mapDebugCheckBox.Margin = new Padding(7, 20, 7, 7);
        _mapDebugCheckBox.Name = "_mapDebugCheckBox";
        _mapDebugCheckBox.Size = new Size(526, 41);
        _mapDebugCheckBox.TabIndex = 4;
        _mapDebugCheckBox.Text = "Show map rendering debug information";
        _mapDebugCheckBox.UseVisualStyleBackColor = true;
        // 
        // _onlineTilesCheckBox
        // 
        _onlineTilesCheckBox.AutoSize = true;
        _onlineTilesCheckBox.Location = new Point(156, 261);
        _onlineTilesCheckBox.Margin = new Padding(7, 7, 7, 7);
        _onlineTilesCheckBox.Name = "_onlineTilesCheckBox";
        _onlineTilesCheckBox.Size = new Size(561, 41);
        _onlineTilesCheckBox.TabIndex = 5;
        _onlineTilesCheckBox.Text = "Allow ONLINE OpenStreetMap tiles (shares viewed area)";
        _onlineTilesCheckBox.UseVisualStyleBackColor = true;
        // 
        // _offlineTilesHintLabel
        // 
        _offlineTilesHintLabel.AutoSize = true;
        _mapLayout.SetColumnSpan(_offlineTilesHintLabel, 2);
        _offlineTilesHintLabel.Location = new Point(156, 309);
        _offlineTilesHintLabel.Margin = new Padding(7, 0, 7, 0);
        _offlineTilesHintLabel.Name = "_offlineTilesHintLabel";
        _offlineTilesHintLabel.Size = new Size(841, 37);
        _offlineTilesHintLabel.TabIndex = 6;
        _offlineTilesHintLabel.Text = "Offline tiles unavailable; route and marker remain visible without tiles.";
        // 
        // _recordingPage
        // 
        _recordingPage.Controls.Add(_recordingLayout);
        _recordingPage.Location = new Point(8, 46);
        _recordingPage.Margin = new Padding(7, 7, 7, 7);
        _recordingPage.Name = "_recordingPage";
        _recordingPage.Padding = new Padding(17, 20, 17, 20);
        _recordingPage.Size = new Size(1478, 977);
        _recordingPage.TabIndex = 3;
        _recordingPage.Text = "Recording";
        _recordingPage.UseVisualStyleBackColor = true;
        // 
        // _recordingLayout
        // 
        _recordingLayout.AutoSize = true;
        _recordingLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _recordingLayout.ColumnCount = 3;
        _recordingLayout.ColumnStyles.Add(new ColumnStyle());
        _recordingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _recordingLayout.ColumnStyles.Add(new ColumnStyle());
        _recordingLayout.Controls.Add(_recordingFolderLabel, 0, 0);
        _recordingLayout.Controls.Add(_recordingFolderTextBox, 1, 0);
        _recordingLayout.Controls.Add(_browseRecordingFolderButton, 2, 0);
        _recordingLayout.Controls.Add(_recordingFormatsGroupBox, 0, 1);
        _recordingLayout.Controls.Add(_autoRecordingCheckBox, 0, 2);
        _recordingLayout.Controls.Add(_rawSnapshotRateLabel, 0, 3);
        _recordingLayout.Controls.Add(_rawSnapshotRate, 1, 3);
        _recordingLayout.Controls.Add(_recordingMarkerHint, 0, 4);
        _recordingLayout.Controls.Add(_recordingMarkerLabel, 1, 4);
        _recordingLayout.Dock = DockStyle.Top;
        _recordingLayout.Location = new Point(17, 20);
        _recordingLayout.Name = "_recordingLayout";
        _recordingLayout.RowCount = 5;
        _recordingLayout.RowStyles.Add(new RowStyle());
        _recordingLayout.RowStyles.Add(new RowStyle());
        _recordingLayout.RowStyles.Add(new RowStyle());
        _recordingLayout.RowStyles.Add(new RowStyle());
        _recordingLayout.RowStyles.Add(new RowStyle());
        _recordingLayout.Size = new Size(1444, 555);
        _recordingLayout.TabIndex = 0;
        // 
        // _recordingFolderLabel
        // 
        _recordingFolderLabel.Anchor = AnchorStyles.Left;
        _recordingFolderLabel.AutoSize = true;
        _recordingFolderLabel.Location = new Point(7, 40);
        _recordingFolderLabel.Margin = new Padding(7, 0, 7, 0);
        _recordingFolderLabel.Name = "_recordingFolderLabel";
        _recordingFolderLabel.Size = new Size(200, 37);
        _recordingFolderLabel.TabIndex = 0;
        _recordingFolderLabel.Text = "Recording root:";
        // 
        // _recordingFolderTextBox
        // 
        _recordingFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _recordingFolderTextBox.Location = new Point(360, 37);
        _recordingFolderTextBox.Margin = new Padding(7, 7, 7, 7);
        _recordingFolderTextBox.Name = "_recordingFolderTextBox";
        _recordingFolderTextBox.Size = new Size(816, 43);
        _recordingFolderTextBox.TabIndex = 1;
        // 
        // _browseRecordingFolderButton
        // 
        _browseRecordingFolderButton.AutoSize = true;
        _browseRecordingFolderButton.Location = new Point(1190, 7);
        _browseRecordingFolderButton.Margin = new Padding(7, 7, 7, 7);
        _browseRecordingFolderButton.Name = "_browseRecordingFolderButton";
        _browseRecordingFolderButton.Size = new Size(247, 104);
        _browseRecordingFolderButton.TabIndex = 2;
        _browseRecordingFolderButton.Text = "Browse...";
        _browseRecordingFolderButton.UseVisualStyleBackColor = true;
        _browseRecordingFolderButton.Click += OnBrowseRecordingFolderClick;
        // 
        // _recordingFormatsGroupBox
        // 
        _recordingFormatsGroupBox.AutoSize = true;
        _recordingFormatsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _recordingLayout.SetColumnSpan(_recordingFormatsGroupBox, 3);
        _recordingFormatsGroupBox.Controls.Add(_recordingFormatsPanel);
        _recordingFormatsGroupBox.Location = new Point(7, 125);
        _recordingFormatsGroupBox.Margin = new Padding(7, 7, 7, 7);
        _recordingFormatsGroupBox.Name = "_recordingFormatsGroupBox";
        _recordingFormatsGroupBox.Padding = new Padding(17, 20, 17, 20);
        _recordingFormatsGroupBox.Size = new Size(589, 241);
        _recordingFormatsGroupBox.TabIndex = 3;
        _recordingFormatsGroupBox.TabStop = false;
        _recordingFormatsGroupBox.Text = "Formats";
        // 
        // _recordingFormatsPanel
        // 
        _recordingFormatsPanel.AutoSize = true;
        _recordingFormatsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _recordingFormatsPanel.Controls.Add(_jsonLinesCheckBox);
        _recordingFormatsPanel.Controls.Add(_compactJsonCheckBox);
        _recordingFormatsPanel.Controls.Add(_csvCheckBox);
        _recordingFormatsPanel.Dock = DockStyle.Fill;
        _recordingFormatsPanel.FlowDirection = FlowDirection.TopDown;
        _recordingFormatsPanel.Location = new Point(17, 56);
        _recordingFormatsPanel.Margin = new Padding(7, 7, 7, 7);
        _recordingFormatsPanel.Name = "_recordingFormatsPanel";
        _recordingFormatsPanel.Size = new Size(555, 165);
        _recordingFormatsPanel.TabIndex = 0;
        _recordingFormatsPanel.WrapContents = false;
        // 
        // _jsonLinesCheckBox
        // 
        _jsonLinesCheckBox.AutoSize = true;
        _jsonLinesCheckBox.Location = new Point(7, 7);
        _jsonLinesCheckBox.Margin = new Padding(7, 7, 7, 7);
        _jsonLinesCheckBox.Name = "_jsonLinesCheckBox";
        _jsonLinesCheckBox.Size = new Size(180, 41);
        _jsonLinesCheckBox.TabIndex = 0;
        _jsonLinesCheckBox.Text = "JSON Lines";
        _jsonLinesCheckBox.UseVisualStyleBackColor = true;
        // 
        // _compactJsonCheckBox
        // 
        _compactJsonCheckBox.AutoSize = true;
        _compactJsonCheckBox.Checked = true;
        _compactJsonCheckBox.CheckState = CheckState.Checked;
        _compactJsonCheckBox.Enabled = false;
        _compactJsonCheckBox.Location = new Point(7, 62);
        _compactJsonCheckBox.Margin = new Padding(7, 7, 7, 7);
        _compactJsonCheckBox.Name = "_compactJsonCheckBox";
        _compactJsonCheckBox.Size = new Size(541, 41);
        _compactJsonCheckBox.TabIndex = 1;
        _compactJsonCheckBox.Text = "Full-rate compact JSON journal (required)";
        _compactJsonCheckBox.UseVisualStyleBackColor = true;
        // 
        // _csvCheckBox
        // 
        _csvCheckBox.AutoSize = true;
        _csvCheckBox.Checked = true;
        _csvCheckBox.CheckState = CheckState.Checked;
        _csvCheckBox.Enabled = false;
        _csvCheckBox.Location = new Point(7, 117);
        _csvCheckBox.Margin = new Padding(7, 7, 7, 7);
        _csvCheckBox.Name = "_csvCheckBox";
        _csvCheckBox.Size = new Size(472, 41);
        _csvCheckBox.TabIndex = 2;
        _csvCheckBox.Text = "RawV2 / FuseV2 CSV pair (required)";
        _csvCheckBox.UseVisualStyleBackColor = true;
        // 
        // _autoRecordingCheckBox
        // 
        _autoRecordingCheckBox.AutoSize = true;
        _recordingLayout.SetColumnSpan(_autoRecordingCheckBox, 3);
        _autoRecordingCheckBox.Location = new Point(7, 393);
        _autoRecordingCheckBox.Margin = new Padding(7, 20, 7, 7);
        _autoRecordingCheckBox.Name = "_autoRecordingCheckBox";
        _autoRecordingCheckBox.Size = new Size(376, 41);
        _autoRecordingCheckBox.TabIndex = 4;
        _autoRecordingCheckBox.Text = "Start recording with sensors";
        _autoRecordingCheckBox.UseVisualStyleBackColor = true;
        // 
        // _rawSnapshotRateLabel
        // 
        _rawSnapshotRateLabel.Anchor = AnchorStyles.Left;
        _rawSnapshotRateLabel.AutoSize = true;
        _rawSnapshotRateLabel.Location = new Point(7, 451);
        _rawSnapshotRateLabel.Margin = new Padding(7, 0, 7, 0);
        _rawSnapshotRateLabel.Name = "_rawSnapshotRateLabel";
        _rawSnapshotRateLabel.Size = new Size(339, 37);
        _rawSnapshotRateLabel.TabIndex = 5;
        _rawSnapshotRateLabel.Text = "RawV2 snapshots / second:";
        // 
        // _rawSnapshotRate
        // 
        _rawSnapshotRate.AccessibleName = "RawV2 snapshots per second (next recording)";
        _rawSnapshotRate.Location = new Point(360, 448);
        _rawSnapshotRate.Margin = new Padding(7, 7, 7, 7);
        _rawSnapshotRate.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
        _rawSnapshotRate.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        _rawSnapshotRate.Name = "_rawSnapshotRate";
        _rawSnapshotRate.Size = new Size(257, 43);
        _rawSnapshotRate.TabIndex = 6;
        _rawSnapshotRate.Value = new decimal(new int[] { 10, 0, 0, 0 });
        // 
        // _recordingMarkerHint
        // 
        _recordingMarkerHint.Anchor = AnchorStyles.Left;
        _recordingMarkerHint.AutoSize = true;
        _recordingMarkerHint.Location = new Point(7, 508);
        _recordingMarkerHint.Margin = new Padding(7, 0, 7, 0);
        _recordingMarkerHint.Name = "_recordingMarkerHint";
        _recordingMarkerHint.Size = new Size(326, 37);
        _recordingMarkerHint.TabIndex = 7;
        _recordingMarkerHint.Text = "Ctrl+M marker note/label:";
        // 
        // _recordingMarkerLabel
        // 
        _recordingMarkerLabel.AccessibleName = "Recording marker note (Ctrl+M)";
        _recordingMarkerLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _recordingMarkerLabel.Location = new Point(360, 505);
        _recordingMarkerLabel.Margin = new Padding(7, 7, 7, 7);
        _recordingMarkerLabel.Name = "_recordingMarkerLabel";
        _recordingMarkerLabel.Size = new Size(816, 43);
        _recordingMarkerLabel.TabIndex = 8;
        // 
        // _replayPage
        // 
        _replayPage.Controls.Add(_replayLayout);
        _replayPage.Location = new Point(8, 46);
        _replayPage.Margin = new Padding(7, 7, 7, 7);
        _replayPage.Name = "_replayPage";
        _replayPage.Padding = new Padding(17, 20, 17, 20);
        _replayPage.Size = new Size(1478, 977);
        _replayPage.TabIndex = 4;
        _replayPage.Text = "Replay";
        _replayPage.UseVisualStyleBackColor = true;
        // 
        // _replayLayout
        // 
        _replayLayout.AutoSize = true;
        _replayLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _replayLayout.Controls.Add(_detectReplaySequenceCheckBox);
        _replayLayout.Controls.Add(_hideReplayControlsCheckBox);
        _replayLayout.Dock = DockStyle.Top;
        _replayLayout.FlowDirection = FlowDirection.TopDown;
        _replayLayout.Location = new Point(17, 20);
        _replayLayout.Margin = new Padding(7, 7, 7, 7);
        _replayLayout.Name = "_replayLayout";
        _replayLayout.Size = new Size(1444, 110);
        _replayLayout.TabIndex = 0;
        _replayLayout.WrapContents = false;
        // 
        // _detectReplaySequenceCheckBox
        // 
        _detectReplaySequenceCheckBox.AutoSize = true;
        _detectReplaySequenceCheckBox.Location = new Point(7, 7);
        _detectReplaySequenceCheckBox.Margin = new Padding(7, 7, 7, 7);
        _detectReplaySequenceCheckBox.Name = "_detectReplaySequenceCheckBox";
        _detectReplaySequenceCheckBox.Size = new Size(947, 41);
        _detectReplaySequenceCheckBox.TabIndex = 0;
        _detectReplaySequenceCheckBox.Text = "Detect numbered data-sequence files and replay the next part automatically";
        _detectReplaySequenceCheckBox.UseVisualStyleBackColor = true;
        // 
        // _hideReplayControlsCheckBox
        // 
        _hideReplayControlsCheckBox.AutoSize = true;
        _hideReplayControlsCheckBox.Location = new Point(7, 62);
        _hideReplayControlsCheckBox.Margin = new Padding(7, 7, 7, 7);
        _hideReplayControlsCheckBox.Name = "_hideReplayControlsCheckBox";
        _hideReplayControlsCheckBox.Size = new Size(731, 41);
        _hideReplayControlsCheckBox.TabIndex = 1;
        _hideReplayControlsCheckBox.Text = "Hide replay controls until the pointer enters the status bar";
        _hideReplayControlsCheckBox.UseVisualStyleBackColor = true;
        // 
        // _buttonPanel
        // 
        _buttonPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
        _buttonPanel.AutoSize = true;
        _buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.Location = new Point(1152, 1132);
        _buttonPanel.Margin = new Padding(6, 12, 6, 6);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new Size(368, 83);
        _buttonPanel.TabIndex = 2;
        // 
        // _okButton
        // 
        _okButton.AutoSize = true;
        _okButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _okButton.Dock = DockStyle.Fill;
        _okButton.Location = new Point(12, 6);
        _okButton.Margin = new Padding(12, 6, 12, 6);
        _okButton.MinimumSize = new Size(160, 0);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(160, 71);
        _okButton.TabIndex = 0;
        _okButton.Text = "OK";
        _okButton.UseVisualStyleBackColor = true;
        _okButton.Click += OnAcceptClick;
        // 
        // _cancelButton
        // 
        _cancelButton.AutoSize = true;
        _cancelButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(196, 6);
        _cancelButton.Margin = new Padding(12, 6, 12, 6);
        _cancelButton.MinimumSize = new Size(160, 0);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Padding = new Padding(12, 12, 12, 12);
        _cancelButton.Size = new Size(160, 71);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Cancel";
        _cancelButton.UseVisualStyleBackColor = true;
        // 
        // _bluetoothPage
        //
        _bluetoothPage.Text = "Bluetooth";
        _bluetoothPage.Name = "_bluetoothPage";
        _bluetoothPage.Padding = new Padding(12);
        _bluetoothPage.Controls.Add(_bluetoothLayout);
        _bluetoothSectionButton.Text = "Bluetooth";
        _bluetoothSectionButton.Name = "_bluetoothSectionButton";
        _bluetoothSectionButton.Click += OnBluetoothSectionClick;
        _bluetoothLayout.Dock = DockStyle.Fill;
        _bluetoothLayout.ColumnCount = 2;
        _bluetoothLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _bluetoothLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _bluetoothLayout.RowCount = 8;
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _bluetoothLayout.Controls.Add(_bluetoothHint, 0, 0);
        _bluetoothLayout.SetColumnSpan(_bluetoothHint, 2);
        _bluetoothLayout.Controls.Add(_bluetoothActions, 0, 1);
        _bluetoothLayout.SetColumnSpan(_bluetoothActions, 2);
        _bluetoothLayout.Controls.Add(_bluetoothDevices, 0, 2);
        _bluetoothLayout.SetColumnSpan(_bluetoothDevices, 2);
        _bluetoothLayout.Controls.Add(_obdDeviceLabel, 0, 3);
        _bluetoothLayout.Controls.Add(_obdDeviceComboBox, 1, 3);
        _bluetoothLayout.Controls.Add(_sensorDeviceLabel, 0, 4);
        _bluetoothLayout.Controls.Add(_sensorDeviceComboBox, 1, 4);
        _bluetoothLayout.Controls.Add(_obdAdapterLabel, 0, 5);
        _bluetoothLayout.Controls.Add(_obdAdapterComboBox, 1, 5);
        _bluetoothLayout.Controls.Add(_obdSearchLabel, 0, 6);
        _bluetoothLayout.Controls.Add(_obdSearchUpDown, 1, 6);
        _bluetoothLayout.Controls.Add(_bluetoothSearchLabel, 0, 7);
        _bluetoothLayout.Controls.Add(_bluetoothSearchUpDown, 1, 7);
        _bluetoothHint.AutoSize = true;
        _bluetoothHint.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothHint.Text = "Scan paired and unpaired devices, then choose each role. No pairing is requested. Automatic uses paired compatible devices. The OBD protocol and WT901 sensor endpoints are still verified. Some devices may require Windows pairing or access consent.";
        _bluetoothActions.AutoSize = true;
        _bluetoothActions.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothActions.Controls.Add(_scanBluetoothButton);
        _bluetoothActions.Controls.Add(_stopBluetoothScanButton);
        _scanBluetoothButton.AutoSize = true;
        _scanBluetoothButton.Text = "Scan again";
        _scanBluetoothButton.TabIndex = 0;
        _scanBluetoothButton.Click += OnScanBluetoothClick;
        _stopBluetoothScanButton.AutoSize = true;
        _stopBluetoothScanButton.Text = "Stop scan";
        _stopBluetoothScanButton.Enabled = false;
        _stopBluetoothScanButton.TabIndex = 1;
        _stopBluetoothScanButton.Click += OnStopBluetoothScanClick;
        _bluetoothDevices.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothDevices.View = View.Details;
        _bluetoothDevices.FullRowSelect = true;
        _bluetoothDevices.HideSelection = false;
        _bluetoothDevices.MultiSelect = false;
        _bluetoothDevices.TabIndex = 2;
        _bluetoothDevices.AccessibleName = "Discovered Bluetooth devices";
        _bluetoothDevices.Columns.AddRange(new ColumnHeader[] { _deviceNameColumn, _deviceAddressColumn, _deviceKindColumn, _devicePairingColumn, _deviceSignalColumn });
        _deviceNameColumn.Text = "Device";
        _deviceNameColumn.Width = 360;
        _deviceAddressColumn.Text = "Address";
        _deviceAddressColumn.Width = 310;
        _deviceKindColumn.Text = "Transport";
        _deviceKindColumn.Width = 180;
        _devicePairingColumn.Text = "Pairing";
        _devicePairingColumn.Width = 150;
        _deviceSignalColumn.Text = "RSSI (dBm)";
        _deviceSignalColumn.Width = 180;
        _obdDeviceLabel.Text = "OBD2 device:";
        _obdDeviceLabel.AutoSize = true;
        _obdDeviceLabel.Anchor = AnchorStyles.Left;
        _obdDeviceComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _obdDeviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _obdDeviceComboBox.AccessibleName = "OBD2 Bluetooth device";
        _obdDeviceComboBox.TabIndex = 3;
        _obdDeviceComboBox.SelectedIndexChanged += OnBluetoothDeviceSelectionChanged;
        _sensorDeviceLabel.Text = "Sensor device (WT901):";
        _sensorDeviceLabel.AutoSize = true;
        _sensorDeviceLabel.Anchor = AnchorStyles.Left;
        _sensorDeviceComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sensorDeviceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sensorDeviceComboBox.AccessibleName = "Bluetooth sensor device";
        _sensorDeviceComboBox.TabIndex = 4;
        _sensorDeviceComboBox.SelectedIndexChanged += OnBluetoothDeviceSelectionChanged;
        _bluetoothStatusStrip.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _bluetoothStatusStrip.Dock = DockStyle.None;
        _bluetoothStatusStrip.SizingGrip = false;
        _bluetoothStatusStrip.ShowItemToolTips = true;
        _bluetoothStatusStrip.Items.AddRange(new ToolStripItem[] { _bluetoothScanStatus, _bluetoothScanProgress });
        _bluetoothScanStatus.Spring = true;
        _bluetoothScanStatus.TextAlign = ContentAlignment.MiddleLeft;
        _bluetoothScanStatus.Text = "Bluetooth: open the Bluetooth page to discover devices.";
        _bluetoothScanProgress.Maximum = 20;
        _bluetoothScanProgress.Visible = false;
        _bluetoothScanTimer.Interval = 250;
        _bluetoothScanTimer.Tick += OnBluetoothScanTick;
        //
        // OptionsDialog
        // 
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(15F, 37F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _cancelButton;
        ClientSize = new Size(1543, 1241);
        Controls.Add(_rootLayout);
        Font = new Font("Segoe UI", 10.125F, FontStyle.Regular, GraphicsUnit.Point, 0);
        Margin = new Padding(7, 7, 7, 7);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(1256, 1179);
        Name = "OptionsDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "WARP sensor demo options";
        FormClosing += OnBluetoothOptionsClosing;
        Disposed += OnBluetoothOptionsDisposed;
        _bluetoothPage.ResumeLayout(false);
        _bluetoothLayout.ResumeLayout(false);
        _bluetoothLayout.PerformLayout();
        _bluetoothActions.ResumeLayout(false);
        _bluetoothActions.PerformLayout();
        _bluetoothStatusStrip.ResumeLayout(false);
        _bluetoothStatusStrip.PerformLayout();
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        _sectionToolStrip.ResumeLayout(false);
        _sectionToolStrip.PerformLayout();
        _tabControl.ResumeLayout(false);
        _sourcePage.ResumeLayout(false);
        _sourcePage.PerformLayout();
        _sourceLayout.ResumeLayout(false);
        _sourceLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_obdSearchUpDown).EndInit();
        _sensorsPage.ResumeLayout(false);
        _sensorsPage.PerformLayout();
        _sensorsLayout.ResumeLayout(false);
        _sensorsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_bluetoothSearchUpDown).EndInit();
        _internalFrameButtons.ResumeLayout(false);
        _internalFrameButtons.PerformLayout();
        _bluetoothFrameButtons.ResumeLayout(false);
        _bluetoothFrameButtons.PerformLayout();
        _displayPage.ResumeLayout(false);
        _displayPage.PerformLayout();
        _displayLayout.ResumeLayout(false);
        _displayLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_accelerationDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gpsDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_speedDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_otherDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dampingUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gScaleUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_baseFontUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_exceptionPopupUpDown).EndInit();
        _mapPage.ResumeLayout(false);
        _mapLayout.ResumeLayout(false);
        _mapLayout.PerformLayout();
        _recordingPage.ResumeLayout(false);
        _recordingPage.PerformLayout();
        _recordingLayout.ResumeLayout(false);
        _recordingLayout.PerformLayout();
        _recordingFormatsGroupBox.ResumeLayout(false);
        _recordingFormatsGroupBox.PerformLayout();
        _recordingFormatsPanel.ResumeLayout(false);
        _recordingFormatsPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_rawSnapshotRate).EndInit();
        _replayPage.ResumeLayout(false);
        _replayPage.PerformLayout();
        _replayLayout.ResumeLayout(false);
        _replayLayout.PerformLayout();
        _buttonPanel.ResumeLayout(false);
        _buttonPanel.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel _rootLayout;
    private SymbolToolStrip _sectionToolStrip;
    private SymbolToolStripButton _sourceSectionButton;
    private SymbolToolStripButton _displaySectionButton;
    private SymbolToolStripButton _mapSectionButton;
    private SymbolToolStripButton _recordingSectionButton;
    private SymbolToolStripButton _replaySectionButton;
    private TabControl _tabControl;
    private TabPage _sourcePage;
    private TableLayoutPanel _sourceLayout;
    private Label _sourceLabel;
    private ComboBox _sourceComboBox;
    private Label _serialPortLabel;
    private ComboBox _serialPortComboBox;
    private Label _gnssProfileLabel;
    private ComboBox _gnssProfileComboBox;
    private Label _gpsTargetRateLabel;
    private ComboBox _gpsTargetRateComboBox;
    private Label _obdSearchLabel;
    private NumericUpDown _obdSearchUpDown;
    private Label _obdAdapterLabel;
    private ComboBox _obdAdapterComboBox;
    private Label _accelerationDampingLabel;
    private ComboBox _accelerationDampingComboBox;
    private Label _accelerationDampingHintLabel;
    private Label _exceptionPopupLabel;
    private NumericUpDown _exceptionPopupUpDown;
    private Label _sourceApplyHintLabel;
    private CheckBox _allowReceiverCommandsCheckBox;
    private Label _receiverWarningLabel;
    private TabPage _displayPage;
    private TableLayoutPanel _displayLayout;
    private CheckBox _forceObdGaugesCheckBox;
    private CheckBox _sphericalHorizonCheckBox;
    private CheckBox _alwaysOnCheckBox;
    private SymbolToolStripButton _sensorsSectionButton;
    private TabPage _sensorsPage;
    private TableLayoutPanel _sensorsLayout;
    private Label _sensorSourceLabel;
    private ComboBox _sensorSourceComboBox;
    private Label _bluetoothSearchLabel;
    private NumericUpDown _bluetoothSearchUpDown;
    private Label _sensorSourceHintLabel;
    private Label _internalFrameLabel;
    private FlowLayoutPanel _internalFrameButtons;
    private Button _editInternalReferenceButton;
    private Button _resetInternalReferenceButton;
    private Label _bluetoothFrameLabel;
    private FlowLayoutPanel _bluetoothFrameButtons;
    private Button _editBluetoothReferenceButton;
    private Button _resetBluetoothReferenceButton;
    private Label _sensorNoteLabel;
    private Label _accelerationDigitsLabel;
    private NumericUpDown _accelerationDigitsUpDown;
    private Label _gpsDigitsLabel;
    private NumericUpDown _gpsDigitsUpDown;
    private Label _speedDigitsLabel;
    private NumericUpDown _speedDigitsUpDown;
    private Label _otherDigitsLabel;
    private NumericUpDown _otherDigitsUpDown;
    private Label _dampingLabel;
    private NumericUpDown _dampingUpDown;
    private Label _gScaleLabel;
    private NumericUpDown _gScaleUpDown;
    private Label _baseFontLabel;
    private NumericUpDown _baseFontUpDown;
    private Label _toolbarSizeLabel;
    private ComboBox _toolbarSizeComboBox;
    private Label _numberPreviewLabel;
    private WarpToolkit.WinForms.Sensors.DirectX.SpeedometerOptionsView _speedometerOptions;
    private TabPage _mapPage;
    private TableLayoutPanel _mapLayout;
    private Label _routePathLabel;
    private TextBox _routePathTextBox;
    private Button _browseRouteButton;
    private CheckBox _followPositionCheckBox;
    private CheckBox _mapDebugCheckBox;
    private CheckBox _onlineTilesCheckBox;
    private Label _offlineTilesHintLabel;
    private TabPage _recordingPage;
    private TableLayoutPanel _recordingLayout;
    private Label _recordingFolderLabel;
    private TextBox _recordingFolderTextBox;
    private Button _browseRecordingFolderButton;
    private GroupBox _recordingFormatsGroupBox;
    private FlowLayoutPanel _recordingFormatsPanel;
    private CheckBox _jsonLinesCheckBox;
    private CheckBox _compactJsonCheckBox;
    private CheckBox _csvCheckBox;
    private CheckBox _autoRecordingCheckBox;
    private NumericUpDown _rawSnapshotRate;
    private Label _rawSnapshotRateLabel;
    private TextBox _recordingMarkerLabel;
    private Label _recordingMarkerHint;
    private TabPage _replayPage;
    private FlowLayoutPanel _replayLayout;
    private CheckBox _detectReplaySequenceCheckBox;
    private CheckBox _hideReplayControlsCheckBox;
    private FlowLayoutPanel _buttonPanel;
    private Button _okButton;
    private Button _cancelButton;
    private SymbolToolStripButton _bluetoothSectionButton;
    private TabPage _bluetoothPage;
    private TableLayoutPanel _bluetoothLayout;
    private Label _bluetoothHint;
    private FlowLayoutPanel _bluetoothActions;
    private Button _scanBluetoothButton;
    private Button _stopBluetoothScanButton;
    private ListView _bluetoothDevices;
    private ColumnHeader _deviceNameColumn;
    private ColumnHeader _deviceAddressColumn;
    private ColumnHeader _deviceKindColumn;
    private ColumnHeader _devicePairingColumn;
    private ColumnHeader _deviceSignalColumn;
    private Label _obdDeviceLabel;
    private ComboBox _obdDeviceComboBox;
    private Label _sensorDeviceLabel;
    private ComboBox _sensorDeviceComboBox;
    private StatusStrip _bluetoothStatusStrip;
    private ToolStripStatusLabel _bluetoothScanStatus;
    private ToolStripProgressBar _bluetoothScanProgress;
    private System.Windows.Forms.Timer _bluetoothScanTimer;
}
