using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies fusion-reference export, scenarios, and atomic completion.
/// </summary>
public class FusionReferenceTests
{
    [Fact]
    public async Task Export_writes_one_row_per_imu_and_repeat_hash()
    {
        string folder = await CreateBaselineFolderAsync();
        string firstOutput = Path.Combine(folder, "ref-a");
        string secondOutput = Path.Combine(folder, "ref-b");
        FusionReferenceOptions optionsA = CreateOptions(folder, firstOutput);
        FusionReferenceOptions optionsB = CreateOptions(folder, secondOutput);

        FusionReferenceResult first = await FusionReferenceExporter.ExportAsync(
            optionsA,
            new TickEngine(),
            TestContext.Current.CancellationToken);
        FusionReferenceResult second = await FusionReferenceExporter.ExportAsync(
            optionsB,
            new TickEngine(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ArtifactCompletion.Complete, first.Completion);
        Assert.Equal(3, first.ImuRows);
        Assert.Equal(3, first.OutputRows);
        Assert.Equal(first.TraceSha256, second.TraceSha256);
        Assert.True(Directory.Exists(firstOutput));
        Assert.True(File.Exists(Path.Combine(firstOutput, "golden.trace.csv")));
        Assert.True(File.Exists(Path.Combine(firstOutput, "manifest.json")));
        string[] lines = await File.ReadAllLinesAsync(
            Path.Combine(firstOutput, "golden.trace.csv"),
            TestContext.Current.CancellationToken);
        Assert.Equal(4, lines.Length);
        Assert.Equal(FusionTraceSchema.Header, lines[0]);
        Assert.Equal(FusionCalibrationState.Calibrated, first.FinalCalibration);
        Assert.Equal(3, first.Manifest.Calibration.CalibratedRows);
        Assert.Equal(1d, first.Manifest.Confidence.Mean);
        Assert.Equal(3, first.Manifest.Ranges.PitchDegrees.FiniteCount);
        Assert.Equal(0, first.Manifest.Counts.RejectedRows);
        Assert.False(string.IsNullOrWhiteSpace(first.Manifest.EngineAssemblySha256));
    }

    [Fact]
    public async Task Existing_output_is_rejected_and_source_is_untouched()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "exists");
        Directory.CreateDirectory(output);
        FusionReferenceOptions options = CreateOptions(folder, output);

        IOException exception = await Assert.ThrowsAsync<IOException>(
            () => FusionReferenceExporter.ExportAsync(
                options,
                new TickEngine(),
                TestContext.Current.CancellationToken));

        Assert.Contains("already exists", exception.Message);
        Assert.True(File.Exists(Directory.GetFiles(folder, "part-*.csv")[0]));
    }

    [Fact]
    public async Task Cancelled_export_is_marked_incomplete()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "cancelled");
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => FusionReferenceExporter.ExportAsync(
                CreateOptions(folder, output),
                new TickEngine(),
                cancellation.Token));

        Assert.False(Directory.Exists(output));
    }

    [Fact]
    public async Task Gps_withheld_scenario_does_not_feed_engine_and_writes_sidecar()
    {
        string folder = await CreateBaselineFolderAsync();
        string scenarioPath = Path.Combine(folder, "scenario.json");
        await File.WriteAllTextAsync(
            scenarioPath,
            """
            {
              "schema": "warp-fusion-scenario",
              "version": 1,
              "name": "gps-withheld",
              "exportTransformedCopy": true,
              "faults": [
                {
                  "type": "GpsWithheldFixes",
                  "startSeconds": 0,
                  "durationSeconds": 10
                }
              ]
            }
            """,
            TestContext.Current.CancellationToken);
        string output = Path.Combine(folder, "withheld");
        CountingEngine engine = new();

        FusionReferenceResult result = await FusionReferenceExporter.ExportAsync(
            CreateOptions(folder, output, scenarioPath),
            engine,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, engine.GpsCount);
        Assert.Equal(3, engine.ImuCount);
        Assert.True(File.Exists(Path.Combine(output, "withheld-gps.csv")));
        Assert.True(File.Exists(Path.Combine(output, "transformed", "part-0000--merged.csv")));
        string transformed = await File.ReadAllTextAsync(
            Path.Combine(output, "transformed", "part-0000--merged.csv"),
            TestContext.Current.CancellationToken);
        Assert.DoesNotContain("Geolocation", transformed);
        Assert.Equal(3, result.OutputRows);
    }

    [Fact]
    public async Task Imu_spike_scenario_changes_raw_input_seen_by_engine()
    {
        string folder = await CreateBaselineFolderAsync();
        string scenarioPath = Path.Combine(folder, "spike.json");
        await File.WriteAllTextAsync(
            scenarioPath,
            """
            {
              "schema": "warp-fusion-scenario",
              "version": 1,
              "name": "imu-spike",
              "faults": [
                {
                  "type": "ImuSpike",
                  "startSeconds": 0,
                  "durationSeconds": 1,
                  "axis": "X",
                  "delta": 20
                }
              ]
            }
            """,
            TestContext.Current.CancellationToken);
        string output = Path.Combine(folder, "spike");
        TickEngine engine = new();

        await FusionReferenceExporter.ExportAsync(
            CreateOptions(folder, output, scenarioPath),
            engine,
            TestContext.Current.CancellationToken);

        string[] lines = await File.ReadAllLinesAsync(
            Path.Combine(output, "golden.trace.csv"),
            TestContext.Current.CancellationToken);
        int imuAx = FusionTraceSchema.Columns.ToList().IndexOf("imu_ax");
        double ax = double.Parse(
            lines[1].Split(';')[imuAx],
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(21d, ax, 6);
    }

    [Fact]
    public async Task Fusion_reference_command_uses_the_golden_engine()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "cli-out");
        StringWriter stdout = new();
        StringWriter stderr = new();

        int exit = await ProcessorApplication.RunAsync(
            ["fusion-reference", "--recording-folder", folder, "--output", output],
            stdout,
            stderr,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.True(Directory.Exists(output));
        Assert.True(File.Exists(Path.Combine(output, "golden.trace.csv")));
        Assert.Contains("Fusion reference export complete.", stdout.ToString());
        Assert.Contains("Engine assembly:", stdout.ToString());
        Assert.Empty(stderr.ToString());
        string[] lines = await File.ReadAllLinesAsync(
            Path.Combine(output, "golden.trace.csv"),
            TestContext.Current.CancellationToken);
        Assert.Equal(FusionTraceSchema.Header, lines[0]);
        Assert.Equal(4, lines.Length);
    }

    [Fact]
    public async Task Missing_imu_estimate_marks_the_export_incomplete()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "no-estimate");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReferenceExporter.ExportAsync(
                CreateOptions(folder, output),
                new NullImuEngine(),
                TestContext.Current.CancellationToken));

        Assert.Contains("did not produce a fusion estimate", exception.Message);
        Assert.False(Directory.Exists(output));
        Assert.DoesNotContain(
            Directory.EnumerateDirectories(folder),
            path => Path.GetFileName(path).Contains(".writing", StringComparison.Ordinal));
        Assert.True(Directory.Exists(output + ".incomplete"));
    }

    [Fact]
    public async Task Auxiliary_estimate_marks_the_export_incomplete()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "aux-estimate");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReferenceExporter.ExportAsync(
                CreateOptions(folder, output),
                new AlwaysEstimateEngine(),
                TestContext.Current.CancellationToken));

        Assert.Contains("produced a fusion estimate", exception.Message);
        Assert.True(Directory.Exists(output + ".incomplete"));
    }

    [Fact]
    public async Task Gps_only_recording_is_incomplete()
    {
        string folder = TestRecordings.CreateFolder();
        await TestRecordings.WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-0--STR.csv"),
            [TestRecordings.CreateGpsRow(0, TestRecordings.Origin, 47d, 8d)]);
        string output = Path.Combine(folder, "gps-only");

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => FusionReferenceExporter.ExportAsync(
                CreateOptions(folder, output),
                new TickEngine(),
                TestContext.Current.CancellationToken));

        Assert.Contains("no IMU rows", exception.Message);
        Assert.True(Directory.Exists(output + ".incomplete"));
    }

    [Fact]
    public async Task Invalid_scenario_does_not_leave_a_writing_directory()
    {
        string folder = await CreateBaselineFolderAsync();
        string scenarioPath = Path.Combine(folder, "bad.json");
        await File.WriteAllTextAsync(
            scenarioPath,
            "{ not-json",
            TestContext.Current.CancellationToken);
        string output = Path.Combine(folder, "bad-scenario");

        await Assert.ThrowsAnyAsync<Exception>(
            () => FusionReferenceExporter.ExportAsync(
                CreateOptions(folder, output, scenarioPath),
                new TickEngine(),
                TestContext.Current.CancellationToken));

        Assert.False(Directory.Exists(output));
        Assert.DoesNotContain(
            Directory.EnumerateDirectories(folder),
            path => Path.GetFileName(path).Contains(".writing", StringComparison.Ordinal));
        Assert.False(Directory.Exists(output + ".incomplete"));
    }

    [Fact]
    public async Task Manifest_includes_engine_assembly_identity()
    {
        string folder = await CreateBaselineFolderAsync();
        string output = Path.Combine(folder, "identity");

        FusionReferenceResult result = await FusionReferenceExporter.ExportAsync(
            CreateOptions(folder, output),
            new TickEngine(),
            TestContext.Current.CancellationToken);

        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.EngineAssemblyVersion));
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.EngineAssemblySha256));
        Assert.Equal(64, result.Manifest.EngineAssemblySha256!.Length);
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.EngineAssemblyPath));
        Assert.False(string.IsNullOrWhiteSpace(result.Manifest.EngineAssemblyLastWriteUtc));
        Assert.Contains("T", result.Manifest.EngineAssemblyLastWriteUtc, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_requires_output_directory()
    {
        bool parsed = FusionReferenceOptions.TryParse(
            ["--recording-folder", "."],
            out FusionReferenceOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("--output", error);
    }

    private static FusionReferenceOptions CreateOptions(
        string folder,
        string output,
        string? scenario = null)
        => new(folder, output, scenario, ShowHelp: false);

    private static async Task<string> CreateBaselineFolderAsync()
    {
        string folder = TestRecordings.CreateFolder();
        DateTimeOffset origin = TestRecordings.Origin;
        await TestRecordings.WritePartAsync(
            Path.Combine(folder, "part-0000--rows-0-3--STR.csv"),
            [
                TestRecordings.CreateImuRow(0, origin, 1d),
                TestRecordings.CreateGpsRow(1, origin.AddMilliseconds(1), 47d, 8d, speed: 3d),
                TestRecordings.CreateImuRow(2, origin.AddMilliseconds(3), 1.1d),
                TestRecordings.CreateImuRow(3, origin.AddMilliseconds(6), 1.2d)
            ]);

        return folder;
    }

    /// <summary>
    ///  Emits one estimate per IMU using the raw X acceleration as pitch.
    /// </summary>
    private sealed class TickEngine : IFusionEngine
    {
        private long _sequence;

        public FusionEstimate? Process(SensorEnvelope envelope)
        {
            if (envelope.Kind != SensorDataKind.Imu || envelope.Imu is not { } imu)
            {
                return null;
            }

            return new FusionEstimate
            {
                Sequence = _sequence++,
                Timestamp = envelope.SourceTimestamp,
                Pitch = imu.Acceleration.X,
                CalibrationState = FusionCalibrationState.Calibrated,
                Confidence = 1d,
                Validity = FusionOutputValidity.Attitude
            };
        }

        public void Reset() => _sequence = 0;
    }

    /// <summary>
    ///  Returns no estimate for IMU ticks.
    /// </summary>
    private sealed class NullImuEngine : IFusionEngine
    {
        public FusionEstimate? Process(SensorEnvelope envelope) => null;

        public void Reset()
        {
        }
    }

    /// <summary>
    ///  Returns an estimate for every envelope, including auxiliary kinds.
    /// </summary>
    private sealed class AlwaysEstimateEngine : IFusionEngine
    {
        private long _sequence;

        public FusionEstimate? Process(SensorEnvelope envelope)
            => new()
            {
                Sequence = _sequence++,
                Timestamp = envelope.SourceTimestamp
            };

        public void Reset() => _sequence = 0;
    }

    /// <summary>
    ///  Counts IMU versus GPS observations reaching the engine.
    /// </summary>
    private sealed class CountingEngine : IFusionEngine
    {
        internal int ImuCount;
        internal int GpsCount;
        private readonly TickEngine _inner = new();

        public FusionEstimate? Process(SensorEnvelope envelope)
        {
            if (envelope.Kind == SensorDataKind.Imu)
            {
                ImuCount++;
            }
            else if (envelope.Kind == SensorDataKind.Geolocation)
            {
                GpsCount++;
            }

            return _inner.Process(envelope);
        }

        public void Reset()
        {
            ImuCount = 0;
            GpsCount = 0;
            _inner.Reset();
        }
    }
}
