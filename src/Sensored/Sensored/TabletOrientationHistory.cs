using System.Globalization;
using System.Text;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>
///  Appends accepted tablet-orientation captures under the local application data folder.
/// </summary>
internal static class TabletOrientationHistory
{
    private const string Header =
        "captured_at_utc;source;ax_g;ay_g;az_g;magnitude_g;sample_count;window_s;gyro_rms_dps;quality;gps_speed_mps;gps_fresh;bias_x;bias_y;bias_z;tilt_from_upright_deg";

    /// <summary>Gets the history file path.</summary>
    internal static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WARP",
        "Sensored",
        "tablet-orientation.csv");

    /// <summary>
    ///  Appends one accepted capture. Failures are returned instead of thrown.
    /// </summary>
    internal static string? TryAppend(SessionDownLock sample, TabletFrameCaptureSource source)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            bool writeHeader = !File.Exists(FilePath);
            Vec3 raw = sample.RawAccelerationG;
            Vec3 bias = sample.AccelBiasMetersPerSecondSquared;
            double tilt = AngleDegrees(raw, new Vec3(0d, -1d, 0d));
            string row = string.Create(
                CultureInfo.InvariantCulture,
                $"{sample.CapturedAt:O};{source};{raw.X:R};{raw.Y:R};{raw.Z:R};{raw.Magnitude:R};{sample.SampleCount};{sample.WindowSeconds:R};{sample.GyroRmsDegreesPerSecond:R};{sample.Quality};{sample.GpsSpeedMetersPerSecond:R};{sample.GpsSpeedFresh};{bias.X:R};{bias.Y:R};{bias.Z:R};{tilt:R}");

            using FileStream stream = new(FilePath, FileMode.Append, FileAccess.Write, FileShare.Read);
            using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            if (writeHeader)
            {
                writer.WriteLine(Header);
            }

            writer.WriteLine(row);

            return null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return exception.Message;
        }
    }

    /// <summary>
    ///  Reads the last accepted row, if the history file can be parsed.
    /// </summary>
    internal static bool TryReadLatest(out SessionDownLock sample)
    {
        sample = default;

        if (!File.Exists(FilePath))
        {
            return false;
        }

        string? last = null;

        foreach (string line in File.ReadLines(FilePath))
        {
            if (line.Length > 0 && !line.StartsWith("captured_at_utc", StringComparison.Ordinal))
            {
                last = line;
            }
        }

        if (last is null)
        {
            return false;
        }

        string[] parts = last.Split(';');

        if (parts.Length < 15)
        {
            return false;
        }

        sample = new SessionDownLock
        {
            CapturedAt = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            RawAccelerationG = new Vec3(
                double.Parse(parts[2], CultureInfo.InvariantCulture),
                double.Parse(parts[3], CultureInfo.InvariantCulture),
                double.Parse(parts[4], CultureInfo.InvariantCulture)),
            SampleCount = int.Parse(parts[6], CultureInfo.InvariantCulture),
            WindowSeconds = double.Parse(parts[7], CultureInfo.InvariantCulture),
            GyroRmsDegreesPerSecond = double.Parse(parts[8], CultureInfo.InvariantCulture),
            Quality = Enum.Parse<TabletFrameQuality>(parts[9]),
            GpsSpeedMetersPerSecond = double.Parse(parts[10], CultureInfo.InvariantCulture),
            GpsSpeedFresh = bool.Parse(parts[11]),
            AccelBiasMetersPerSecondSquared = new Vec3(
                double.Parse(parts[12], CultureInfo.InvariantCulture),
                double.Parse(parts[13], CultureInfo.InvariantCulture),
                double.Parse(parts[14], CultureInfo.InvariantCulture))
        };

        return sample.DownDevice != Vec3.Zero;
    }

    private static double AngleDegrees(Vec3 left, Vec3 right)
    {
        Vec3 a = left.Normalized();
        Vec3 b = right.Normalized();

        if (a == Vec3.Zero || b == Vec3.Zero)
        {
            return double.NaN;
        }

        return Math.Acos(Math.Clamp(a.Dot(b), -1d, 1d)) * (180d / Math.PI);
    }
}
