using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;

namespace WarpSensorsDemo;

/// <summary>
///  Contains the source, display, route, and recording choices owned by the demo UI.
/// </summary>
internal sealed record DemoOptions
{
    /// <summary>
    ///  Gets the default demo options.
    /// </summary>
    internal static DemoOptions Default { get; } = new();

    /// <summary>
    ///  Gets the selected sensor source combination.
    /// </summary>
    internal WindowsSensorSourceKind SourceKind { get; init; } = WindowsSensorSourceKind.ImuAndGps;

    /// <summary>
    ///  Gets the optional serial GPS port name.
    /// </summary>
    internal string? SerialPortName { get; init; }

    /// <summary>
    ///  Gets the number formats used by diagnostic displays.
    /// </summary>
    internal ReadingFormats ReadingFormats { get; init; } = ReadingFormats.Default;

    /// <summary>
    ///  Gets the control-owned display damping interval in milliseconds.
    /// </summary>
    internal int DampingMilliseconds { get; init; } = 100;

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

    /// <summary>
    ///  Gets a value indicating whether recording starts with every sensor session.
    /// </summary>
    internal bool AutoRecording { get; init; }

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
            SerialGpsOptions = new SerialNmeaGpsSourceOptions
            {
                PortName = string.IsNullOrWhiteSpace(SerialPortName)
                    ? null
                    : SerialPortName.Trim()
            }
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
