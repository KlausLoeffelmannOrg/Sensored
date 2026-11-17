using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Applies deterministic exponential damping to recorded IMU and GPS measurements.
/// </summary>
internal sealed class SensorDataDamper
{
    private readonly ExponentialValueDamper _accelerationX;
    private readonly ExponentialValueDamper _accelerationY;
    private readonly ExponentialValueDamper _accelerationZ;
    private readonly ExponentialValueDamper _angularRateX;
    private readonly ExponentialValueDamper _angularRateY;
    private readonly ExponentialValueDamper _angularRateZ;
    private readonly ExponentialValueDamper _pitch;
    private readonly ExponentialValueDamper _roll;
    private readonly ExponentialValueDamper _yaw;
    private readonly ExponentialValueDamper _magneticHeading;
    private readonly ExponentialValueDamper _latitude;
    private readonly ExponentialValueDamper _longitude;
    private readonly ExponentialValueDamper _altitude;
    private readonly ExponentialValueDamper _speed;
    private readonly ExponentialValueDamper _course;

    /// <summary>
    ///  Initializes a new damping pipeline.
    /// </summary>
    /// <param name="dampingMilliseconds">The exponential time constant.</param>
    internal SensorDataDamper(int dampingMilliseconds)
    {
        _accelerationX = new(dampingMilliseconds);
        _accelerationY = new(dampingMilliseconds);
        _accelerationZ = new(dampingMilliseconds);
        _angularRateX = new(dampingMilliseconds);
        _angularRateY = new(dampingMilliseconds);
        _angularRateZ = new(dampingMilliseconds);
        _pitch = new(dampingMilliseconds);
        _roll = new(dampingMilliseconds);
        _yaw = new(dampingMilliseconds, circular: true);
        _magneticHeading = new(dampingMilliseconds, circular: true);
        _latitude = new(dampingMilliseconds);
        _longitude = new(dampingMilliseconds, circular: true, signedCircular: true);
        _altitude = new(dampingMilliseconds);
        _speed = new(dampingMilliseconds);
        _course = new(dampingMilliseconds, circular: true);
    }

    /// <summary>
    ///  Returns a row with damped measurement values.
    /// </summary>
    /// <param name="row">The recorded row.</param>
    internal SensorEnvelopeRow Apply(SensorEnvelopeRow row)
    {
        SensorEnvelope envelope = row.Envelope;
        SensorEnvelope dampedEnvelope = envelope.Kind switch
        {
            SensorDataKind.Imu when envelope.Imu is { } imu
                => envelope with { Imu = Dampen(imu, envelope.SourceTimestamp) },
            SensorDataKind.Geolocation when envelope.Geolocation is { } geolocation
                => envelope with
                {
                    Geolocation = Dampen(geolocation, envelope.SourceTimestamp)
                },
            _ => envelope
        };

        return row with { Envelope = dampedEnvelope };
    }

    private RawImuPayload Dampen(RawImuPayload value, DateTimeOffset timestamp)
        => value with
        {
            Acceleration = new Vec3(
                _accelerationX.Apply(value.Acceleration.X, timestamp),
                _accelerationY.Apply(value.Acceleration.Y, timestamp),
                _accelerationZ.Apply(value.Acceleration.Z, timestamp)),
            AngularRate = value.HasAngularRate
                ? new Vec3(
                    _angularRateX.Apply(value.AngularRate.X, timestamp),
                    _angularRateY.Apply(value.AngularRate.Y, timestamp),
                    _angularRateZ.Apply(value.AngularRate.Z, timestamp))
                : value.AngularRate,
            AbsolutePitchDegrees = _pitch.Apply(value.AbsolutePitchDegrees, timestamp),
            AbsoluteRollDegrees = _roll.Apply(value.AbsoluteRollDegrees, timestamp),
            AbsoluteYawDegrees = _yaw.Apply(value.AbsoluteYawDegrees, timestamp),
            MagneticHeadingDegrees = _magneticHeading.Apply(
                value.MagneticHeadingDegrees,
                timestamp)
        };

    private GeolocationPayload Dampen(
        GeolocationPayload value,
        DateTimeOffset timestamp)
        => value with
        {
            LatitudeDegrees = _latitude.Apply(value.LatitudeDegrees, timestamp),
            LongitudeDegrees = _longitude.Apply(value.LongitudeDegrees, timestamp),
            AltitudeMeters = _altitude.Apply(value.AltitudeMeters, timestamp),
            SpeedMetersPerSecond = value.SpeedValid
                ? _speed.Apply(value.SpeedMetersPerSecond, timestamp)
                : value.SpeedMetersPerSecond,
            CourseDegrees = value.CourseValid
                ? _course.Apply(value.CourseDegrees, timestamp)
                : value.CourseDegrees
        };
}

/// <summary>
///  Dampens one scalar or circular measurement using an exponential time constant.
/// </summary>
internal sealed class ExponentialValueDamper(
    int dampingMilliseconds,
    bool circular = false,
    bool signedCircular = false)
{
    private double _value;
    private DateTimeOffset _lastTimestamp;
    private bool _hasValue;

    /// <summary>
    ///  Applies the next measurement.
    /// </summary>
    /// <param name="value">The measurement value.</param>
    /// <param name="timestamp">The measurement timestamp.</param>
    internal double Apply(double value, DateTimeOffset timestamp)
    {
        if (!double.IsFinite(value))
        {
            return value;
        }

        if (!_hasValue || timestamp <= _lastTimestamp)
        {
            _value = value;
        }
        else
        {
            double elapsedMilliseconds = (timestamp - _lastTimestamp).TotalMilliseconds;
            double alpha = 1d - Math.Exp(-elapsedMilliseconds / dampingMilliseconds);
            double delta = circular
                ? NormalizeSignedDegrees(value - _value)
                : value - _value;
            _value += delta * alpha;

            if (circular)
            {
                _value = signedCircular
                    ? NormalizeSignedDegrees(_value)
                    : NormalizeDegrees(_value);
            }
        }

        _hasValue = true;
        _lastTimestamp = timestamp;

        return _value;
    }

    private static double NormalizeSignedDegrees(double value)
    {
        double normalized = NormalizeDegrees(value);

        return normalized > 180d ? normalized - 360d : normalized;
    }

    private static double NormalizeDegrees(double value)
    {
        double normalized = value % 360d;

        return normalized < 0d ? normalized + 360d : normalized;
    }
}
