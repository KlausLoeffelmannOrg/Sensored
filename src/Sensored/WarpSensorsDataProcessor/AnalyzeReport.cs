using System.Text.Json.Serialization;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Machine-readable recording analysis used to inspect calibration excitation and timing.
/// </summary>
internal sealed class AnalyzeReport
{
    [JsonPropertyOrder(0)]
    public string Schema { get; init; } = "warp-fusion-analyze";

    [JsonPropertyOrder(1)]
    public int Version { get; init; } = 1;

    [JsonPropertyOrder(2)]
    public required string RecordingFolder { get; init; }

    [JsonPropertyOrder(3)]
    public required AnalyzeCounts Counts { get; init; }

    [JsonPropertyOrder(4)]
    public required AnalyzeContinuity Continuity { get; init; }

    [JsonPropertyOrder(5)]
    public required IReadOnlyList<AnalyzePartSummary> Parts { get; init; }

    [JsonPropertyOrder(6)]
    public required AnalyzeTiming Timing { get; init; }

    [JsonPropertyOrder(7)]
    public required AnalyzeAvailability Availability { get; init; }

    [JsonPropertyOrder(8)]
    public required AnalyzeCompassConfidence CompassConfidence { get; init; }

    [JsonPropertyOrder(9)]
    public required IReadOnlyDictionary<string, AnalyzeRangeStats> Ranges { get; init; }

    [JsonPropertyOrder(10)]
    public required AnalyzeFutureGps FutureGps { get; init; }

    [JsonPropertyOrder(11)]
    public required AnalyzeMotion Motion { get; init; }

    [JsonPropertyOrder(12)]
    public required AnalyzeExcitation Excitation { get; init; }

    [JsonPropertyOrder(13)]
    public required AnalyzeGpsDerivatives GpsDerivatives { get; init; }
}

/// <summary>
///  Per-kind and total row counts.
/// </summary>
internal sealed class AnalyzeCounts
{
    public long Rows { get; init; }
    public long Imu { get; init; }
    public long Geolocation { get; init; }
    public long Health { get; init; }
    public long Gap { get; init; }
    public long Classification { get; init; }
}

/// <summary>
///  Row, sequence, and timestamp continuity.
/// </summary>
internal sealed class AnalyzeContinuity
{
    public bool RowContiguous { get; init; }
    public long RowRegressions { get; init; }
    public long RowGaps { get; init; }
    public long DuplicateRows { get; init; }
    public bool SequenceContiguous { get; init; }
    public long SequenceRegressions { get; init; }
    public long SourceTimestampRegressions { get; init; }
    public long ArrivalTimestampRegressions { get; init; }
    public long DuplicateImuSourceTimestamps { get; init; }
    public long DuplicateGpsSourceTimestamps { get; init; }
}

/// <summary>
///  One recording part.
/// </summary>
internal sealed class AnalyzePartSummary
{
    public required string FileName { get; init; }
    public long RowCount { get; init; }
    public long? FirstRow { get; init; }
    public long? LastRow { get; init; }
    public string? FirstSourceTimestamp { get; init; }
    public string? LastSourceTimestamp { get; init; }
}

/// <summary>
///  Timing, rates, lag, and interval quantization.
/// </summary>
internal sealed class AnalyzeTiming
{
    public string? FirstSourceTimestamp { get; init; }
    public string? LastSourceTimestamp { get; init; }
    public double? DurationSeconds { get; init; }
    public required AnalyzeSourceTiming Imu { get; init; }
    public required AnalyzeSourceTiming Geolocation { get; init; }
}

/// <summary>
///  Timing for one sensor kind.
/// </summary>
internal sealed class AnalyzeSourceTiming
{
    public double? RateHz { get; init; }
    public required AnalyzeRangeStats IntervalSeconds { get; init; }
    public required AnalyzeRangeStats ArrivalLagSeconds { get; init; }
    public required IReadOnlyDictionary<string, long> IntervalMillisecondBuckets { get; init; }
    public long IntervalOverflowBuckets { get; init; }
    public double? MedianPositiveIntervalSeconds { get; init; }
    public long ZeroMillisecondIntervals { get; init; }
}

/// <summary>
///  Availability of optional measurements.
/// </summary>
internal sealed class AnalyzeAvailability
{
    public long ImuGyroAvailable { get; init; }
    public long ImuGyroMissing { get; init; }
    public long ImuCompassFinite { get; init; }
    public long ImuCompassMissing { get; init; }
    public long GpsSpeedValid { get; init; }
    public long GpsSpeedInvalid { get; init; }
    public long GpsCourseValid { get; init; }
    public long GpsCourseInvalid { get; init; }
    public long GpsPositionFinite { get; init; }
}

/// <summary>
///  Compass confidence distribution.
/// </summary>
internal sealed class AnalyzeCompassConfidence
{
    public long Missing { get; init; }
    public required AnalyzeRangeStats Values { get; init; }
    public required IReadOnlyDictionary<string, long> TenthsBuckets { get; init; }
}

/// <summary>
///  Streaming min/max/mean/stddev for finite samples.
/// </summary>
internal sealed class AnalyzeRangeStats
{
    public double? Min { get; init; }
    public double? Max { get; init; }
    public double? Mean { get; init; }
    public double? StdDev { get; init; }
    public long FiniteCount { get; init; }
    public long NonFiniteCount { get; init; }
}

/// <summary>
///  GPS timestamps ahead of the preceding IMU measurement time.
/// </summary>
internal sealed class AnalyzeFutureGps
{
    public long Count { get; init; }
    public double? MaxLeadSeconds { get; init; }
    public required IReadOnlyList<AnalyzeFutureGpsExample> Examples { get; init; }
}

/// <summary>
///  One future-dated GPS example.
/// </summary>
internal sealed class AnalyzeFutureGpsExample
{
    public long Row { get; init; }
    public double LeadSeconds { get; init; }
    public required string GpsMeasurementTimestamp { get; init; }
    public required string PrecedingImuTimestamp { get; init; }
}

/// <summary>
///  Stationary and moving GPS windows.
/// </summary>
internal sealed class AnalyzeMotion
{
    public double? FirstSpeedAbove2MpsSeconds { get; init; }
    public long StationaryWindowCount { get; init; }
    public double StationaryDurationSeconds { get; init; }
    public double? LongestStationarySeconds { get; init; }
    public long MovingWindowCount { get; init; }
    public double MovingDurationSeconds { get; init; }
    public required AnalyzeRangeStats GpsSpeedMetersPerSecond { get; init; }
}

/// <summary>
///  Excitation evidence useful for mounting calibration.
/// </summary>
internal sealed class AnalyzeExcitation
{
    public required AnalyzeRangeStats GyroMagnitudeDegreesPerSecond { get; init; }
    public required AnalyzeRangeStats AccelerometerMagnitudeG { get; init; }
    public long GpsSpeedChangeIntervals { get; init; }
    public long GpsSpeedChangeAboveHalfMps { get; init; }
    public long TurnIntervalsAtSpeed { get; init; }
    public double? MaxAbsGpsSpeedDerivativeMps2 { get; init; }
    public double? MaxAbsCourseRateDegreesPerSecond { get; init; }
    public double? MaxGpsGapSeconds { get; init; }
    public long GpsGapsOverTwoSeconds { get; init; }
}

/// <summary>
///  Interval-aligned GPS speed and course derivatives.
/// </summary>
internal sealed class AnalyzeGpsDerivatives
{
    public required AnalyzeRangeStats SpeedDerivativeMps2 { get; init; }
    public required AnalyzeRangeStats CourseRateDegreesPerSecond { get; init; }
}
