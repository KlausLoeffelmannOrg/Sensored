namespace WarpSensorsDataProcessor.Tests;

/// <summary>Verifies independent timing grading rather than self-reported sample cadence.</summary>
public sealed class ReplayTimingTests
{
    [Theory]
    [InlineData(0.25d)]
    [InlineData(0.5d)]
    [InlineData(1d)]
    [InlineData(1.5d)]
    [InlineData(2d)]
    [InlineData(3d)]
    public void Every_requested_rate_passes_when_observed_delivery_matches(double speed)
    {
        ReplayTimingAssessment result = ReplayTimingAssessment.Evaluate(
            CreateEvidence(speed), [new(0d, 0d, speed)], complete: true);

        Assert.True(result.Passed);
        Assert.Equal(speed, result.ObservedRate, precision: 8);
        Assert.InRange(result.RelativeRateError, 0d, 1e-12d);
    }

    [Fact]
    public void Correct_recorded_timestamps_delivered_six_times_too_slow_fail()
    {
        ReplayTimingAssessment result = ReplayTimingAssessment.Evaluate(
            CreateEvidence(1d / 6d), [new(0d, 0d, 1d)], complete: true);

        Assert.False(result.Passed);
        Assert.Equal(1d / 6d, result.ObservedRate, precision: 8);
        Assert.True(result.RelativeRateError > 0.8d);
    }

    [Fact]
    public void Pause_and_rate_change_segments_are_accounted_for_without_reading_a_clock()
    {
        ReplayTimingObservation[] evidence =
        [
            Row(0, 0d, 0d), Row(1, 1d, 1d), Row(2, 2d, 2d), Row(3, 3d, 5.5d), Row(4, 4d, 6d)
        ];
        ReplayTimingSegment[] segments = [new(0d, 0d, 1d), new(2d, 2d, 0d), new(5d, 2d, 2d)];
        ReplayTimingAssessment first = ReplayTimingAssessment.Evaluate(evidence, segments, complete: true);
        ReplayTimingAssessment second = ReplayTimingAssessment.Evaluate(evidence, segments, complete: true);

        Assert.True(first.Passed);
        Assert.Equal(3d, first.ActiveWallSeconds);
        Assert.Equal(first, second);
        Assert.Equal(0d, first.Lateness.Maximum);
    }

    [Fact]
    public void Incomplete_evidence_cannot_pass_even_with_perfect_timing()
        => Assert.False(ReplayTimingAssessment.Evaluate(
            CreateEvidence(1d), [new(0d, 0d, 1d)], complete: false).Passed);

    [Fact]
    public void Missing_or_discontinuous_evidence_is_rejected()
    {
        Assert.Throws<InvalidDataException>(() => ReplayTimingAssessment.Evaluate([], [], complete: true));
        Assert.Throws<InvalidDataException>(() => ReplayTimingAssessment.Evaluate(
            CreateEvidence(1d), [new(0d, 0d, 1d), new(10d, 50d, 1d)], complete: true));
    }

    [Fact]
    public void Actual_delivery_during_a_pause_fails()
    {
        ReplayTimingObservation[] evidence =
        [
            Row(0, 0d, 0d), Row(1, 1d, 1d), Row(2, 2d, 3d), Row(3, 3d, 6d)
        ];

        Assert.False(ReplayTimingAssessment.Evaluate(
            evidence, [new(0d, 0d, 1d), new(2d, 2d, 0d), new(5d, 2d, 1d)], complete: true).Passed);
    }

    [Theory]
    [InlineData("--speed", "0")]
    [InlineData("--speed", "NaN")]
    [InlineData("--speed", "Infinity")]
    [InlineData("--duration-seconds", "0")]
    [InlineData("--start-seconds", "-1")]
    public void Invalid_timing_arguments_are_rejected(string name, string value)
    {
        Assert.False(ReplayTimingOptions.TryParse(
            ["--recording-folder", ".", "--output", "timing.json", name, value],
            out _, out string? error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }

    [Fact]
    public async Task Existing_output_is_not_overwritten()
    {
        string directory = TestRecordings.CreateFolder();
        string output = Path.Combine(directory, "existing.json");
        await File.WriteAllTextAsync(output, "unchanged", TestContext.Current.CancellationToken);
        StringWriter error = new();
        int exit = await ProcessorApplication.RunAsync(
            ["replay-timing", "--recording-folder", directory, "--output", output],
            new StringWriter(), error, TestContext.Current.CancellationToken);

        Assert.Equal(1, exit);
        Assert.Contains("already exists", error.ToString());
        Assert.Equal("unchanged", await File.ReadAllTextAsync(output, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Real_pipeline_diagnostic_delivers_a_bounded_window_without_loss(bool fusion)
    {
        string directory = TestRecordings.CreateFolder();
        await TestRecordings.WritePartAsync(
            Path.Combine(directory, "part-0000--timing.csv"),
            Enumerable.Range(0, 21).Select(index =>
                TestRecordings.CreateImuRow(index, TestRecordings.Origin.AddMilliseconds(index * 10), 0d)));
        ReplayTimingOptions options = new(
            directory, Path.Combine(directory, "timing.json"), 1d, 0d, 0.05d, fusion, false);

        ReplayTimingReport report = await ReplayTimingApplication.MeasureAsync(options, TestContext.Current.CancellationToken);

        Assert.True(report.Complete);
        Assert.Equal(6, report.ExpectedRows);
        Assert.Equal(report.ExpectedRows, report.ObservedRows);
        Assert.Equal(report.ExpectedRows, report.ConsumedRows);
        Assert.Equal(0, report.IngressDropped);
        Assert.Equal(0, report.SubscriptionDropped);
        Assert.Equal(0, report.SubscriptionThrottled);
        Assert.Equal(fusion ? 6L : null, report.FusionOutputRows);
        Assert.Equal(0.05d, report.Timing.RecordedSeconds, precision: 8);
        Assert.Equal("warp-replay-timing", report.Schema);
        Assert.Equal(0, report.GpsClock.Fixes);
        Assert.Null(report.GpsClock.Rate);
    }

    private static ReplayTimingObservation[] CreateEvidence(double speed)
        => [.. Enumerable.Range(0, 24001).Select(index => Row(index, index / 400d, index / 400d / speed))];

    private static ReplayTimingObservation Row(int index, double recording, double wall)
        => new(index, index, "Imu", recording, wall, wall + 0.001d, 0.001d);
}
