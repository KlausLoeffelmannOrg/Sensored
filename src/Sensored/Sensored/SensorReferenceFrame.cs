namespace Sensored;

/// <summary>Identifies the independent display/reference collection, not the fusion IMU.</summary>
internal enum SensorDisplaySource
{
    /// <summary>Use the internal Windows sensors.</summary>
    Internal,
    /// <summary>Use the paired Bluetooth WT901 collection.</summary>
    Bluetooth
}

/// <summary>Stores the zero attitude and optional display-axis swap; all zeroes are the identity.</summary>
/// <remarks>
///  Angles use forward/right/down axes: yaw clockwise, pitch nose-up, roll right-side-down.
///  Corrections compose rigid rotations, rather than subtracting individual Euler angles.
///  The optional pitch/roll swap applies after mounting correction and leaves heading unchanged.
///  These display references never change the fusion engine's measured installation.
/// </remarks>
internal readonly record struct SensorReferenceFrame(
    double YawDegrees, double PitchDegrees, double RollDegrees, bool SwapPitchAndRoll = false)
{
    /// <summary>Gets the level WT901 reference; non-level installations require calibration.</summary>
    internal static SensorReferenceFrame BluetoothDefault => default;

    internal bool IsFinite
        => double.IsFinite(YawDegrees) && double.IsFinite(PitchDegrees) && double.IsFinite(RollDegrees);

    internal SensorAttitude Correct(double roll, double pitch, double yaw)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(IsFinite, true, nameof(SensorReferenceFrame));
        double[,] current = Rotation(double.IsFinite(yaw) ? yaw : 0d, pitch, roll);
        // Heading zero is not a mounting-axis rotation: applying it on the right
        // would exchange pitch and roll when capturing a compass heading near 90 degrees.
        double[,] reference = Rotation(0d, PitchDegrees, RollDegrees);
        double[,] relative = new double[3, 3];
        for (int row = 0; row < 3; row++)
        {
            for (int column = 0; column < 3; column++)
            {
                for (int axis = 0; axis < 3; axis++)
                {
                    relative[row, column] += current[row, axis] * reference[column, axis];
                }
            }
        }

        const double ToDegrees = 180d / Math.PI;
        double correctedRoll = Math.Atan2(relative[2, 1], relative[2, 2]) * ToDegrees;
        double correctedPitch = Math.Asin(Math.Clamp(-relative[2, 0], -1d, 1d)) * ToDegrees;

        return new(
            SwapPitchAndRoll ? correctedPitch : correctedRoll,
            SwapPitchAndRoll ? correctedRoll : correctedPitch,
            double.IsFinite(yaw)
                ? WarpToolkit.Sensors.Presentation.GaugeGeometry.NormalizeDegrees(
                    Math.Atan2(relative[1, 0], relative[0, 0]) * ToDegrees - YawDegrees)
                : double.NaN);
    }

    private static double[,] Rotation(double yaw, double pitch, double roll)
    {
        const double ToRadians = Math.PI / 180d;
        double cy = Math.Cos(yaw * ToRadians);
        double sy = Math.Sin(yaw * ToRadians);
        double cp = Math.Cos(pitch * ToRadians);
        double sp = Math.Sin(pitch * ToRadians);
        double cr = Math.Cos(roll * ToRadians);
        double sr = Math.Sin(roll * ToRadians);

        return new[,]
        {
            { cy * cp, cy * sp * sr - sy * cr, cy * sp * cr + sy * sr },
            { sy * cp, sy * sp * sr + cy * cr, sy * sp * cr - cy * sr },
            { -sp, cp * sr, cp * cr }
        };
    }
}

/// <summary>Contains one rigidly corrected attitude, with NaN for unavailable heading.</summary>
internal readonly record struct SensorAttitude(double RollDegrees, double PitchDegrees, double HeadingDegrees)
{
    /// <summary>Converts forward/right/down gravity components into the display tilt convention.</summary>
    internal static SensorAttitude FromGravity(WarpToolkit.Sensors.Vec3 down, double heading)
    {
        double magnitude = down.Magnitude;
        if (!double.IsFinite(magnitude) || magnitude < 1e-6d)
        {
            return new(double.NaN, double.NaN, heading);
        }

        const double ToDegrees = 180d / Math.PI;

        return new(
            Math.Atan2(down.Y, down.Z) * ToDegrees,
            Math.Asin(Math.Clamp(-down.X / magnitude, -1d, 1d)) * ToDegrees,
            heading);
    }
}

/// <summary>Contains an unreferenced live pose and Windows-style device gravity for the original pose viewer.</summary>
internal sealed record SensorReferenceSample(
    WarpToolkit.Sensors.Vec3 GravityDevice, double RollDegrees, double PitchDegrees, double HeadingDegrees)
{
    /// <summary>Uses the same Bluetooth tilt input as the selected horizon mode.</summary>
    internal static SensorReferenceSample FromBluetooth(
        WarpToolkit.Windows.Bluetooth.Wt901Reading reading, bool useDeviceAttitude)
    {
        SensorAttitude attitude = useDeviceAttitude
            ? new(reading.Roll, reading.Pitch, reading.Yaw)
            : SensorAttitude.FromGravity(
                new(reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ), reading.Yaw);

        return new(
            new(reading.AccelerationY, reading.AccelerationX, -reading.AccelerationZ),
            attitude.RollDegrees,
            attitude.PitchDegrees,
            attitude.HeadingDegrees);
    }
}
