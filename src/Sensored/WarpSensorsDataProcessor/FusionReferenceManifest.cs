using System.Text.Json.Serialization;

using WarpToolkit.Sensors.Fusion;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Reproducibility manifest published next to <c>golden.trace.csv</c>.
/// </summary>
internal sealed class FusionReferenceManifest
{
    [JsonPropertyOrder(0)]
    public string Schema { get; init; } = "warp-fusion-reference-manifest";

    [JsonPropertyOrder(1)]
    public int Version { get; init; } = 1;

    [JsonPropertyOrder(2)]
    public ArtifactCompletion Completion { get; init; }

    [JsonPropertyOrder(3)]
    public string? CompletionReason { get; init; }

    [JsonPropertyOrder(4)]
    public string TraceSchema { get; init; } = FusionTraceSchema.SchemaId;

    [JsonPropertyOrder(5)]
    public int TraceSchemaVersion { get; init; } = FusionTraceSchema.Version;

    [JsonPropertyOrder(6)]
    public string TraceFile { get; init; } = "golden.trace.csv";

    [JsonPropertyOrder(7)]
    public string? TraceSha256 { get; init; }

    [JsonPropertyOrder(8)]
    public required string RecordingFolder { get; init; }

    [JsonPropertyOrder(9)]
    public required IReadOnlyList<FusionReferenceInputPart> InputParts { get; init; }

    [JsonPropertyOrder(10)]
    public required string InputSha256 { get; init; }

    [JsonPropertyOrder(11)]
    public string? ScenarioName { get; init; }

    [JsonPropertyOrder(12)]
    public string? ScenarioPath { get; init; }

    [JsonPropertyOrder(13)]
    public string? ScenarioSha256 { get; init; }

    [JsonPropertyOrder(14)]
    public required string EngineType { get; init; }

    [JsonPropertyOrder(15)]
    public required string EngineAssemblyVersion { get; init; }

    [JsonPropertyOrder(16)]
    public string? EngineAssemblySha256 { get; init; }

    [JsonPropertyOrder(17)]
    public string? EngineAssemblyPath { get; init; }

    [JsonPropertyOrder(18)]
    public string? EngineAssemblyFileVersion { get; init; }

    [JsonPropertyOrder(19)]
    public string? EngineAssemblyLastWriteUtc { get; init; }

    [JsonPropertyOrder(20)]
    public object EngineOptions { get; init; } = "default";

    [JsonPropertyOrder(21)]
    public required FusionReferenceRuntime Runtime { get; init; }

    [JsonPropertyOrder(22)]
    public required FusionReferenceCounts Counts { get; init; }

    [JsonPropertyOrder(23)]
    public required FusionReferenceCalibration Calibration { get; init; }

    [JsonPropertyOrder(24)]
    public required FusionReferenceCadence Cadence { get; init; }

    [JsonPropertyOrder(25)]
    public required FusionReferenceConfidence Confidence { get; init; }

    [JsonPropertyOrder(26)]
    public required FusionReferenceValidity Validity { get; init; }

    [JsonPropertyOrder(27)]
    public required FusionReferenceOutputRanges Ranges { get; init; }

    [JsonPropertyOrder(28)]
    public required IReadOnlyList<string> Warnings { get; init; }
}

/// <summary>
///  One hashed input part.
/// </summary>
internal sealed class FusionReferenceInputPart
{
    public required string FileName { get; init; }
    public required string Sha256 { get; init; }
    public long Length { get; init; }
}

/// <summary>
///  Variable run provenance. Not part of the deterministic trace hash.
/// </summary>
internal sealed class FusionReferenceRuntime
{
    public required string Framework { get; init; }
    public required string OsDescription { get; init; }
    public required string GeneratedAtUtc { get; init; }
}

/// <summary>
///  Row and decision counts.
/// </summary>
internal sealed class FusionReferenceCounts
{
    public long InputRows { get; init; }
    public long ImuRows { get; init; }
    public long GpsRows { get; init; }
    public long GpsFedToEngine { get; init; }
    public long GpsWithheld { get; init; }
    public long OutputRows { get; init; }
    public long RejectedRows { get; init; }
    public double? RejectionRate { get; init; }
}

/// <summary>
///  Calibration coverage observed on the output ticks.
/// </summary>
internal sealed class FusionReferenceCalibration
{
    public FusionCalibrationState FinalState { get; init; }
    public double? FirstCalibratedSeconds { get; init; }
    public long? FirstCalibratedOutputSequence { get; init; }
    public long UncalibratedRows { get; init; }
    public long ConvergingRows { get; init; }
    public long CalibratedRows { get; init; }
    public double? CalibratedCoverage { get; init; }
    public double? CalibratedDurationSeconds { get; init; }
}

/// <summary>
///  Output cadence.
/// </summary>
internal sealed class FusionReferenceCadence
{
    public double? OutputHz { get; init; }
    public double? MinIntervalSeconds { get; init; }
    public double? MaxIntervalSeconds { get; init; }
    public double? MeanIntervalSeconds { get; init; }
}

/// <summary>
///  Confidence summary.
/// </summary>
internal sealed class FusionReferenceConfidence
{
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Mean { get; init; }
}

/// <summary>
///  Output-validity flag coverage.
/// </summary>
internal sealed class FusionReferenceValidity
{
    public long Attitude { get; init; }
    public long Heading { get; init; }
    public long Acceleration { get; init; }
    public long Navigation { get; init; }
}

/// <summary>
///  Finite output ranges from the exported estimates.
/// </summary>
internal sealed class FusionReferenceOutputRanges
{
    public required FusionReferenceRange PitchDegrees { get; init; }
    public required FusionReferenceRange RollDegrees { get; init; }
    public required FusionReferenceRange HeadingDegrees { get; init; }
    public required FusionReferenceRange LongitudinalAccelMps2 { get; init; }
    public required FusionReferenceRange LateralAccelMps2 { get; init; }
    public required FusionReferenceRange VerticalAccelMps2 { get; init; }
    public required FusionReferenceRange SpeedMps { get; init; }
}

/// <summary>
///  Min/max over finite samples.
/// </summary>
internal sealed class FusionReferenceRange
{
    public double? Min { get; init; }
    public double? Max { get; init; }
    public long FiniteCount { get; init; }
    public long NonFiniteCount { get; init; }
}
