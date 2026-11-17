using System.Diagnostics;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>Preserves an independently read input row and its nondecreasing arrival position.</summary>
internal sealed record ReplayExpectedRow(SensorEnvelopeRow Row, TimeSpan Position);

/// <summary>Observes actual callbacks before pipeline enqueue, independently of the replay clock.</summary>
internal sealed class ReplayTimingObserver(IReadOnlyList<ReplayExpectedRow> expected)
{
    private readonly long[] _emitted = new long[expected.Count];
    private readonly long[] _consumed = new long[expected.Count];
    private readonly double[] _processingSeconds = new double[expected.Count];
    private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _observedCount;
    private int _consumedCount;
    private long _outputCount;

    internal Task Completion => _completed.Task;
    internal int ObservedCount => Volatile.Read(ref _observedCount);
    internal int ConsumedCount => Volatile.Read(ref _consumedCount);
    internal long OutputCount => Interlocked.Read(ref _outputCount);
    internal long FirstEmission => _emitted[0];

    /// <summary>Attaches before the service installs its enqueue callbacks.</summary>
    internal void AttachBeforePipeline(SensorReplayPlayer player)
    {
        player.Imu.SampleReady += (_, sample) => ObserveImu(sample);
        player.Gps.FixReady += (_, sample) => ObserveGps(sample);
    }

    /// <summary>Signals the window end only after its final sample has been enqueued.</summary>
    internal void AttachAfterPipeline(SensorReplayPlayer player)
    {
        player.Imu.SampleReady += (_, _) => CompleteWindow(player);
        player.Gps.FixReady += (_, _) => CompleteWindow(player);
    }

    private void CompleteWindow(SensorReplayPlayer player)
    {
        if (ObservedCount == expected.Count)
        {
            player.Pause();
            _completed.TrySetResult();
        }
    }

    private ReplayExpectedRow NextExpected(SensorDataKind kind)
    {
        if (_observedCount >= expected.Count || expected[_observedCount].Row.Envelope.Kind != kind)
        {
            throw new InvalidDataException("Replay callbacks are missing, extra, or out of recorded order.");
        }

        _emitted[_observedCount] = Stopwatch.GetTimestamp();

        return expected[_observedCount];
    }

    private void ObserveImu(ImuSample actual)
    {
        ReplayExpectedRow item = NextExpected(SensorDataKind.Imu);
        RawImuPayload raw = item.Row.Envelope.Imu!.Value;
        ImuSample original = new(
            raw.Acceleration, raw.AngularRate, item.Row.Envelope.SourceTimestamp,
            raw.AbsolutePitchDegrees, raw.AbsoluteRollDegrees, raw.AbsoluteYawDegrees,
            raw.YawSource, raw.HasAngularRate, raw.MagneticHeadingDegrees, raw.MagneticHeadingConfidence);

        if (actual != original)
        {
            throw new InvalidDataException($"IMU payload or measurement time changed at recording row {item.Row.Row}.");
        }

        Interlocked.Increment(ref _observedCount);
    }

    private void ObserveGps(GpsSample actual)
    {
        ReplayExpectedRow item = NextExpected(SensorDataKind.Geolocation);
        GeolocationPayload raw = item.Row.Envelope.Geolocation!.Value;
        GpsSample original = new(
            raw.SpeedMetersPerSecond, raw.CourseDegrees, raw.CourseValid, raw.HorizontalAccuracyMeters,
            raw.CoordinateTimestamp, raw.LatitudeDegrees, raw.LongitudeDegrees, raw.PositionSource,
            raw.AltitudeMeters, raw.VerticalAccuracyMeters, raw.IsRemoteSource, raw.SourceTimestamp,
            raw.PositionDilutionOfPrecision, raw.HorizontalDilutionOfPrecision, raw.VerticalDilutionOfPrecision,
            raw.FixQuality, raw.SatelliteCount);

        if (actual != original)
        {
            throw new InvalidDataException($"GPS payload or measurement time changed at recording row {item.Row.Row}.");
        }

        Interlocked.Increment(ref _observedCount);
    }

    /// <summary>Observes full-rate consumption, optionally surrounding the numerical engine.</summary>
    internal FusionEstimate? Process(SensorEnvelope envelope, IFusionEngine? engine)
    {
        int index = _consumedCount;

        if (index >= expected.Count || expected[index].Row.Envelope.Kind != envelope.Kind)
        {
            throw new InvalidDataException("The full-rate consumer received a gap, missing row, or reordered row.");
        }

        SensorEnvelope original = expected[index].Row.Envelope;
        DateTimeOffset timestamp = original.Geolocation?.CoordinateTimestamp ?? original.SourceTimestamp;

        if (envelope.SourceTimestamp != timestamp)
        {
            throw new InvalidDataException("The full-rate consumer received a modified measurement timestamp.");
        }

        long start = Stopwatch.GetTimestamp();
        FusionEstimate? estimate = engine?.Process(envelope);
        long end = Stopwatch.GetTimestamp();
        _processingSeconds[index] = Stopwatch.GetElapsedTime(start, end).TotalSeconds;
        _consumed[index] = end;

        if (estimate is not null)
        {
            Interlocked.Increment(ref _outputCount);
        }

        Interlocked.Increment(ref _consumedCount);

        return estimate;
    }

    /// <summary>Builds immutable evidence only after source and consumer queues have drained.</summary>
    internal IReadOnlyList<ReplayTimingObservation> CreateObservations()
    {
        if (ObservedCount != expected.Count || ConsumedCount != expected.Count)
        {
            throw new InvalidDataException("The timing window did not deliver and consume every expected row.");
        }

        List<ReplayTimingObservation> result = new(expected.Count);

        for (int index = 0; index < expected.Count; index++)
        {
            ReplayExpectedRow item = expected[index];
            result.Add(new(
                item.Row.Row, item.Row.Envelope.Sequence, item.Row.Envelope.Kind.ToString(),
                (item.Position - expected[0].Position).TotalSeconds,
                Stopwatch.GetElapsedTime(_emitted[0], _emitted[index]).TotalSeconds,
                Stopwatch.GetElapsedTime(_emitted[0], _consumed[index]).TotalSeconds,
                _processingSeconds[index]));
        }

        return result;
    }

    /// <summary>Returns the GPS measurement-clock cross-check without treating fixes as a metronome.</summary>
    internal ReplayGpsClockEvidence GpsEvidence()
    {
        int[] indices = [.. Enumerable.Range(0, expected.Count)
            .Where(index => expected[index].Row.Envelope.Kind == SensorDataKind.Geolocation)];

        if (indices.Length < 2)
        {
            return new(indices.Length, null, null, null);
        }

        DateTimeOffset first = expected[indices[0]].Row.Envelope.Geolocation!.Value.CoordinateTimestamp;
        DateTimeOffset last = expected[indices[^1]].Row.Envelope.Geolocation!.Value.CoordinateTimestamp;
        double wall = Stopwatch.GetElapsedTime(_emitted[indices[0]], _emitted[indices[^1]]).TotalSeconds;
        double measurement = (last - first).TotalSeconds;

        return new(indices.Length, measurement, wall, wall > 0d ? measurement / wall : null);
    }

    /// <summary>Decorates the real estimator without changing its numerical time or inputs.</summary>
    internal sealed class ObservedEngine(ReplayTimingObserver observer, IFusionEngine engine) : IFusionEngine
    {
        public FusionEstimate? Process(SensorEnvelope envelope) => observer.Process(envelope, engine);
        public void Reset() => engine.Reset();
    }
}
