using Sensored;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Bluetooth;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.DirectX.Gauges;

namespace Sensored.Tests;

/// <summary>Checks OBD freshness, instrument units, cross-source trip integration and exact journal replay.</summary>
[Collection(nameof(SensoredUiTestCollection))]
public sealed class SensoredObdTests
{
    private static readonly DateTimeOffset Origin = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void BatterySharesTheFuelInstrumentButRetainsIndependentFreshnessAndConnectionState()
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using ObdTelemetryView view = new();
            TableLayoutPanel layout = Assert.IsType<TableLayoutPanel>(Assert.Single(view.Controls.Cast<Control>()));
            D2DVehicleGaugePanel gauges = Assert.IsType<D2DVehicleGaugePanel>(Assert.Single(layout.Controls.Cast<Control>()));
            ObdTelemetrySnapshot snapshot = Snapshot(36, Origin);
            snapshot = snapshot with
            {
                Readings = snapshot.Readings with
                {
                    FuelLevel = new(60, Origin, ObdReadingStatus.Valid),
                    Battery = new(42, Origin.AddSeconds(-4), ObdReadingStatus.Valid),
                    CommandedThrottle = new(80, Origin.AddSeconds(-2.5), ObdReadingStatus.Valid),
                    ActualThrottle = new(35, Origin, ObdReadingStatus.Valid)
                }
            };
            view.Present(snapshot, Origin, 6000);
            Assert.Equal(42, gauges.FuelAndBattery.BatteryValue);
            Assert.Equal(60, gauges.FuelAndBattery.Value);
            Assert.Equal(80, gauges.Rpm.SecondaryValue);
            Assert.Equal(35, gauges.Rpm.TertiaryValue);
            view.Present(snapshot, Origin.AddSeconds(2), 6000);
            Assert.True(double.IsNaN(gauges.FuelAndBattery.BatteryValue));
            Assert.Equal(60, gauges.FuelAndBattery.Value);
            Assert.True(double.IsNaN(gauges.Rpm.SecondaryValue));
            Assert.Equal(35, gauges.Rpm.TertiaryValue);
            view.Present(snapshot with { Connected = false }, Origin, 6000);
            Assert.True(double.IsNaN(gauges.FuelAndBattery.BatteryValue));
        });

    [Fact]
    public void AbsentMountControlDoesNotClearARestoredMountButExplicitIdleDoes()
    {
        IFusionEngine engine = FusionEngineFactory.CreateUnavailable();
        ITabletFrameController mounting = Assert.IsAssignableFrom<ITabletFrameController>(engine);
        SessionDownLock frame = new() { RawAccelerationG = new(0, -1, 0), CapturedAt = Origin };
        mounting.ApplySessionDownLock(frame);
        ObdFusionState absent = new(ObdTelemetrySnapshot.Disabled, null, null);
        absent.Apply(engine);
        Assert.Equal(frame, mounting.FrameStatus.Lock);
        (absent with { Phase = TabletFramePhase.Idle }).Apply(engine);
        Assert.False(mounting.HasSessionDownLock);
    }

    [Fact]
    public void EstablishedModeSurvivesConnectionLossWithoutInventingSpeed()
    {
        ObdTelemetrySnapshot connected = Snapshot(36d, Origin);
        Assert.True(connected.ModeEstablished);
        ObdTelemetrySnapshot reconnecting = connected with { Connected = false, HasConnected = true, Searching = true };
        Assert.True(reconnecting.ModeEstablished);
        Assert.Null(reconnecting.Speed);
        Assert.False((reconnecting with { Enabled = false }).ModeEstablished);
        Assert.False(ObdTelemetrySnapshot.Disabled.ModeEstablished);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ThreeInstrumentsRetainReadableLayoutAndRpmMenuAtScaledFonts(float scale)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Form host = D2DTestEnvironment.CreateOffscreenForm();
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, 12f * scale);
            host.Font = font;
            using ObdTelemetryView view = new() { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.ClientSize = new Size((int)(380 * scale), view.GetPreferredSize(Size.Empty).Height);
            host.Show();
            view.Present(Snapshot(0d, Origin), Origin, 6000);
            TableLayoutPanel layout = Assert.IsType<TableLayoutPanel>(Assert.Single(view.Controls.Cast<Control>()));
            D2DVehicleGaugePanel gauges = Assert.IsType<D2DVehicleGaugePanel>(Assert.Single(layout.Controls.Cast<Control>()));
            Assert.True(gauges.TabStop);
            Assert.Equal(4, gauges.CompositionLayers.Count);
            Assert.Empty(gauges.Controls.Cast<Control>());
            Assert.Equal(D2DGaugeKind.Temperature, gauges.Temperature.Kind);
            Assert.Equal(D2DGaugeKind.FuelAndBattery, gauges.FuelAndBattery.Kind);
            Assert.Equal(6000, Assert.Single(gauges.ContextMenuStrip!.Items.OfType<ToolStripMenuItem>(),
                item => item.Checked).Tag);
            Assert.All(layout.Controls.Cast<Control>(),
                control => Assert.True(layout.ClientRectangle.Contains(control.Bounds)));
            using Bitmap image = new(view.Width, view.Height);
            view.DrawToBitmap(image, view.ClientRectangle);
            using D2DSpeedometer speedometer = new();
            speedometer.SetTripKilometers(12.5d);
            speedometer.SetVehicleSpeed(20d, Origin);
            Assert.Equal(12.5d, speedometer.TripKilometers);
            speedometer.SetSpeed(10d, WarpToolkit.Sensors.Presentation.SpeedUnit.MetersPerSecond, Origin.AddSeconds(1));
            Assert.Equal(12.5d, speedometer.TripKilometers);
            speedometer.SetTripKilometers(null);
        });

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void ThreeInstrumentsFitShortSlotsWithoutVerticalScrolling(float scale)
        => D2DTestEnvironment.RunOnSta(() =>
        {
            using Form host = D2DTestEnvironment.CreateOffscreenForm();
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, 12f * scale);
            host.Font = font;
            using ObdTelemetryView view = new() { Dock = DockStyle.Fill };
            host.Controls.Add(view);
            host.ClientSize = new Size((int)(360 * scale), (int)(240 * scale));
            host.Show();
            host.PerformLayout();
            TableLayoutPanel layout = Assert.IsType<TableLayoutPanel>(Assert.Single(view.Controls.Cast<Control>()));
            D2DVehicleGaugePanel gauges = Assert.IsType<D2DVehicleGaugePanel>(Assert.Single(layout.Controls.Cast<Control>()));
            Assert.False(view.VerticalScroll.Visible);
            Assert.True(view.ClientRectangle.Contains(layout.Bounds));
            Assert.True(layout.ClientRectangle.Contains(gauges.Bounds));
            foreach (global::WarpToolkit.WinForms.DirectX.Controls.Compositing.DCompLayer visual in gauges.CompositionLayers.Skip(1))
            {
                Assert.True(gauges.ClientRectangle.Contains(visual.Bounds));
                Assert.True(visual.Bounds.Height > 0);
            }
        });

    [Fact]
    public void SpeedFreshnessIncludesZeroAndRejectsFutureStaleAndDisconnectedReadings()
    {
        ObdTelemetrySnapshot snapshot = Snapshot(0d, Origin);
        VehicleSpeedMeasurement speed = Assert.IsType<VehicleSpeedMeasurement>(snapshot.Speed);
        Assert.Equal(0d, speed.MetersPerSecond);
        Assert.True(speed.IsFreshAt(Origin.AddMilliseconds(300)));
        Assert.False(speed.IsFreshAt(Origin.AddMilliseconds(300).AddTicks(1)));
        Assert.False(speed.IsFreshAt(Origin.AddTicks(-1)));
        Assert.Null((snapshot with { Connected = false }).Speed);
        Assert.Null((snapshot with { Enabled = false }).Speed);
        Assert.True(double.IsNaN(ObdTelemetryView.ReadMetric(
            new(10d, Origin, ObdReadingStatus.Valid), true, Origin.AddSeconds(6d), 5d)));
    }

    [Fact]
    public void TripUsesMeasurementTimeAcrossSourceSwitchesAndNeverIntegratesMissingIntervals()
    {
        VehicleTripAccumulator trip = new();
        trip.Observe(Origin, 10d);
        trip.Observe(Origin.AddSeconds(1d), 10d);
        trip.Observe(Origin.AddSeconds(2d), 20d);
        trip.Observe(Origin.AddSeconds(2d), 100d);
        trip.Observe(Origin.AddSeconds(1d), 100d);
        Assert.Equal(0.025d, trip.Kilometers, 12);
        trip.Observe(Origin.AddSeconds(3d), double.NaN);
        trip.Observe(Origin.AddSeconds(4d), 20d);
        trip.Observe(Origin.AddSeconds(10d), 20d);
        Assert.Equal(0.025d, trip.Kilometers, 12);
        trip.Observe(Origin.AddSeconds(11d), 20d);
        Assert.Equal(0.045d, trip.Kilometers, 12);
    }

    [Fact]
    public async Task JournalRoundTripsExactEstimatesIncludingMidSessionWarmupAndDuplicateTimestamps()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"Sensored-replay-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            await using ObdRecordingCompanion journal = new();
            IFusionEngine live = FusionEngineFactory.CreateDefault(
                allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);
            ITabletFrameController mounting = Assert.IsAssignableFrom<ITabletFrameController>(live);
            IVehicleSpeedConsumer vehicle = Assert.IsAssignableFrom<IVehicleSpeedConsumer>(live);
            mounting.BeginTabletFrameCapture();
            List<FusionEstimate> expected = [];
            List<SensorEnvelope> inputs = [];
            for (int index = 0; index < 1000; index++)
            {
                DateTimeOffset time = Origin.AddMilliseconds(index / 2 * 40);
                SensorEnvelope input = Imu(index, time);
                inputs.Add(input);
                ObdTelemetrySnapshot telemetry = index is > 300 and < 400
                    ? ObdTelemetrySnapshot.Disabled : Snapshot(index < 250 ? 0d : 36d, time);
                ObdFusionState state = new(telemetry, mounting.FrameStatus.Lock, mounting.FrameStatus.Phase);
                vehicle.SetVehicleSpeed(telemetry.Speed);
                journal.Publish(input, state);
                FusionEstimate estimate = live.Process(input)!;
                expected.Add(estimate);
            }

            await journal.ExportAsync(folder, TestContext.Current.CancellationToken);
            using ObdReplayCursor cursor = Assert.IsType<ObdReplayCursor>(ObdReplayCursor.Open(folder));
            IFusionEngine replay = FusionEngineFactory.CreateDefault(
                allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);
            int count = 0;
            while (cursor.ReadNext() is { } recorded)
            {
                recorded.State.Apply(replay);
                FusionEstimate actual = replay.Process(recorded.Input)!;
                Assert.Equal(expected[count], actual);
                Assert.Equal(count, recorded.Input.Sequence);
                count++;
            }

            Assert.Equal(1000, count);

            foreach (int start in new[] { 500, 751 })
            {
                using ObdReplayCursor seek = Assert.IsType<ObdReplayCursor>(ObdReplayCursor.Open(folder));
                IFusionEngine sought = FusionEngineFactory.CreateDefault(
                    allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);
                int warmed = 0;
                for (int index = start; index < 1000; index++)
                {
                    ObdRecordedInput recorded = seek.Resolve(inputs[index], start, sought, _ => warmed++);
                    Assert.Equal(index, recorded.Input.Sequence);
                    recorded.State.Apply(sought);
                    FusionEstimate actual = sought.Process(recorded.Input)!;
                    Assert.Equal(expected[index], actual);
                }

                Assert.Equal(start, warmed);
            }
        }
        finally
        {
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName));
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName + ".writing"));
            Directory.Delete(folder);
        }
    }

    [Theory]
    [InlineData(0, 9)]
    [InlineData(5000, 10)]
    [InlineData(5100, 11)]
    [InlineData(5101, 13)]
    [InlineData(5201, long.MaxValue)]
    public async Task SeekUsesMonotonicReplayArrivalDespiteDuplicateSourceTimestamps(int milliseconds, long expected)
    {
        Assert.Equal(expected, await ObdReplayCursor.FindMinimumSequenceAsync(
            Rows(), TimeSpan.FromMilliseconds(milliseconds)));

        static async IAsyncEnumerable<SensorEnvelopeRow> Rows()
        {
            await Task.Yield();
            yield return new(0, Imu(9, Origin) with { Kind = SensorDataKind.Health, ArrivalTimestamp = Origin.AddSeconds(-5) });
            yield return new(1, Imu(10, Origin) with { ArrivalTimestamp = Origin });
            yield return new(2, Imu(11, Origin) with { ArrivalTimestamp = Origin.AddMilliseconds(100) });
            yield return new(3, Imu(12, Origin) with { ArrivalTimestamp = Origin.AddMilliseconds(90) });
            yield return new(4, Imu(13, Origin) with { ArrivalTimestamp = Origin.AddMilliseconds(200) });
        }
    }

    [Fact]
    public void OldRecordingsHaveNoObdAndIncompleteCompanionsFailVisibly()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"Sensored-incomplete-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        string pending = Path.Combine(folder, ObdRecordingCompanion.FileName + ".pending");
        try
        {
            Assert.Null(ObdReplayCursor.Open(folder));
            File.WriteAllText(pending, string.Empty);
            Assert.Throws<InvalidDataException>(() => ObdReplayCursor.Open(folder));
            File.Delete(pending);
            File.WriteAllText(Path.Combine(folder, ObdRecordingCompanion.FileName), "bad header");
            Assert.Throws<InvalidDataException>(() => ObdReplayCursor.Open(folder));
        }
        finally
        {
            File.Delete(pending);
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName));
            Directory.Delete(folder);
        }
    }

    private static ObdTelemetrySnapshot Snapshot(double kilometersPerHour, DateTimeOffset time)
        => new(true, true, new(new(kilometersPerHour, time, ObdReadingStatus.Valid, "7E8"),
            ObdMetric.Pending, ObdMetric.Pending, ObdMetric.Pending), "OBD2 connected");

    private static SensorEnvelope Imu(long sequence, DateTimeOffset time) => new()
    {
        Sequence = sequence,
        SourceTimestamp = time,
        ArrivalTimestamp = time.AddMilliseconds(7d),
        Kind = SensorDataKind.Imu,
        Imu = new(new(0d, -1d, 0d), Vec3.Zero, true, double.NaN, 0d,
            double.NaN, double.NaN, double.NaN, DeviceYawSource.None)
    };
}
