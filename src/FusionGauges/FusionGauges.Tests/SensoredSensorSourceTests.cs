using System.Text.Json;
using Sensored;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.Windows.Bluetooth;
using WarpToolkit.WinForms.AppServices;

namespace FusionGauges.Tests;

/// <summary>Verifies independent horizon sources, rigid references and causal comparison metadata.</summary>
public sealed class SensoredSensorSourceTests
{
    [Fact]
    public void SelectingBluetoothUsesInclinometerEvenWhenForwardAccelerationSuggestsADifferentTilt()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Wt901Reading bluetooth = External(now) with { AccelerationX = 0.6, Roll = -7, Pitch = 12 };
        HorizonSourceModel model = new();
        model.ObserveBluetooth(bluetooth, 0);
        HorizonSourceReading reading = model.Resolve(
            HorizonSourceModel.PreferredMode(SensorDisplaySource.Bluetooth, default),
            Estimate(now), Internal(), true, new(true, false, "connected", bluetooth),
            now, 200, default, default);
        Assert.Equal(-7, reading.RollDegrees, 8);
        Assert.Equal(12, reading.PitchDegrees, 8);
        Assert.Equal("Bluetooth inclinometer", reading.AttitudeSource);
    }

    [Fact]
    public void BluetoothHasNoAssumedQuarterTurnAndSavedMountsRemainValid()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-mount-options-{Guid.NewGuid():N}");
        string path = Path.Combine(folder, "settings.json");
        try
        {
            using WinFormsUserSettingsService settings = new(new()
            {
                SettingsFileProvider = () => new FileInfo(path)
            });
            settings.Set(DemoSettings.BluetoothSensorFrameKey, default(SensorReferenceFrame));
            DemoOptions migrated = DemoSettings.Load(settings, DemoOptions.Default);
            Assert.Equal(SensorReferenceFrame.BluetoothDefault, migrated.BluetoothSensorFrame);
            Assert.Equal(default, migrated.InternalSensorFrame);
            Assert.Equal(default, DemoOptions.Default.BluetoothSensorFrame);
            DemoSettings.Save(settings, migrated with { BluetoothSensorFrame = new(0, 0, -90) });
            Assert.Equal(new SensorReferenceFrame(0, 0, -90),
                DemoSettings.Load(settings, DemoOptions.Default).BluetoothSensorFrame);
            DemoSettings.Save(settings, migrated with { BluetoothSensorFrame = default });
            Assert.Equal(default, DemoSettings.Load(settings, DemoOptions.Default).BluetoothSensorFrame);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                File.Delete(path);
                Directory.Delete(folder);
            }
        }
    }

    [Theory]
    [InlineData(0, 3, 4, 200)]
    [InlineData(1, 3, 4, 60)]
    [InlineData(2, 0, 0, 200)]
    [InlineData(3, 8, 15, 200)]
    [InlineData(4, 0, 0, 200)]
    [InlineData(5, 10, 20, 200)]
    [InlineData(6, 10, 20, 120)]
    public void SevenModesUseOnlyTheirDeclaredAttitudeAndHeading(
        int mode, double roll, double pitch, double heading)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        RawImuPayload local = Internal();
        Wt901Reading bluetooth = External(now);
        HorizonSourceModel model = new();
        model.ObserveInternal(local, now, 0);
        model.ObserveBluetooth(bluetooth, 0);
        HorizonSourceReading actual = model.Resolve((HorizonSourceMode)mode, Estimate(now), local, true,
            new(true, false, "connected", bluetooth), now, 200, default, default);
        Assert.Equal(roll, actual.RollDegrees, 8);
        Assert.Equal(pitch, actual.PitchDegrees, 8);
        Assert.Equal(heading, actual.HeadingDegrees, 8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void MissingGpsFallsBackToInternalCompassNotBluetoothYaw(int mode)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HorizonSourceModel model = new();
        model.ObserveInternal(Internal(), now, 0);
        model.ObserveBluetooth(External(now), 0);
        HorizonSourceReading reading = model.Resolve((HorizonSourceMode)mode, Estimate(now), Internal(), true,
            new(true, false, "connected", External(now)), now, double.NaN, default, default);
        Assert.Equal(60, reading.HeadingDegrees, 8);
        Assert.Equal("Internal compass", reading.HeadingSource);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    public void BluetoothModesPreferGpsThenTheirOwnSensorCollectionCompass(int mode)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HorizonSourceModel model = new();
        Wt901Reading bluetooth = External(now) with { Roll = 0, Pitch = 0 };
        model.ObserveBluetooth(bluetooth, 0);
        HorizonSourceReading reading = model.Resolve((HorizonSourceMode)mode, Estimate(now), Internal(), true,
            new(true, false, "connected", bluetooth), now, double.NaN, new(5, 0, 0), new(10, 0, 0));
        Assert.Equal(110, reading.HeadingDegrees, 8);
        Assert.Equal("Bluetooth device compass/yaw", reading.HeadingSource);
    }

    [Fact]
    public void MissingOrStaleBluetoothNeverDisplaysPlausibleZeroOrInternalFallback()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HorizonSourceModel model = new();
        Wt901Reading old = External(now.AddSeconds(-3));
        model.ObserveBluetooth(old, 0);
        foreach (HorizonSourceMode mode in new[]
        {
            HorizonSourceMode.BluetoothGravityGps, HorizonSourceMode.BluetoothInclinometerGps,
            HorizonSourceMode.BluetoothCompass
        })
        {
            HorizonSourceReading reading = model.Resolve(mode, Estimate(now), Internal(), true,
                new(true, false, "connected", old), now, 200, default, default);
            Assert.False(reading.HasAttitude);
            Assert.True(double.IsNaN(reading.RollDegrees));
            Assert.Equal(mode == HorizonSourceMode.BluetoothCompass ? double.NaN : 200, reading.HeadingDegrees);
        }
    }

    [Fact]
    public void InternalCompassCanRemainAvailableWithoutInclinometerAndWithoutGps()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HorizonSourceModel model = new();
        RawImuPayload raw = Internal() with { AbsoluteRollDegrees = double.NaN, AbsolutePitchDegrees = double.NaN };
        HorizonSourceReading reading = model.Resolve(HorizonSourceMode.InternalInclinometerGps, null, raw,
            true, null, now, double.NaN, new(10, 0, 0), default);
        Assert.False(reading.HasAttitude);
        Assert.Equal(50, reading.HeadingDegrees, 8);
        reading = model.Resolve(HorizonSourceMode.FusionCompass, Estimate(now), raw, false,
            null, now, double.NaN, default, default);
        Assert.False(reading.HasAttitude);
        Assert.True(double.IsNaN(reading.HeadingDegrees));
    }

    [Fact]
    public void ReferenceFramesComposeRigidlyAndZeroResetRestoresIdentity()
    {
        SensorReferenceFrame reference = new(70, 25, -30);
        SensorAttitude zero = reference.Correct(-30, 25, 70);
        Assert.Equal(0, zero.RollDegrees, 8);
        Assert.Equal(0, zero.PitchDegrees, 8);
        Assert.True(Math.Abs(GaugeGeometry.ShortestAngleDegrees(0, zero.HeadingDegrees)) < 1e-8);
        SensorAttitude tilted = reference.Correct(-10, 40, 100);
        Assert.True(Math.Abs(tilted.RollDegrees - 20) > 1);
        SensorAttitude identity = default(SensorReferenceFrame).Correct(-10, 40, 100);
        Assert.Equal(-10, identity.RollDegrees, 8);
        Assert.Equal(40, identity.PitchDegrees, 8);
        Assert.Equal(100, identity.HeadingDegrees, 8);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void BluetoothAxisSwapExchangesCorrectedTiltWithoutChangingHeadingOrRawData(int mode)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        const double Roll = -25d;
        const double Pitch = 30d;
        double r = Roll * Math.PI / 180d;
        double p = Pitch * Math.PI / 180d;
        Wt901Reading bluetooth = External(now) with
        {
            AccelerationX = -Math.Sin(p),
            AccelerationY = Math.Sin(r) * Math.Cos(p),
            AccelerationZ = Math.Cos(r) * Math.Cos(p),
            Roll = Roll,
            Pitch = Pitch
        };
        HorizonSourceModel model = new();
        model.ObserveBluetooth(bluetooth, 0);
        BluetoothSensorSnapshot snapshot = new(true, false, "connected", bluetooth);
        SensorReferenceFrame reference = new(10, -5, 15);
        HorizonSourceReading normal = model.Resolve((HorizonSourceMode)mode, null, null, false,
            snapshot, now, double.NaN, default, reference);
        HorizonSourceReading swapped = model.Resolve((HorizonSourceMode)mode, null, null, false,
            snapshot, now, double.NaN, default, reference with { SwapPitchAndRoll = true });
        Assert.Equal(normal.PitchDegrees, swapped.RollDegrees, 8);
        Assert.Equal(normal.RollDegrees, swapped.PitchDegrees, 8);
        Assert.Equal(normal.HeadingDegrees, swapped.HeadingDegrees);
        Assert.Equal(Roll, bluetooth.Roll);
        Assert.Equal(Pitch, bluetooth.Pitch);
    }

    [Fact]
    public void LegacyReferenceJsonDefaultsToUnswappedAxes()
    {
        SensorReferenceFrame reference = JsonSerializer.Deserialize<SensorReferenceFrame>(
            """{"YawDegrees":10,"PitchDegrees":20,"RollDegrees":30}""");
        Assert.Equal(new SensorReferenceFrame(10, 20, 30), reference);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    public void HeadingZeroCannotRotateRollIntoPitch(double headingZero)
    {
        SensorReferenceFrame reference = new(headingZero, 0, 0);
        SensorAttitude rolled = reference.Correct(15, 0, 120);
        SensorAttitude pitched = reference.Correct(0, 20, 120);
        Assert.Equal(15, rolled.RollDegrees, 8);
        Assert.Equal(0, rolled.PitchDegrees, 8);
        Assert.Equal(0, pitched.RollDegrees, 8);
        Assert.Equal(20, pitched.PitchDegrees, 8);
        Assert.Equal(GaugeGeometry.NormalizeDegrees(120 - headingZero), rolled.HeadingDegrees, 8);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BluetoothCaptureZerosTheAttitudePathActuallyDisplayed(bool deviceAttitude)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Wt901Reading bluetooth = External(now) with
        {
            AccelerationX = -0.3,
            AccelerationY = 0.8,
            AccelerationZ = 0.5,
            Roll = -74,
            Pitch = 25,
            Yaw = 90
        };
        SensorReferenceSample capture = SensorReferenceSample.FromBluetooth(bluetooth, deviceAttitude);
        SensorReferenceFrame reference = new(capture.HeadingDegrees, capture.PitchDegrees, capture.RollDegrees);
        HorizonSourceModel model = new();
        model.ObserveBluetooth(bluetooth, 0);
        HorizonSourceReading reading = model.Resolve(
            deviceAttitude ? HorizonSourceMode.BluetoothInclinometerGps : HorizonSourceMode.BluetoothGravityGps,
            null, null, false, new(true, false, "connected", bluetooth), now, double.NaN, default, reference);
        Assert.Equal(0, reading.RollDegrees, 8);
        Assert.Equal(0, reading.PitchDegrees, 8);
        Assert.Equal(0, GaugeGeometry.ShortestAngleDegrees(0, reading.HeadingDegrees), 8);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void LevelBluetoothWithDefaultReferenceHasNoQuarterTurn(int mode)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Wt901Reading bluetooth = External(now) with { Roll = 0, Pitch = 0 };
        HorizonSourceModel model = new();
        model.ObserveBluetooth(bluetooth, 0);
        HorizonSourceReading reading = model.Resolve((HorizonSourceMode)mode, null, null, false,
            new(true, false, "connected", bluetooth), now, double.NaN, default, DemoOptions.Default.BluetoothSensorFrame);
        Assert.Equal(0, reading.RollDegrees, 8);
        Assert.Equal(0, reading.PitchDegrees, 8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(180)]
    [InlineData(270)]
    [InlineData(double.NaN)]
    public void SensorTiltReferenceIsIndependentOfChangingOrMissingCompassHeading(double heading)
    {
        SensorReferenceFrame reference = new(70, 25, -30);
        SensorAttitude level = reference.Correct(-30, 25, heading);
        Assert.Equal(0, level.RollDegrees, 8);
        Assert.Equal(0, level.PitchDegrees, 8);
        SensorAttitude tilted = reference.Correct(-10, 40, heading);
        SensorAttitude baseline = reference.Correct(-10, 40, 0);
        Assert.Equal(baseline.RollDegrees, tilted.RollDegrees, 8);
        Assert.Equal(baseline.PitchDegrees, tilted.PitchDegrees, 8);
    }

    [Theory]
    [InlineData(-25, 30)]
    [InlineData(20, -40)]
    public void BluetoothGravityAndDeviceAttitudeUseTheSameTiltConvention(double roll, double pitch)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        double p = pitch * Math.PI / 180;
        double r = roll * Math.PI / 180;
        Wt901Reading bluetooth = External(now) with
        {
            AccelerationX = -Math.Sin(p),
            AccelerationY = Math.Sin(r) * Math.Cos(p),
            AccelerationZ = Math.Cos(r) * Math.Cos(p),
            Roll = roll,
            Pitch = pitch
        };
        HorizonSourceModel model = new();
        model.ObserveBluetooth(bluetooth, 0);
        BluetoothSensorSnapshot snapshot = new(true, false, "connected", bluetooth);
        HorizonSourceReading gravity = model.Resolve(HorizonSourceMode.BluetoothGravityGps, null,
            null, false, snapshot, now, double.NaN, default, default);
        HorizonSourceReading inclination = model.Resolve(HorizonSourceMode.BluetoothInclinometerGps, null,
            null, false, snapshot, now, double.NaN, default, default);
        Assert.Equal(inclination.RollDegrees, gravity.RollDegrees, 8);
        Assert.Equal(inclination.PitchDegrees, gravity.PitchDegrees, 8);
    }

    [Fact]
    public void RawGravityIsDampedAsAVectorUsingDistinctMeasurementTimes()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        HorizonSourceModel model = new();
        model.ObserveInternal(Internal(), now, 100);
        RawImuPayload upright = Internal() with { Acceleration = new(0, -1, 0) };
        model.ObserveInternal(upright, now.AddMilliseconds(100), 100);
        HorizonSourceReading reading = model.Resolve(HorizonSourceMode.InternalGravityGps, null,
            upright, true, null, now.AddMilliseconds(100), 200, default, default);
        double expected = Math.Atan2(1 - Math.Exp(-1), Math.Exp(-1)) * 180 / Math.PI;
        Assert.Equal(expected, reading.PitchDegrees, 8);
        model.ObserveInternal(upright, now.AddMilliseconds(100), 100);
        Assert.Equal(reading, model.Resolve(HorizonSourceMode.InternalGravityGps, null,
            upright, true, null, now.AddMilliseconds(100), 200, default, default));
    }

    [Fact]
    public void SourceTimeSpeedDerivativeIgnoresRepeatedFramesAndBreaksAtOutages()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SensorReferenceSpeed derivative = new();
        double? acceleration = null;
        for (int sample = 0; sample < 4; sample++)
        {
            VehicleSpeedMeasurement speed = new(10 + sample * 0.2, now.AddMilliseconds(sample * 100));
            for (int frame = 0; frame < 5; frame++)
            {
                acceleration = derivative.Observe(speed, speed.Timestamp.AddMilliseconds(frame));
            }
        }

        Assert.Equal(2, acceleration!.Value, 8);
        Assert.Null(derivative.Observe(new(10, now), now.AddSeconds(1)));
        Assert.Null(derivative.Observe(new(20, now.AddSeconds(2)), now.AddSeconds(2)));
    }

    [Fact]
    public void ComparisonMetadataRoundTripsButCannotInfluenceFusion()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SensorComparisonReference reference = new(SensorDisplaySource.Bluetooth,
            new(true, false, "connected", External(now)), new(10, 20, 30), new(40, 50, 60, true), 10, now, 2);
        ObdFusionState state = new(ObdTelemetrySnapshot.Disabled, null, null) { Comparison = reference };
        ObdFusionState restored = JsonSerializer.Deserialize<ObdFusionState>(
            JsonSerializer.Serialize(state, ObdRecordingCompanion.Json), ObdRecordingCompanion.Json)!;
        Assert.Equal(reference, restored.Comparison);
        IFusionEngine withReference = DemoOptions.Default.CreateFusionEngine(isReplay: false);
        IFusionEngine withoutReference = DemoOptions.Default.CreateFusionEngine(isReplay: false);
        restored.Apply(withReference);
        (state with { Comparison = null }).Apply(withoutReference);
        for (int index = 0; index < 5; index++)
        {
            SensorEnvelope input = new()
            {
                Kind = SensorDataKind.Imu,
                Sequence = index,
                SourceTimestamp = now.AddMilliseconds(index * 20),
                ArrivalTimestamp = now.AddMilliseconds(index * 20),
                Imu = Internal()
            };
            Assert.Equal(withoutReference.Process(input), withReference.Process(input));
        }
    }

    [Fact]
    public void SensorFramesSphereAndAlwaysOnChoicesPersistIndependently()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-sensor-options-{Guid.NewGuid():N}");
        string path = Path.Combine(folder, "settings.json");
        try
        {
            DemoOptions options = DemoOptions.Default with
            {
                SensorSource = SensorDisplaySource.Bluetooth,
                BluetoothSensorSearchMinutes = 2,
                InternalSensorFrame = new(10, 20, 30),
                BluetoothSensorFrame = new(-40, -50, -60, true),
                UseSphericalHorizon = false,
                AlwaysOn = false
            };
            using (WinFormsUserSettingsService settings = new(new()
            {
                SettingsFileProvider = () => new FileInfo(path)
            }))
            {
                DemoSettings.Save(settings, options);
                settings.Flush();
            }

            using WinFormsUserSettingsService restarted = new(new()
            {
                SettingsFileProvider = () => new FileInfo(path)
            });
            DemoOptions restored = DemoSettings.Load(restarted, DemoOptions.Default);
            Assert.Equal(options.SensorSource, restored.SensorSource);
            Assert.Equal(options.BluetoothSensorSearchMinutes, restored.BluetoothSensorSearchMinutes);
            Assert.Equal(options.InternalSensorFrame, restored.InternalSensorFrame);
            Assert.Equal(options.BluetoothSensorFrame, restored.BluetoothSensorFrame);
            Assert.False(restored.UseSphericalHorizon);
            Assert.False(restored.AlwaysOn);
            DemoSettings.Save(restarted, restored with { BluetoothSensorFrame = default });
            Assert.Equal(default, DemoSettings.Load(restarted, DemoOptions.Default).BluetoothSensorFrame);
            Assert.Equal(options.InternalSensorFrame, DemoSettings.Load(restarted, DemoOptions.Default).InternalSensorFrame);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    private static RawImuPayload Internal()
        => new(new(0, 0, -1), Vec3.Zero, true, 60, 1, 15, 8, 60, DeviceYawSource.WindowsFusedOrientation);

    private static Wt901Reading External(DateTimeOffset now)
        => new(now, 0, 0, 1, 0, 0, 0, 10, 20, 120);

    private static FusionEstimate Estimate(DateTimeOffset now)
        => new() { Sequence = 1, Timestamp = now, Roll = 3, Pitch = 4, Validity = FusionOutputValidity.Attitude };
}
