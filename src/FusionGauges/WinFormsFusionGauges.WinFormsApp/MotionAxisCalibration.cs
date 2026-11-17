using WarpToolkit.Sensors;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>
///  Measures one instructed rotation in the untouched device gyroscope frame.
/// </summary>
internal sealed record MotionAxisCapture(Vec3 Axis, int Samples, double AngleDegrees, double Purity)
{
    internal static readonly TimeSpan CaptureDuration = TimeSpan.FromSeconds(6);

    internal static MotionAxisCapture Analyze(IReadOnlyList<SensorEnvelope> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        List<(Vec3 Rate, double Seconds)> movement = [];
        double[,] covariance = new double[3, 3];
        DateTimeOffset? previous = null;
        int gyroSamples = 0;
        double duration = 0d;

        foreach (SensorEnvelope sample in samples)
        {
            if (sample.Imu is not { HasAngularRate: true } imu)
            {
                continue;
            }

            Vec3 rate = imu.AngularRate;

            if (!double.IsFinite(rate.X) || !double.IsFinite(rate.Y) || !double.IsFinite(rate.Z))
            {
                throw new InvalidDataException("The gyroscope reported a non-finite angular rate.");
            }

            DateTimeOffset time = sample.SourceTimestamp;

            if (previous is not { } before)
            {
                previous = time;

                continue;
            }

            double seconds = (time - before).TotalSeconds;

            if (seconds <= 0d)
            {
                throw new InvalidDataException("Gyroscope capture timestamps did not advance.");
            }

            if (seconds > 0.2d)
            {
                throw new InvalidDataException("The gyroscope capture has a gap longer than 200 ms. Repeat this step.");
            }

            previous = time;
            duration += seconds;
            gyroSamples++;

            if (rate.Dot(rate) < 25d)
            {
                continue;
            }

            movement.Add((rate, seconds));
            double[] components = [rate.X, rate.Y, rate.Z];

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 3; column++)
                {
                    covariance[row, column] += components[row] * components[column] * seconds;
                }
            }
        }

        if (gyroSamples < 30 || duration < 3d || movement.Count < 20)
        {
            throw new InvalidDataException(
                "Not enough fresh gyroscope motion. Rotate back and forth about only the requested axis.");
        }

        int strongest = covariance[1, 1] > covariance[0, 0] ? 1 : 0;
        strongest = covariance[2, 2] > covariance[strongest, strongest] ? 2 : strongest;
        double trace = covariance[0, 0] + covariance[1, 1] + covariance[2, 2];

        if (!double.IsFinite(trace))
        {
            throw new InvalidDataException("The gyroscope capture exceeded the numeric measurement range.");
        }

        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                covariance[row, column] /= trace;
            }
        }

        Vec3 axis = strongest switch
        {
            0 => new(1d, 0d, 0d),
            1 => new(0d, 1d, 0d),
            _ => new(0d, 0d, 1d)
        };

        for (int iteration = 0; iteration < 24; iteration++)
        {
            axis = Multiply(covariance, axis).Normalized();
        }

        double purity = axis.Dot(Multiply(covariance, axis));

        if (purity < 0.9d)
        {
            throw new InvalidDataException("More than one rotation axis was used. Repeat with only the requested motion.");
        }

        double firstStroke = 0d;

        foreach ((Vec3 rate, double seconds) in movement)
        {
            firstStroke += rate.Dot(axis) * seconds;

            if (Math.Abs(firstStroke) >= 5d)
            {
                break;
            }
        }

        if (Math.Abs(firstStroke) < 5d)
        {
            throw new InvalidDataException("The first motion was too small to determine its sign. Make a deliberate first stroke.");
        }

        axis *= Math.Sign(firstStroke);
        double positive = 0d;
        double negative = 0d;

        foreach ((Vec3 rate, double seconds) in movement)
        {
            double angle = rate.Dot(axis) * seconds;
            positive += Math.Max(angle, 0d);
            negative += Math.Max(-angle, 0d);
        }

        if (positive < 10d || negative < 10d)
        {
            throw new InvalidDataException("Rotate in both directions by at least 10 degrees, not just in one direction.");
        }

        return new(axis, gyroSamples, positive + negative, purity);
    }

    private static Vec3 Multiply(double[,] matrix, Vec3 value)
        => new(
            matrix[0, 0] * value.X + matrix[0, 1] * value.Y + matrix[0, 2] * value.Z,
            matrix[1, 0] * value.X + matrix[1, 1] * value.Y + matrix[1, 2] * value.Z,
            matrix[2, 0] * value.X + matrix[2, 1] * value.Y + matrix[2, 2] * value.Z);
}

/// <summary>
///  Holds measured landscape motion axes for diagnosis, without relabelling fused attitude.
/// </summary>
/// <remarks>
///  Signs follow the wizard's first stroke, not an assumed Windows Euler convention.
///  This is not a gravity lock, vehicle mounting calibration, or absolute heading reference.
/// </remarks>
internal sealed record MotionAxisCalibration(
    MotionAxisCapture Pitch, MotionAxisCapture Roll, MotionAxisCapture Yaw)
{
    internal static MotionAxisCalibration Create(
        MotionAxisCapture pitch, MotionAxisCapture roll, MotionAxisCapture yaw)
    {
        ArgumentNullException.ThrowIfNull(pitch);
        ArgumentNullException.ThrowIfNull(roll);
        ArgumentNullException.ThrowIfNull(yaw);

        if (Math.Abs(pitch.Axis.Dot(roll.Axis)) > 0.3d
            || Math.Abs(pitch.Axis.Dot(yaw.Axis)) > 0.3d
            || Math.Abs(roll.Axis.Dot(yaw.Axis)) > 0.3d)
        {
            throw new InvalidDataException(
                "The three measured axes are not independent and near perpendicular. Start over and separate pitch, roll and yaw.");
        }

        return new(pitch, roll, yaw);
    }

    internal (double Pitch, double Roll, double Yaw) Project(Vec3 angularRate)
        => (angularRate.Dot(Pitch.Axis), angularRate.Dot(Roll.Axis), angularRate.Dot(Yaw.Axis));

    internal string Describe()
        => $"MEASURED LANDSCAPE GYROSCOPE AXES (raw device XYZ)\r\n"
            + DescribeAxis("Pitch: top toward you first", Pitch)
            + DescribeAxis("Roll: right edge down first", Roll)
            + DescribeAxis("Yaw: turn right first", Yaw)
            + "Signs identify the instructed first stroke; these are angular rates, not attitude angles.\r\n"
            + "Diagnostic mapping only. Gravity/vehicle fusion and absolute heading are unchanged.";

    private static string DescribeAxis(string name, MotionAxisCapture capture)
        => $"{name}: [{capture.Axis.X:+0.000;-0.000;0.000}, "
            + $"{capture.Axis.Y:+0.000;-0.000;0.000}, {capture.Axis.Z:+0.000;-0.000;0.000}]\r\n"
            + $"  {capture.Samples} gyro samples; {capture.AngleDegrees:F1} deg total motion; "
            + $"{capture.Purity:P1} single-axis energy\r\n";
}
