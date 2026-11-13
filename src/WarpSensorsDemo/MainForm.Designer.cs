#nullable disable

using WarpToolkit.WinForms.Containers;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.ToolStrips;
using WarpSensorsDemo.TabViews;

namespace WarpSensorsDemo;

partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _startButton;
    private SymbolToolStripButton _restartButton;
    private SymbolToolStripButton _pauseButton;
    private SymbolToolStripButton _stopButton;
    private ToolStripSeparator _sessionSeparator;
    private ToolStripDropDownButton _gpsSourceButton;
    private SymbolToolStripMenuItem _windowsGpsMenuItem;
    private SymbolToolStripMenuItem _serialGpsMenuItem;
    private SymbolToolStripButton _optionsButton;
    private ToolStripDropDownButton _mapButton;
    private SymbolToolStripMenuItem _mapEnabledMenuItem;
    private SymbolToolStripMenuItem _mapRowSpanMenuItem;
    private SymbolToolStripMenuItem _mapColumnSpanMenuItem;
    private ToolStripSeparator _displaySeparator;
    private ToolStripDropDownButton _dampingButton;
    private SymbolToolStripMenuItem _dampingOffMenuItem;
    private SymbolToolStripMenuItem _damping50MenuItem;
    private SymbolToolStripMenuItem _damping100MenuItem;
    private SymbolToolStripMenuItem _damping200MenuItem;
    private SymbolToolStripMenuItem _damping500MenuItem;
    private ToolStripDropDownButton _gScaleButton;
    private SymbolToolStripMenuItem _gScale075MenuItem;
    private SymbolToolStripMenuItem _gScale100MenuItem;
    private SymbolToolStripMenuItem _gScale150MenuItem;
    private SymbolToolStripMenuItem _gScale200MenuItem;
    private SymbolToolStripMenuItem _gScale300MenuItem;
    private SymbolToolStripButton _rawSensorsButton;
    private SymbolToolStripButton _rawLogButton;
    private ToolStripDropDownButton _baseFontButton;
    private SymbolToolStripMenuItem _baseFont10MenuItem;
    private SymbolToolStripMenuItem _baseFont12MenuItem;
    private SymbolToolStripMenuItem _baseFont14MenuItem;
    private SymbolToolStripMenuItem _baseFont18MenuItem;
    private SymbolToolStripMenuItem _baseFont24MenuItem;
    private SymbolToolStripButton _recordingButton;
    private SymbolToolStripButton _fullScreenButton;
    private ToolStripSeparator _exitSeparator;
    private SymbolToolStripButton _exitButton;
    private TableLayoutPanel _dashboardLayout;
    private TableLayoutPanel _gaugeLayout;
    private D2DSpeedometer _speedometer;
    private D2DArtificialHorizon _artificialHorizon;
    private D2DAccelerationGauge _accelerationGauge;
    private D2DCompassGauge _compassGauge;
    private TableLayoutPanel _mapLayout;
    private SensorRouteMapControl _routeMap;
    private TrackBar _mapZoomTrackBar;
    private Panel _bottomPanel;
    private FluentTabControl _bottomTabControl;
    private TelemetryTabView _telemetryTabHost;
    private RouteSectionTabView _routeSectionTabHost;
    private DebugTabView _debugTabHost;
    private StatusStrip _statusStrip;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _imuRateLabel;
    private ToolStripStatusLabel _gpsRateLabel;
    private ToolStripDropDownButton _lastExceptionButton;
    private System.Windows.Forms.Timer _statusTimer;
    private ToolTip _readingsToolTip;

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
        _toolStrip = new SymbolToolStrip();
        _startButton = new SymbolToolStripButton();
        _restartButton = new SymbolToolStripButton();
        _pauseButton = new SymbolToolStripButton();
        _stopButton = new SymbolToolStripButton();
        _sessionSeparator = new ToolStripSeparator();
        _gpsSourceButton = new ToolStripDropDownButton();
        _windowsGpsMenuItem = new SymbolToolStripMenuItem();
        _serialGpsMenuItem = new SymbolToolStripMenuItem();
        _optionsButton = new SymbolToolStripButton();
        _mapButton = new ToolStripDropDownButton();
        _mapEnabledMenuItem = new SymbolToolStripMenuItem();
        _mapRowSpanMenuItem = new SymbolToolStripMenuItem();
        _mapColumnSpanMenuItem = new SymbolToolStripMenuItem();
        _displaySeparator = new ToolStripSeparator();
        _dampingButton = new ToolStripDropDownButton();
        _dampingOffMenuItem = new SymbolToolStripMenuItem();
        _damping50MenuItem = new SymbolToolStripMenuItem();
        _damping100MenuItem = new SymbolToolStripMenuItem();
        _damping200MenuItem = new SymbolToolStripMenuItem();
        _damping500MenuItem = new SymbolToolStripMenuItem();
        _gScaleButton = new ToolStripDropDownButton();
        _gScale075MenuItem = new SymbolToolStripMenuItem();
        _gScale100MenuItem = new SymbolToolStripMenuItem();
        _gScale150MenuItem = new SymbolToolStripMenuItem();
        _gScale200MenuItem = new SymbolToolStripMenuItem();
        _gScale300MenuItem = new SymbolToolStripMenuItem();
        _rawSensorsButton = new SymbolToolStripButton();
        _rawLogButton = new SymbolToolStripButton();
        _baseFontButton = new ToolStripDropDownButton();
        _baseFont10MenuItem = new SymbolToolStripMenuItem();
        _baseFont12MenuItem = new SymbolToolStripMenuItem();
        _baseFont14MenuItem = new SymbolToolStripMenuItem();
        _baseFont18MenuItem = new SymbolToolStripMenuItem();
        _baseFont24MenuItem = new SymbolToolStripMenuItem();
        _recordingButton = new SymbolToolStripButton();
        _fullScreenButton = new SymbolToolStripButton();
        _exitSeparator = new ToolStripSeparator();
        _exitButton = new SymbolToolStripButton();
        _dashboardLayout = new TableLayoutPanel();
        _gaugeLayout = new TableLayoutPanel();
        _speedometer = new D2DSpeedometer();
        _artificialHorizon = new D2DArtificialHorizon();
        _accelerationGauge = new D2DAccelerationGauge();
        _compassGauge = new D2DCompassGauge();
        _mapLayout = new TableLayoutPanel();
        _routeMap = new SensorRouteMapControl();
        _mapZoomTrackBar = new TrackBar();
        _bottomPanel = new Panel();
        _bottomTabControl = new FluentTabControl();
        _telemetryTabHost = new TelemetryTabView();
        _routeSectionTabHost = new RouteSectionTabView();
        _debugTabHost = new DebugTabView();
        _statusStrip = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _imuRateLabel = new ToolStripStatusLabel();
        _gpsRateLabel = new ToolStripStatusLabel();
        _lastExceptionButton = new ToolStripDropDownButton();
        _statusTimer = new System.Windows.Forms.Timer(components);
        _readingsToolTip = new ToolTip(components);
        _toolStrip.SuspendLayout();
        _dashboardLayout.SuspendLayout();
        _gaugeLayout.SuspendLayout();
        _mapLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_mapZoomTrackBar).BeginInit();
        _bottomPanel.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        // 
        // _toolStrip
        // 
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.ImageScalingSize = new Size(64, 64);
        _toolStrip.Items.AddRange(new ToolStripItem[] { _startButton, _restartButton, _pauseButton, _stopButton, _sessionSeparator, _gpsSourceButton, _optionsButton, _mapButton, _displaySeparator, _dampingButton, _gScaleButton, _rawSensorsButton, _rawLogButton, _baseFontButton, _recordingButton, _fullScreenButton, _exitSeparator, _exitButton });
        _toolStrip.Location = new Point(0, 0);
        _toolStrip.Name = "_toolStrip";
        _toolStrip.Padding = new Padding(9, 5, 9, 5);
        _toolStrip.Size = new Size(2000, 59);
        _toolStrip.TabIndex = 0;
        // 
        // _startButton
        // 
        _startButton.Image = null;
        _startButton.Margin = new Padding(5, 10, 5, 10);
        _startButton.Name = "_startButton";
        _startButton.Size = new Size(52, 29);
        _startButton.Text = "Start";
        // 
        // _restartButton
        // 
        _restartButton.Image = null;
        _restartButton.Margin = new Padding(5, 10, 5, 10);
        _restartButton.Name = "_restartButton";
        _restartButton.Size = new Size(70, 29);
        _restartButton.Text = "Restart";
        // 
        // _pauseButton
        // 
        _pauseButton.CheckOnClick = true;
        _pauseButton.Image = null;
        _pauseButton.Margin = new Padding(5, 10, 5, 10);
        _pauseButton.Name = "_pauseButton";
        _pauseButton.Size = new Size(61, 29);
        _pauseButton.Text = "Pause";
        // 
        // _stopButton
        // 
        _stopButton.Image = null;
        _stopButton.Margin = new Padding(5, 10, 5, 10);
        _stopButton.Name = "_stopButton";
        _stopButton.Size = new Size(53, 29);
        _stopButton.Text = "Stop";
        // 
        // _sessionSeparator
        // 
        _sessionSeparator.Name = "_sessionSeparator";
        _sessionSeparator.Size = new Size(6, 49);
        // 
        // _gpsSourceButton
        // 
        _gpsSourceButton.DropDownItems.AddRange(new ToolStripItem[] { _windowsGpsMenuItem, _serialGpsMenuItem });
        _gpsSourceButton.Margin = new Padding(5, 10, 5, 10);
        _gpsSourceButton.Name = "_gpsSourceButton";
        _gpsSourceButton.Size = new Size(119, 29);
        _gpsSourceButton.Text = "GPS source";
        // 
        // _windowsGpsMenuItem
        // 
        _windowsGpsMenuItem.Image = null;
        _windowsGpsMenuItem.Name = "_windowsGpsMenuItem";
        _windowsGpsMenuItem.Size = new Size(276, 34);
        _windowsGpsMenuItem.Text = "Windows Location";
        // 
        // _serialGpsMenuItem
        // 
        _serialGpsMenuItem.Image = null;
        _serialGpsMenuItem.Name = "_serialGpsMenuItem";
        _serialGpsMenuItem.Size = new Size(276, 34);
        _serialGpsMenuItem.Text = "Serial NMEA receiver";
        // 
        // _optionsButton
        // 
        _optionsButton.Image = null;
        _optionsButton.Margin = new Padding(5, 10, 5, 10);
        _optionsButton.Name = "_optionsButton";
        _optionsButton.Size = new Size(80, 29);
        _optionsButton.Text = "Options";
        // 
        // _mapButton
        // 
        _mapButton.DropDownItems.AddRange(new ToolStripItem[] { _mapEnabledMenuItem, _mapRowSpanMenuItem, _mapColumnSpanMenuItem });
        _mapButton.Margin = new Padding(5, 10, 5, 10);
        _mapButton.Name = "_mapButton";
        _mapButton.Size = new Size(66, 29);
        _mapButton.Text = "Map";
        // 
        // _mapEnabledMenuItem
        // 
        _mapEnabledMenuItem.CheckOnClick = true;
        _mapEnabledMenuItem.Image = null;
        _mapEnabledMenuItem.Name = "_mapEnabledMenuItem";
        _mapEnabledMenuItem.Size = new Size(269, 34);
        _mapEnabledMenuItem.Text = "Map enabled";
        // 
        // _mapRowSpanMenuItem
        // 
        _mapRowSpanMenuItem.CheckOnClick = true;
        _mapRowSpanMenuItem.Image = null;
        _mapRowSpanMenuItem.Name = "_mapRowSpanMenuItem";
        _mapRowSpanMenuItem.Size = new Size(269, 34);
        _mapRowSpanMenuItem.Text = "Span both rows";
        // 
        // _mapColumnSpanMenuItem
        // 
        _mapColumnSpanMenuItem.CheckOnClick = true;
        _mapColumnSpanMenuItem.Image = null;
        _mapColumnSpanMenuItem.Name = "_mapColumnSpanMenuItem";
        _mapColumnSpanMenuItem.Size = new Size(269, 34);
        _mapColumnSpanMenuItem.Text = "Span both columns";
        // 
        // _displaySeparator
        // 
        _displaySeparator.Name = "_displaySeparator";
        _displaySeparator.Size = new Size(6, 49);
        // 
        // _dampingButton
        // 
        _dampingButton.DropDownItems.AddRange(new ToolStripItem[] { _dampingOffMenuItem, _damping50MenuItem, _damping100MenuItem, _damping200MenuItem, _damping500MenuItem });
        _dampingButton.Margin = new Padding(5, 10, 5, 10);
        _dampingButton.Name = "_dampingButton";
        _dampingButton.Size = new Size(104, 29);
        _dampingButton.Text = "Damping";
        // 
        // _dampingOffMenuItem
        // 
        _dampingOffMenuItem.Image = null;
        _dampingOffMenuItem.Name = "_dampingOffMenuItem";
        _dampingOffMenuItem.Size = new Size(173, 34);
        _dampingOffMenuItem.Text = "Off";
        // 
        // _damping50MenuItem
        // 
        _damping50MenuItem.Image = null;
        _damping50MenuItem.Name = "_damping50MenuItem";
        _damping50MenuItem.Size = new Size(173, 34);
        _damping50MenuItem.Text = "50 ms";
        // 
        // _damping100MenuItem
        // 
        _damping100MenuItem.Image = null;
        _damping100MenuItem.Name = "_damping100MenuItem";
        _damping100MenuItem.Size = new Size(173, 34);
        _damping100MenuItem.Text = "100 ms";
        // 
        // _damping200MenuItem
        // 
        _damping200MenuItem.Image = null;
        _damping200MenuItem.Name = "_damping200MenuItem";
        _damping200MenuItem.Size = new Size(173, 34);
        _damping200MenuItem.Text = "200 ms";
        // 
        // _damping500MenuItem
        // 
        _damping500MenuItem.Image = null;
        _damping500MenuItem.Name = "_damping500MenuItem";
        _damping500MenuItem.Size = new Size(173, 34);
        _damping500MenuItem.Text = "500 ms";
        // 
        // _gScaleButton
        // 
        _gScaleButton.DropDownItems.AddRange(new ToolStripItem[] { _gScale075MenuItem, _gScale100MenuItem, _gScale150MenuItem, _gScale200MenuItem, _gScale300MenuItem });
        _gScaleButton.Margin = new Padding(5, 10, 5, 10);
        _gScaleButton.Name = "_gScaleButton";
        _gScaleButton.Size = new Size(85, 29);
        _gScaleButton.Text = "G scale";
        // 
        // _gScale075MenuItem
        // 
        _gScale075MenuItem.Image = null;
        _gScale075MenuItem.Name = "_gScale075MenuItem";
        _gScale075MenuItem.Size = new Size(164, 34);
        _gScale075MenuItem.Text = "0.75 g";
        // 
        // _gScale100MenuItem
        // 
        _gScale100MenuItem.Image = null;
        _gScale100MenuItem.Name = "_gScale100MenuItem";
        _gScale100MenuItem.Size = new Size(164, 34);
        _gScale100MenuItem.Text = "1.00 g";
        // 
        // _gScale150MenuItem
        // 
        _gScale150MenuItem.Image = null;
        _gScale150MenuItem.Name = "_gScale150MenuItem";
        _gScale150MenuItem.Size = new Size(164, 34);
        _gScale150MenuItem.Text = "1.50 g";
        // 
        // _gScale200MenuItem
        // 
        _gScale200MenuItem.Image = null;
        _gScale200MenuItem.Name = "_gScale200MenuItem";
        _gScale200MenuItem.Size = new Size(164, 34);
        _gScale200MenuItem.Text = "2.00 g";
        // 
        // _gScale300MenuItem
        // 
        _gScale300MenuItem.Image = null;
        _gScale300MenuItem.Name = "_gScale300MenuItem";
        _gScale300MenuItem.Size = new Size(164, 34);
        _gScale300MenuItem.Text = "3.00 g";
        // 
        // _rawSensorsButton
        // 
        _rawSensorsButton.Image = null;
        _rawSensorsButton.Margin = new Padding(5, 10, 5, 10);
        _rawSensorsButton.Name = "_rawSensorsButton";
        _rawSensorsButton.Size = new Size(114, 29);
        _rawSensorsButton.Text = "Raw sensors";
        // 
        // _rawLogButton
        // 
        _rawLogButton.Image = null;
        _rawLogButton.Margin = new Padding(5, 10, 5, 10);
        _rawLogButton.Name = "_rawLogButton";
        _rawLogButton.Size = new Size(80, 29);
        _rawLogButton.Text = "Raw log";
        // 
        // _baseFontButton
        // 
        _baseFontButton.DropDownItems.AddRange(new ToolStripItem[] { _baseFont10MenuItem, _baseFont12MenuItem, _baseFont14MenuItem, _baseFont18MenuItem, _baseFont24MenuItem });
        _baseFontButton.Margin = new Padding(5, 10, 5, 10);
        _baseFontButton.Name = "_baseFontButton";
        _baseFontButton.Size = new Size(104, 29);
        _baseFontButton.Text = "Base font";
        // 
        // _baseFont10MenuItem
        // 
        _baseFont10MenuItem.Image = null;
        _baseFont10MenuItem.Name = "_baseFont10MenuItem";
        _baseFont10MenuItem.Size = new Size(156, 34);
        _baseFont10MenuItem.Text = "10 pt";
        // 
        // _baseFont12MenuItem
        // 
        _baseFont12MenuItem.Image = null;
        _baseFont12MenuItem.Name = "_baseFont12MenuItem";
        _baseFont12MenuItem.Size = new Size(156, 34);
        _baseFont12MenuItem.Text = "12 pt";
        // 
        // _baseFont14MenuItem
        // 
        _baseFont14MenuItem.Image = null;
        _baseFont14MenuItem.Name = "_baseFont14MenuItem";
        _baseFont14MenuItem.Size = new Size(156, 34);
        _baseFont14MenuItem.Text = "14 pt";
        // 
        // _baseFont18MenuItem
        // 
        _baseFont18MenuItem.Image = null;
        _baseFont18MenuItem.Name = "_baseFont18MenuItem";
        _baseFont18MenuItem.Size = new Size(156, 34);
        _baseFont18MenuItem.Text = "18 pt";
        // 
        // _baseFont24MenuItem
        // 
        _baseFont24MenuItem.Image = null;
        _baseFont24MenuItem.Name = "_baseFont24MenuItem";
        _baseFont24MenuItem.Size = new Size(156, 34);
        _baseFont24MenuItem.Text = "24 pt";
        // 
        // _recordingButton
        // 
        _recordingButton.Image = null;
        _recordingButton.Margin = new Padding(5, 10, 5, 10);
        _recordingButton.Name = "_recordingButton";
        _recordingButton.Size = new Size(96, 29);
        _recordingButton.Text = "Recording";
        // 
        // _fullScreenButton
        // 
        _fullScreenButton.CheckOnClick = true;
        _fullScreenButton.Image = null;
        _fullScreenButton.Margin = new Padding(5, 10, 5, 10);
        _fullScreenButton.Name = "_fullScreenButton";
        _fullScreenButton.Size = new Size(98, 29);
        _fullScreenButton.Text = "Full screen";
        // 
        // _exitSeparator
        // 
        _exitSeparator.Name = "_exitSeparator";
        _exitSeparator.Size = new Size(6, 49);
        // 
        // _exitButton
        // 
        _exitButton.Image = null;
        _exitButton.Margin = new Padding(5, 10, 5, 10);
        _exitButton.Name = "_exitButton";
        _exitButton.Size = new Size(43, 29);
        _exitButton.Text = "Exit";
        // 
        // _dashboardLayout
        // 
        _dashboardLayout.ColumnCount = 2;
        _dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _dashboardLayout.Controls.Add(_gaugeLayout, 0, 0);
        _dashboardLayout.Controls.Add(_mapLayout, 1, 0);
        _dashboardLayout.Controls.Add(_bottomPanel, 1, 1);
        _dashboardLayout.Dock = DockStyle.Fill;
        _dashboardLayout.Location = new Point(0, 59);
        _dashboardLayout.Name = "_dashboardLayout";
        _dashboardLayout.Padding = new Padding(11, 13, 11, 13);
        _dashboardLayout.RowCount = 2;
        _dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        _dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _dashboardLayout.Size = new Size(2000, 1372);
        _dashboardLayout.TabIndex = 1;
        // 
        // _gaugeLayout
        // 
        _gaugeLayout.ColumnCount = 2;
        _gaugeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gaugeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gaugeLayout.Controls.Add(_speedometer, 0, 0);
        _gaugeLayout.Controls.Add(_artificialHorizon, 1, 0);
        _gaugeLayout.Controls.Add(_accelerationGauge, 0, 1);
        _gaugeLayout.Controls.Add(_compassGauge, 1, 1);
        _gaugeLayout.Dock = DockStyle.Fill;
        _gaugeLayout.Location = new Point(15, 18);
        _gaugeLayout.Margin = new Padding(4, 5, 4, 5);
        _gaugeLayout.Name = "_gaugeLayout";
        _gaugeLayout.RowCount = 2;
        _dashboardLayout.SetRowSpan(_gaugeLayout, 2);
        _gaugeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _gaugeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _gaugeLayout.Size = new Size(981, 1336);
        _gaugeLayout.TabIndex = 0;
        // 
        // _speedometer
        // 
        _speedometer.AccessibleName = "Speedometer";
        _speedometer.AccessibleRole = AccessibleRole.Graphic;
        _speedometer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _speedometer.Location = new Point(4, 5);
        _speedometer.Margin = new Padding(4, 5, 4, 5);
        _speedometer.Name = "_speedometer";
        _speedometer.Padding = new Padding(11, 13, 11, 13);
        _speedometer.Size = new Size(482, 658);
        _speedometer.TabIndex = 0;
        _speedometer.TabStop = false;
        // 
        // _artificialHorizon
        // 
        _artificialHorizon.AccessibleName = "Artificial horizon";
        _artificialHorizon.AccessibleRole = AccessibleRole.Graphic;
        _artificialHorizon.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _artificialHorizon.Location = new Point(494, 5);
        _artificialHorizon.Margin = new Padding(4, 5, 4, 5);
        _artificialHorizon.Name = "_artificialHorizon";
        _artificialHorizon.Padding = new Padding(11, 13, 11, 13);
        _artificialHorizon.Size = new Size(483, 658);
        _artificialHorizon.TabIndex = 1;
        _artificialHorizon.TabStop = false;
        // 
        // _accelerationGauge
        // 
        _accelerationGauge.AccessibleDescription = "Raw device X is caller-selected forward; raw device Y is caller-selected lateral.";
        _accelerationGauge.AccessibleName = "Acceleration gauge";
        _accelerationGauge.AccessibleRole = AccessibleRole.Graphic;
        _accelerationGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _accelerationGauge.Location = new Point(4, 673);
        _accelerationGauge.Margin = new Padding(4, 5, 4, 5);
        _accelerationGauge.Name = "_accelerationGauge";
        _accelerationGauge.Padding = new Padding(11, 13, 11, 13);
        _accelerationGauge.Size = new Size(482, 658);
        _accelerationGauge.TabIndex = 2;
        _accelerationGauge.TabStop = false;
        // 
        // _compassGauge
        // 
        _compassGauge.AccessibleName = "Compass";
        _compassGauge.AccessibleRole = AccessibleRole.Graphic;
        _compassGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _compassGauge.Location = new Point(494, 673);
        _compassGauge.Margin = new Padding(4, 5, 4, 5);
        _compassGauge.Name = "_compassGauge";
        _compassGauge.Padding = new Padding(11, 13, 11, 13);
        _compassGauge.Size = new Size(483, 658);
        _compassGauge.TabIndex = 3;
        _compassGauge.TabStop = false;
        // 
        // _mapLayout
        // 
        _mapLayout.ColumnCount = 2;
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mapLayout.ColumnStyles.Add(new ColumnStyle());
        _mapLayout.Controls.Add(_routeMap, 0, 0);
        _mapLayout.Controls.Add(_mapZoomTrackBar, 1, 0);
        _mapLayout.Dock = DockStyle.Fill;
        _mapLayout.Location = new Point(1004, 18);
        _mapLayout.Margin = new Padding(4, 5, 4, 5);
        _mapLayout.Name = "_mapLayout";
        _mapLayout.RowCount = 1;
        _mapLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mapLayout.Size = new Size(981, 528);
        _mapLayout.TabIndex = 0;
        // 
        // _routeMap
        // 
        _routeMap.AccessibleName = "Sensor route map";
        _routeMap.Dock = DockStyle.Fill;
        _routeMap.Location = new Point(4, 5);
        _routeMap.Margin = new Padding(4, 5, 4, 5);
        _routeMap.Name = "_routeMap";
        _routeMap.Size = new Size(896, 518);
        _routeMap.TabIndex = 0;
        // 
        // _mapZoomTrackBar
        // 
        _mapZoomTrackBar.AccessibleName = "Map zoom";
        _mapZoomTrackBar.Dock = DockStyle.Fill;
        _mapZoomTrackBar.LargeChange = 1;
        _mapZoomTrackBar.Location = new Point(908, 5);
        _mapZoomTrackBar.Margin = new Padding(4, 5, 4, 5);
        _mapZoomTrackBar.Maximum = 4;
        _mapZoomTrackBar.Minimum = -2;
        _mapZoomTrackBar.Name = "_mapZoomTrackBar";
        _mapZoomTrackBar.Orientation = Orientation.Vertical;
        _mapZoomTrackBar.Size = new Size(69, 518);
        _mapZoomTrackBar.TabIndex = 1;
        _mapZoomTrackBar.TickStyle = TickStyle.Both;
        _mapZoomTrackBar.ValueChanged += OnMapZoomChanged;
        // 
        // _bottomPanel
        // 
        _bottomPanel.Controls.Add(_bottomTabControl);
        _bottomPanel.Controls.Add(_telemetryTabHost);
        _bottomPanel.Controls.Add(_routeSectionTabHost);
        _bottomPanel.Controls.Add(_debugTabHost);
        _bottomPanel.Dock = DockStyle.Fill;
        _bottomPanel.Location = new Point(1004, 556);
        _bottomPanel.Margin = new Padding(4, 5, 4, 5);
        _bottomPanel.Name = "_bottomPanel";
        _bottomPanel.Size = new Size(981, 798);
        _bottomPanel.TabIndex = 1;
        // 
        // _bottomTabControl
        // 
        _bottomTabControl.Dock = DockStyle.Fill;
        _bottomTabControl.Location = new Point(0, 0);
        _bottomTabControl.Margin = new Padding(4, 5, 4, 5);
        _bottomTabControl.Name = "_bottomTabControl";
        _bottomTabControl.Size = new Size(981, 798);
        _bottomTabControl.TabIndex = 0;
        // 
        // _telemetryTabHost
        // 
        _telemetryTabHost.AccessibleName = "Telemetry";
        _telemetryTabHost.Dock = DockStyle.Fill;
        _telemetryTabHost.Location = new Point(0, 0);
        _telemetryTabHost.Margin = new Padding(4, 5, 4, 5);
        _telemetryTabHost.Name = "_telemetryTabHost";
        _telemetryTabHost.Size = new Size(981, 798);
        _telemetryTabHost.TabIndex = 1;
        _telemetryTabHost.VisualStylesMode = VisualStylesMode.Net11;
        // 
        // _routeSectionTabHost
        // 
        _routeSectionTabHost.AccessibleName = "Route sections";
        _routeSectionTabHost.Dock = DockStyle.Fill;
        _routeSectionTabHost.Location = new Point(0, 0);
        _routeSectionTabHost.Margin = new Padding(4, 5, 4, 5);
        _routeSectionTabHost.Name = "_routeSectionTabHost";
        _routeSectionTabHost.Size = new Size(981, 798);
        _routeSectionTabHost.TabIndex = 2;
        // 
        // _debugTabHost
        // 
        _debugTabHost.AccessibleName = "Debug";
        _debugTabHost.Dock = DockStyle.Fill;
        _debugTabHost.Location = new Point(0, 0);
        _debugTabHost.Margin = new Padding(4, 5, 4, 5);
        _debugTabHost.Name = "_debugTabHost";
        _debugTabHost.Padding = new Padding(10);
        _debugTabHost.Size = new Size(981, 798);
        _debugTabHost.TabIndex = 3;
        // 
        // _statusStrip
        // 
        _statusStrip.ImageScalingSize = new Size(24, 24);
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _imuRateLabel, _gpsRateLabel, _lastExceptionButton });
        _statusStrip.Location = new Point(0, 1431);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Padding = new Padding(1, 0, 31, 0);
        _statusStrip.Size = new Size(2000, 36);
        _statusStrip.TabIndex = 2;
        // 
        // _statusLabel
        // 
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(1555, 29);
        _statusLabel.Spring = true;
        _statusLabel.Text = "Ready.";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _imuRateLabel
        // 
        _imuRateLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _imuRateLabel.Name = "_imuRateLabel";
        _imuRateLabel.Size = new Size(133, 29);
        _imuRateLabel.Text = "Sensors 0.0 Hz";
        // 
        // _gpsRateLabel
        // 
        _gpsRateLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _gpsRateLabel.Name = "_gpsRateLabel";
        _gpsRateLabel.Size = new Size(103, 29);
        _gpsRateLabel.Text = "GPS 0.0 Hz";
        // 
        // _lastExceptionButton
        // 
        _lastExceptionButton.Enabled = false;
        _lastExceptionButton.Name = "_lastExceptionButton";
        _lastExceptionButton.ShowDropDownArrow = false;
        _lastExceptionButton.Size = new Size(177, 33);
        _lastExceptionButton.Text = "Last exception: none";
        // 
        // _statusTimer
        // 
        _statusTimer.Enabled = true;
        _statusTimer.Interval = 1000;
        _statusTimer.Tick += OnStatusTimerTick;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(10F, 25F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(2000, 1467);
        Controls.Add(_dashboardLayout);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);
        Margin = new Padding(4, 5, 4, 5);
        MinimumSize = new Size(1419, 1129);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "WARP Sensors Dashboard";
        WindowState = FormWindowState.Maximized;
        _toolStrip.ResumeLayout(false);
        _toolStrip.PerformLayout();
        _dashboardLayout.ResumeLayout(false);
        _gaugeLayout.ResumeLayout(false);
        _mapLayout.ResumeLayout(false);
        _mapLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_mapZoomTrackBar).EndInit();
        _bottomPanel.ResumeLayout(false);
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
