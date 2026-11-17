using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Shared recording fixtures for processor tests.
/// </summary>
internal static class TestRecordings
{
    internal static readonly DateTimeOffset Origin =
        new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    internal static string CreateFolder()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "ProcessorTestArtifacts",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        return path;
    }

    internal static async Task WritePartAsync(string path, IEnumerable<SensorEnvelopeRow> rows)
    {
        await using SemicolonCsvRecordingWriter writer = new(path);
        await writer.WriteHeaderAsync(TestContext.Current.CancellationToken);

        foreach (SensorEnvelopeRow row in rows)
        {
            await writer.WriteRowAsync(row, TestContext.Current.CancellationToken);
        }
    }

    internal static SensorEnvelopeRow CreateImuRow(
        long row,
        DateTimeOffset timestamp,
        double value,
        double magneticHeading = 10d,
        double magneticConfidence = 0.2d)
        => new(
            row,
            new SensorEnvelope
            {
                Sequence = row,
                Kind = SensorDataKind.Imu,
                SourceTimestamp = timestamp,
                ArrivalTimestamp = timestamp,
                Imu = new RawImuPayload(
                    new Vec3(value, value + 0.1d, value + 0.2d),
                    new Vec3(value, value, value),
                    HasAngularRate: true,
                    MagneticHeadingDegrees: magneticHeading,
                    MagneticHeadingConfidence: magneticConfidence,
                    AbsolutePitchDegrees: value,
                    AbsoluteRollDegrees: value,
                    AbsoluteYawDegrees: value,
                    YawSource: DeviceYawSource.WindowsFusedOrientation)
            });

    internal static SensorEnvelopeRow CreateGpsRow(
        long row,
        DateTimeOffset timestamp,
        double latitude,
        double longitude,
        double speed = 0d,
        bool courseValid = false,
        DateTimeOffset? coordinateTimestamp = null,
        DateTimeOffset? fixTimestamp = null)
        => new(
            row,
            new SensorEnvelope
            {
                Sequence = row,
                Kind = SensorDataKind.Geolocation,
                SourceTimestamp = timestamp,
                ArrivalTimestamp = timestamp,
                Geolocation = new GeolocationPayload(
                    latitude,
                    longitude,
                    AltitudeMeters: 100d,
                    HorizontalAccuracyMeters: 3d,
                    VerticalAccuracyMeters: 5d,
                    SpeedMetersPerSecond: speed,
                    SpeedValid: double.IsFinite(speed) && speed >= 0d,
                    CourseDegrees: 90d,
                    CourseValid: courseValid,
                    PositionSource: GpsPositionSource.Satellite,
                    IsRemoteSource: false,
                    CoordinateTimestamp: coordinateTimestamp ?? timestamp,
                    SourceTimestamp: fixTimestamp ?? timestamp,
                    PositionDilutionOfPrecision: 1d,
                    HorizontalDilutionOfPrecision: 1d,
                    VerticalDilutionOfPrecision: 1d,
                    FixQuality: 1,
                    SatelliteCount: 8,
                    Confidence: 1d)
            });
}
