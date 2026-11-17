using System.Reflection;
using System.Runtime.CompilerServices;
using WarpToolkit.WinForms.Drawing.Symbols;

namespace Sensored;

/// <summary>
///  Provides the embedded SVG symbols used by the sensor demo.
/// </summary>
internal static class SensorDemoSymbols
{
    /// <summary>Gets the healthy status checkmark.</summary>
    internal static SvgSymbol StatusOk { get; } = SvgSymbol.FromMarkup(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>No exceptions</title><path d="M4 12 L9 18 L21 5" fill="none" stroke="currentColor" stroke-width="3"/></svg>""");

    /// <summary>Gets the exception-circle status mark.</summary>
    internal static SvgSymbol StatusException { get; } = SvgSymbol.FromMarkup(
        """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>Exception recorded</title><circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2"/><path d="M12 5 V14 M12 17 V19" stroke="currentColor" stroke-width="3"/></svg>""");

    /// <summary>Creates an SVG slider variation for one ordered setting.</summary>
    internal static SvgSymbol CreateAdjustmentSymbol(int index, int count)
    {
        int position = 4 + index * 16 / Math.Max(1, count - 1);

        return SvgSymbol.FromMarkup(
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>Adjustment {index + 1} of {count}</title><path d="M2 12 H22 M{position} 6 V18 M4 20 H20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"/></svg>""");
    }

    /// <summary>
    ///  Gets the symbol used for Windows Location.
    /// </summary>
    internal static SvgSymbol GpsWindows { get; } = Load();

    /// <summary>
    ///  Gets the symbol used for a serial NMEA receiver.
    /// </summary>
    internal static SvgSymbol GpsSerial { get; } = Load();

    /// <summary>
    ///  Gets the symbol used to start a raw sensor recording.
    /// </summary>
    internal static SvgSymbol StartRecording { get; } = Load();

    /// <summary>
    ///  Gets the symbol used to stop a raw sensor recording.
    /// </summary>
    internal static SvgSymbol StopRecording { get; } = Load();

    /// <summary>
    ///  Gets the symbol used to open the active recording folder.
    /// </summary>
    internal static SvgSymbol OpenRecordingFolder { get; } = Load();

    /// <summary>
    ///  Gets the symbol used to close raw sensor diagnostics.
    /// </summary>
    internal static SvgSymbol CloseRawSensors { get; } = Load();

    internal static SvgSymbol Accelerometer { get; } = Load();

    internal static SvgSymbol LinearAcceleration { get; } = Load();

    internal static SvgSymbol Gravity { get; } = Load();

    internal static SvgSymbol Gyrometer { get; } = Load();

    internal static SvgSymbol Inclinometer { get; } = Load();

    internal static SvgSymbol OrientationAbsolute { get; } = Load();

    internal static SvgSymbol OrientationRelative { get; } = Load();

    internal static SvgSymbol Compass { get; } = Load();

    internal static SvgSymbol LightSensor { get; } = Load();

    internal static SvgSymbol RecordingPage { get; } = Load();

    internal static SvgSymbol OptionsSource { get; } = Load();

    internal static SvgSymbol OptionsDisplay { get; } = Load();

    internal static SvgSymbol OptionsMap { get; } = Load();

    internal static SvgSymbol OptionsRecording { get; } = Load();

    /// <summary>Gets the recording playback command symbol.</summary>
    internal static SvgSymbol ReplayPlay { get; } = Load();

    /// <summary>Gets the recording stop command symbol.</summary>
    internal static SvgSymbol ReplayStop { get; } = Load();

    /// <summary>Gets the recording pause command symbol.</summary>
    internal static SvgSymbol ReplayPause { get; } = Load();

    /// <summary>Gets the recording resume command symbol.</summary>
    internal static SvgSymbol ReplayResume { get; } = Load();

    /// <summary>Gets the five-second backward seek symbol.</summary>
    internal static SvgSymbol ReplayBack { get; } = Load();

    /// <summary>Gets the thirty-second backward seek symbol.</summary>
    internal static SvgSymbol ReplayBackFast { get; } = Load();

    /// <summary>Gets the five-second forward seek symbol.</summary>
    internal static SvgSymbol ReplayForward { get; } = Load();

    /// <summary>Gets the thirty-second forward seek symbol.</summary>
    internal static SvgSymbol ReplayForwardFast { get; } = Load();

    private static SvgSymbol Load([CallerMemberName] string symbolName = "")
    {
        string resourceName = $"Sensored.Symbols.{symbolName}.svg";
        Assembly assembly = typeof(SensorDemoSymbols).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The embedded sensor-demo symbol '{resourceName}' was not found.");

        return SvgSymbol.FromStream(stream);
    }
}
