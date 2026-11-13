namespace WarpSensorsDemo;

using WarpToolkit.Sensors;

/// <summary>
///  Holds the demo's currently active service-owned recording so the crash guard can flush or stop
///  it from a last-chance handler without reaching into the UI.
/// </summary>
internal static class DemoRecordingRegistry
{
    private static ISensorRecordingSession? s_active;

    /// <summary>
    ///  Gets the active recording session, or <see langword="null"/>.
    /// </summary>
    public static ISensorRecordingSession? Active => Volatile.Read(ref s_active);

    /// <summary>
    ///  Publishes the active recording session.
    /// </summary>
    /// <param name="session">The session, or <see langword="null"/> when none is active.</param>
    public static void Set(ISensorRecordingSession? session) => Volatile.Write(ref s_active, session);
}
