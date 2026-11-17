using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Defines the seven double-click display modes independently of fusion acquisition.</summary>
internal enum HorizonSourceMode
{
    /// <summary>Fusion attitude with GPS direction, falling back to internal compass.</summary>
    FusionGps,
    /// <summary>Fusion attitude with internal compass direction.</summary>
    FusionCompass,
    /// <summary>Damped internal gravity with GPS direction and internal compass fallback.</summary>
    InternalGravityGps,
    /// <summary>Internal inclinometer with GPS direction and internal compass fallback.</summary>
    InternalInclinometerGps,
    /// <summary>Damped Bluetooth gravity with GPS direction and Bluetooth compass fallback.</summary>
    BluetoothGravityGps,
    /// <summary>Bluetooth device attitude with GPS direction and Bluetooth compass fallback.</summary>
    BluetoothInclinometerGps,
    /// <summary>Bluetooth device attitude and its device-fused compass/yaw.</summary>
    BluetoothCompass
}

/// <summary>Contains selected display values and explicit source provenance.</summary>
internal sealed record HorizonSourceReading(
    double RollDegrees, double PitchDegrees, double HeadingDegrees, string AttitudeSource, string HeadingSource)
{
    internal bool HasAttitude => double.IsFinite(RollDegrees) && double.IsFinite(PitchDegrees);
}

/// <summary>Resolves display-only comparisons while leaving raw acquisition and fusion unchanged.</summary>
internal sealed class HorizonSourceModel
{
    private readonly GravityFilter _internalGravity = new();
    private readonly GravityFilter _bluetoothGravity = new();

    /// <summary>Uses device-fused Bluetooth inclination by default, never moving accelerometer tilt.</summary>
    internal static HorizonSourceMode PreferredMode(SensorDisplaySource source, SensorReferenceFrame internalFrame)
        => source == SensorDisplaySource.Bluetooth
            ? HorizonSourceMode.BluetoothInclinometerGps
            : internalFrame != default ? HorizonSourceMode.InternalGravityGps : HorizonSourceMode.FusionGps;

    internal void ObserveInternal(RawImuPayload sample, DateTimeOffset timestamp, int dampingMilliseconds)
        => _internalGravity.Observe(new(sample.Acceleration.Y, sample.Acceleration.X, -sample.Acceleration.Z),
            timestamp, dampingMilliseconds);

    internal void ObserveBluetooth(Wt901Reading sample, int dampingMilliseconds)
        => _bluetoothGravity.Observe(new(sample.AccelerationX, sample.AccelerationY, sample.AccelerationZ),
            sample.Timestamp, dampingMilliseconds);

    internal void MarkBluetoothUnavailable()
        => _bluetoothGravity.Reset();

    internal void Reset()
    {
        _internalGravity.Reset();
        _bluetoothGravity.Reset();
    }

    internal HorizonSourceReading Resolve(HorizonSourceMode mode, FusionEstimate? fusion,
        RawImuPayload? internalSample, bool internalFresh, BluetoothSensorSnapshot? bluetooth,
        DateTimeOffset now, double gpsCourse, SensorReferenceFrame internalFrame, SensorReferenceFrame bluetoothFrame)
    {
        bool bluetoothFresh = bluetooth?.IsFreshAt(now) == true;
        Wt901Reading? external = bluetoothFresh ? bluetooth!.Reading : null;
        RawImuPayload? local = internalFresh ? internalSample : null;
        double compass = local is { MagneticHeadingConfidence: > 0d } imu
            ? imu.MagneticHeadingDegrees : double.NaN;
        bool gpsAllowed = mode is not (HorizonSourceMode.FusionCompass or HorizonSourceMode.BluetoothCompass);
        double heading = gpsAllowed && double.IsFinite(gpsCourse) ? gpsCourse : compass;
        string headingSource = gpsAllowed && double.IsFinite(gpsCourse) ? "GPS course" : "Internal compass";
        SensorAttitude attitude = new(double.NaN, double.NaN, double.NaN);
        string attitudeSource;
        switch (mode)
        {
            case HorizonSourceMode.FusionGps:
            case HorizonSourceMode.FusionCompass:
                attitudeSource = "Fusion (internal IMU)";
                if (internalFresh && fusion?.IsAttitudeSupported == true)
                {
                    attitude = new(fusion.Roll, fusion.Pitch, double.NaN);
                }

                if (double.IsFinite(compass))
                {
                    compass = GaugeGeometry.NormalizeDegrees(compass - internalFrame.YawDegrees);
                    heading = gpsAllowed && double.IsFinite(gpsCourse) ? gpsCourse : compass;
                }

                break;

            case HorizonSourceMode.InternalGravityGps:
                attitudeSource = "Internal damped raw gravity";
                if (local is not null)
                {
                    attitude = _internalGravity.GetAttitude(internalFrame, compass);
                }

                if (!double.IsFinite(gpsCourse))
                {
                    heading = double.IsFinite(attitude.HeadingDegrees) ? attitude.HeadingDegrees
                        : GaugeGeometry.NormalizeDegrees(compass - internalFrame.YawDegrees);
                }

                break;

            case HorizonSourceMode.InternalInclinometerGps:
                attitudeSource = "Internal inclinometer";
                if (local is { } inclination)
                {
                    attitude = internalFrame.Correct(inclination.AbsoluteRollDegrees,
                        inclination.AbsolutePitchDegrees, compass);
                }

                if (!double.IsFinite(gpsCourse))
                {
                    heading = double.IsFinite(attitude.HeadingDegrees) ? attitude.HeadingDegrees
                        : GaugeGeometry.NormalizeDegrees(compass - internalFrame.YawDegrees);
                }

                break;

            case HorizonSourceMode.BluetoothGravityGps:
                attitudeSource = "Bluetooth damped raw gravity";
                if (external is not null)
                {
                    attitude = _bluetoothGravity.GetAttitude(bluetoothFrame, external.Yaw);
                }

                heading = double.IsFinite(gpsCourse) ? gpsCourse : attitude.HeadingDegrees;
                if (!double.IsFinite(gpsCourse))
                {
                    headingSource = "Bluetooth device compass/yaw";
                }

                break;

            case HorizonSourceMode.BluetoothInclinometerGps:
            case HorizonSourceMode.BluetoothCompass:
                attitudeSource = "Bluetooth inclinometer";
                if (external is not null)
                {
                    attitude = bluetoothFrame.Correct(external.Roll, external.Pitch, external.Yaw);
                }

                if (mode == HorizonSourceMode.BluetoothCompass)
                {
                    heading = attitude.HeadingDegrees;
                    headingSource = "Bluetooth device compass/yaw";
                }
                else
                {
                    heading = double.IsFinite(gpsCourse) ? gpsCourse : attitude.HeadingDegrees;
                    if (!double.IsFinite(gpsCourse))
                    {
                        headingSource = "Bluetooth device compass/yaw";
                    }

                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        if (!double.IsFinite(heading))
        {
            headingSource += " unavailable";
        }

        return new(attitude.RollDegrees, attitude.PitchDegrees, heading, attitudeSource, headingSource);
    }

    /// <summary>Damps original gravity-vector samples before converting them to angles.</summary>
    private sealed class GravityFilter
    {
        private readonly ExponentialDamper _x = new();
        private readonly ExponentialDamper _y = new();
        private readonly ExponentialDamper _z = new();
        private DateTimeOffset? _lastTimestamp;

        internal void Observe(Vec3 down, DateTimeOffset timestamp, int dampingMilliseconds)
        {
            if (timestamp == _lastTimestamp)
            {
                return;
            }

            if (!double.IsFinite(down.Magnitude) || down.Magnitude < 1e-6d)
            {
                Reset();

                return;
            }

            if (_lastTimestamp is { } last && (timestamp < last || timestamp - last > TimeSpan.FromSeconds(2)))
            {
                Reset();
            }

            _x.DampingMilliseconds = _y.DampingMilliseconds = _z.DampingMilliseconds = dampingMilliseconds;
            _x.Update(down.X, timestamp);
            _y.Update(down.Y, timestamp);
            _z.Update(down.Z, timestamp);
            _lastTimestamp = timestamp;
        }

        internal SensorAttitude GetAttitude(SensorReferenceFrame frame, double heading)
        {
            if (!_x.HasValue)
            {
                return new(double.NaN, double.NaN, double.NaN);
            }

            Vec3 down = new(_x.Value, _y.Value, _z.Value);
            SensorAttitude attitude = SensorAttitude.FromGravity(down, heading);

            return frame.Correct(attitude.RollDegrees, attitude.PitchDegrees, attitude.HeadingDegrees);
        }

        internal void Reset()
        {
            _x.Reset();
            _y.Reset();
            _z.Reset();
            _lastTimestamp = null;
        }
    }
}
