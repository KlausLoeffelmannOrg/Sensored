#nullable disable

using WarpToolkit.WinForms.ToolStrips;

namespace WarpSensorsDemo;

partial class OptionsDialog
{
    private System.ComponentModel.IContainer components;
    private TableLayoutPanel _rootLayout;
    private SymbolToolStrip _sectionToolStrip;
    private SymbolToolStripButton _sourceSectionButton;
    private SymbolToolStripButton _displaySectionButton;
    private SymbolToolStripButton _mapSectionButton;
    private SymbolToolStripButton _recordingSectionButton;
    private TabControl _tabControl;
    private TabPage _sourcePage;
    private TableLayoutPanel _sourceLayout;
    private Label _sourceLabel;
    private ComboBox _sourceComboBox;
    private Label _serialPortLabel;
    private ComboBox _serialPortComboBox;
    private TabPage _displayPage;
    private TableLayoutPanel _displayLayout;
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
    private TabPage _mapPage;
    private TableLayoutPanel _mapLayout;
    private Label _routePathLabel;
    private TextBox _routePathTextBox;
    private Button _browseRouteButton;
    private CheckBox _followPositionCheckBox;
    private CheckBox _mapDebugCheckBox;
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
    private FlowLayoutPanel _buttonPanel;
    private Button _okButton;
    private Button _cancelButton;

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
        components = new System.ComponentModel.Container();
        _rootLayout = new TableLayoutPanel();
        _sectionToolStrip = new SymbolToolStrip();
        _sourceSectionButton = new SymbolToolStripButton();
        _displaySectionButton = new SymbolToolStripButton();
        _mapSectionButton = new SymbolToolStripButton();
        _recordingSectionButton = new SymbolToolStripButton();
        _tabControl = new TabControl();
        _sourcePage = new TabPage();
        _sourceLayout = new TableLayoutPanel();
        _sourceLabel = new Label();
        _sourceComboBox = new ComboBox();
        _serialPortLabel = new Label();
        _serialPortComboBox = new ComboBox();
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
        _mapPage = new TabPage();
        _mapLayout = new TableLayoutPanel();
        _routePathLabel = new Label();
        _routePathTextBox = new TextBox();
        _browseRouteButton = new Button();
        _followPositionCheckBox = new CheckBox();
        _mapDebugCheckBox = new CheckBox();
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
        _buttonPanel = new FlowLayoutPanel();
        _okButton = new Button();
        _cancelButton = new Button();
        _rootLayout.SuspendLayout();
        _sectionToolStrip.SuspendLayout();
        _tabControl.SuspendLayout();
        _sourcePage.SuspendLayout();
        _sourceLayout.SuspendLayout();
        _displayPage.SuspendLayout();
        _displayLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_accelerationDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gpsDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_speedDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_otherDigitsUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_dampingUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_gScaleUpDown).BeginInit();
        ((System.ComponentModel.ISupportInitialize)_baseFontUpDown).BeginInit();
        _mapPage.SuspendLayout();
        _mapLayout.SuspendLayout();
        _recordingPage.SuspendLayout();
        _recordingLayout.SuspendLayout();
        _recordingFormatsGroupBox.SuspendLayout();
        _recordingFormatsPanel.SuspendLayout();
        _buttonPanel.SuspendLayout();
        SuspendLayout();
        //
        // _rootLayout
        //
        _rootLayout.ColumnCount = 1;
        _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _rootLayout.Controls.Add(_sectionToolStrip, 0, 0);
        _rootLayout.Controls.Add(_tabControl, 0, 1);
        _rootLayout.Controls.Add(_buttonPanel, 0, 2);
        _rootLayout.Dock = DockStyle.Fill;
        _rootLayout.Location = new Point(0, 0);
        _rootLayout.Name = "_rootLayout";
        _rootLayout.Padding = new Padding(8);
        _rootLayout.RowCount = 3;
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _rootLayout.Size = new Size(720, 580);
        _rootLayout.TabIndex = 0;
        //
        // _sectionToolStrip
        //
        _sectionToolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _sectionToolStrip.ImageScalingSize = new Size(64, 64);
        _sectionToolStrip.Items.AddRange(new ToolStripItem[]
        {
            _sourceSectionButton,
            _displaySectionButton,
            _mapSectionButton,
            _recordingSectionButton
        });
        _sectionToolStrip.Location = new Point(11, 11);
        _sectionToolStrip.Name = "_sectionToolStrip";
        _sectionToolStrip.Padding = new Padding(6, 3, 6, 3);
        _sectionToolStrip.Size = new Size(698, 88);
        _sectionToolStrip.TabIndex = 0;
        //
        // _sourceSectionButton
        //
        _sourceSectionButton.CheckOnClick = true;
        _sourceSectionButton.Name = "_sourceSectionButton";
        _sourceSectionButton.Size = new Size(68, 82);
        _sourceSectionButton.Text = "Source";
        _sourceSectionButton.Click += OnSourceSectionClick;
        //
        // _displaySectionButton
        //
        _displaySectionButton.CheckOnClick = true;
        _displaySectionButton.Name = "_displaySectionButton";
        _displaySectionButton.Size = new Size(68, 82);
        _displaySectionButton.Text = "Display";
        _displaySectionButton.Click += OnDisplaySectionClick;
        //
        // _mapSectionButton
        //
        _mapSectionButton.CheckOnClick = true;
        _mapSectionButton.Name = "_mapSectionButton";
        _mapSectionButton.Size = new Size(68, 82);
        _mapSectionButton.Text = "Map / route";
        _mapSectionButton.Click += OnMapSectionClick;
        //
        // _recordingSectionButton
        //
        _recordingSectionButton.CheckOnClick = true;
        _recordingSectionButton.Name = "_recordingSectionButton";
        _recordingSectionButton.Size = new Size(68, 82);
        _recordingSectionButton.Text = "Recording";
        _recordingSectionButton.Click += OnRecordingSectionClick;
        //
        // _tabControl
        //
        _tabControl.Controls.Add(_sourcePage);
        _tabControl.Controls.Add(_displayPage);
        _tabControl.Controls.Add(_mapPage);
        _tabControl.Controls.Add(_recordingPage);
        _tabControl.Dock = DockStyle.Fill;
        _tabControl.Location = new Point(11, 102);
        _tabControl.Name = "_tabControl";
        _tabControl.SelectedIndex = 0;
        _tabControl.Size = new Size(698, 423);
        _tabControl.TabIndex = 1;
        _tabControl.SelectedIndexChanged += OnSelectedSectionChanged;
        //
        // _sourcePage
        //
        _sourcePage.Controls.Add(_sourceLayout);
        _sourcePage.Location = new Point(4, 24);
        _sourcePage.Name = "_sourcePage";
        _sourcePage.Padding = new Padding(8);
        _sourcePage.Size = new Size(690, 395);
        _sourcePage.TabIndex = 0;
        _sourcePage.Text = "Source";
        _sourcePage.UseVisualStyleBackColor = true;
        //
        // _sourceLayout
        //
        _sourceLayout.ColumnCount = 2;
        _sourceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _sourceLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _sourceLayout.Controls.Add(_sourceLabel, 0, 0);
        _sourceLayout.Controls.Add(_sourceComboBox, 1, 0);
        _sourceLayout.Controls.Add(_serialPortLabel, 0, 1);
        _sourceLayout.Controls.Add(_serialPortComboBox, 1, 1);
        _sourceLayout.Dock = DockStyle.Top;
        _sourceLayout.Location = new Point(8, 8);
        _sourceLayout.Name = "_sourceLayout";
        _sourceLayout.RowCount = 2;
        _sourceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sourceLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _sourceLayout.Size = new Size(674, 68);
        _sourceLayout.TabIndex = 0;
        //
        // _sourceLabel
        //
        _sourceLabel.Anchor = AnchorStyles.Left;
        _sourceLabel.AutoSize = true;
        _sourceLabel.Location = new Point(3, 8);
        _sourceLabel.Name = "_sourceLabel";
        _sourceLabel.Size = new Size(84, 15);
        _sourceLabel.TabIndex = 0;
        _sourceLabel.Text = "Sensor source:";
        //
        // _sourceComboBox
        //
        _sourceComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _sourceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _sourceComboBox.FormattingEnabled = true;
        _sourceComboBox.Location = new Point(96, 4);
        _sourceComboBox.Name = "_sourceComboBox";
        _sourceComboBox.Size = new Size(575, 23);
        _sourceComboBox.TabIndex = 1;
        _sourceComboBox.SelectedIndexChanged += OnSourceChanged;
        //
        // _serialPortLabel
        //
        _serialPortLabel.Anchor = AnchorStyles.Left;
        _serialPortLabel.AutoSize = true;
        _serialPortLabel.Location = new Point(3, 42);
        _serialPortLabel.Name = "_serialPortLabel";
        _serialPortLabel.Size = new Size(62, 15);
        _serialPortLabel.TabIndex = 2;
        _serialPortLabel.Text = "Serial port:";
        //
        // _serialPortComboBox
        //
        _serialPortComboBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _serialPortComboBox.FormattingEnabled = true;
        _serialPortComboBox.Location = new Point(96, 38);
        _serialPortComboBox.Name = "_serialPortComboBox";
        _serialPortComboBox.Size = new Size(575, 23);
        _serialPortComboBox.TabIndex = 3;
        //
        // _displayPage
        //
        _displayPage.Controls.Add(_displayLayout);
        _displayPage.Location = new Point(4, 24);
        _displayPage.Name = "_displayPage";
        _displayPage.Padding = new Padding(8);
        _displayPage.Size = new Size(690, 395);
        _displayPage.TabIndex = 1;
        _displayPage.Text = "Display";
        _displayPage.UseVisualStyleBackColor = true;
        //
        // _displayLayout
        //
        _displayLayout.ColumnCount = 2;
        _displayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _displayLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
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
        _displayLayout.SetColumnSpan(_numberPreviewLabel, 2);
        _displayLayout.Dock = DockStyle.Top;
        _displayLayout.Location = new Point(8, 8);
        _displayLayout.Name = "_displayLayout";
        _displayLayout.RowCount = 9;
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _displayLayout.Size = new Size(674, 302);
        _displayLayout.TabIndex = 0;
        //
        // _accelerationDigitsLabel
        //
        _accelerationDigitsLabel.Anchor = AnchorStyles.Left;
        _accelerationDigitsLabel.AutoSize = true;
        _accelerationDigitsLabel.Location = new Point(3, 8);
        _accelerationDigitsLabel.Name = "_accelerationDigitsLabel";
        _accelerationDigitsLabel.Size = new Size(110, 15);
        _accelerationDigitsLabel.TabIndex = 0;
        _accelerationDigitsLabel.Text = "Acceleration digits:";
        //
        // _accelerationDigitsUpDown
        //
        _accelerationDigitsUpDown.Location = new Point(158, 4);
        _accelerationDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _accelerationDigitsUpDown.Name = "_accelerationDigitsUpDown";
        _accelerationDigitsUpDown.Size = new Size(80, 23);
        _accelerationDigitsUpDown.TabIndex = 1;
        _accelerationDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        //
        // _gpsDigitsLabel
        //
        _gpsDigitsLabel.Anchor = AnchorStyles.Left;
        _gpsDigitsLabel.AutoSize = true;
        _gpsDigitsLabel.Location = new Point(3, 41);
        _gpsDigitsLabel.Name = "_gpsDigitsLabel";
        _gpsDigitsLabel.Size = new Size(107, 15);
        _gpsDigitsLabel.TabIndex = 2;
        _gpsDigitsLabel.Text = "GPS coordinate digits:";
        //
        // _gpsDigitsUpDown
        //
        _gpsDigitsUpDown.Location = new Point(158, 37);
        _gpsDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _gpsDigitsUpDown.Name = "_gpsDigitsUpDown";
        _gpsDigitsUpDown.Size = new Size(80, 23);
        _gpsDigitsUpDown.TabIndex = 3;
        _gpsDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        //
        // _speedDigitsLabel
        //
        _speedDigitsLabel.Anchor = AnchorStyles.Left;
        _speedDigitsLabel.AutoSize = true;
        _speedDigitsLabel.Location = new Point(3, 74);
        _speedDigitsLabel.Name = "_speedDigitsLabel";
        _speedDigitsLabel.Size = new Size(75, 15);
        _speedDigitsLabel.TabIndex = 4;
        _speedDigitsLabel.Text = "Speed digits:";
        //
        // _speedDigitsUpDown
        //
        _speedDigitsUpDown.Location = new Point(158, 70);
        _speedDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _speedDigitsUpDown.Name = "_speedDigitsUpDown";
        _speedDigitsUpDown.Size = new Size(80, 23);
        _speedDigitsUpDown.TabIndex = 5;
        _speedDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        //
        // _otherDigitsLabel
        //
        _otherDigitsLabel.Anchor = AnchorStyles.Left;
        _otherDigitsLabel.AutoSize = true;
        _otherDigitsLabel.Location = new Point(3, 107);
        _otherDigitsLabel.Name = "_otherDigitsLabel";
        _otherDigitsLabel.Size = new Size(73, 15);
        _otherDigitsLabel.TabIndex = 6;
        _otherDigitsLabel.Text = "Other digits:";
        //
        // _otherDigitsUpDown
        //
        _otherDigitsUpDown.Location = new Point(158, 103);
        _otherDigitsUpDown.Maximum = new decimal(new int[] { 8, 0, 0, 0 });
        _otherDigitsUpDown.Name = "_otherDigitsUpDown";
        _otherDigitsUpDown.Size = new Size(80, 23);
        _otherDigitsUpDown.TabIndex = 7;
        _otherDigitsUpDown.ValueChanged += OnNumberFormatChanged;
        //
        // _dampingLabel
        //
        _dampingLabel.Anchor = AnchorStyles.Left;
        _dampingLabel.AutoSize = true;
        _dampingLabel.Location = new Point(3, 140);
        _dampingLabel.Name = "_dampingLabel";
        _dampingLabel.Size = new Size(132, 15);
        _dampingLabel.TabIndex = 8;
        _dampingLabel.Text = "Control damping (ms):";
        //
        // _dampingUpDown
        //
        _dampingUpDown.Increment = new decimal(new int[] { 10, 0, 0, 0 });
        _dampingUpDown.Location = new Point(158, 136);
        _dampingUpDown.Maximum = new decimal(new int[] { 2000, 0, 0, 0 });
        _dampingUpDown.Name = "_dampingUpDown";
        _dampingUpDown.Size = new Size(100, 23);
        _dampingUpDown.TabIndex = 9;
        //
        // _gScaleLabel
        //
        _gScaleLabel.Anchor = AnchorStyles.Left;
        _gScaleLabel.AutoSize = true;
        _gScaleLabel.Location = new Point(3, 173);
        _gScaleLabel.Name = "_gScaleLabel";
        _gScaleLabel.Size = new Size(83, 15);
        _gScaleLabel.TabIndex = 10;
        _gScaleLabel.Text = "G full scale (g):";
        //
        // _gScaleUpDown
        //
        _gScaleUpDown.DecimalPlaces = 2;
        _gScaleUpDown.Increment = new decimal(new int[] { 25, 0, 0, 131072 });
        _gScaleUpDown.Location = new Point(158, 169);
        _gScaleUpDown.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
        _gScaleUpDown.Minimum = new decimal(new int[] { 25, 0, 0, 131072 });
        _gScaleUpDown.Name = "_gScaleUpDown";
        _gScaleUpDown.Size = new Size(100, 23);
        _gScaleUpDown.TabIndex = 11;
        //
        // _baseFontLabel
        //
        _baseFontLabel.Anchor = AnchorStyles.Left;
        _baseFontLabel.AutoSize = true;
        _baseFontLabel.Location = new Point(3, 206);
        _baseFontLabel.Name = "_baseFontLabel";
        _baseFontLabel.Size = new Size(104, 15);
        _baseFontLabel.TabIndex = 12;
        _baseFontLabel.Text = "Base font size (pt):";
        //
        // _baseFontUpDown
        //
        _baseFontUpDown.DecimalPlaces = 1;
        _baseFontUpDown.Increment = new decimal(new int[] { 5, 0, 0, 65536 });
        _baseFontUpDown.Location = new Point(158, 202);
        _baseFontUpDown.Maximum = new decimal(new int[] { 36, 0, 0, 0 });
        _baseFontUpDown.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
        _baseFontUpDown.Name = "_baseFontUpDown";
        _baseFontUpDown.Size = new Size(100, 23);
        _baseFontUpDown.TabIndex = 13;
        //
        // _toolbarSizeLabel
        //
        _toolbarSizeLabel.Anchor = AnchorStyles.Left;
        _toolbarSizeLabel.AutoSize = true;
        _toolbarSizeLabel.Location = new Point(3, 239);
        _toolbarSizeLabel.Name = "_toolbarSizeLabel";
        _toolbarSizeLabel.Size = new Size(97, 15);
        _toolbarSizeLabel.TabIndex = 14;
        _toolbarSizeLabel.Text = "Toolbar icon size:";
        //
        // _toolbarSizeComboBox
        //
        _toolbarSizeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _toolbarSizeComboBox.FormattingEnabled = true;
        _toolbarSizeComboBox.Location = new Point(158, 235);
        _toolbarSizeComboBox.Name = "_toolbarSizeComboBox";
        _toolbarSizeComboBox.Size = new Size(140, 23);
        _toolbarSizeComboBox.TabIndex = 15;
        //
        // _numberPreviewLabel
        //
        _numberPreviewLabel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _numberPreviewLabel.AutoSize = true;
        _numberPreviewLabel.Location = new Point(3, 273);
        _numberPreviewLabel.Margin = new Padding(3, 12, 3, 3);
        _numberPreviewLabel.Name = "_numberPreviewLabel";
        _numberPreviewLabel.Size = new Size(668, 15);
        _numberPreviewLabel.TabIndex = 16;
        _numberPreviewLabel.Text = "Number format preview";
        //
        // _mapPage
        //
        _mapPage.Controls.Add(_mapLayout);
        _mapPage.Location = new Point(4, 24);
        _mapPage.Name = "_mapPage";
        _mapPage.Padding = new Padding(8);
        _mapPage.Size = new Size(690, 395);
        _mapPage.TabIndex = 2;
        _mapPage.Text = "Map / route";
        _mapPage.UseVisualStyleBackColor = true;
        //
        // _mapLayout
        //
        _mapLayout.ColumnCount = 3;
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _mapLayout.Controls.Add(_routePathLabel, 0, 0);
        _mapLayout.Controls.Add(_routePathTextBox, 1, 0);
        _mapLayout.Controls.Add(_browseRouteButton, 2, 0);
        _mapLayout.Controls.Add(_followPositionCheckBox, 1, 1);
        _mapLayout.Controls.Add(_mapDebugCheckBox, 1, 2);
        _mapLayout.Dock = DockStyle.Top;
        _mapLayout.Location = new Point(8, 8);
        _mapLayout.Name = "_mapLayout";
        _mapLayout.RowCount = 3;
        _mapLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mapLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mapLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _mapLayout.Size = new Size(674, 98);
        _mapLayout.TabIndex = 0;
        //
        // _routePathLabel
        //
        _routePathLabel.Anchor = AnchorStyles.Left;
        _routePathLabel.AutoSize = true;
        _routePathLabel.Location = new Point(3, 8);
        _routePathLabel.Name = "_routePathLabel";
        _routePathLabel.Size = new Size(60, 15);
        _routePathLabel.TabIndex = 0;
        _routePathLabel.Text = "Route file:";
        //
        // _routePathTextBox
        //
        _routePathTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _routePathTextBox.Location = new Point(72, 4);
        _routePathTextBox.Name = "_routePathTextBox";
        _routePathTextBox.Size = new Size(514, 23);
        _routePathTextBox.TabIndex = 1;
        //
        // _browseRouteButton
        //
        _browseRouteButton.AutoSize = true;
        _browseRouteButton.Location = new Point(592, 3);
        _browseRouteButton.Name = "_browseRouteButton";
        _browseRouteButton.Size = new Size(79, 25);
        _browseRouteButton.TabIndex = 2;
        _browseRouteButton.Text = "Browse...";
        _browseRouteButton.UseVisualStyleBackColor = true;
        _browseRouteButton.Click += OnBrowseRouteClick;
        //
        // _followPositionCheckBox
        //
        _followPositionCheckBox.AutoSize = true;
        _followPositionCheckBox.Location = new Point(72, 40);
        _followPositionCheckBox.Margin = new Padding(3, 8, 3, 3);
        _followPositionCheckBox.Name = "_followPositionCheckBox";
        _followPositionCheckBox.Size = new Size(159, 19);
        _followPositionCheckBox.TabIndex = 3;
        _followPositionCheckBox.Text = "Follow current GPS point";
        _followPositionCheckBox.UseVisualStyleBackColor = true;
        //
        // _mapDebugCheckBox
        //
        _mapDebugCheckBox.AutoSize = true;
        _mapDebugCheckBox.Location = new Point(72, 70);
        _mapDebugCheckBox.Margin = new Padding(3, 8, 3, 3);
        _mapDebugCheckBox.Name = "_mapDebugCheckBox";
        _mapDebugCheckBox.Size = new Size(230, 19);
        _mapDebugCheckBox.TabIndex = 4;
        _mapDebugCheckBox.Text = "Show map rendering debug information";
        _mapDebugCheckBox.UseVisualStyleBackColor = true;
        //
        // _recordingPage
        //
        _recordingPage.Controls.Add(_recordingLayout);
        _recordingPage.Location = new Point(4, 24);
        _recordingPage.Name = "_recordingPage";
        _recordingPage.Padding = new Padding(8);
        _recordingPage.Size = new Size(690, 395);
        _recordingPage.TabIndex = 3;
        _recordingPage.Text = "Recording";
        _recordingPage.UseVisualStyleBackColor = true;
        //
        // _recordingLayout
        //
        _recordingLayout.ColumnCount = 3;
        _recordingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _recordingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _recordingLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _recordingLayout.Controls.Add(_recordingFolderLabel, 0, 0);
        _recordingLayout.Controls.Add(_recordingFolderTextBox, 1, 0);
        _recordingLayout.Controls.Add(_browseRecordingFolderButton, 2, 0);
        _recordingLayout.Controls.Add(_recordingFormatsGroupBox, 0, 1);
        _recordingLayout.SetColumnSpan(_recordingFormatsGroupBox, 3);
        _recordingLayout.Controls.Add(_autoRecordingCheckBox, 0, 2);
        _recordingLayout.SetColumnSpan(_autoRecordingCheckBox, 3);
        _recordingLayout.Dock = DockStyle.Top;
        _recordingLayout.Location = new Point(8, 8);
        _recordingLayout.Name = "_recordingLayout";
        _recordingLayout.RowCount = 3;
        _recordingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _recordingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _recordingLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _recordingLayout.Size = new Size(674, 138);
        _recordingLayout.TabIndex = 0;
        //
        // _recordingFolderLabel
        //
        _recordingFolderLabel.Anchor = AnchorStyles.Left;
        _recordingFolderLabel.AutoSize = true;
        _recordingFolderLabel.Location = new Point(3, 8);
        _recordingFolderLabel.Name = "_recordingFolderLabel";
        _recordingFolderLabel.Size = new Size(94, 15);
        _recordingFolderLabel.TabIndex = 0;
        _recordingFolderLabel.Text = "Recording root:";
        //
        // _recordingFolderTextBox
        //
        _recordingFolderTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _recordingFolderTextBox.Location = new Point(106, 4);
        _recordingFolderTextBox.Name = "_recordingFolderTextBox";
        _recordingFolderTextBox.Size = new Size(480, 23);
        _recordingFolderTextBox.TabIndex = 1;
        //
        // _browseRecordingFolderButton
        //
        _browseRecordingFolderButton.AutoSize = true;
        _browseRecordingFolderButton.Location = new Point(592, 3);
        _browseRecordingFolderButton.Name = "_browseRecordingFolderButton";
        _browseRecordingFolderButton.Size = new Size(79, 25);
        _browseRecordingFolderButton.TabIndex = 2;
        _browseRecordingFolderButton.Text = "Browse...";
        _browseRecordingFolderButton.UseVisualStyleBackColor = true;
        _browseRecordingFolderButton.Click += OnBrowseRecordingFolderClick;
        //
        // _recordingFormatsGroupBox
        //
        _recordingFormatsGroupBox.AutoSize = true;
        _recordingFormatsGroupBox.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _recordingFormatsGroupBox.Controls.Add(_recordingFormatsPanel);
        _recordingFormatsGroupBox.Location = new Point(3, 34);
        _recordingFormatsGroupBox.Name = "_recordingFormatsGroupBox";
        _recordingFormatsGroupBox.Padding = new Padding(8);
        _recordingFormatsGroupBox.Size = new Size(318, 68);
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
        _recordingFormatsPanel.Location = new Point(8, 24);
        _recordingFormatsPanel.Name = "_recordingFormatsPanel";
        _recordingFormatsPanel.Size = new Size(302, 36);
        _recordingFormatsPanel.TabIndex = 0;
        //
        // _jsonLinesCheckBox
        //
        _jsonLinesCheckBox.AutoSize = true;
        _jsonLinesCheckBox.Location = new Point(3, 3);
        _jsonLinesCheckBox.Name = "_jsonLinesCheckBox";
        _jsonLinesCheckBox.Size = new Size(84, 19);
        _jsonLinesCheckBox.TabIndex = 0;
        _jsonLinesCheckBox.Text = "JSON Lines";
        _jsonLinesCheckBox.UseVisualStyleBackColor = true;
        //
        // _compactJsonCheckBox
        //
        _compactJsonCheckBox.AutoSize = true;
        _compactJsonCheckBox.Location = new Point(93, 3);
        _compactJsonCheckBox.Name = "_compactJsonCheckBox";
        _compactJsonCheckBox.Size = new Size(102, 19);
        _compactJsonCheckBox.TabIndex = 1;
        _compactJsonCheckBox.Text = "Compact JSON";
        _compactJsonCheckBox.UseVisualStyleBackColor = true;
        //
        // _csvCheckBox
        //
        _csvCheckBox.AutoSize = true;
        _csvCheckBox.Location = new Point(201, 3);
        _csvCheckBox.Name = "_csvCheckBox";
        _csvCheckBox.Size = new Size(98, 19);
        _csvCheckBox.TabIndex = 2;
        _csvCheckBox.Text = "Semicolon CSV";
        _csvCheckBox.UseVisualStyleBackColor = true;
        //
        // _autoRecordingCheckBox
        //
        _autoRecordingCheckBox.AutoSize = true;
        _autoRecordingCheckBox.Location = new Point(3, 113);
        _autoRecordingCheckBox.Margin = new Padding(3, 8, 3, 3);
        _autoRecordingCheckBox.Name = "_autoRecordingCheckBox";
        _autoRecordingCheckBox.Size = new Size(249, 29);
        _autoRecordingCheckBox.TabIndex = 4;
        _autoRecordingCheckBox.Text = "Start recording with sensors";
        _autoRecordingCheckBox.UseVisualStyleBackColor = true;
        //
        // _buttonPanel
        //
        _buttonPanel.Anchor = AnchorStyles.Right;
        _buttonPanel.AutoSize = true;
        _buttonPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _buttonPanel.Controls.Add(_okButton);
        _buttonPanel.Controls.Add(_cancelButton);
        _buttonPanel.FlowDirection = FlowDirection.LeftToRight;
        _buttonPanel.Location = new Point(547, 540);
        _buttonPanel.Name = "_buttonPanel";
        _buttonPanel.Size = new Size(162, 29);
        _buttonPanel.TabIndex = 2;
        //
        // _okButton
        //
        _okButton.AutoSize = true;
        _okButton.Location = new Point(3, 3);
        _okButton.MinimumSize = new Size(75, 0);
        _okButton.Name = "_okButton";
        _okButton.Size = new Size(75, 23);
        _okButton.TabIndex = 0;
        _okButton.Text = "OK";
        _okButton.UseVisualStyleBackColor = true;
        _okButton.Click += OnAcceptClick;
        //
        // _cancelButton
        //
        _cancelButton.AutoSize = true;
        _cancelButton.DialogResult = DialogResult.Cancel;
        _cancelButton.Location = new Point(84, 3);
        _cancelButton.MinimumSize = new Size(75, 0);
        _cancelButton.Name = "_cancelButton";
        _cancelButton.Size = new Size(75, 23);
        _cancelButton.TabIndex = 1;
        _cancelButton.Text = "Cancel";
        _cancelButton.UseVisualStyleBackColor = true;
        //
        // OptionsDialog
        //
        AcceptButton = _okButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = _cancelButton;
        ClientSize = new Size(720, 580);
        Controls.Add(_rootLayout);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(600, 520);
        Name = "OptionsDialog";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "WARP sensor demo options";
        _sectionToolStrip.ResumeLayout(false);
        _sectionToolStrip.PerformLayout();
        _rootLayout.ResumeLayout(false);
        _rootLayout.PerformLayout();
        _tabControl.ResumeLayout(false);
        _sourcePage.ResumeLayout(false);
        _sourceLayout.ResumeLayout(false);
        _sourceLayout.PerformLayout();
        _displayPage.ResumeLayout(false);
        _displayLayout.ResumeLayout(false);
        _displayLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_accelerationDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gpsDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_speedDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_otherDigitsUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_dampingUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_gScaleUpDown).EndInit();
        ((System.ComponentModel.ISupportInitialize)_baseFontUpDown).EndInit();
        _mapPage.ResumeLayout(false);
        _mapLayout.ResumeLayout(false);
        _mapLayout.PerformLayout();
        _recordingPage.ResumeLayout(false);
        _recordingLayout.ResumeLayout(false);
        _recordingLayout.PerformLayout();
        _recordingFormatsGroupBox.ResumeLayout(false);
        _recordingFormatsGroupBox.PerformLayout();
        _recordingFormatsPanel.ResumeLayout(false);
        _recordingFormatsPanel.PerformLayout();
        _buttonPanel.ResumeLayout(false);
        _buttonPanel.PerformLayout();
        ResumeLayout(false);
    }
}
