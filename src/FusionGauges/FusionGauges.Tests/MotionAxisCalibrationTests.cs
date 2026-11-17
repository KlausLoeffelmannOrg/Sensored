using System.Reflection;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;
using WinFormsFusionGauges.WinFormsApp;

namespace FusionGauges.Tests;

/// <summary>
///  Checks motion-axis identification independently of hardware, gravity fusion, and UI timing.
/// </summary>
public sealed class MotionAxisCalibrationTests
{
    [Theory]
    [InlineData(1d, 0d, 0d)]
    [InlineData(-1d, 0d, 0d)]
    [InlineData(0d, 1d, 0d)]
    [InlineData(0d, 0d, -1d)]
    [InlineData(0.2d, -0.8d, 0.5d)]
    public void Back_and_forth_identifies_the_full_vector_and_first_stroke_sign(double x, double y, double z)
    {
        Vec3 expected = new Vec3(x, y, z).Normalized();
        MotionAxisCapture capture = MotionAxisCapture.Analyze(Samples(index =>
            expected * (index % 200 < 100 ? 30d : -30d)));

        Assert.InRange(capture.Axis.Dot(expected), 0.999999d, 1.000001d);
        Assert.InRange(capture.Purity, 0.999999d, 1.000001d);
        Assert.InRange(capture.AngleDegrees, 170d, 181d);
        Assert.Equal(599, capture.Samples);
    }

    [Fact]
    public void Landscape_permutation_maps_pitch_roll_and_yaw_without_assuming_raw_XYZ_labels()
    {
        MotionAxisCapture pitch = Capture(new Vec3(1d, 0d, 0d));
        MotionAxisCapture roll = Capture(new Vec3(0d, 0d, -1d));
        MotionAxisCapture yaw = Capture(new Vec3(0d, 1d, 0d));
        MotionAxisCalibration result = MotionAxisCalibration.Create(pitch, roll, yaw);
        var (pitchRate, rollRate, yawRate) = result.Project(new Vec3(20d, 30d, -10d));

        Assert.Equal(20d, pitchRate, 6);
        Assert.Equal(10d, rollRate, 6);
        Assert.Equal(30d, yawRate, 6);
        Assert.Contains("Diagnostic mapping only", result.Describe());
    }

    [Fact]
    public void Stationary_capture_is_rejected()
        => Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(Samples(_ => Vec3.Zero)));

    [Fact]
    public void One_way_motion_is_rejected()
        => Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(
            Samples(_ => new Vec3(30d, 0d, 0d))));

    [Fact]
    public void Mixed_axis_capture_is_rejected()
        => Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(
            Samples(index => index < 300 ? new Vec3(30d, 0d, 0d) : new Vec3(0d, 30d, 0d))));

    [Fact]
    public void Duplicate_step_axes_are_rejected()
    {
        MotionAxisCapture axis = Capture(new Vec3(1d, 0d, 0d));

        Assert.Throws<InvalidDataException>(() => MotionAxisCalibration.Create(axis, axis, axis));
    }

    [Fact]
    public void Missing_gyro_is_not_treated_as_zero_motion()
    {
        SensorEnvelope[] missing = Samples(_ => new Vec3(30d, 0d, 0d))
            .Select(sample => sample with { Imu = sample.Imu!.Value with { HasAngularRate = false } })
            .ToArray();

        Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(missing));
    }

    [Fact]
    public void Non_finite_gyro_and_regressing_or_gapped_timestamps_are_rejected()
    {
        Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(
            Samples(_ => new Vec3(double.NaN, 0d, 0d))));
        SensorEnvelope[] samples = Samples(index => new Vec3(index % 200 < 100 ? 30d : -30d, 0d, 0d));
        samples[100] = samples[100] with { SourceTimestamp = samples[99].SourceTimestamp };
        Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(samples));
        samples[100] = samples[100] with { SourceTimestamp = samples[99].SourceTimestamp.AddSeconds(1) };
        Assert.Throws<InvalidDataException>(() => MotionAxisCapture.Analyze(samples));
    }

    [Fact]
    public async Task Capture_requires_a_running_session_and_does_not_start_hardware()
    {
        await using SensorHub hub = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            hub.CaptureMotionAxisAsync(TestContext.Current.CancellationToken));
        Assert.False(hub.IsRunning);
    }

    [Fact]
    public async Task Full_rate_capture_and_cancel_leave_the_sensor_session_running()
    {
        CalibrationImuSource imu = new();
        await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("motion-calibration",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
        await hub.StartAsync(new());
        using CancellationTokenSource producerStop = new();
        Task<MotionAxisCapture> capture = hub.CaptureMotionAxisAsync(TestContext.Current.CancellationToken);
        Task producer = Task.Run(async () =>
        {
            DateTimeOffset origin = DateTimeOffset.UtcNow;
            int index = 0;

            while (!producerStop.IsCancellationRequested)
            {
                imu.Emit(origin.AddMilliseconds(index * 10), index % 200 < 100 ? 30d : -30d);
                index++;
                await Task.Delay(10, TestContext.Current.CancellationToken);
            }
        }, TestContext.Current.CancellationToken);

        try
        {
            MotionAxisCapture result = await capture.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.True(result.Samples > 300);
            Assert.InRange(result.Axis.X, 0.999999d, 1.000001d);
            Assert.True(hub.IsRunning);
            Assert.Equal(0, imu.StopCount);
            using CancellationTokenSource cancel = new();
            Task<MotionAxisCapture> cancelledCapture = hub.CaptureMotionAxisAsync(cancel.Token);
            await cancel.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledCapture);
            Assert.True(hub.IsRunning);
            Assert.Equal(0, imu.StopCount);
        }
        finally
        {
            await producerStop.CancelAsync();
            await producer;
            await hub.StopAsync();
        }
    }

    [Fact]
    public async Task Wizard_is_Designer_constructible_and_does_not_accept_an_uncaptured_step()
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() =>
        {
            try
            {
                using MotionCalibrationDialog dialog = new();
                BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                Button next = (Button)typeof(MotionCalibrationDialog).GetField("_next", flags)!.GetValue(dialog)!;
                Label heading = (Label)typeof(MotionCalibrationDialog).GetField("_heading", flags)!.GetValue(dialog)!;
                Assert.False(next.Enabled);
                Assert.Contains("Step 1 of 3: Pitch", heading.Text);
                Assert.Null(dialog.Result);
                MotionAxisCapture?[] captures = (MotionAxisCapture?[])typeof(MotionCalibrationDialog)
                    .GetField("_captures", flags)!.GetValue(dialog)!;
                captures[0] = Capture(new Vec3(1d, 0d, 0d));
                captures[1] = Capture(new Vec3(0d, 0d, -1d));
                captures[2] = Capture(new Vec3(0d, 1d, 0d));
                MethodInfo advance = typeof(MotionCalibrationDialog).GetMethod("OnNext", flags)!;
                advance.Invoke(dialog, [null, EventArgs.Empty]);
                Assert.Contains("Step 2 of 3: Roll", heading.Text);
                advance.Invoke(dialog, [null, EventArgs.Empty]);
                Assert.Contains("Step 3 of 3: Yaw", heading.Text);
                advance.Invoke(dialog, [null, EventArgs.Empty]);
                Assert.NotNull(dialog.Result);
                Assert.Equal("Keep diagnostic mapping", next.Text);
                advance.Invoke(dialog, [null, EventArgs.Empty]);
                Assert.Equal(DialogResult.OK, dialog.DialogResult);
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
    }

    private static MotionAxisCapture Capture(Vec3 axis)
        => MotionAxisCapture.Analyze(Samples(index => axis * (index % 200 < 100 ? 30d : -30d)));

    private static SensorEnvelope[] Samples(Func<int, Vec3> rate)
    {
        DateTimeOffset origin = new(2026, 10, 12, 0, 0, 0, TimeSpan.Zero);

        return Enumerable.Range(0, 600).Select(index => new SensorEnvelope
        {
            Sequence = index,
            Kind = SensorDataKind.Imu,
            SourceTimestamp = origin.AddMilliseconds(index * 10),
            ArrivalTimestamp = origin.AddMilliseconds(index * 10 + 2),
            Imu = new RawImuPayload(
                new Vec3(0d, -1d, 0d), rate(index), true, double.NaN, 0d,
                double.NaN, double.NaN, double.NaN, DeviceYawSource.None)
        }).ToArray();
    }

    /// <summary>Supplies raw gyro rotations without any physical hardware or rendering.</summary>
    private sealed class CalibrationImuSource : IImuSource
    {
        public event EventHandler<ImuSample>? SampleReady;
        public bool IsAccelerometerAvailable => true;
        public bool IsGyroscopeAvailable => true;
        public DateTimeOffset? LastCallbackReceivedUtc { get; private set; }
        public int StopCount { get; private set; }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;

            return Task.CompletedTask;
        }

        internal void Emit(DateTimeOffset timestamp, double rate)
        {
            LastCallbackReceivedUtc = DateTimeOffset.UtcNow;
            SampleReady?.Invoke(this,
                new(new Vec3(0d, -1d, 0d), new Vec3(rate, 0d, 0d), timestamp));
        }
    }
}
