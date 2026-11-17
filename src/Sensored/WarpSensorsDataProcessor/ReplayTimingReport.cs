namespace WarpSensorsDataProcessor;

/// <summary>A trusted observer's playback-rate segment; zero speed denotes a pause.</summary>
internal sealed record ReplayTimingSegment(double WallStartSeconds, double RecordingStartSeconds, double Speed);

/// <summary>One independently timestamped source delivery and its full-rate consumption.</summary>
internal sealed record ReplayTimingObservation(
    long RecordingRow,
    long RecordedSequence,
    string Kind,
    double RecordingSeconds,
    double ObservedWallSeconds,
    double ConsumedWallSeconds,
    double ProcessingSeconds);

/// <summary>Summarizes a finite set of timing measurements in seconds.</summary>
internal sealed record ReplayTimingDistribution(double Minimum, double Median, double P95, double Maximum)
{
    internal static ReplayTimingDistribution From(IEnumerable<double> values)
    {
        double[] sorted = [.. values.Order()];

        if (sorted.Length == 0 || sorted.Any(static value => !double.IsFinite(value)))
        {
            throw new InvalidDataException("Timing evidence must contain finite measurements.");
        }

        return new(sorted[0], sorted[sorted.Length / 2],
            sorted[(int)Math.Ceiling((sorted.Length - 1) * 0.95d)], sorted[^1]);
    }
}

/// <summary>Separates measurement-clock cadence from externally observed delivery pacing.</summary>
internal sealed record ReplayTimingAssessment(
    bool Passed,
    double ExpectedRecordingSeconds,
    double RecordedSeconds,
    double WallSeconds,
    double ActiveWallSeconds,
    double ObservedRate,
    double RelativeRateError,
    ReplayTimingDistribution Lateness)
{
    internal const double RelativeRateTolerance = 0.05d;

    /// <summary>Grades immutable evidence, without calling a clock or the replay scheduler.</summary>
    internal static ReplayTimingAssessment Evaluate(
        IReadOnlyList<ReplayTimingObservation> observations,
        IReadOnlyList<ReplayTimingSegment> segments,
        bool complete)
    {
        if (observations.Count < 2 || segments.Count == 0
            || segments[0].WallStartSeconds != 0d || segments[0].RecordingStartSeconds != 0d
            || observations[0].ObservedWallSeconds != 0d || observations[0].RecordingSeconds != 0d)
        {
            throw new InvalidDataException("Timing evidence requires observations and an origin rate segment.");
        }

        for (int index = 0; index < segments.Count; index++)
        {
            ReplayTimingSegment segment = segments[index];

            if (!double.IsFinite(segment.WallStartSeconds) || !double.IsFinite(segment.RecordingStartSeconds)
                || !double.IsFinite(segment.Speed) || segment.Speed < 0d
                || segment.WallStartSeconds < 0d || segment.RecordingStartSeconds < 0d
                || segment.WallStartSeconds > observations[^1].ObservedWallSeconds)
            {
                throw new InvalidDataException("A timing rate segment is invalid.");
            }

            if (index > 0)
            {
                ReplayTimingSegment previous = segments[index - 1];
                double expected = previous.RecordingStartSeconds
                    + ((segment.WallStartSeconds - previous.WallStartSeconds) * previous.Speed);

                if (segment.WallStartSeconds <= previous.WallStartSeconds
                    || Math.Abs(segment.RecordingStartSeconds - expected) > 1e-6d)
                {
                    throw new InvalidDataException("Timing rate segments must be continuous and ordered.");
                }
            }
        }

        double wall = observations[^1].ObservedWallSeconds;
        double recorded = observations[^1].RecordingSeconds;
        double expectedRecorded = 0d;
        double activeWall = 0d;
        bool deliveredWhilePaused = false;

        for (int index = 0; index < segments.Count; index++)
        {
            ReplayTimingSegment segment = segments[index];
            double end = index + 1 < segments.Count ? Math.Min(wall, segments[index + 1].WallStartSeconds) : wall;
            double elapsed = Math.Max(0d, end - segment.WallStartSeconds);
            expectedRecorded += elapsed * segment.Speed;

            if (segment.Speed > 0d)
            {
                activeWall += elapsed;
            }
            else
            {
                deliveredWhilePaused |= observations.Any(observation =>
                    observation.ObservedWallSeconds > segment.WallStartSeconds
                    && observation.ObservedWallSeconds < end);
            }
        }

        if (!double.IsFinite(wall) || wall <= 0d || !double.IsFinite(recorded) || recorded <= 0d
            || !double.IsFinite(activeWall) || activeWall <= 0d
            || !double.IsFinite(expectedRecorded) || expectedRecorded <= 0d)
        {
            throw new InvalidDataException("Timing evidence has no measurable active duration.");
        }

        double previousWall = -1d;
        double previousRecorded = -1d;
        long previousRow = -1;
        long previousSequence = -1;
        List<double> lateness = new(observations.Count);

        foreach (ReplayTimingObservation observation in observations)
        {
            if (!double.IsFinite(observation.ObservedWallSeconds) || !double.IsFinite(observation.RecordingSeconds)
                || !double.IsFinite(observation.ConsumedWallSeconds) || !double.IsFinite(observation.ProcessingSeconds)
                || observation.ObservedWallSeconds < previousWall || observation.RecordingSeconds < previousRecorded
                || observation.RecordingRow <= previousRow || observation.RecordedSequence <= previousSequence
                || observation.ConsumedWallSeconds < observation.ObservedWallSeconds || observation.ProcessingSeconds < 0d)
            {
                throw new InvalidDataException("Timing observations are invalid or out of order.");
            }

            lateness.Add(observation.ObservedWallSeconds - ExpectedWall(observation.RecordingSeconds, segments));
            previousWall = observation.ObservedWallSeconds;
            previousRecorded = observation.RecordingSeconds;
            previousRow = observation.RecordingRow;
            previousSequence = observation.RecordedSequence;
        }

        double error = Math.Abs((recorded / expectedRecorded) - 1d);

        return new(complete && !deliveredWhilePaused && error <= RelativeRateTolerance,
            expectedRecorded, recorded, wall, activeWall, recorded / activeWall, error,
            ReplayTimingDistribution.From(lateness));
    }

    private static double ExpectedWall(double recording, IReadOnlyList<ReplayTimingSegment> segments)
    {
        for (int index = 0; index < segments.Count; index++)
        {
            ReplayTimingSegment segment = segments[index];
            double end = index + 1 < segments.Count ? segments[index + 1].RecordingStartSeconds : double.PositiveInfinity;

            if (segment.Speed > 0d && recording >= segment.RecordingStartSeconds && recording <= end)
            {
                return segment.WallStartSeconds + ((recording - segment.RecordingStartSeconds) / segment.Speed);
            }
        }

        throw new InvalidDataException("An observation is outside the recorded playback-rate segments.");
    }
}

/// <summary>Compares GPS measurement elapsed time with observed fix delivery, when available.</summary>
internal sealed record ReplayGpsClockEvidence(int Fixes, double? MeasurementSeconds, double? WallSeconds, double? Rate);

/// <summary>Versioned timing evidence; never substitutes wall time into the locked fusion CSV.</summary>
internal sealed record ReplayTimingReport
{
    public string Schema { get; init; } = "warp-replay-timing";
    public int Version { get; init; } = 1;
    public required string Mode { get; init; }
    public required IReadOnlyList<FusionReferenceInputPart> InputParts { get; init; }
    public required string InputSha256 { get; init; }
    public required FusionReferenceRuntime Runtime { get; init; }
    public string? EngineType { get; init; }
    public string? EngineAssemblyVersion { get; init; }
    public string? EngineAssemblySha256 { get; init; }
    public object? EngineOptions { get; init; }
    public long ClockFrequency { get; init; }
    public double RequestedStartSeconds { get; init; }
    public double RequestedDurationSeconds { get; init; }
    public double StartupSeconds { get; init; }
    public double DrainSeconds { get; init; }
    public bool TraceWritingEnabled { get; init; }
    public int ExpectedRows { get; init; }
    public int ObservedRows { get; init; }
    public int ConsumedRows { get; init; }
    public long? FusionOutputRows { get; init; }
    public long IngressDropped { get; init; }
    public long SubscriptionDropped { get; init; }
    public long SubscriptionThrottled { get; init; }
    public bool Complete { get; init; }
    public required ReplayTimingAssessment Timing { get; init; }
    public required ReplayTimingDistribution ConsumerLatency { get; init; }
    public ReplayTimingDistribution? ProcessingDuration { get; init; }
    public required ReplayGpsClockEvidence GpsClock { get; init; }
    public required IReadOnlyList<ReplayTimingSegment> Segments { get; init; }
    public required IReadOnlyList<ReplayTimingObservation> Observations { get; init; }
}
