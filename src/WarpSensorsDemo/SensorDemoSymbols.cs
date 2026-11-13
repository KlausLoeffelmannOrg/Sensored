using System.Reflection;
using System.Runtime.CompilerServices;
using WarpToolkit.WinForms.Drawing.Symbols;

namespace WarpSensorsDemo;

/// <summary>
///  Provides the embedded SVG symbols used by the sensor demo.
/// </summary>
internal static class SensorDemoSymbols
{
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

    private static SvgSymbol Load([CallerMemberName] string symbolName = "")
    {
        string resourceName = $"WarpSensorsDemo.Symbols.{symbolName}.svg";
        Assembly assembly = typeof(SensorDemoSymbols).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The embedded sensor-demo symbol '{resourceName}' was not found.");

        return SvgSymbol.FromStream(stream);
    }
}
