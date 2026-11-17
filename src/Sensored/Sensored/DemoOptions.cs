using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>
///  Identifies whether a live session captures a new tablet frame or keeps learning the mount.
/// </summary>
internal enum TabletFrameMode
{
    /// <summary>Capture gravity at session start and lock the down axis.</summary>
    SessionStartLock,

    /// <summary>Keep the existing motion-only mount calibrator.</summary>
    ContinuousMotion
}

/// <summary>
///  Contains the source, display, route, and recording choices owned by the demo UI.
/// </summary>
internal sealed record DemoOptions
{
    /// <summary>
    ///  Gets the default demo options.
    /// </summary>
    internal static DemoOptions Default { get; } = new();

    /// <summary>Gets selectable target intervals; zero keeps automatic rate negotiation.</summary>
    internal static IReadOnlyList<int> GpsTargetIntervals { get; } = Array.AsReadOnly<int>([0, 1000, 500, 200, 100]);

    /// <summary>
    ///  Gets the selected sensor source combination.
    /// </summary>
    internal WindowsSensorSourceKind SourceKind { get; init; } = WindowsSensorSourceKind.ImuAndGps;

    /// <summary>Gets the remembered preference for OBD2 speed and telemetry.</summary>
    internal bool UseObd { get; init; }

    /// <summary>Gets the reference/display sensor source; fusion continues to use the internal IMU.</summary>
    internal SensorDisplaySource SensorSource { get; init; } = SensorDisplaySource.Internal;

    /// <summary>Gets the bounded Bluetooth sensor discovery and reconnect window.</summary>
    internal int BluetoothSensorSearchMinutes { get; init; } = 3;

    /// <summary>Gets the independent internal sensor display reference frame.</summary>
    internal SensorReferenceFrame InternalSensorFrame { get; init; }

    /// <summary>Gets the independent Bluetooth sensor display reference frame.</summary>
    internal SensorReferenceFrame BluetoothSensorFrame { get; init; } = SensorReferenceFrame.BluetoothDefault;

    /// <summary>Gets whether the app opts in to the spherical attitude/direction display.</summary>
    internal bool UseSphericalHorizon { get; init; } = true;

    /// <summary>Gets whether kiosk mode prevents the display/screensaver from idling.</summary>
    internal bool AlwaysOn { get; init; } = true;

    /// <summary>Gets the adapter protocol used at the next OBD connection.</summary>
    internal ObdAdapterKind ObdAdapter { get; init; } = ObdAdapterKind.ObdLinkCx;

    /// <summary>Gets the explicitly chosen OBD device, or null for automatic paired discovery.</summary>
    internal BluetoothDeviceInfo? ObdDevice { get; init; }

    /// <summary>Gets the explicitly chosen sensor, or null for automatic paired WT901 discovery.</summary>
    internal BluetoothDeviceInfo? BluetoothSensorDevice { get; init; }

    /// <summary>Gets whether OBD instruments remain visible without an adapter.</summary>
    internal bool ForceObdGaugesOn { get; init; }

    /// <summary>Gets the initial OBD2 search window; an established adapter reconnects until disabled.</summary>
    internal int ObdSearchMinutes { get; init; } = 10;

    /// <summary>Gets how long the last-exception popup remains open.</summary>
    internal int ExceptionPopupSeconds { get; init; } = 15;

    /// <summary>Gets whether GPS receiver changes require an additional consent prompt.</summary>
    internal bool ConfirmGpsReceiverChanges { get; init; } = true;

    /// <summary>Gets whether starting receiver commands requires parked-vehicle confirmation.</summary>
    internal bool ConfirmSensorStart { get; init; } = true;

    /// <summary>Gets the return-to-root delay after engaging a toolbar leaf command.</summary>
    internal int ToolbarCommandTimeoutSeconds { get; init; } = 5;

    /// <summary>Gets the return-to-root delay while browsing a toolbar submenu.</summary>
    internal int ToolbarIdleTimeoutSeconds { get; init; } = 15;

    /// <summary>Gets the positive upper endpoint of the compact RPM LED scale.</summary>
    internal int MaximumRpm { get; init; } = 8000;

    /// <summary>
    ///  Gets the optional serial GPS port name.
    /// </summary>
    internal string? SerialPortName { get; init; }

    /// <summary>Gets the requested Windows Location reporting interval, not the observed fix cadence.</summary>
    internal uint WindowsGpsReportIntervalMilliseconds { get; init; } = 1000u;

    /// <summary>Gets the desired serial fix interval, or null for fastest acknowledged negotiation.</summary>
    internal int? TargetGpsFixIntervalMilliseconds { get; init; }

    /// <summary>Gets the explicitly selected serial receiver constellation profile.</summary>
    internal SerialGnssProfile SerialGnssProfile { get; init; } = SerialGnssProfile.Passive;

    /// <summary>Gets the non-persisted per-session receiver command authorization.</summary>
    internal bool AllowReceiverCommandsThisSession { get; init; }

    /// <summary>
    ///  Gets the number formats used by telemetry and diagnostic displays.
    /// </summary>
    internal ReadingFormats ReadingFormats { get; init; } = new(2, 7, 1, 2);

    /// <summary>Gets the library-owned LED ring appearance settings.</summary>
    internal WarpToolkit.WinForms.Sensors.DirectX.SpeedometerSettings Speedometer { get; init; }
        = WarpToolkit.WinForms.Sensors.DirectX.SpeedometerSettings.Default;

    /// <summary>
    ///  Gets the control-owned display damping interval in milliseconds.
    /// </summary>
    internal int DampingMilliseconds { get; init; } = 100;

    /// <summary>Gets the instrument-owned, cadence-aware forward acceleration damping level.</summary>
    internal SpeedometerAccelerationDamping AccelerationDamping { get; init; }

    /// <summary>
    ///  Gets the full-scale acceleration gauge range in g.
    /// </summary>
    internal double GScale { get; init; } = 1.5d;

    /// <summary>
    ///  Gets the dashboard base font size in points.
    /// </summary>
    internal float BaseFontPointSize { get; init; } = 12f;

    /// <summary>
    ///  Gets the main toolbar symbol size.
    /// </summary>
    internal DashboardToolbarSize ToolbarSize { get; init; } = DashboardToolbarSize.Medium;

    /// <summary>
    ///  Gets the optional route file displayed by the map.
    /// </summary>
    internal string? RouteFilePath { get; init; }

    /// <summary>
    ///  Gets a value indicating whether the map follows the latest GPS point.
    /// </summary>
    internal bool FollowCurrentPosition { get; init; } = true;

    /// <summary>
    ///  Gets a value indicating whether the map renders its debug overlay.
    /// </summary>
    internal bool ShowMapDebugInformation { get; init; }

    /// <summary>Gets whether the user opted in to online OpenStreetMap tile requests.</summary>
    internal bool AllowOnlineMapTiles { get; init; }

    /// <summary>
    ///  Gets the zoom offset applied on top of the map's automatic route fit.
    /// </summary>
    internal int MapZoomOffset { get; init; }

    /// <summary>
    ///  Gets the root folder for service-owned recordings.
    /// </summary>
    internal string RecordingRootFolder { get; init; } = MainForm.RecordingsRootFolder;

    /// <summary>
    ///  Gets the file formats written for each recording.
    /// </summary>
    internal SensorRecordingFormats RecordingFormats { get; init; } = SensorRecordingFormats.All;

    /// <summary>Gets the RawV2 snapshot rate captured at the next recording start.</summary>
    internal int RawSnapshotsPerSecond { get; init; } = 10;
    /// <summary>Gets the note/label attached to the next Ctrl+M recording marker.</summary>
    internal string RecordingMarkerLabel { get; init; } = "Marker";

    /// <summary>
    ///  Gets a value indicating whether recording starts with every sensor session.
    /// </summary>
    internal bool AutoRecording { get; init; }

    /// <summary>
    ///  Gets a value indicating whether numbered replay parts continue automatically.
    /// </summary>
    internal bool DetectReplaySequenceFiles { get; init; } = true;

    /// <summary>
    ///  Gets a value indicating whether replay transport controls use compact hover mode.
    /// </summary>
    internal bool HideReplayControls { get; init; }

    /// <summary>Gets the selected recording playback speed in percent.</summary>
    internal int ReplaySpeedPercent { get; init; } = 100;

    /// <summary>Gets how the tablet mounting frame is established.</summary>
    internal TabletFrameMode FrameMode { get; init; } = TabletFrameMode.SessionStartLock;

    /// <summary>
    ///  Gets a value indicating whether the last accepted tablet frame is reused because the
    ///  device stays mounted between sessions.
    /// </summary>
    internal bool AssumeStaticMountedDevice { get; init; }

    /// <summary>Gets the independently measured complete installation used only for live sessions.</summary>
    internal VehicleMountReference? VehicleMount { get; init; }

    /// <summary>Creates live fusion with the measured mount, never assigning it to historical input.</summary>
    internal IFusionEngine CreateFusionEngine(bool isReplay)
        => !isReplay && VehicleMount is { } mount
            ? FusionEngineFactory.CreateDefault(mount,
                allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true)
            : FusionEngineFactory.CreateDefault(
                allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);

    /// <summary>
    ///  Converts the source-related choices into service options.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   The demo shortens the live classification window: the service default closes a window
    ///   after 512 observed rows or 30 seconds of source time, which makes a dashboard that only
    ///   receives about one GPS fix per second look as if its route-section list had stopped
    ///   updating. A five-second window keeps the list visibly alive without changing what the
    ///   classifier decides.
    ///  </para>
    /// </remarks>
    internal static SensorServiceOptions CreateSensorServiceOptions()
        => SensorServiceOptions.Default with
        {
            Classification = new SensorClassificationOptions
            {
                Enabled = true,
                WindowRowCount = 64,
                WindowDuration = TimeSpan.FromSeconds(5d)
            }
        };

    /// <summary>
    ///  Converts the platform source choices into Windows source-factory options.
    /// </summary>
    internal WindowsSensorSourceOptions CreateWindowsSourceOptions()
        => new()
        {
            Sources = SourceKind,
            GpsReportIntervalMilliseconds = WindowsGpsReportIntervalMilliseconds,
            SerialGpsOptions = new SerialNmeaGpsSourceOptions
            {
                PortName = string.IsNullOrWhiteSpace(SerialPortName)
                    ? null
                    : SerialPortName.Trim(),
                GnssProfile = SerialGnssProfile,
                TargetFixIntervalMilliseconds = TargetGpsFixIntervalMilliseconds,
                AllowReceiverCommandsThisSession = AllowReceiverCommandsThisSession
            }
        };

    /// <summary>Describes a selected provider and profile without implying receiver activation.</summary>
    internal static string DescribeGpsSource(DemoOptions options)
        => options.SourceKind switch
        {
            WindowsSensorSourceKind.ImuAndSerialNmeaGps
                => $"Serial NMEA ({options.SerialPortName ?? "auto port"}), "
                    + DescribeGpsProfile(options.SerialGnssProfile),
            WindowsSensorSourceKind.ImuAndGps => "Windows Location",
            WindowsSensorSourceKind.ImuOnly => "IMU only (no GPS)",
            _ => "No sources"
        };

    /// <summary>Describes the selected constellation profile, not satellites used in a fix.</summary>
    internal static string DescribeGpsProfile(SerialGnssProfile profile)
        => profile switch
        {
            SerialGnssProfile.Gps => "GPS + QZSS",
            SerialGnssProfile.BeiDou => "BeiDou",
            SerialGnssProfile.Galileo => "Galileo",
            SerialGnssProfile.Glonass => "GLONASS",
            SerialGnssProfile.GpsBeiDouQzss => "GPS + QZSS + BeiDou",
            SerialGnssProfile.Current => "Current constellations (rate only)",
            _ => "Passive (receiver unchanged)"
        };
}

/// <summary>
///  Defines the selectable main-toolbar symbol sizes.
/// </summary>
internal enum DashboardToolbarSize
{
    /// <summary>Uses 48 × 48 pixel symbols.</summary>
    Small = 48,

    /// <summary>Uses 64 × 64 pixel symbols.</summary>
    Medium = 64,

    /// <summary>Uses 96 × 96 pixel symbols.</summary>
    Large = 96
}
