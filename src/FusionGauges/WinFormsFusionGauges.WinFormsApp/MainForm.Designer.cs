namespace WinFormsFusionGauges.WinFormsApp;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components is not null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _menu = new MenuStrip();
        _fileMenu = new ToolStripMenuItem();
        _exitMenu = new ToolStripMenuItem();
        _toolsMenu = new ToolStripMenuItem();
        _optionsMenu = new ToolStripMenuItem();
        _motionCalibrationMenu = new ToolStripMenuItem();
        _toolbar = new WarpToolkit.WinForms.ToolStrips.SymbolToolStrip();
        _gpsConfigurationButton = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _sensorsButton = new ToolStripButton();
        _recordButton = new ToolStripButton();
        _markerButton = new ToolStripButton();
        _toolbarGroupAfterCommands = new ToolStripSeparator();
        _instrumentsTabButton = new ToolStripButton();
        _diagnosticsTabButton = new ToolStripButton();
        _recordingTabButton = new ToolStripButton();
        _retryButton = new ToolStripButton();
        _toolbarGroupAfterRetry = new ToolStripSeparator();
        _calibrateButton = new ToolStripButton();
        _optionsButton = new ToolStripButton();
        _fullscreenButton = new ToolStripButton();
        _toolbarGroupAfterWindow = new ToolStripSeparator();
        _specialButton = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _resetDisplayItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _mercSensorsItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _toolbarGroupAfterSpecial = new ToolStripSeparator();
        _clipboardButton = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _copyDiagnosticsItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _copyLogItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _copyCanDiagnosticsItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _copyAllItem = new WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem();
        _exitButton = new ToolStripButton();
        _tabs = new WarpToolkit.WinForms.Containers.FluentTabControl();
        _instruments = new InstrumentsView();
        _diagnostics = new DiagnosticsView();
        _recordingView = new RecordingView();
        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _frameTimer = new System.Windows.Forms.Timer(components);
        _kiosk = new KioskModeManager();
        components.Add(_kiosk);
        _menu.SuspendLayout();
        _toolbar.SuspendLayout();
        _status.SuspendLayout();
        SuspendLayout();
        _menu.Items.AddRange(new ToolStripItem[] { _fileMenu, _toolsMenu });
        _menu.Dock = DockStyle.Top;
        _menu.Name = "_menu";
        _fileMenu.Text = "&File";
        _fileMenu.DropDownItems.AddRange(new ToolStripItem[] { _exitMenu });
        _exitMenu.Text = "E&xit";
        _exitMenu.Click += OnExit;
        _toolsMenu.Text = "&Tools";
        _toolsMenu.DropDownItems.AddRange(new ToolStripItem[] { _optionsMenu, _motionCalibrationMenu });
        _optionsMenu.Text = "&Options";
        _optionsMenu.Click += OnOptions;
        _motionCalibrationMenu.Text = "&Landscape motion calibration...";
        _motionCalibrationMenu.Click += OnMotionCalibration;
        _toolbar.Items.AddRange(new ToolStripItem[] {
            _sensorsButton, _recordButton, _markerButton, _toolbarGroupAfterCommands,
            _instrumentsTabButton, _diagnosticsTabButton, _recordingTabButton, _retryButton, _toolbarGroupAfterRetry,
            _calibrateButton, _optionsButton, _gpsConfigurationButton, _fullscreenButton, _toolbarGroupAfterWindow,
            _specialButton, _clipboardButton, _toolbarGroupAfterSpecial,
            _exitButton });
        _toolbar.Dock = DockStyle.Top;
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Name = "_toolbar";
        _toolbar.Padding = new Padding(6);
        _sensorsButton.Name = "_sensorsButton";
        _sensorsButton.Text = "Start sensors";
        _sensorsButton.Click += OnSensors;
        _recordButton.Name = "_recordButton";
        _recordButton.Text = "Start recording";
        _recordButton.Click += OnRecord;
        _markerButton.Name = "_markerButton";
        _markerButton.Text = "Marker";
        _markerButton.Click += OnMarker;
        _toolbarGroupAfterCommands.Name = "_toolbarGroupAfterCommands";
        _instrumentsTabButton.Name = "_instrumentsTabButton";
        _instrumentsTabButton.Text = "Instruments";
        _diagnosticsTabButton.Name = "_diagnosticsTabButton";
        _diagnosticsTabButton.Text = "Diagnostics";
        _recordingTabButton.Name = "_recordingTabButton";
        _recordingTabButton.Text = "Log";
        _retryButton.Name = "_retryButton";
        _retryButton.Text = "Retry ZIPs";
        _retryButton.Click += OnRetry;
        _toolbarGroupAfterRetry.Name = "_toolbarGroupAfterRetry";
        _calibrateButton.Name = "_calibrateButton";
        _calibrateButton.Text = "Fusion setup";
        _calibrateButton.Click += OnCalibrate;
        _optionsButton.Name = "_optionsButton";
        _optionsButton.Text = "Options";
        _optionsButton.Click += OnOptions;
        _gpsConfigurationButton.Name = "_gpsConfigurationButton";
        _gpsConfigurationButton.Text = "Speed source";
        _fullscreenButton.Name = "_fullscreenButton";
        _fullscreenButton.Text = "Full screen";
        _fullscreenButton.Click += OnFullscreen;
        _toolbarGroupAfterWindow.Name = "_toolbarGroupAfterWindow";
        _specialButton.Name = "_specialButton";
        _specialButton.Text = "Special";
        _specialButton.DropDownItems.AddRange(new ToolStripItem[] { _resetDisplayItem, _mercSensorsItem });
        _resetDisplayItem.Name = "_resetDisplayItem";
        _resetDisplayItem.Text = "Reset display";
        _resetDisplayItem.Click += OnResetDisplay;
        _mercSensorsItem.Name = "_mercSensorsItem";
        _mercSensorsItem.Text = "Merc sensors";
        _mercSensorsItem.CheckOnClick = true;
        _mercSensorsItem.Click += OnMercSensors;
        _clipboardButton.Name = "_clipboardButton";
        _clipboardButton.Text = "Clipboard";
        _clipboardButton.DropDownItems.AddRange(new ToolStripItem[] {
            _copyDiagnosticsItem, _copyLogItem, _copyCanDiagnosticsItem, _copyAllItem });
        _copyDiagnosticsItem.Name = "_copyDiagnosticsItem";
        _copyDiagnosticsItem.Text = "Copy Diagnostics";
        _copyDiagnosticsItem.Click += OnCopyDiagnostics;
        _copyLogItem.Name = "_copyLogItem";
        _copyLogItem.Text = "Copy Log";
        _copyLogItem.Click += OnCopyLog;
        _copyCanDiagnosticsItem.Name = "_copyCanDiagnosticsItem";
        _copyCanDiagnosticsItem.Text = "Copy CAN Diagnostics";
        _copyCanDiagnosticsItem.Click += OnCopyCanDiagnostics;
        _copyAllItem.Name = "_copyAllItem";
        _copyAllItem.Text = "Copy all";
        _copyAllItem.Click += OnCopyAllDiagnostics;
        _toolbarGroupAfterSpecial.Name = "_toolbarGroupAfterSpecial";
        _exitButton.Name = "_exitButton";
        _exitButton.Text = "Exit";
        _exitButton.Click += OnExit;
        _tabs.Dock = DockStyle.Fill;
        _tabs.Name = "_tabs";
        _tabs.ShowTabHeaders = false;
        _instruments.Name = "_instruments";
        _diagnostics.Name = "_diagnostics";
        _recordingView.Name = "_recordingView";
        _status.Items.AddRange(new ToolStripItem[] { _statusLabel });
        _status.Dock = DockStyle.Bottom;
        _status.Name = "_status";
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _statusLabel.Text = "Stopped - configure sources while parked, then Start sensors.";
        _frameTimer.Interval = 16;
        _frameTimer.Tick += OnFrameTick;
        _kiosk.ContainerControl = this;
        _kiosk.AlwaysOn = true;
        _kiosk.ToggleFullScreenKeys = Keys.F11;
        _kiosk.EscapeExitsFullScreen = true;
        _kiosk.TopMostInFullScreen = false;
        _kiosk.FullScreenChanged += OnFullscreenChanged;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1200, 850);
        MinimumSize = new Size(700, 580);
        Controls.Add(_tabs);
        Controls.Add(_toolbar);
        Controls.Add(_menu);
        Controls.Add(_status);
        MainMenuStrip = _menu;
        Name = "MainForm";
        Text = "FusionGauges - WARP driving diagnostics";
        _menu.ResumeLayout(false);
        _menu.PerformLayout();
        _toolbar.ResumeLayout(false);
        _toolbar.PerformLayout();
        _status.ResumeLayout(false);
        _status.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private MenuStrip _menu;
    private ToolStripMenuItem _fileMenu;
    private ToolStripMenuItem _exitMenu;
    private ToolStripMenuItem _toolsMenu;
    private ToolStripMenuItem _optionsMenu;
    private ToolStripMenuItem _motionCalibrationMenu;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStrip _toolbar;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _gpsConfigurationButton;
    private ToolStripButton _sensorsButton;
    private ToolStripButton _recordButton;
    private ToolStripButton _markerButton;
    private ToolStripSeparator _toolbarGroupAfterCommands;
    private ToolStripButton _instrumentsTabButton;
    private ToolStripButton _diagnosticsTabButton;
    private ToolStripButton _recordingTabButton;
    private ToolStripButton _retryButton;
    private ToolStripSeparator _toolbarGroupAfterRetry;
    private ToolStripButton _calibrateButton;
    private ToolStripButton _optionsButton;
    private ToolStripButton _fullscreenButton;
    private ToolStripSeparator _toolbarGroupAfterWindow;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _specialButton;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _resetDisplayItem;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _mercSensorsItem;
    private ToolStripSeparator _toolbarGroupAfterSpecial;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _clipboardButton;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _copyDiagnosticsItem;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _copyLogItem;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _copyCanDiagnosticsItem;
    private WarpToolkit.WinForms.ToolStrips.SymbolToolStripMenuItem _copyAllItem;
    private ToolStripButton _exitButton;
    private WarpToolkit.WinForms.Containers.FluentTabControl _tabs;
    private InstrumentsView _instruments;
    private DiagnosticsView _diagnostics;
    private RecordingView _recordingView;
    private StatusStrip _status;
    private ToolStripStatusLabel _statusLabel;
    private System.Windows.Forms.Timer _frameTimer;
    private KioskModeManager _kiosk;
}
