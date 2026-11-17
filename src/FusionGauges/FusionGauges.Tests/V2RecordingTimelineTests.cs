using Sensored;
using WarpToolkit.Sensors;

namespace FusionGauges.Tests;

/// <summary>Checks recorded V2 aggregation, independent availability, quoting and source-row provenance.</summary>
public sealed class V2RecordingTimelineTests
{
    [Theory]
    [InlineData(50, 3)]
    [InlineData(1000, 1)]
    public async Task RawValuesAreAveragedPerValidChannelWithoutInventingMissingSamples(int milliseconds, int buckets)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.RawV2.csv");
        try
        {
            await File.WriteAllTextAsync(path,
                "recording_id;capture_utc;accel_x_g;accel_x_g_state;accel_y_g;accel_y_g_state;accel_z_g;accel_z_g_state;note\n"
                + "r;2026-10-01T12:00:00.010Z;1;Valid;100;Stale;;Missing;\"source;one\"\n"
                + "r;2026-10-01T12:00:00.110Z;3;Valid;2;Valid;NaN;Valid;\"line one\nline \"\"two\"\"\"\n"
                + "r;2026-10-01T12:00:00.210Z;500;Stale;4;Valid;Infinity;Valid;last\n",
                TestContext.Current.CancellationToken);
            using V2RecordingTimelineIndex index = await V2RecordingTimelineIndex.BuildAsync(
                path, milliseconds, TestContext.Current.CancellationToken);
            Assert.Equal(buckets, index.Count);
            V2TimelineRow first = index.Get(0);
            if (milliseconds == 1000)
            {
                Assert.Equal("2", first.Values[2]);
                Assert.Equal("3", first.Values[4]);
                Assert.Equal("", first.Values[6]);
                Assert.Equal(2, first.SampleCounts[2]);
                Assert.Equal(2, first.SampleCounts[4]);
                Assert.Equal(0, first.SampleCounts[6]);
                Assert.Equal(1, first.FirstSourceRow);
                Assert.Equal(3, first.LastSourceRow);
            }
            else
            {
                Assert.Equal("1", first.Values[2]);
                Assert.Equal("", index.Get(2).Values[2]);
                Assert.Equal("4", index.Get(2).Values[4]);
            }

            string[] original = V2RecordingTimelineIndex.ReadSourceRow(path, 2, TestContext.Current.CancellationToken);
            Assert.Equal("line one\nline \"two\"", original[8]);
            Assert.Equal("3", original[2]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(50, 2)]
    [InlineData(1000, 1)]
    public async Task LegacyRecordingResolutionAlsoAveragesTheActualValidObservations(int milliseconds, int buckets)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        DateTimeOffset time = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        string first = SensorTimestampFormat.Format(time.AddMilliseconds(10));
        string second = SensorTimestampFormat.Format(time.AddMilliseconds(110));
        try
        {
            await File.WriteAllTextAsync(path,
                "row;seq;kind;t_source;t_arrival;validity;geo_speed_mps;geo_speed_valid;geo_course_deg;geo_course_valid\n"
                + $"1;1;Geolocation;{first};{first};None;10;true;359;true\n"
                + $"2;2;Geolocation;{second};{second};None;20;true;1;true\n",
                TestContext.Current.CancellationToken);
            using RawRecordingTimelineIndex index = await RawRecordingTimelineIndex.BuildAsync(
                path, TestContext.Current.CancellationToken, milliseconds);
            Assert.Equal(buckets, index.Count);
            Assert.Equal(milliseconds == 50 ? 10 : 15, index.Get(0).Speed);
            Assert.Equal(milliseconds == 50 ? 359d : 0d, index.Get(0).Course, 9);
            Assert.True(double.IsNaN(index.Get(0).Forward));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DirectionsWrapAtNorthAndSourceChangesBreakTheAveragingWindow()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.RawV2.csv");
        try
        {
            await File.WriteAllTextAsync(path,
                "recording_id;capture_utc;compass_deg;compass_deg_state;compass_deg_generation\n"
                + "r;2026-10-01T12:00:00.010Z;359;Valid;1\n"
                + "r;2026-10-01T12:00:00.110Z;1;Valid;1\n"
                + "r;2026-10-01T12:00:00.210Z;90;Valid;2\n",
                TestContext.Current.CancellationToken);
            using V2RecordingTimelineIndex index = await V2RecordingTimelineIndex.BuildAsync(
                path, 1000, TestContext.Current.CancellationToken);
            Assert.Equal(2, index.Count);
            Assert.Equal("0", index.Get(0).Values[2]);
            Assert.Equal("90", index.Get(1).Values[2]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task FuseValuesComeFromTheFileAndRespectIndependentQuantityAndSourceStates()
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.FuseV2.csv");
        try
        {
            await File.WriteAllTextAsync(path,
                "recording_id;out_t;epoch;speed_mps;speed_state;pitch_deg;body_attitude_state;acc_long_mps2;acceleration_state;obd_speed_mps;obd_speed_state\n"
                + "r;2026-10-01T12:00:00.010Z;1;10;Supported;70;Unavailable;1;Supported;50;Stale\n"
                + "r;2026-10-01T12:00:00.110Z;1;20;Supported;5;Supported;900;Unavailable;12;Valid\n"
                + "r;2026-10-01T12:00:00.210Z;2;30;Supported;8;Supported;3;Supported;30;Valid\n",
                TestContext.Current.CancellationToken);
            using V2RecordingTimelineIndex index = await V2RecordingTimelineIndex.BuildAsync(
                path, 1000, TestContext.Current.CancellationToken);
            Assert.False(index.IsRaw);
            Assert.Equal(2, index.Count);
            V2TimelineRow first = index.Get(0);
            Assert.Equal("15", first.Values[3]);
            Assert.Equal("5", first.Values[5]);
            Assert.Equal("1", first.Values[7]);
            Assert.Equal("12", first.Values[9]);
            Assert.Equal(2, first.LastSourceRow);
            Assert.Equal("30", index.Get(1).Values[3]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("r;2026-10-01T12:00:00.010Z;1\nr;2026-10-01T12:00:00.009Z;2\n")]
    [InlineData("r;invalid;1\n")]
    [InlineData("r;2026-10-01T12:00:00.010Z\n")]
    public async Task MalformedOrRegressingDataIsReportedInsteadOfProducingASuccessfulIndex(string rows)
    {
        string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.RawV2.csv");
        try
        {
            await File.WriteAllTextAsync(path, "recording_id;capture_utc;accel_x_g\n" + rows,
                TestContext.Current.CancellationToken);
            await Assert.ThrowsAsync<InvalidDataException>(
                () => V2RecordingTimelineIndex.BuildAsync(path, 50, TestContext.Current.CancellationToken));
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => V2RecordingTimelineIndex.BuildAsync(path, 50, cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
