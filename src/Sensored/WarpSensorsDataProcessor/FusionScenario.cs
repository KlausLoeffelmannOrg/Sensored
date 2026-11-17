using System.Globalization;
using System.Text.Json;

using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Identifies a deterministic, relative-time input fault applied outside the estimator.
/// </summary>
internal enum FusionFaultType
{
    ImuSpike,
    CompassDisturbance,
    GpsDropout,
    GpsTunnel,
    GpsWithheldFixes
}

/// <summary>
///  One relative-time fault window.
/// </summary>
internal sealed class FusionFault
{
    public FusionFaultType Type { get; init; }

    public double StartSeconds { get; init; }

    public double DurationSeconds { get; init; }

    public string? Axis { get; init; }

    public double Delta { get; init; }

    public double HeadingDeltaDegrees { get; init; }

    public double? Confidence { get; init; }
}

/// <summary>
///  A deterministic scenario applied to a recording copy before the engine sees it.
/// </summary>
internal sealed class FusionScenario
{
    public string Schema { get; init; } = "warp-fusion-scenario";

    public int Version { get; init; } = 1;

    public string Name { get; init; } = "baseline";

    public bool ExportTransformedCopy { get; init; }

    public IReadOnlyList<FusionFault> Faults { get; init; } = [];

    /// <summary>
    ///  Loads a scenario JSON file.
    /// </summary>
    internal static FusionScenario Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Scenario file '{path}' does not exist.", path);
        }

        string json = File.ReadAllText(path);
        FusionScenario scenario = JsonSerializer.Deserialize<FusionScenario>(json, ProcessorJson.Options)
            ?? throw new InvalidOperationException("The scenario file is empty.");

        if (!string.Equals(scenario.Schema, "warp-fusion-scenario", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Unsupported scenario schema '{scenario.Schema}'.");
        }

        if (scenario.Version != 1)
        {
            throw new InvalidOperationException(
                $"Unsupported scenario version {scenario.Version}.");
        }

        if (scenario.Faults is null)
        {
            throw new InvalidOperationException("Scenario faults must be an array.");
        }

        foreach (FusionFault fault in scenario.Faults)
        {
            Validate(fault);
        }

        return scenario;
    }

    /// <summary>
    ///  Returns whether a GPS observation in this window must not reach the engine.
    /// </summary>
    internal bool ShouldWithholdGps(SensorEnvelope envelope, DateTimeOffset origin)
    {
        if (envelope.Kind != SensorDataKind.Geolocation)
        {
            return false;
        }

        double elapsed = (envelope.SourceTimestamp - origin).TotalSeconds;

        foreach (FusionFault fault in Faults)
        {
            if (IsGpsWithholding(fault.Type) && Contains(fault, elapsed))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///  Applies IMU/compass transforms for the elapsed time. GPS withholding is separate.
    /// </summary>
    internal SensorEnvelope Transform(SensorEnvelope envelope, DateTimeOffset origin)
    {
        if (envelope.Kind != SensorDataKind.Imu || envelope.Imu is not { } imu)
        {
            return envelope;
        }

        double elapsed = (envelope.SourceTimestamp - origin).TotalSeconds;
        RawImuPayload transformed = imu;

        foreach (FusionFault fault in Faults)
        {
            if (!Contains(fault, elapsed))
            {
                continue;
            }

            transformed = fault.Type switch
            {
                FusionFaultType.ImuSpike => ApplySpike(transformed, fault),
                FusionFaultType.CompassDisturbance => ApplyCompass(transformed, fault),
                _ => transformed
            };
        }

        return transformed.Equals(imu)
            ? envelope
            : envelope with { Imu = transformed };
    }

    internal static bool IsGpsWithholding(FusionFaultType type)
        => type is FusionFaultType.GpsDropout
            or FusionFaultType.GpsTunnel
            or FusionFaultType.GpsWithheldFixes;

    private static void Validate(FusionFault fault)
    {
        if (!double.IsFinite(fault.StartSeconds) || fault.StartSeconds < 0d)
        {
            throw new InvalidOperationException("Scenario fault startSeconds must be a finite value ≥ 0.");
        }

        if (!double.IsFinite(fault.DurationSeconds) || fault.DurationSeconds <= 0d)
        {
            throw new InvalidOperationException("Scenario fault durationSeconds must be a finite value > 0.");
        }

        if (fault.Type == FusionFaultType.ImuSpike)
        {
            if (ParseAxis(fault.Axis) is null)
            {
                throw new InvalidOperationException("ImuSpike requires axis X, Y, or Z.");
            }

            if (!double.IsFinite(fault.Delta))
            {
                throw new InvalidOperationException("ImuSpike delta must be finite.");
            }
        }

        if (fault.Type == FusionFaultType.CompassDisturbance
            && !double.IsFinite(fault.HeadingDeltaDegrees))
        {
            throw new InvalidOperationException("CompassDisturbance headingDeltaDegrees must be finite.");
        }
    }

    private static bool Contains(FusionFault fault, double elapsedSeconds)
        => elapsedSeconds >= fault.StartSeconds
            && elapsedSeconds < fault.StartSeconds + fault.DurationSeconds;

    private static RawImuPayload ApplySpike(RawImuPayload imu, FusionFault fault)
    {
        Vec3 acceleration = ParseAxis(fault.Axis) switch
        {
            'X' => imu.Acceleration with { X = imu.Acceleration.X + fault.Delta },
            'Y' => imu.Acceleration with { Y = imu.Acceleration.Y + fault.Delta },
            'Z' => imu.Acceleration with { Z = imu.Acceleration.Z + fault.Delta },
            _ => imu.Acceleration
        };

        return imu with { Acceleration = acceleration };
    }

    private static RawImuPayload ApplyCompass(RawImuPayload imu, FusionFault fault)
    {
        if (!double.IsFinite(imu.MagneticHeadingDegrees))
        {
            return imu;
        }

        double heading = imu.MagneticHeadingDegrees + fault.HeadingDeltaDegrees;
        heading %= 360d;

        if (heading < 0d)
        {
            heading += 360d;
        }

        double confidence = fault.Confidence is { } requested && double.IsFinite(requested)
            ? requested
            : imu.MagneticHeadingConfidence;

        return imu with
        {
            MagneticHeadingDegrees = heading,
            MagneticHeadingConfidence = confidence
        };
    }

    private static char? ParseAxis(string? axis)
    {
        if (string.IsNullOrWhiteSpace(axis) || axis.Length != 1)
        {
            return null;
        }

        char value = char.ToUpper(axis[0], CultureInfo.InvariantCulture);

        return value is 'X' or 'Y' or 'Z' ? value : null;
    }
}
