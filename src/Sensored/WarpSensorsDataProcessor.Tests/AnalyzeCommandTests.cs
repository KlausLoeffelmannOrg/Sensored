using System.Text.Json;

using WarpToolkit.Sensors;

namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies streaming analysis, JSON stability, and output safety.
/// </summary>
public class AnalyzeCommandTests
{
    [Fact]
    public async Task Analyze_reports_counts_future_gps_and_excitation()
    {
        string folder = TestRecordings.CreateFolder();
        DateTimeOffset origin = TestRecordings.Origin;
        await TestRecordings.WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-4--STR.csv"),
            [
                TestRecordings.CreateImuRow(0, origin, 1d),
                TestRecordings.CreateGpsRow(
                    1,
                    origin.AddMilliseconds(1),
                    47d,
                    8d,
                    speed: 0.1d,
                    coordinateTimestamp: origin.AddSeconds(1),
                    fixTimestamp: origin.AddSeconds(1)),
                TestRecordings.CreateImuRow(2, origin.AddMilliseconds(3), 1.1d, magneticHeading: double.NaN),
                TestRecordings.CreateGpsRow(3, origin.AddSeconds(1), 47.001d, 8.001d, speed: 5d, courseValid: true),
                TestRecordings.CreateImuRow(4, origin.AddSeconds(1).AddMilliseconds(3), 1.2d)
            ]);
        string outputPath = Path.Combine(folder, "analyze.json");
        StringWriter stdout = new();
        StringWriter stderr = new();

        int exit = await ProcessorApplication.RunAsync(
            ["analyze", "--recording-folder", folder, "--output", outputPath],
            stdout,
            stderr,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(File.Exists(outputPath));
        AnalyzeReport report = JsonSerializer.Deserialize<AnalyzeReport>(
            await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken),
            ProcessorJson.Options)!;
        Assert.Equal(5, report.Counts.Rows);
        Assert.Equal(3, report.Counts.Imu);
        Assert.Equal(2, report.Counts.Geolocation);
        Assert.Equal(2, report.FutureGps.Count);
        Assert.True(report.FutureGps.MaxLeadSeconds > 0.5d);
        Assert.NotNull(report.Motion.FirstSpeedAbove2MpsSeconds);
        Assert.Equal(1, report.Availability.ImuCompassMissing);
        Assert.Contains("IMU", stdout.ToString());
        Assert.Empty(stderr.ToString());
    }

    [Fact]
    public async Task Analyze_json_does_not_crash_on_nan_and_is_repeatable()
    {
        string folder = TestRecordings.CreateFolder();
        await TestRecordings.WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-0--STR.csv"),
            [TestRecordings.CreateImuRow(0, TestRecordings.Origin, double.NaN, magneticHeading: double.NaN)]);
        string firstPath = Path.Combine(folder, "a.json");
        string secondPath = Path.Combine(folder, "b.json");

        int first = await ProcessorApplication.RunAsync(
            ["analyze", "--recording-folder", folder, "--output", firstPath],
            new StringWriter(),
            new StringWriter(),
            TestContext.Current.CancellationToken);
        int second = await ProcessorApplication.RunAsync(
            ["analyze", "--recording-folder", folder, "--output", secondPath],
            new StringWriter(),
            new StringWriter(),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, first);
        Assert.Equal(0, second);
        string firstJson = await File.ReadAllTextAsync(firstPath, TestContext.Current.CancellationToken);
        string secondJson = await File.ReadAllTextAsync(secondPath, TestContext.Current.CancellationToken);
        Assert.Equal(Normalize(firstJson), Normalize(secondJson));
        Assert.DoesNotContain("NaN.NaN", firstJson);
        AnalyzeReport report = JsonSerializer.Deserialize<AnalyzeReport>(firstJson, ProcessorJson.Options)!;
        Assert.Equal(1, report.Ranges["imuAx"].NonFiniteCount);
    }

    [Fact]
    public async Task Analyze_refuses_to_overwrite_an_existing_output()
    {
        string folder = TestRecordings.CreateFolder();
        await TestRecordings.WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-0--STR.csv"),
            [TestRecordings.CreateImuRow(0, TestRecordings.Origin, 1d)]);
        string outputPath = Path.Combine(folder, "exists.json");
        await File.WriteAllTextAsync(outputPath, "{}", TestContext.Current.CancellationToken);
        StringWriter stderr = new();

        int exit = await ProcessorApplication.RunAsync(
            ["analyze", "--recording-folder", folder, "--output", outputPath],
            new StringWriter(),
            stderr,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, exit);
        Assert.Contains("already exists", stderr.ToString());
        Assert.Equal("{}", await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Analyze_requires_output_path()
    {
        bool parsed = AnalyzeOptions.TryParse(
            ["--recording-folder", "."],
            out AnalyzeOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("--output", error);
    }

    private static string Normalize(string json)
        => json
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Trim();
}
