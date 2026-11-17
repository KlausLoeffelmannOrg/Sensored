using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies recording merge, damping, and stationary-tail trimming.
/// </summary>
public class RecordingProcessorTests
{
    private static readonly DateTimeOffset s_origin =
        new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Merge_orders_parts_by_numeric_part_index()
    {
        string folder = CreateTestFolder();
        await WritePartAsync(
            Path.Combine(folder, "part-0010--rows-1-1--STR.csv"),
            [CreateImuRow(1, s_origin.AddSeconds(1), 10d)]);
        await WritePartAsync(
            Path.Combine(folder, "part-0002--rows-0-0--STR.csv"),
            [CreateImuRow(0, s_origin, 2d)]);
        string outputPath = Path.Combine(folder, "merged.csv");
        CommandLineOptions options = new(
            folder,
            outputPath,
            DampingMilliseconds: null,
            MergeParts: true,
            TrimStationaryEnd: false,
            ShowHelp: false);

        RecordingProcessingResult result = await RecordingProcessor.ProcessAsync(
            options,
            TestContext.Current.CancellationToken);
        List<SensorEnvelopeRow> rows = await ReadRowsAsync(outputPath);

        Assert.Equal(2, result.InputRows);
        Assert.Equal(2, result.OutputRows);
        Assert.Equal([0L, 1L], rows.Select(static row => row.Row));
        Assert.Equal(
            [2d, 10d],
            rows.Select(static row => row.Envelope.Imu!.Value.Acceleration.X));
    }

    [Fact]
    public async Task Damping_uses_the_recorded_time_delta_across_parts()
    {
        string folder = CreateTestFolder();
        await WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-0--STR.csv"),
            [CreateImuRow(0, s_origin, 0d)]);
        await WritePartAsync(
            Path.Combine(folder, "part-0001--rows-1-1--STR.csv"),
            [CreateImuRow(1, s_origin.AddMilliseconds(100), 10d)]);
        string outputDirectory = Path.Combine(folder, "processed");
        CommandLineOptions options = new(
            folder,
            outputDirectory,
            DampingMilliseconds: 100,
            MergeParts: false,
            TrimStationaryEnd: false,
            ShowHelp: false);

        await RecordingProcessor.ProcessAsync(
            options,
            TestContext.Current.CancellationToken);
        List<SensorEnvelopeRow> rows = await ReadRowsAsync(
            Path.Combine(outputDirectory, "part-0001--rows-1-1--STR.csv"));

        double expected = 10d * (1d - Math.Exp(-1d));
        Assert.Equal(expected, rows[0].Envelope.Imu!.Value.Acceleration.X, 10);
    }

    [Fact]
    public async Task Stationary_tail_keeps_one_minute_and_trims_every_later_kind()
    {
        string folder = CreateTestFolder();
        SensorEnvelopeRow[] rows =
        [
            CreateGpsRow(0, s_origin, 47d, 8d),
            CreateGpsRow(1, s_origin.AddSeconds(10), 48d, 9d),
            CreateGpsRow(2, s_origin.AddSeconds(40), 48.00001d, 9d),
            CreateGpsRow(3, s_origin.AddSeconds(70), 48d, 9.00001d),
            CreateImuRow(4, s_origin.AddSeconds(80), 4d),
            CreateGpsRow(5, s_origin.AddSeconds(100), 48d, 9d)
        ];
        await WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-5--STR.csv"),
            rows);
        string outputPath = Path.Combine(folder, "trimmed.csv");
        CommandLineOptions options = new(
            folder,
            outputPath,
            DampingMilliseconds: null,
            MergeParts: true,
            TrimStationaryEnd: true,
            ShowHelp: false);

        RecordingProcessingResult result = await RecordingProcessor.ProcessAsync(
            options,
            TestContext.Current.CancellationToken);
        List<SensorEnvelopeRow> outputRows = await ReadRowsAsync(outputPath);

        Assert.Equal(6, result.InputRows);
        Assert.Equal(4, result.OutputRows);
        Assert.Equal(2, result.TrimmedRows);
        Assert.All(
            outputRows,
            row => Assert.True(row.Envelope.SourceTimestamp <= s_origin.AddSeconds(70)));
    }

    private static async Task WritePartAsync(
        string path,
        IEnumerable<SensorEnvelopeRow> rows)
    {
        await using SemicolonCsvRecordingWriter writer = new(path);
        await writer.WriteHeaderAsync(TestContext.Current.CancellationToken);

        foreach (SensorEnvelopeRow row in rows)
        {
            await writer.WriteRowAsync(row, TestContext.Current.CancellationToken);
        }
    }

    private static async Task<List<SensorEnvelopeRow>> ReadRowsAsync(string path)
    {
        List<SensorEnvelopeRow> rows = [];

        await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadPartAsync(
            path,
            TestContext.Current.CancellationToken))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static SensorEnvelopeRow CreateImuRow(
        long row,
        DateTimeOffset timestamp,
        double value)
        => new(
            row,
            new SensorEnvelope
            {
                Sequence = row,
                Kind = SensorDataKind.Imu,
                SourceTimestamp = timestamp,
                ArrivalTimestamp = timestamp,
                Imu = new RawImuPayload(
                    new Vec3(value, value, value),
                    new Vec3(value, value, value),
                    HasAngularRate: true,
                    MagneticHeadingDegrees: value,
                    MagneticHeadingConfidence: 1d,
                    AbsolutePitchDegrees: value,
                    AbsoluteRollDegrees: value,
                    AbsoluteYawDegrees: value,
                    YawSource: DeviceYawSource.WindowsFusedOrientation)
            });

    private static SensorEnvelopeRow CreateGpsRow(
        long row,
        DateTimeOffset timestamp,
        double latitude,
        double longitude)
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
                    SpeedMetersPerSecond: 0d,
                    SpeedValid: true,
                    CourseDegrees: 0d,
                    CourseValid: false,
                    PositionSource: GpsPositionSource.Satellite,
                    IsRemoteSource: false,
                    CoordinateTimestamp: timestamp,
                    SourceTimestamp: timestamp,
                    PositionDilutionOfPrecision: 1d,
                    HorizontalDilutionOfPrecision: 1d,
                    VerticalDilutionOfPrecision: 1d,
                    FixQuality: 1,
                    SatelliteCount: 8,
                    Confidence: 1d)
            });

    private static string CreateTestFolder()
    {
        string path = Path.Combine(
            AppContext.BaseDirectory,
            "ProcessorTestArtifacts",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        return path;
    }
}
