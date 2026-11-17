#nullable disable

using WarpToolkit.WinForms.Containers;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.ToolStrips;
using Sensored.TabViews;

namespace Sensored;

partial class MainForm
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
        components = new System.ComponentModel.Container();
        _toolStrip = new SymbolToolStrip();
        _startButton = new SymbolToolStripButton();
        _restartButton = new SymbolToolStripButton();
        _pauseButton = new SymbolToolStripButton();
        _stopButton = new SymbolToolStripButton();
        _sessionSeparator = new ToolStripSeparator();
        _sourcesButton = new ToolStripDropDownButton();
        _gpsSourceButton = new SymbolToolStripMenuItem();
        _obdButton = new SymbolToolStripMenuItem();
        _sensorsMenuItem = new SymbolToolStripMenuItem();
        _internalSensorsMenuItem = new SymbolToolStripMenuItem();
        _bluetoothSensorsMenuItem = new SymbolToolStripMenuItem();
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
        _damping300MenuItem = new SymbolToolStripMenuItem();
        _damping400MenuItem = new SymbolToolStripMenuItem();
        _damping500MenuItem = new SymbolToolStripMenuItem();
        _gScaleButton = new ToolStripDropDownButton();
        _gScale075MenuItem = new SymbolToolStripMenuItem();
        _gScale100MenuItem = new SymbolToolStripMenuItem();
        _gScale150MenuItem = new SymbolToolStripMenuItem();
        _gScale200MenuItem = new SymbolToolStripMenuItem();
        _gScale300MenuItem = new SymbolToolStripMenuItem();
        _rawLogButton = new SymbolToolStripButton();
        _replayButton = new ToolStripDropDownButton();
        _replayOpenMenuItem = new SymbolToolStripMenuItem();
        _replaySpeedSeparator = new ToolStripSeparator();
        _replaySpeed25MenuItem = new SymbolToolStripMenuItem();
        _replaySpeed50MenuItem = new SymbolToolStripMenuItem();
        _replaySpeed100MenuItem = new SymbolToolStripMenuItem();
        _replaySpeed150MenuItem = new SymbolToolStripMenuItem();
        _replaySpeed200MenuItem = new SymbolToolStripMenuItem();
        _replaySpeed300MenuItem = new SymbolToolStripMenuItem();
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
        _compassGauge = new WarpToolkit.WinForms.Sensors.DirectX.D2DCompassGauge();
        _mapLayout = new TableLayoutPanel();
        _routeMap = new SensorRouteMapControl();
        _obdView = new ObdTelemetryView();
        _mapZoomTrackBar = new TrackBar();
        _bottomPanel = new Panel();
        _bottomTabControl = new FluentTabControl();
        _telemetryTabHost = new TelemetryTabView();
        _routeSectionTabHost = new RouteSectionTabView();
        _debugTabHost = new DebugTabView();
        _statusStrip = new SymbolStatusStrip();
        _rawObdTabHost = new TabViews.ObdRawTabView();
        _statusLabel = new ToolStripStatusLabel();
        _replayPlayStatusButton = new SymbolStatusStripCommand();
        _replayStopStatusButton = new SymbolStatusStripCommand();
        _replayBackwardSeparator = new ToolStripSeparator();
        _replayFastBackwardButton = new SymbolStatusStripCommand();
        _replaySlowBackwardButton = new SymbolStatusStripCommand();
        _replayPauseSeparator = new ToolStripSeparator();
        _replayPauseStatusButton = new SymbolStatusStripCommand();
        _replayForwardSeparator = new ToolStripSeparator();
        _replaySlowForwardButton = new SymbolStatusStripCommand();
        _replayFastForwardButton = new SymbolStatusStripCommand();
        _replayTimeSeparator = new ToolStripSeparator();
        _replayTimeButton = new ToolStripDropDownButton();
        _replay20MenuItem = new ToolStripMenuItem();
        _replay40MenuItem = new ToolStripMenuItem();
        _replay60MenuItem = new ToolStripMenuItem();
        _replay80MenuItem = new ToolStripMenuItem();
        _replayGpsSeparator = new ToolStripSeparator();
        _replayFirstGpsMenuItem = new ToolStripMenuItem();
        _replayLastGpsMenuItem = new ToolStripMenuItem();
        _replayResetSeparator = new ToolStripSeparator();
        _replayResetMenuItem = new ToolStripMenuItem();
        _replayFileLabel = new ToolStripStatusLabel();
        _imuRateLabel = new ToolStripStatusLabel();
        _gpsRateLabel = new ToolStripStatusLabel();
        _obdStatusLabel = new ToolStripStatusLabel();
        _rawStatusLabel = new ToolStripStatusLabel();
        _statusSpring = new ToolStripStatusLabel();
        _helpStatusButton = new SymbolStatusStripCommand();
        _lastExceptionButton = new SymbolStatusStripCommand();
        _exceptionPopupTimer = new System.Windows.Forms.Timer(components);
        _statusTimer = new System.Windows.Forms.Timer(components);
        _readingsToolTip = new ToolTip(components);
        _kiosk = new KioskModeManager();
        components.Add(_kiosk);
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
        _toolStrip.Items.AddRange(new ToolStripItem[] { _startButton, _restartButton, _pauseButton, _stopButton, _sessionSeparator, _sourcesButton, _optionsButton, _mapButton, _displaySeparator, _dampingButton, _gScaleButton, _rawLogButton, _replayButton, _baseFontButton, _recordingButton, _fullScreenButton, _exitSeparator, _exitButton });
        _toolStrip.Location = new Point(0, 0);
        _toolStrip.Name = "_toolStrip";
        _toolStrip.Padding = new Padding(12, 6, 12, 6);
        _toolStrip.Size = new Size(1975, 68);
        _toolStrip.TabIndex = 0;
        // 
        // _startButton
        // 
        _startButton.Image = null;
        _startButton.Margin = new Padding(5, 10, 5, 10);
        _startButton.Name = "_startButton";
        _startButton.Size = new Size(66, 36);
        _startButton.Text = "Start";
        // 
        // _restartButton
        // 
        _restartButton.Image = null;
        _restartButton.Margin = new Padding(5, 10, 5, 10);
        _restartButton.Name = "_restartButton";
        _restartButton.Size = new Size(90, 36);
        _restartButton.Text = "Restart";
        // 
        // _pauseButton
        // 
        _pauseButton.CheckOnClick = true;
        _pauseButton.Image = null;
        _pauseButton.Margin = new Padding(5, 10, 5, 10);
        _pauseButton.Name = "_pauseButton";
        _pauseButton.Size = new Size(79, 36);
        _pauseButton.Text = "Pause";
        // 
        // _stopButton
        // 
        _stopButton.Image = null;
        _stopButton.Margin = new Padding(5, 10, 5, 10);
        _stopButton.Name = "_stopButton";
        _stopButton.Size = new Size(66, 36);
        _stopButton.Text = "Stop";
        // 
        // _sessionSeparator
        // 
        _sessionSeparator.Name = "_sessionSeparator";
        _sessionSeparator.Size = new Size(6, 56);
        // 
        // _sourcesButton
        //
        _sourcesButton.DropDownItems.AddRange(new ToolStripItem[] { _gpsSourceButton, _obdButton, _sensorsMenuItem });
        _sourcesButton.Margin = new Padding(5, 10, 5, 10);
        _sourcesButton.Name = "_sourcesButton";
        _sourcesButton.Size = new Size(102, 36);
        _sourcesButton.Text = "Sources";
        _sourcesButton.AccessibleName = "GPS, OBD2, and sensor sources";
        //
        // _gpsSourceButton
        // 
        _gpsSourceButton.DropDownItems.AddRange(new ToolStripItem[] { _windowsGpsMenuItem, _serialGpsMenuItem });
        _gpsSourceButton.Name = "_gpsSourceButton";
        _gpsSourceButton.Size = new Size(370, 44);
        _gpsSourceButton.Text = "GPS";
        //
        // _obdButton
        //
        _obdButton.AccessibleName = "Prefer OBD2 speed and telemetry";
        _obdButton.CheckOnClick = true;
        _obdButton.Name = "_obdButton";
        _obdButton.Size = new Size(370, 44);
        _obdButton.Text = "OBD2";
        _obdButton.Click += OnObdCommand;
        // 
        // _sensorsMenuItem
        //
        _sensorsMenuItem.DropDownItems.AddRange(new ToolStripItem[] { _internalSensorsMenuItem, _bluetoothSensorsMenuItem });
        _sensorsMenuItem.Name = "_sensorsMenuItem";
        _sensorsMenuItem.Size = new Size(370, 44);
        _sensorsMenuItem.Text = "Sensors";
        //
        // _internalSensorsMenuItem
        //
        _internalSensorsMenuItem.Name = "_internalSensorsMenuItem";
        _internalSensorsMenuItem.Size = new Size(370, 44);
        _internalSensorsMenuItem.Text = "Internal";
        _internalSensorsMenuItem.Click += OnInternalSensorsCommand;
        //
        // _bluetoothSensorsMenuItem
        //
        _bluetoothSensorsMenuItem.Enabled = false;
        _bluetoothSensorsMenuItem.Name = "_bluetoothSensorsMenuItem";
        _bluetoothSensorsMenuItem.Size = new Size(370, 44);
        _bluetoothSensorsMenuItem.Text = "Bluetooth";
        _bluetoothSensorsMenuItem.Click += OnBluetoothSensorsCommand;
        //
        // _windowsGpsMenuItem
        // 
        _windowsGpsMenuItem.Image = null;
        _windowsGpsMenuItem.Name = "_windowsGpsMenuItem";
        _windowsGpsMenuItem.Size = new Size(370, 44);
        _windowsGpsMenuItem.Text = "Windows Location";
        // 
        // _serialGpsMenuItem
        // 
        _serialGpsMenuItem.Image = null;
        _serialGpsMenuItem.Name = "_serialGpsMenuItem";
        _serialGpsMenuItem.Size = new Size(370, 44);
        _serialGpsMenuItem.Text = "Serial NMEA receiver";
        // 
        // _optionsButton
        // 
        _optionsButton.Image = null;
        _optionsButton.Margin = new Padding(5, 10, 5, 10);
        _optionsButton.Name = "_optionsButton";
        _optionsButton.Size = new Size(102, 36);
        _optionsButton.Text = "Options";
        // 
        // _mapButton
        // 
        _mapButton.DropDownItems.AddRange(new ToolStripItem[] { _mapEnabledMenuItem, _mapRowSpanMenuItem, _mapColumnSpanMenuItem });
        _mapButton.Margin = new Padding(5, 10, 5, 10);
        _mapButton.Name = "_mapButton";
        _mapButton.Size = new Size(84, 36);
        _mapButton.Text = "Map";
        // 
        // _mapEnabledMenuItem
        // 
        _mapEnabledMenuItem.CheckOnClick = true;
        _mapEnabledMenuItem.Image = null;
        _mapEnabledMenuItem.Name = "_mapEnabledMenuItem";
        _mapEnabledMenuItem.Size = new Size(354, 44);
        _mapEnabledMenuItem.Text = "Map enabled";
        // 
        // _mapRowSpanMenuItem
        // 
        _mapRowSpanMenuItem.CheckOnClick = true;
        _mapRowSpanMenuItem.Image = null;
        _mapRowSpanMenuItem.Name = "_mapRowSpanMenuItem";
        _mapRowSpanMenuItem.Size = new Size(354, 44);
        _mapRowSpanMenuItem.Text = "Span both rows";
        // 
        // _mapColumnSpanMenuItem
        // 
        _mapColumnSpanMenuItem.CheckOnClick = true;
        _mapColumnSpanMenuItem.Image = null;
        _mapColumnSpanMenuItem.Name = "_mapColumnSpanMenuItem";
        _mapColumnSpanMenuItem.Size = new Size(354, 44);
        _mapColumnSpanMenuItem.Text = "Span both columns";
        // 
        // _displaySeparator
        // 
        _displaySeparator.Name = "_displaySeparator";
        _displaySeparator.Size = new Size(6, 56);
        // 
        // _dampingButton
        // 
        _dampingButton.DropDownItems.AddRange(new ToolStripItem[] { _dampingOffMenuItem, _damping50MenuItem, _damping100MenuItem, _damping200MenuItem, _damping300MenuItem, _damping400MenuItem, _damping500MenuItem });
        _dampingButton.Margin = new Padding(5, 10, 5, 10);
        _dampingButton.Name = "_dampingButton";
        _dampingButton.Size = new Size(134, 36);
        _dampingButton.Text = "Damping";
        // 
        // _dampingOffMenuItem
        // 
        _dampingOffMenuItem.Image = null;
        _dampingOffMenuItem.Name = "_dampingOffMenuItem";
        _dampingOffMenuItem.Size = new Size(224, 44);
        _dampingOffMenuItem.Text = "Off";
        // 
        // _damping50MenuItem
        // 
        _damping50MenuItem.Image = null;
        _damping50MenuItem.Name = "_damping50MenuItem";
        _damping50MenuItem.Size = new Size(224, 44);
        _damping50MenuItem.Text = "50 ms";
        // 
        // _damping100MenuItem
        // 
        _damping100MenuItem.Image = null;
        _damping100MenuItem.Name = "_damping100MenuItem";
        _damping100MenuItem.Size = new Size(224, 44);
        _damping100MenuItem.Text = "100 ms";
        // 
        // _damping200MenuItem
        // 
        _damping200MenuItem.Image = null;
        _damping200MenuItem.Name = "_damping200MenuItem";
        _damping200MenuItem.Size = new Size(224, 44);
        _damping200MenuItem.Text = "200 ms";
        // 
        // _damping300MenuItem
        // 
        _damping300MenuItem.Image = null;
        _damping300MenuItem.Name = "_damping300MenuItem";
        _damping300MenuItem.Size = new Size(224, 44);
        _damping300MenuItem.Text = "300 ms";
        // 
        // _damping400MenuItem
        // 
        _damping400MenuItem.Image = null;
        _damping400MenuItem.Name = "_damping400MenuItem";
        _damping400MenuItem.Size = new Size(224, 44);
        _damping400MenuItem.Text = "400 ms";
        // 
        // _damping500MenuItem
        // 
        _damping500MenuItem.Image = null;
        _damping500MenuItem.Name = "_damping500MenuItem";
        _damping500MenuItem.Size = new Size(224, 44);
        _damping500MenuItem.Text = "500 ms";
        // 
        // _gScaleButton
        // 
        _gScaleButton.DropDownItems.AddRange(new ToolStripItem[] { _gScale075MenuItem, _gScale100MenuItem, _gScale150MenuItem, _gScale200MenuItem, _gScale300MenuItem });
        _gScaleButton.Margin = new Padding(5, 10, 5, 10);
        _gScaleButton.Name = "_gScaleButton";
        _gScaleButton.Size = new Size(111, 36);
        _gScaleButton.Text = "G scale";
        // 
        // _gScale075MenuItem
        // 
        _gScale075MenuItem.Image = null;
        _gScale075MenuItem.Name = "_gScale075MenuItem";
        _gScale075MenuItem.Size = new Size(212, 44);
        _gScale075MenuItem.Text = "0.75 g";
        // 
        // _gScale100MenuItem
        // 
        _gScale100MenuItem.Image = null;
        _gScale100MenuItem.Name = "_gScale100MenuItem";
        _gScale100MenuItem.Size = new Size(212, 44);
        _gScale100MenuItem.Text = "1.00 g";
        // 
        // _gScale150MenuItem
        // 
        _gScale150MenuItem.Image = null;
        _gScale150MenuItem.Name = "_gScale150MenuItem";
        _gScale150MenuItem.Size = new Size(212, 44);
        _gScale150MenuItem.Text = "1.50 g";
        // 
        // _gScale200MenuItem
        // 
        _gScale200MenuItem.Image = null;
        _gScale200MenuItem.Name = "_gScale200MenuItem";
        _gScale200MenuItem.Size = new Size(212, 44);
        _gScale200MenuItem.Text = "2.00 g";
        // 
        // _gScale300MenuItem
        // 
        _gScale300MenuItem.Image = null;
        _gScale300MenuItem.Name = "_gScale300MenuItem";
        _gScale300MenuItem.Size = new Size(212, 44);
        _gScale300MenuItem.Text = "3.00 g";
        // 
        // _rawLogButton
        // 
        _rawLogButton.Image = null;
        _rawLogButton.Margin = new Padding(5, 10, 5, 10);
        _rawLogButton.Name = "_rawLogButton";
        _rawLogButton.Size = new Size(102, 36);
        _rawLogButton.Text = "Raw log";
        // 
        // _replayButton
        // 
        _replayButton.DropDownItems.AddRange(new ToolStripItem[] { _replayOpenMenuItem, _replaySpeedSeparator, _replaySpeed25MenuItem, _replaySpeed50MenuItem, _replaySpeed100MenuItem, _replaySpeed150MenuItem, _replaySpeed200MenuItem, _replaySpeed300MenuItem });
        _replayButton.Margin = new Padding(5, 10, 5, 10);
        _replayButton.Name = "_replayButton";
        _replayButton.Size = new Size(106, 36);
        _replayButton.Text = "Replay";
        // 
        // _replayOpenMenuItem
        // 
        _replayOpenMenuItem.Image = null;
        _replayOpenMenuItem.Name = "_replayOpenMenuItem";
        _replayOpenMenuItem.Size = new Size(330, 44);
        _replayOpenMenuItem.Text = "Open recording...";
        _replayOpenMenuItem.Click += OnReplayCommand;
        // 
        // _replaySpeedSeparator
        // 
        _replaySpeedSeparator.Name = "_replaySpeedSeparator";
        _replaySpeedSeparator.Size = new Size(327, 6);
        // 
        // _replaySpeed25MenuItem
        // 
        _replaySpeed25MenuItem.Name = "_replaySpeed25MenuItem";
        _replaySpeed25MenuItem.Size = new Size(330, 44);
        _replaySpeed25MenuItem.Text = "25%";
        // 
        // _replaySpeed50MenuItem
        // 
        _replaySpeed50MenuItem.Name = "_replaySpeed50MenuItem";
        _replaySpeed50MenuItem.Size = new Size(330, 44);
        _replaySpeed50MenuItem.Text = "50%";
        // 
        // _replaySpeed100MenuItem
        // 
        _replaySpeed100MenuItem.Checked = true;
        _replaySpeed100MenuItem.CheckState = CheckState.Checked;
        _replaySpeed100MenuItem.Name = "_replaySpeed100MenuItem";
        _replaySpeed100MenuItem.Size = new Size(330, 44);
        _replaySpeed100MenuItem.Text = "100%";
        // 
        // _replaySpeed150MenuItem
        // 
        _replaySpeed150MenuItem.Name = "_replaySpeed150MenuItem";
        _replaySpeed150MenuItem.Size = new Size(330, 44);
        _replaySpeed150MenuItem.Text = "150%";
        // 
        // _replaySpeed200MenuItem
        // 
        _replaySpeed200MenuItem.Name = "_replaySpeed200MenuItem";
        _replaySpeed200MenuItem.Size = new Size(330, 44);
        _replaySpeed200MenuItem.Text = "200%";
        // 
        // _replaySpeed300MenuItem
        // 
        _replaySpeed300MenuItem.Name = "_replaySpeed300MenuItem";
        _replaySpeed300MenuItem.Size = new Size(330, 44);
        _replaySpeed300MenuItem.Text = "300%";
        // 
        // _baseFontButton
        // 
        _baseFontButton.DropDownItems.AddRange(new ToolStripItem[] { _baseFont10MenuItem, _baseFont12MenuItem, _baseFont14MenuItem, _baseFont18MenuItem, _baseFont24MenuItem });
        _baseFontButton.Margin = new Padding(5, 10, 5, 10);
        _baseFontButton.Name = "_baseFontButton";
        _baseFontButton.Size = new Size(136, 36);
        _baseFontButton.Text = "Base font";
        // 
        // _baseFont10MenuItem
        // 
        _baseFont10MenuItem.Image = null;
        _baseFont10MenuItem.Name = "_baseFont10MenuItem";
        _baseFont10MenuItem.Size = new Size(202, 44);
        _baseFont10MenuItem.Text = "10 pt";
        // 
        // _baseFont12MenuItem
        // 
        _baseFont12MenuItem.Image = null;
        _baseFont12MenuItem.Name = "_baseFont12MenuItem";
        _baseFont12MenuItem.Size = new Size(202, 44);
        _baseFont12MenuItem.Text = "12 pt";
        // 
        // _baseFont14MenuItem
        // 
        _baseFont14MenuItem.Image = null;
        _baseFont14MenuItem.Name = "_baseFont14MenuItem";
        _baseFont14MenuItem.Size = new Size(202, 44);
        _baseFont14MenuItem.Text = "14 pt";
        // 
        // _baseFont18MenuItem
        // 
        _baseFont18MenuItem.Image = null;
        _baseFont18MenuItem.Name = "_baseFont18MenuItem";
        _baseFont18MenuItem.Size = new Size(202, 44);
        _baseFont18MenuItem.Text = "18 pt";
        // 
        // _baseFont24MenuItem
        // 
        _baseFont24MenuItem.Image = null;
        _baseFont24MenuItem.Name = "_baseFont24MenuItem";
        _baseFont24MenuItem.Size = new Size(202, 44);
        _baseFont24MenuItem.Text = "24 pt";
        // 
        // _recordingButton
        // 
        _recordingButton.Image = null;
        _recordingButton.Margin = new Padding(5, 10, 5, 10);
        _recordingButton.Name = "_recordingButton";
        _recordingButton.Size = new Size(125, 36);
        _recordingButton.Text = "Recording";
        // 
        // _fullScreenButton
        // 
        _fullScreenButton.CheckOnClick = true;
        _fullScreenButton.Image = null;
        _fullScreenButton.Margin = new Padding(5, 10, 5, 10);
        _fullScreenButton.Name = "_fullScreenButton";
        _fullScreenButton.Size = new Size(132, 36);
        _fullScreenButton.Text = "Full screen";
        // 
        // _exitSeparator
        // 
        _exitSeparator.Name = "_exitSeparator";
        _exitSeparator.Size = new Size(6, 56);
        // 
        // _exitButton
        // 
        _exitButton.Image = null;
        _exitButton.Margin = new Padding(5, 10, 5, 10);
        _exitButton.Name = "_exitButton";
        _exitButton.Size = new Size(55, 36);
        _exitButton.Text = "Exit";
        // 
        // _dashboardLayout
        // 
        _dashboardLayout.ColumnCount = 2;
        _dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
        _dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
        _dashboardLayout.Controls.Add(_gaugeLayout, 0, 0);
        _dashboardLayout.Controls.Add(_mapLayout, 1, 0);
        _dashboardLayout.Controls.Add(_bottomPanel, 1, 1);
        _dashboardLayout.Dock = DockStyle.Fill;
        _dashboardLayout.Location = new Point(0, 68);
        _dashboardLayout.Margin = new Padding(4, 4, 4, 4);
        _dashboardLayout.Name = "_dashboardLayout";
        _dashboardLayout.Padding = new Padding(14, 17, 14, 17);
        _dashboardLayout.RowCount = 2;
        _dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        _dashboardLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _dashboardLayout.Size = new Size(1975, 1014);
        _dashboardLayout.TabIndex = 1;
        // 
        // _gaugeLayout
        // 
        _gaugeLayout.ColumnCount = 2;
        _gaugeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gaugeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _gaugeLayout.Controls.Add(_speedometer, 0, 0);
        _gaugeLayout.Controls.Add(_artificialHorizon, 1, 0);
        _gaugeLayout.Controls.Add(_accelerationGauge, 1, 1);
        _gaugeLayout.Controls.Add(_compassGauge, 1, 1);
        _gaugeLayout.Dock = DockStyle.Fill;
        _gaugeLayout.Location = new Point(19, 23);
        _gaugeLayout.Margin = new Padding(5, 6, 5, 6);
        _gaugeLayout.Name = "_gaugeLayout";
        _gaugeLayout.RowCount = 2;
        _dashboardLayout.SetRowSpan(_gaugeLayout, 2);
        _gaugeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        _gaugeLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        _gaugeLayout.Size = new Size(963, 968);
        _gaugeLayout.TabIndex = 0;
        // 
        // _speedometer
        // 
        _speedometer.AccessibleName = "Speedometer";
        _speedometer.AccelerationMode = WarpToolkit.WinForms.Sensors.DirectX.SpeedometerAccelerationMode.Auto;
        _speedometer.AccessibleRole = AccessibleRole.Graphic;
        _speedometer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _speedometer.Location = new Point(5, 6);
        _speedometer.Margin = new Padding(5, 6, 5, 6);
        _speedometer.Name = "_speedometer";
        _speedometer.Padding = new Padding(14, 17, 14, 17);
        _speedometer.Size = new Size(471, 472);
        _speedometer.TabIndex = 0;
        _speedometer.TabStop = false;
        // 
        // _artificialHorizon
        // 
        _artificialHorizon.AccessibleName = "Artificial horizon";
        _artificialHorizon.AccessibleRole = AccessibleRole.Graphic;
        _artificialHorizon.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _artificialHorizon.Location = new Point(486, 6);
        _artificialHorizon.Margin = new Padding(5, 6, 5, 6);
        _artificialHorizon.Name = "_artificialHorizon";
        _artificialHorizon.Padding = new Padding(14, 17, 14, 17);
        _artificialHorizon.Size = new Size(472, 472);
        _artificialHorizon.TabIndex = 1;
        _artificialHorizon.TabStop = false;
        // 
        // _accelerationGauge
        // 
        _accelerationGauge.AccessibleDescription = "Raw device X is caller-selected forward; raw device Y is caller-selected lateral.";
        _accelerationGauge.AccessibleName = "Acceleration gauge";
        _accelerationGauge.AccessibleRole = AccessibleRole.Graphic;
        _accelerationGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _accelerationGauge.Location = new Point(5, 490);
        _accelerationGauge.Margin = new Padding(5, 6, 5, 6);
        _accelerationGauge.Name = "_accelerationGauge";
        _accelerationGauge.Padding = new Padding(14, 17, 14, 17);
        _accelerationGauge.Size = new Size(471, 472);
        _accelerationGauge.TabIndex = 2;
        _accelerationGauge.TabStop = false;
        // 
        // _compassGauge
        // 
        _compassGauge.AccessibleName = "Compass";
        _compassGauge.AccessibleRole = AccessibleRole.Graphic;
        _compassGauge.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _compassGauge.Location = new Point(486, 490);
        _compassGauge.Margin = new Padding(5, 6, 5, 6);
        _compassGauge.Name = "_compassGauge";
        _compassGauge.Padding = new Padding(14, 17, 14, 17);
        _compassGauge.Size = new Size(472, 472);
        _compassGauge.TabIndex = 3;
        _compassGauge.TabStop = false;
        // 
        // _mapLayout
        // 
        _mapLayout.ColumnCount = 3;
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 0F));
        _mapLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _mapLayout.ColumnStyles.Add(new ColumnStyle());
        _mapLayout.Controls.Add(_obdView, 0, 0);
        _mapLayout.Controls.Add(_routeMap, 1, 0);
        _mapLayout.Controls.Add(_mapZoomTrackBar, 2, 0);
        _obdView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _obdView.Name = "_obdView";
        _obdView.Margin = new Padding(5, 6, 5, 6);
        _obdView.Visible = false;
        _obdView.RpmScaleChanged += OnObdRpmScaleChanged;
        _mapLayout.Dock = DockStyle.Fill;
        _mapLayout.Location = new Point(992, 23);
        _mapLayout.Margin = new Padding(5, 6, 5, 6);
        _mapLayout.Name = "_mapLayout";
        _mapLayout.RowCount = 1;
        _mapLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _mapLayout.Size = new Size(964, 380);
        _mapLayout.TabIndex = 0;
        // 
        // _routeMap
        // 
        _routeMap.AccessibleName = "Sensor route map";
        _routeMap.Dock = DockStyle.Fill;
        _routeMap.Location = new Point(5, 6);
        _routeMap.Margin = new Padding(5, 6, 5, 6);
        _routeMap.Name = "_routeMap";
        _routeMap.Size = new Size(854, 368);
        _routeMap.TabIndex = 0;
        _routeMap.VisibleChanged += OnNotificationMapVisibilityChanged;
        _routeMap.LocationChanged += OnNotificationMapBoundsChanged;
        _mapLayout.Layout += OnNotificationMapLayout;
        _mapLayout.LocationChanged += OnNotificationMapBoundsChanged;
        // 
        // _mapZoomTrackBar
        // 
        _mapZoomTrackBar.AccessibleName = "Map zoom";
        _mapZoomTrackBar.Dock = DockStyle.Fill;
        _mapZoomTrackBar.LargeChange = 1;
        _mapZoomTrackBar.Location = new Point(869, 6);
        _mapZoomTrackBar.Margin = new Padding(5, 6, 5, 6);
        _mapZoomTrackBar.Maximum = 4;
        _mapZoomTrackBar.Minimum = -2;
        _mapZoomTrackBar.Name = "_mapZoomTrackBar";
        _mapZoomTrackBar.Orientation = Orientation.Vertical;
        _mapZoomTrackBar.Size = new Size(90, 368);
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
        _bottomPanel.Location = new Point(992, 415);
        _bottomPanel.Margin = new Padding(5, 6, 5, 6);
        _bottomPanel.Name = "_bottomPanel";
        _bottomPanel.Size = new Size(964, 576);
        _bottomPanel.TabIndex = 1;
        // 
        // _bottomTabControl
        // 
        _bottomTabControl.Dock = DockStyle.Fill;
        _bottomTabControl.Location = new Point(0, 0);
        _bottomTabControl.Margin = new Padding(5, 6, 5, 6);
        _bottomTabControl.Name = "_bottomTabControl";
        _bottomTabControl.Size = new Size(964, 576);
        _bottomTabControl.TabIndex = 0;
        // 
        // _telemetryTabHost
        // 
        _telemetryTabHost.AccessibleName = "Telemetry";
        _telemetryTabHost.AutoScroll = true;
        _telemetryTabHost.Dock = DockStyle.Fill;
        _telemetryTabHost.Location = new Point(0, 0);
        _telemetryTabHost.Margin = new Padding(5, 6, 5, 6);
        _telemetryTabHost.Name = "_telemetryTabHost";
        _telemetryTabHost.Size = new Size(964, 576);
        _telemetryTabHost.TabIndex = 1;
        _telemetryTabHost.VisualStylesMode = VisualStylesMode.Net11;
        // 
        // _routeSectionTabHost
        // 
        _routeSectionTabHost.AccessibleName = "Route sections";
        _routeSectionTabHost.Dock = DockStyle.Fill;
        _routeSectionTabHost.Location = new Point(0, 0);
        _routeSectionTabHost.Margin = new Padding(5, 6, 5, 6);
        _routeSectionTabHost.Name = "_routeSectionTabHost";
        _routeSectionTabHost.Size = new Size(964, 576);
        _routeSectionTabHost.TabIndex = 2;
        // 
        // _debugTabHost
        // 
        _debugTabHost.AccessibleName = "Debug";
        _debugTabHost.Dock = DockStyle.Fill;
        _debugTabHost.Location = new Point(0, 0);
        _debugTabHost.Margin = new Padding(5, 6, 5, 6);
        _debugTabHost.Name = "_debugTabHost";
        _debugTabHost.Padding = new Padding(13, 13, 13, 13);
        _debugTabHost.Size = new Size(964, 576);
        _debugTabHost.TabIndex = 3;
        // 
        // _statusStrip
        // 
        _statusStrip.ImageScalingSize = new Size(32, 32);
        _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _gpsRateLabel, _obdStatusLabel, _rawStatusLabel, _replayPlayStatusButton, _replayStopStatusButton, _replayBackwardSeparator, _replayFastBackwardButton, _replaySlowBackwardButton, _replayPauseSeparator, _replayPauseStatusButton, _replayForwardSeparator, _replaySlowForwardButton, _replayFastForwardButton, _replayTimeSeparator, _replayTimeButton, _replayFileLabel, _imuRateLabel, _statusSpring, _lastExceptionButton, _helpStatusButton });
        _statusStrip.Location = new Point(0, 1082);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Padding = new Padding(1, 0, 40, 0);
        _statusStrip.ShowItemToolTips = true;
        _statusStrip.Size = new Size(1975, 46);
        _statusStrip.TabIndex = 2;
        _statusStrip.FontChanged += OnHelpStatusFontChanged;
        // 
        // _statusLabel
        // 
        _statusLabel.Name = "_statusLabel";
        _statusLabel.Size = new Size(595, 36);
        _statusLabel.Spring = false;
        _statusLabel.Text = "Stopped";
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // _replayPlayStatusButton
        // 
        _replayPlayStatusButton.AccessibleRole = AccessibleRole.PushButton;
        _replayPlayStatusButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replayPlayStatusButton.Image = null;
        _replayPlayStatusButton.Name = "_replayPlayStatusButton";
        _replayPlayStatusButton.Padding = new Padding(3);
        _replayPlayStatusButton.Size = new Size(6, 36);
        _replayPlayStatusButton.Text = "Play";
        _replayPlayStatusButton.Click += OnReplayPlayStatusClick;
        // 
        // _replayStopStatusButton
        // 
        _replayStopStatusButton.AccessibleRole = AccessibleRole.PushButton;
        _replayStopStatusButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replayStopStatusButton.Image = null;
        _replayStopStatusButton.Name = "_replayStopStatusButton";
        _replayStopStatusButton.Padding = new Padding(4);
        _replayStopStatusButton.Size = new Size(8, 36);
        _replayStopStatusButton.Text = "Stop";
        _replayStopStatusButton.Click += OnReplayStopStatusClick;
        // 
        // _replayBackwardSeparator
        // 
        _replayBackwardSeparator.Name = "_replayBackwardSeparator";
        _replayBackwardSeparator.Size = new Size(6, 46);
        // 
        // _replayFastBackwardButton
        // 
        _replayFastBackwardButton.AccessibleRole = AccessibleRole.PushButton;
        _replayFastBackwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replayFastBackwardButton.Image = null;
        _replayFastBackwardButton.Name = "_replayFastBackwardButton";
        _replayFastBackwardButton.Size = new Size(0, 36);
        _replayFastBackwardButton.Text = "Back thirty seconds";
        _replayFastBackwardButton.ToolTipText = "Move backward 30 seconds";
        _replayFastBackwardButton.Click += OnReplayFastBackwardClick;
        // 
        // _replaySlowBackwardButton
        // 
        _replaySlowBackwardButton.AccessibleRole = AccessibleRole.PushButton;
        _replaySlowBackwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replaySlowBackwardButton.Image = null;
        _replaySlowBackwardButton.Name = "_replaySlowBackwardButton";
        _replaySlowBackwardButton.Size = new Size(0, 36);
        _replaySlowBackwardButton.Text = "Back five seconds";
        _replaySlowBackwardButton.ToolTipText = "Move backward 5 seconds";
        _replaySlowBackwardButton.Click += OnReplaySlowBackwardClick;
        // 
        // _replayPauseSeparator
        // 
        _replayPauseSeparator.Name = "_replayPauseSeparator";
        _replayPauseSeparator.Size = new Size(6, 46);
        // 
        // _replayPauseStatusButton
        // 
        _replayPauseStatusButton.AccessibleRole = AccessibleRole.PushButton;
        _replayPauseStatusButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replayPauseStatusButton.Image = null;
        _replayPauseStatusButton.Name = "_replayPauseStatusButton";
        _replayPauseStatusButton.Size = new Size(0, 36);
        _replayPauseStatusButton.Text = "Pause";
        _replayPauseStatusButton.Click += OnReplayPauseStatusClick;
        // 
        // _replayForwardSeparator
        // 
        _replayForwardSeparator.Name = "_replayForwardSeparator";
        _replayForwardSeparator.Size = new Size(6, 46);
        // 
        // _replaySlowForwardButton
        // 
        _replaySlowForwardButton.AccessibleRole = AccessibleRole.PushButton;
        _replaySlowForwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replaySlowForwardButton.Image = null;
        _replaySlowForwardButton.Name = "_replaySlowForwardButton";
        _replaySlowForwardButton.Size = new Size(0, 36);
        _replaySlowForwardButton.Text = "Forward five seconds";
        _replaySlowForwardButton.ToolTipText = "Move forward 5 seconds";
        _replaySlowForwardButton.Click += OnReplaySlowForwardClick;
        // 
        // _replayFastForwardButton
        // 
        _replayFastForwardButton.AccessibleRole = AccessibleRole.PushButton;
        _replayFastForwardButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        _replayFastForwardButton.Image = null;
        _replayFastForwardButton.Name = "_replayFastForwardButton";
        _replayFastForwardButton.Size = new Size(0, 36);
        _replayFastForwardButton.Text = "Forward thirty seconds";
        _replayFastForwardButton.ToolTipText = "Move forward 30 seconds";
        _replayFastForwardButton.Click += OnReplayFastForwardClick;
        // 
        // _replayTimeSeparator
        // 
        _replayTimeSeparator.Name = "_replayTimeSeparator";
        _replayTimeSeparator.Size = new Size(6, 46);
        // 
        // _replayTimeButton
        // 
        _replayTimeButton.DropDownItems.AddRange(new ToolStripItem[] { _replay20MenuItem, _replay40MenuItem, _replay60MenuItem, _replay80MenuItem, _replayGpsSeparator, _replayFirstGpsMenuItem, _replayLastGpsMenuItem, _replayResetSeparator, _replayResetMenuItem });
        _replayTimeButton.Name = "_replayTimeButton";
        _replayTimeButton.Size = new Size(137, 42);
        _replayTimeButton.Text = "000:00:00";
        // 
        // _replay20MenuItem
        // 
        _replay20MenuItem.Name = "_replay20MenuItem";
        _replay20MenuItem.Size = new Size(450, 44);
        _replay20MenuItem.Text = "000:00:00 - 20%";
        _replay20MenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replay40MenuItem
        // 
        _replay40MenuItem.Name = "_replay40MenuItem";
        _replay40MenuItem.Size = new Size(450, 44);
        _replay40MenuItem.Text = "000:00:00 - 40%";
        _replay40MenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replay60MenuItem
        // 
        _replay60MenuItem.Name = "_replay60MenuItem";
        _replay60MenuItem.Size = new Size(450, 44);
        _replay60MenuItem.Text = "000:00:00 - 60%";
        _replay60MenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replay80MenuItem
        // 
        _replay80MenuItem.Name = "_replay80MenuItem";
        _replay80MenuItem.Size = new Size(450, 44);
        _replay80MenuItem.Text = "000:00:00 - 80%";
        _replay80MenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replayGpsSeparator
        // 
        _replayGpsSeparator.Name = "_replayGpsSeparator";
        _replayGpsSeparator.Size = new Size(447, 6);
        // 
        // _replayFirstGpsMenuItem
        // 
        _replayFirstGpsMenuItem.Name = "_replayFirstGpsMenuItem";
        _replayFirstGpsMenuItem.Size = new Size(450, 44);
        _replayFirstGpsMenuItem.Text = "000:00:00 - First GPS change";
        _replayFirstGpsMenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replayLastGpsMenuItem
        // 
        _replayLastGpsMenuItem.Name = "_replayLastGpsMenuItem";
        _replayLastGpsMenuItem.Size = new Size(450, 44);
        _replayLastGpsMenuItem.Text = "000:00:00 - Last GPS change";
        _replayLastGpsMenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replayResetSeparator
        // 
        _replayResetSeparator.Name = "_replayResetSeparator";
        _replayResetSeparator.Size = new Size(447, 6);
        // 
        // _replayResetMenuItem
        // 
        _replayResetMenuItem.Name = "_replayResetMenuItem";
        _replayResetMenuItem.Size = new Size(450, 44);
        _replayResetMenuItem.Text = "000:00:00 - Reset";
        _replayResetMenuItem.Click += OnReplayTimeMenuClick;
        // 
        // _replayFileLabel
        // 
        _replayFileLabel.DoubleClickEnabled = true;
        _replayFileLabel.Name = "_replayFileLabel";
        _replayFileLabel.Size = new Size(595, 36);
        _replayFileLabel.Spring = true;
        _replayFileLabel.Text = "Replay file";
        _replayFileLabel.TextAlign = ContentAlignment.MiddleLeft;
        _replayFileLabel.DoubleClick += OnReplayFileDoubleClick;
        // 
        // _imuRateLabel
        // 
        _imuRateLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _imuRateLabel.Name = "_imuRateLabel";
        _imuRateLabel.Size = new Size(173, 36);
        _imuRateLabel.Text = "Sensors 0.0 Hz";
        // 
        // _gpsRateLabel
        // 
        _gpsRateLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _gpsRateLabel.AutoSize = false;
        _gpsRateLabel.Name = "_gpsRateLabel";
        _gpsRateLabel.Size = new Size(133, 36);
        _gpsRateLabel.Text = "GPS 0.0 Hz";
        //
        // Compact live status fields
        //
        _obdStatusLabel.Name = "_obdStatusLabel";
        _obdStatusLabel.AutoSize = false;
        _obdStatusLabel.Text = "No OBD2";
        _obdStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _rawStatusLabel.Name = "_rawStatusLabel";
        _rawStatusLabel.Text = "RAW: P-- R-- Y-- KM/H ---";
        _rawStatusLabel.BorderSides = ToolStripStatusLabelBorderSides.Left;
        _statusSpring.Name = "_statusSpring";
        _statusSpring.Spring = true;
        // 
        // _helpStatusButton
        // 
        _helpStatusButton.AccessibleName = "Help: status strip glossary";
        _helpStatusButton.AccessibleRole = AccessibleRole.PushButton;
        _helpStatusButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _helpStatusButton.Image = null;
        _helpStatusButton.Name = "_helpStatusButton";
        _helpStatusButton.Size = new Size(25, 36);
        _helpStatusButton.Text = "?";
        _helpStatusButton.ToolTipText = "Explain dashboard status and GPS conditions";
        _helpStatusButton.Click += OnHelpStatusClick;
        _helpStatusButton.Disposed += OnHelpStatusDisposed;
        // 
        // _lastExceptionButton
        // 
        _lastExceptionButton.Enabled = false;
        _lastExceptionButton.AutoSize = false;
        _lastExceptionButton.Name = "_lastExceptionButton";
        _lastExceptionButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
        _lastExceptionButton.Size = new Size(237, 42);
        _lastExceptionButton.Text = "Last exception: none";
        // 
        // _statusTimer
        // 
        _statusTimer.Enabled = true;
        _statusTimer.Interval = 1000;
        _statusTimer.Tick += OnStatusTimerTick;
        _exceptionPopupTimer.Tick += CloseExceptionPopup;
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(13F, 32F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1975, 1128);
        Controls.Add(_dashboardLayout);
        Controls.Add(_statusStrip);
        Controls.Add(_toolStrip);
        Margin = new Padding(5, 6, 5, 6);
        MinimumSize = new Size(1834, 1199);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Sensored";
        _kiosk.AlwaysOn = true;
        _kiosk.ContainerControl = this;
        _kiosk.EscapeExitsFullScreen = false;
        _kiosk.ToggleFullScreenKeys = Keys.None;
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

    private SymbolToolStrip _toolStrip;
    private SymbolToolStripButton _startButton;
    private SymbolToolStripButton _restartButton;
    private SymbolToolStripButton _pauseButton;
    private SymbolToolStripButton _stopButton;
    private ToolStripSeparator _sessionSeparator;
    private ToolStripDropDownButton _sourcesButton;
    private SymbolToolStripMenuItem _gpsSourceButton;
    private SymbolToolStripMenuItem _obdButton;
    private SymbolToolStripMenuItem _sensorsMenuItem;
    private SymbolToolStripMenuItem _internalSensorsMenuItem;
    private SymbolToolStripMenuItem _bluetoothSensorsMenuItem;
    private KioskModeManager _kiosk;
    private ObdTelemetryView _obdView;
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
    private SymbolToolStripMenuItem _damping300MenuItem;
    private SymbolToolStripMenuItem _damping400MenuItem;
    private SymbolToolStripMenuItem _damping500MenuItem;
    private ToolStripDropDownButton _gScaleButton;
    private SymbolToolStripMenuItem _gScale075MenuItem;
    private SymbolToolStripMenuItem _gScale100MenuItem;
    private SymbolToolStripMenuItem _gScale150MenuItem;
    private SymbolToolStripMenuItem _gScale200MenuItem;
    private SymbolToolStripMenuItem _gScale300MenuItem;
    private SymbolToolStripButton _rawLogButton;
    private ToolStripDropDownButton _replayButton;
    private SymbolToolStripMenuItem _replayOpenMenuItem;
    private ToolStripSeparator _replaySpeedSeparator;
    private SymbolToolStripMenuItem _replaySpeed25MenuItem;
    private SymbolToolStripMenuItem _replaySpeed50MenuItem;
    private SymbolToolStripMenuItem _replaySpeed100MenuItem;
    private SymbolToolStripMenuItem _replaySpeed150MenuItem;
    private SymbolToolStripMenuItem _replaySpeed200MenuItem;
    private SymbolToolStripMenuItem _replaySpeed300MenuItem;
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
    private WarpToolkit.WinForms.Sensors.DirectX.D2DCompassGauge _compassGauge;
    private TableLayoutPanel _mapLayout;
    private SensorRouteMapControl _routeMap;
    private TrackBar _mapZoomTrackBar;
    private Panel _bottomPanel;
    private FluentTabControl _bottomTabControl;
    private TelemetryTabView _telemetryTabHost;
    private RouteSectionTabView _routeSectionTabHost;
    private DebugTabView _debugTabHost;
    private SymbolStatusStrip _statusStrip;
    private TabViews.ObdRawTabView _rawObdTabHost;
    private ToolStripStatusLabel _statusLabel;
    private ToolStripStatusLabel _imuRateLabel;
    private ToolStripStatusLabel _gpsRateLabel;
    private SymbolStatusStripCommand _helpStatusButton;
    private SymbolStatusStripCommand _lastExceptionButton;
    private ToolStripStatusLabel _obdStatusLabel;
    private ToolStripStatusLabel _rawStatusLabel;
    private ToolStripStatusLabel _statusSpring;
    private System.Windows.Forms.Timer _exceptionPopupTimer;
    private SymbolStatusStripCommand _replayPlayStatusButton;
    private SymbolStatusStripCommand _replayStopStatusButton;
    private ToolStripSeparator _replayBackwardSeparator;
    private SymbolStatusStripCommand _replayFastBackwardButton;
    private SymbolStatusStripCommand _replaySlowBackwardButton;
    private ToolStripSeparator _replayPauseSeparator;
    private SymbolStatusStripCommand _replayPauseStatusButton;
    private ToolStripSeparator _replayForwardSeparator;
    private SymbolStatusStripCommand _replaySlowForwardButton;
    private SymbolStatusStripCommand _replayFastForwardButton;
    private ToolStripSeparator _replayTimeSeparator;
    private ToolStripDropDownButton _replayTimeButton;
    private ToolStripMenuItem _replay20MenuItem;
    private ToolStripMenuItem _replay40MenuItem;
    private ToolStripMenuItem _replay60MenuItem;
    private ToolStripMenuItem _replay80MenuItem;
    private ToolStripSeparator _replayGpsSeparator;
    private ToolStripMenuItem _replayFirstGpsMenuItem;
    private ToolStripMenuItem _replayLastGpsMenuItem;
    private ToolStripSeparator _replayResetSeparator;
    private ToolStripMenuItem _replayResetMenuItem;
    private ToolStripStatusLabel _replayFileLabel;
    private System.Windows.Forms.Timer _statusTimer;
    private ToolTip _readingsToolTip;
}
