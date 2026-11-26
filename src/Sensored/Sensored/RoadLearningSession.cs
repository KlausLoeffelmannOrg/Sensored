using WarpToolkit.Sensors;

namespace Sensored;

internal enum RoadCondition
{
    RoughRoad, WavyRoad, Acceleration, Braking, EmergencyBraking,
    LevelToUphill, LevelToDownhill, DownhillToUphill, UphillToDownhill,
    UphillLeftCurve, UphillRightCurve, DownhillLeftCurve, DownhillRightCurve,
    IntersectionStop, StopAndGo, HighwayDriving, EnterHighway, ExitHighway,
    WaitingAtTrafficLight, WaitingAtStopSign, Reversing, ParallelParking,
    UphillStart, UphillStop, UphillBrakingToStandstill, DownhillBrakingToStandstill,
    LevelStart, LevelBrakingToStandstill,
    StartingFromStandstill, BrakingToStandstill, Stopped, LeftCurve, RightCurve
}

internal static class RoadConditions
{
    internal static string Title(RoadCondition condition) => condition switch
    {
        RoadCondition.RoughRoad => "Rough road",
        RoadCondition.WavyRoad => "Wavy road",
        RoadCondition.Acceleration => "Driving - acceleration",
        RoadCondition.Braking => "Driving - braking",
        RoadCondition.EmergencyBraking => "Emergency braking",
        RoadCondition.LevelToUphill => "Level to uphill",
        RoadCondition.LevelToDownhill => "Level to downhill",
        RoadCondition.DownhillToUphill => "Downhill to uphill",
        RoadCondition.UphillToDownhill => "Uphill to downhill",
        RoadCondition.UphillLeftCurve => "Uphill / left curve / driving style",
        RoadCondition.UphillRightCurve => "Uphill / right curve / driving style",
        RoadCondition.DownhillLeftCurve => "Downhill / left curve / driving style",
        RoadCondition.DownhillRightCurve => "Downhill / right curve / driving style",
        RoadCondition.IntersectionStop => "Intersection stop",
        RoadCondition.StopAndGo => "Traffic jam - stop and go",
        RoadCondition.HighwayDriving => "Highway driving",
        RoadCondition.EnterHighway => "Entering highway",
        RoadCondition.ExitHighway => "Exiting highway",
        RoadCondition.WaitingAtTrafficLight => "Waiting at traffic light",
        RoadCondition.WaitingAtStopSign => "Waiting at stop sign",
        RoadCondition.Reversing => "Driving backwards",
        RoadCondition.ParallelParking => "Parallel parking",
        RoadCondition.UphillStart => "Starting off uphill",
        RoadCondition.UphillStop => "Stopping uphill",
        RoadCondition.UphillBrakingToStandstill => "Braking to standstill uphill",
        RoadCondition.DownhillBrakingToStandstill => "Braking to standstill downhill",
        RoadCondition.LevelStart => "Starting from standstill - level road",
        RoadCondition.LevelBrakingToStandstill => "Braking to standstill - level road",
        RoadCondition.StartingFromStandstill => "Starting from standstill (grade unknown)",
        RoadCondition.BrakingToStandstill => "Braking to standstill (grade unknown)",
        RoadCondition.Stopped => "Stopped (cause unknown)",
        RoadCondition.LeftCurve => "Left curve / driving style",
        RoadCondition.RightCurve => "Right curve / driving style",
        _ => throw new ArgumentOutOfRangeException(nameof(condition))
    };

    /// <summary>Conditions the catalogue defines without an intensity level.</summary>
    internal static bool HasLevel(RoadCondition condition) => condition is not (
        RoadCondition.EmergencyBraking or RoadCondition.DownhillToUphill or RoadCondition.UphillToDownhill
        or RoadCondition.IntersectionStop or RoadCondition.StopAndGo or RoadCondition.HighwayDriving
        or RoadCondition.EnterHighway or RoadCondition.ExitHighway or RoadCondition.WaitingAtTrafficLight
        or RoadCondition.WaitingAtStopSign or RoadCondition.Reversing or RoadCondition.ParallelParking
        or RoadCondition.Stopped);

    internal static string Headline(RoadCondition condition, int level)
        => HasLevel(condition) ? $"{Title(condition)} - Level {level}" : Title(condition);
}

internal enum LearningSource { Gps, Vehicle, Internal, Bluetooth }

// Nullable channels preserve missingness; device-frame vibration never becomes vehicle-frame tilt.
internal sealed record RoadLearningSample(
    DateTimeOffset ReceivedAt, DateTimeOffset MeasuredAt, long Sequence, int Epoch,
    string SourceId, long Generation, LearningSource Source,
    double? Speed = null, double? Rpm = null, double? AccelerationMagnitude = null,
    double? AngularRateMagnitude = null, double? SupportedPitch = null,
    double? SupportedLateralAcceleration = null,
    Vec3? AccelerationVectorG = null, Vec3? AngularRateVectorDegreesPerSecond = null,
    double? GpsCourseDegrees = null, double? SupportedLongitudinalAcceleration = null,
    double? GpsAltitudeMeters = null);

internal sealed record RoadSourceEvidence(
    string Abbreviation, string Sensors, int Samples, double? Speed, double? Acceleration,
    double? Rpm, double? Vibration, double? Pitch, double? AngularRate,
    double Contribution, string Explanation);

internal sealed record RoadLearningQuestion(
    Guid Id, Guid SessionId, DateTimeOffset Start, DateTimeOffset End,
    RoadCondition Condition, int Level, int Confidence, string ModelVersion,
    string Profile, string? RecordingPath, RoadSourceEvidence[] Sources, RoadLearningSample[] Samples,
    long Continuity);

internal enum RoadLearningWaitState
{
    WaitingForData, Collecting, WaitingForSpeed, ConflictingSpeed,
    ConfirmingCondition, WaitingForCondition, WaitingForDifferentCondition
}

internal sealed record RoadLearningProgress(RoadLearningWaitState State, int SecondsRemaining, int Samples);

/// <summary>Opt-in shadow interpretation; answers never change physical fusion or calibration.</summary>
internal sealed class RoadLearningSession
{
    internal const string ModelVersion = "road-rules-v1";
    internal static readonly TimeSpan Window = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan QuestionLifetime = TimeSpan.FromSeconds(10);
    private readonly object _gate = new();
    private readonly Queue<RoadLearningSample> _samples = new();
    private readonly Dictionary<LearningSource, (string Id, long Generation)> _identities = [];
    private readonly Dictionary<LearningSource, DateTimeOffset> _lastSample = [];
    private readonly Dictionary<LearningSource, DateTimeOffset> _lastArrival = [];
    private readonly Dictionary<LearningSource, Queue<double>> _clockLags = [];
    private DateTimeOffset? _startedWindow;
    private DateTimeOffset? _lastReceived;
    private int? _epoch;
    private RoadCondition? _lastAsked;
    private RoadCondition? _candidate;
    private DateTimeOffset _candidateSince;
    private DateTimeOffset _pausedUntil;
    private bool _active = true;
    private long _continuity;
    private string? _lastContinuityBreak;

    internal RoadLearningSession(string profile)
    {
        Profile = profile;
    }

    internal Guid Id { get; } = Guid.NewGuid();
    internal string Profile { get; }
    internal bool Active { get { lock (_gate) { return _active; } } }
    internal DateTimeOffset PausedUntil { get { lock (_gate) { return _pausedUntil; } } }
    internal long ContinuityBreaks { get { lock (_gate) { return _continuity; } } }
    internal string? LastContinuityBreak { get { lock (_gate) { return _lastContinuityBreak; } } }
    internal RoadLearningProgress GetProgress(DateTimeOffset now)
    {
        lock (_gate)
        {
            RoadLearningSample[] samples = CurrentSamples(now);
            if (samples.Length == 0 || now - samples.Max(sample => sample.ReceivedAt) > TimeSpan.FromSeconds(2))
            {
                return new(RoadLearningWaitState.WaitingForData, (int)Window.TotalSeconds, samples.Length);
            }

            int remaining = (int)Math.Ceiling(Math.Max(0, (Window - (now - _startedWindow!.Value)).TotalSeconds));
            if (remaining > 0)
            {
                return new(RoadLearningWaitState.Collecting, remaining, samples.Length);
            }

            RoadSourceEvidence[] evidence = BuildEvidence(samples, now);
            RoadSourceEvidence gps = evidence[(int)LearningSource.Gps];
            RoadSourceEvidence vehicle = evidence[(int)LearningSource.Vehicle];
            RoadLearningWaitState state = gps.Speed is null && vehicle.Speed is null
                ? RoadLearningWaitState.WaitingForSpeed
                : SpeedSourcesConflict(gps, vehicle) ? RoadLearningWaitState.ConflictingSpeed
                : _candidate is not null ? RoadLearningWaitState.ConfirmingCondition
                : _lastAsked is not null ? RoadLearningWaitState.WaitingForDifferentCondition
                : RoadLearningWaitState.WaitingForCondition;
            return new(state, 0, samples.Length);
        }
    }

    internal bool IsCurrent(RoadLearningQuestion question, DateTimeOffset now)
    {
        lock (_gate)
        {
            return _active && question.SessionId == Id && question.Continuity == _continuity
                && now >= question.End && now - question.End <= QuestionLifetime
                && now >= _pausedUntil && _lastReceived is { } last
                && now >= last && now - last <= TimeSpan.FromSeconds(2);
        }
    }

    internal void End()
    {
        lock (_gate)
        {
            _active = false;
            ResetWindow("Session ended.");
        }
    }

    internal void Pause(DateTimeOffset now, int minutes)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(minutes, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(minutes, 20);
        lock (_gate)
        {
            _pausedUntil = now.AddMinutes(minutes);
            _candidate = null;
        }
    }

    internal void BreakContinuity(string reason = "Sensor continuity interrupted.")
    {
        lock (_gate) { ResetWindow(reason); }
    }

    /// <summary>
    ///  Drops only one producer's history; other sources keep the collected window. A source that has
    ///  contributed no sample (for example a missing receiver retrying) changes nothing.
    /// </summary>
    internal bool BreakSourceContinuity(string sourceId, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        lock (_gate)
        {
            LearningSource[] affected = [.. _identities.Where(pair => pair.Value.Id == sourceId).Select(pair => pair.Key)];
            return affected.Length > 0 && ResetSources(affected, reason);
        }
    }

    private bool ResetSources(IReadOnlyCollection<LearningSource> sources, string reason)
    {
        int before = _samples.Count;
        RoadLearningSample[] kept = [.. _samples.Where(sample => !sources.Contains(sample.Source))];
        _samples.Clear();
        foreach (RoadLearningSample sample in kept)
        {
            _samples.Enqueue(sample);
        }

        foreach (LearningSource source in sources)
        {
            _identities.Remove(source);
            _lastSample.Remove(source);
            _lastArrival.Remove(source);
            _clockLags.Remove(source);
        }

        if (kept.Length == before)
        {
            return false;
        }

        _continuity++;
        _lastContinuityBreak = reason;
        _candidate = null;
        if (kept.Length == 0)
        {
            _startedWindow = null;
        }

        return true;
    }

    private void ResetWindow(string reason)
    {
        _continuity++;
        _lastContinuityBreak = reason;
        _samples.Clear();
        _identities.Clear();
        _lastSample.Clear();
        _lastArrival.Clear();
        _clockLags.Clear();
        _startedWindow = null;
        _lastReceived = null;
        _epoch = null;
        _candidate = null;
    }

    /// <summary>
    ///  Maps a source whose clock differs from the local arrival clock (for example GNSS UTC on a tablet
    ///  with a skewed clock, or NMEA output latency) onto arrival time. The minimum recent lag is taken as the
    ///  clock offset, so source spacing is preserved; sources already on the local clock stay unchanged.
    /// </summary>
    private RoadLearningSample AlignClock(RoadLearningSample sample)
    {
        double lag = (sample.ReceivedAt - sample.MeasuredAt).TotalSeconds;
        if (!_clockLags.TryGetValue(sample.Source, out Queue<double>? lags))
        {
            _clockLags[sample.Source] = lags = new();
        }

        lags.Enqueue(lag);
        while (lags.Count > 40)
        {
            lags.Dequeue();
        }

        double offset = lags.Min();
        return offset is >= 0 and <= 1 ? sample : sample with { MeasuredAt = sample.MeasuredAt.AddSeconds(offset) };
    }

    internal void Observe(RoadLearningSample sample)
    {
        lock (_gate)
        {
            if (!_active)
            {
                return;
            }

            string? interruption = _epoch is { } epoch && epoch != sample.Epoch ? "Fusion epoch changed."
                : _lastReceived is { } last && sample.ReceivedAt - last > TimeSpan.FromSeconds(3) ? "No samples for more than 3 seconds."
                : null;
            if (interruption is not null)
            {
                ResetWindow(interruption);
            }

            string? sourceInterruption = _lastArrival.TryGetValue(sample.Source, out DateTimeOffset arrival) && sample.ReceivedAt < arrival
                    ? $"{sample.Source} arrival timestamp moved backwards."
                : _identities.TryGetValue(sample.Source, out var identity) && identity != (sample.SourceId, sample.Generation)
                    ? $"{sample.Source} identity or generation changed." : null;
            if (sourceInterruption is not null)
            {
                // A replaced or recovered producer restarts only its own history.
                ResetSources([sample.Source], sourceInterruption);
            }

            _epoch = sample.Epoch;
            _identities[sample.Source] = (sample.SourceId, sample.Generation);
            _lastArrival[sample.Source] = sample.ReceivedAt;
            if (_lastReceived is null || sample.ReceivedAt > _lastReceived)
            {
                _lastReceived = sample.ReceivedAt;
            }

            sample = AlignClock(sample);
            if (sample.MeasuredAt > sample.ReceivedAt
                || sample.ReceivedAt - sample.MeasuredAt > TimeSpan.FromSeconds(2)
                || new[] { sample.Speed, sample.Rpm, sample.AccelerationMagnitude,
                    sample.AngularRateMagnitude, sample.SupportedPitch, sample.SupportedLateralAcceleration,
                    sample.GpsCourseDegrees, sample.SupportedLongitudinalAcceleration,
                    sample.AccelerationVectorG?.Magnitude, sample.AngularRateVectorDegreesPerSecond?.Magnitude }
                    .Any(value => value is { } number && !double.IsFinite(number)))
            {
                return;
            }

            if (_lastSample.TryGetValue(sample.Source, out DateTimeOffset previous)
                && sample.MeasuredAt <= previous)
            {
                return;
            }

            // Retain at most 20 Hz per channel family, with original measurement identities.
            if (_lastSample.TryGetValue(sample.Source, out previous)
                && sample.MeasuredAt - previous < TimeSpan.FromMilliseconds(50))
            {
                return;
            }

            _lastSample[sample.Source] = sample.MeasuredAt;
            if (_startedWindow is null || sample.ReceivedAt < _startedWindow)
            {
                _startedWindow = sample.ReceivedAt;
            }
            _samples.Enqueue(sample);
            while (_samples.TryPeek(out RoadLearningSample? first)
                && _lastReceived - first.ReceivedAt > Window)
            {
                _samples.Dequeue();
            }
        }
    }

    internal RoadLearningQuestion? TryQuestion(DateTimeOffset now, string? recordingPath)
    {
        lock (_gate)
        {
            if (!_active || now < _pausedUntil || _startedWindow is null
                || now - _startedWindow < Window || _lastReceived is null
                || now < _lastReceived || now - _lastReceived > TimeSpan.FromSeconds(2))
            {
                return null;
            }

            RoadLearningSample[] samples = CurrentSamples(now);
            if (samples.Length == 0)
            {
                return null;
            }

            RoadSourceEvidence[] evidence = BuildEvidence(samples, now);
            var decision = Recognize(samples, evidence, now);
            if (decision is not { } guess || guess.Condition == _lastAsked)
            {
                _candidate = null;
                return null;
            }

            if (_candidate != guess.Condition)
            {
                _candidate = guess.Condition;
                _candidateSince = now;
                return null;
            }

            if (now - _candidateSince < TimeSpan.FromSeconds(2))
            {
                return null;
            }

            double total = evidence.Sum(source => source.Contribution);
            evidence = [.. evidence.Select(source => source with
            {
                Contribution = total > 0 ? 100 * source.Contribution / total : 0
            })];
            _lastAsked = guess.Condition;
            _candidate = null;
            return new(Guid.NewGuid(), Id, now - Window, now, guess.Condition, guess.Level,
                guess.Confidence, ModelVersion, Profile, recordingPath, evidence, samples, _continuity);
        }
    }

    private RoadLearningSample[] CurrentSamples(DateTimeOffset now)
        => [.. _samples.Where(item => now >= item.ReceivedAt && now - item.ReceivedAt <= Window
            && now >= item.MeasuredAt && now - item.MeasuredAt <= Window)
            .OrderBy(item => item.MeasuredAt)];

    private static RoadSourceEvidence[] BuildEvidence(RoadLearningSample[] samples, DateTimeOffset now)
        => [.. Enum.GetValues<LearningSource>().Select(source =>
        {
            RoadLearningSample[] window = [.. samples.Where(item => item.Source == source)];
            RoadLearningSample[] recent = [.. window.Where(item => now - item.MeasuredAt <= TimeSpan.FromSeconds(3))];
            RoadLearningSample? last = recent.LastOrDefault();
            bool fresh = last is not null && now - last.MeasuredAt <= TimeSpan.FromSeconds(2);
            double? acceleration = fresh ? Derivative(recent) : null;
            double[] magnitudes = [.. recent.Where(item => item.AccelerationMagnitude is not null)
                .Select(item => item.AccelerationMagnitude!.Value)];
            double? vibration = fresh && magnitudes.Length >= 10 ? StandardDeviation(magnitudes) : null;
            return new RoadSourceEvidence(source switch
            {
                LearningSource.Gps => "GPS v/a",
                LearningSource.Vehicle => "VEH v/a/RPM",
                LearningSource.Internal => "INT acc/gyro",
                _ => "BT acc/gyro"
            }, string.Join(", ", window.Select(item => item.SourceId).Distinct()), window.Length,
                fresh ? last?.Speed : null, acceleration, fresh ? last?.Rpm : null,
                vibration, fresh ? last?.SupportedPitch : null,
                fresh ? last?.AngularRateMagnitude : null, 0,
                fresh ? "Available; not used by this rule." : "Unavailable or stale; no contribution.");
        })];

    private static double? Derivative(RoadLearningSample[] recent)
    {
        RoadLearningSample[] speeds = [.. recent.Where(item => item.Speed is not null)];
        if (speeds.Length < 3 || (speeds[^1].MeasuredAt - speeds[0].MeasuredAt).TotalSeconds < 1)
        {
            return null;
        }

        for (int index = 1; index < speeds.Length; index++)
        {
            if (speeds[index].MeasuredAt - speeds[index - 1].MeasuredAt > TimeSpan.FromSeconds(2))
            {
                return null;
            }
        }

        double meanTime = speeds.Average(item => (item.MeasuredAt - speeds[0].MeasuredAt).TotalSeconds);
        double meanSpeed = speeds.Average(item => item.Speed!.Value);
        double denominator = speeds.Sum(item => Math.Pow(
            (item.MeasuredAt - speeds[0].MeasuredAt).TotalSeconds - meanTime, 2));
        return speeds.Sum(item => ((item.MeasuredAt - speeds[0].MeasuredAt).TotalSeconds - meanTime)
            * (item.Speed!.Value - meanSpeed)) / denominator;
    }

    private static double StandardDeviation(double[] values)
    {
        double mean = values.Average();
        return Math.Sqrt(values.Average(value => (value - mean) * (value - mean)));
    }

    private static bool SpeedSourcesConflict(RoadSourceEvidence gps, RoadSourceEvidence vehicle)
        => gps.Speed is { } gpsSpeed && vehicle.Speed is { } vehicleSpeed
            && (Math.Abs(gpsSpeed - vehicleSpeed) > 3
                || (gps.Acceleration is { } gpsAcceleration && vehicle.Acceleration is { } vehicleAcceleration
                    && Math.Abs(gpsAcceleration - vehicleAcceleration) > 1.5));

    private static (RoadCondition Condition, int Level, int Confidence)? Recognize(
        RoadLearningSample[] samples, RoadSourceEvidence[] sources, DateTimeOffset now)
    {
        RoadSourceEvidence gps = sources[(int)LearningSource.Gps];
        RoadSourceEvidence vehicle = sources[(int)LearningSource.Vehicle];
        RoadSourceEvidence imu = sources[(int)LearningSource.Internal];
        LearningSource speedSource = vehicle.Speed is not null ? LearningSource.Vehicle : LearningSource.Gps;
        RoadSourceEvidence speedEvidence = sources[(int)speedSource];
        double? speed = speedEvidence.Speed;
        double? acceleration = speedEvidence.Acceleration;
        if (speed is null)
        {
            return null;
        }

        if (SpeedSourcesConflict(gps, vehicle))
        {
            return null;
        }

        RoadWindow w = new(samples, speedSource, now);
        RoadLearningSample[] recent = [.. w.Speeds.Where(item => now - item.MeasuredAt <= TimeSpan.FromSeconds(8))];
        double? earlierSpeed = recent.Length > 0 ? recent[0].Speed : null;
        double? fusedPitch = w.MeanPitch(Window.TotalSeconds - 5, Window.TotalSeconds);
        // Fusion support lapses between quiet stops; fall back to damped GPS altitude grade and course-rate lateral.
        double? pitch = fusedPitch ?? w.AltitudeGrade(Window.TotalSeconds - 12, Window.TotalSeconds);
        bool pitchFromAltitude = fusedPitch is null && pitch is not null;
        RoadLearningSample? lastImu = samples.LastOrDefault(item => item.Source == LearningSource.Internal
            && now - item.MeasuredAt <= TimeSpan.FromSeconds(2));
        double? lateral = lastImu?.SupportedLateralAcceleration ?? w.CourseLateral(3);
        bool lateralFromCourse = lastImu?.SupportedLateralAcceleration is null && lateral is not null;
        StopInterval? last = w.Stops.LastOrDefault();
        double standstillSpeed = w.StandstillDamping > 1 && w.CurrentStandstillSpeed is { } damped ? damped : speed.Value;
        RoadCondition? condition = null;
        int level = 0;
        bool semantic = false, usesPitch = false, usesLateral = false, usesLongitudinal = false;
        bool usesVibration = false, usesCourse = false;

        if (w.PeakDeceleration(4) > 5.5)
        {
            condition = RoadCondition.EmergencyBraking;
        }
        else if (w.MaxSpeed < 3 && speed < 3 && w.Stops.Count(stop => stop.Approach > 0.8) >= 2 && w.AnyReverse(out bool viaCourse))
        {
            condition = RoadCondition.ParallelParking;
            semantic = true;
            usesLongitudinal = !viaCourse;
            usesCourse = viaCourse;
        }
        else if (speed is > 0.3 and < 4 && w.ReversingNow(out bool reverseViaCourse))
        {
            condition = RoadCondition.Reversing;
            semantic = true;
            usesLongitudinal = !reverseViaCourse;
            usesCourse = reverseViaCourse;
        }
        else if (w.Stops.Count(stop => stop.Approach > 1.5) >= 2 && w.MaxSpeed < 12)
        {
            condition = RoadCondition.StopAndGo;
            semantic = true;
        }
        else if (standstillSpeed < w.StandstillThreshold && earlierSpeed > 2)
        {
            double decel = w.PeakDeceleration(8);
            if (pitch is { } p && p >= 2)
            {
                condition = decel < 1.5 ? RoadCondition.UphillStop : RoadCondition.UphillBrakingToStandstill;
                level = Level(p, 2);
            }
            else if (pitch is { } d && d <= -2)
            {
                condition = RoadCondition.DownhillBrakingToStandstill;
                level = Level(-d, 2);
            }
            else
            {
                condition = pitch is null ? RoadCondition.BrakingToStandstill : RoadCondition.LevelBrakingToStandstill;
                level = Level(decel, 0.7);
            }

            usesPitch = pitch is not null;
        }
        else if (speed > 1 && earlierSpeed < 0.6 && acceleration > 0.3)
        {
            if (pitch is { } p && p >= 2)
            {
                condition = RoadCondition.UphillStart;
                level = Level(p, 2);
            }
            else
            {
                condition = pitch is { } d && Math.Abs(d) < 2 ? RoadCondition.LevelStart : RoadCondition.StartingFromStandstill;
                level = Level(acceleration.Value, 0.7);
            }

            usesPitch = pitch is not null;
        }
        else if (last is { End: null } stopped && standstillSpeed < w.StandstillThreshold)
        {
            double duration = w.LastT - stopped.Start;
            if (stopped.Approach > 2 && duration >= 12)
            {
                condition = RoadCondition.WaitingAtTrafficLight;
                semantic = true;
            }
            else if (stopped.Approach == 0 && duration >= 3)
            {
                condition = RoadCondition.Stopped;
            }
        }
        else if (last is { End: { } end, Approach: > 3 } completed && speed > 1
            && completed.End - completed.Start is >= 0.5 and <= 10 && w.LastT - end is >= 3 and <= 14)
        {
            double? turn = w.CourseChange(end);
            bool turned = turn is { } t && Math.Abs(t) >= 45;
            bool lateralTurn = w.MaxLateral(end) >= 1.5;
            if (turned || lateralTurn)
            {
                condition = RoadCondition.IntersectionStop;
                usesCourse = turned;
                usesLateral = !turned;
                semantic = true;
            }
            else if (completed.End - completed.Start <= 6 && w.LastT - end >= 6)
            {
                condition = RoadCondition.WaitingAtStopSign;
                semantic = true;
            }
        }

        if (condition is null && speed > 3)
        {
            double? ramp = w.CourseChange(0);
            bool rampByCourse = ramp is { } r && Math.Abs(r) >= 45;
            bool rampEvidence = rampByCourse || w.MaxLateral(0) >= 1.5;
            if (w.SpeedBins.Length >= 20 && w.SpeedBins.Min(bin => bin.V) > 20 && speed > 22
                && acceleration is { } steady && Math.Abs(steady) < 0.6)
            {
                condition = RoadCondition.HighwayDriving;
                semantic = true;
            }
            else if (rampEvidence && speed > 22 && w.MinSpeedBetween(0, 10) < 17)
            {
                condition = RoadCondition.EnterHighway;
            }
            else if (rampEvidence && speed < 17 && w.MaxSpeedBetween(0, 10) > 22)
            {
                condition = RoadCondition.ExitHighway;
            }

            if (condition is not null and not RoadCondition.HighwayDriving)
            {
                semantic = true;
                usesCourse = rampByCourse;
                usesLateral = !rampByCourse;
            }
        }

        if (condition is null && speed > 3 && pitch is { } late
            && (pitchFromAltitude ? w.AltitudeGrade(0, 12) : w.MeanPitch(0, 10)) is { } early)
        {
            if (Math.Abs(early) < 1.5 && late >= 2.5)
            {
                (condition, level) = (RoadCondition.LevelToUphill, Level(late, 2));
            }
            else if (Math.Abs(early) < 1.5 && late <= -2.5)
            {
                (condition, level) = (RoadCondition.LevelToDownhill, Level(-late, 2));
            }
            else if (early <= -2.5 && late >= 2.5)
            {
                condition = RoadCondition.DownhillToUphill;
            }
            else if (early >= 2.5 && late <= -2.5)
            {
                condition = RoadCondition.UphillToDownhill;
            }

            usesPitch = condition is not null;
        }

        if (condition is null && speed > 2 && lateral is { } lat && Math.Abs(lat) > 0.6)
        {
            bool right = lat > 0;
            condition = pitch switch
            {
                >= 2 => right ? RoadCondition.UphillRightCurve : RoadCondition.UphillLeftCurve,
                <= -2 => right ? RoadCondition.DownhillRightCurve : RoadCondition.DownhillLeftCurve,
                _ => right ? RoadCondition.RightCurve : RoadCondition.LeftCurve
            };
            usesLateral = true;
            usesPitch = pitch is >= 2 or <= -2;
            level = Level(Math.Abs(lat), 0.8);
        }

        if (condition is null && acceleration is { } a && Math.Abs(a) > 0.4)
        {
            condition = a > 0 ? RoadCondition.Acceleration : RoadCondition.Braking;
            level = Level(Math.Abs(a), 0.7);
        }

        if (condition is null && speed > 2 && imu.Vibration > 0.035)
        {
            // Separate slow waves from fast roughness using one-second magnitude means.
            double[] bins = [.. samples.Where(item => item.Source == LearningSource.Internal
                && item.AccelerationMagnitude is not null)
                .GroupBy(item => (long)(item.MeasuredAt - samples[0].ReceivedAt).TotalSeconds)
                .Select(group => group.Average(item => item.AccelerationMagnitude!.Value))];
            bool wavy = bins.Length >= 10 && StandardDeviation(bins) > imu.Vibration * 0.65;
            condition = wavy ? RoadCondition.WavyRoad : RoadCondition.RoughRoad;
            level = Level(imu.Vibration.Value, 0.04);
            usesVibration = true;
        }

        if (condition is null)
        {
            return null;
        }

        bool usesAltitude = usesPitch && pitchFromAltitude;
        if (usesAltitude)
        {
            usesPitch = false;
        }

        if (usesLateral && lateralFromCourse)
        {
            usesLateral = false;
            usesCourse = true;
        }

        bool usesImu = usesVibration || usesLateral || usesPitch || usesLongitudinal;
        sources[(int)speedSource] = speedEvidence with
        {
            Contribution = usesImu ? 1 : 2,
            Explanation = "Measured speed and source-time speed slope; not fused speed."
                + (w.StandstillDamping > 1 ? $" Standstill found with {w.StandstillDamping}-second median damping." : "")
        };
        if (speedSource == LearningSource.Vehicle && (usesCourse || usesAltitude
            || (gps.Acceleration is { } ga && vehicle.Acceleration is { } va && Math.Abs(ga - va) < 0.5)))
        {
            sources[(int)LearningSource.Gps] = gps with
            {
                Contribution = usesAltitude ? 2 : 1,
                Explanation = usesAltitude ? "Damped GPS altitude-versus-distance grade (fusion pitch unsupported)."
                    : usesCourse ? "GPS course change/reversal over the window."
                    : "GPS speed slope agrees with vehicle speed slope (correlated evidence)."
            };
        }
        else if (usesCourse || usesAltitude)
        {
            sources[(int)LearningSource.Gps] = sources[(int)LearningSource.Gps] with
            {
                Contribution = 3,
                Explanation = usesAltitude ? "GPS speed plus damped altitude-versus-distance grade."
                    : "GPS speed plus GPS course change/reversal over the window."
            };
        }

        if (usesImu)
        {
            List<string> used = [];
            if (usesPitch) { used.Add("supported pitch (grade)"); }
            if (usesLateral) { used.Add("supported lateral acceleration; bank and turn remain confounded"); }
            if (usesLongitudinal) { used.Add("supported longitudinal acceleration opposing speed slope"); }
            if (usesVibration) { used.Add("device-frame acceleration magnitude variability"); }
            sources[(int)LearningSource.Internal] = imu with
            {
                Contribution = 2,
                Explanation = string.Join("; ", used) + "."
            };
        }

        RoadSourceEvidence bluetooth = sources[(int)LearningSource.Bluetooth];
        if (usesVibration && bluetooth.Vibration is { } vibration && imu.Vibration is { } internalVibration
            && Math.Abs(vibration - internalVibration) < 0.04)
        {
            sources[(int)LearningSource.Bluetooth] = bluetooth with
            {
                Contribution = 1,
                Explanation = "Independent device acceleration variability corroborates vibration."
            };
        }

        // Semantic causes (light, sign, ramp, parking) are inferred from motion only, so they start lower.
        int confidence = (semantic ? 45 : 55) + (sources.Count(source => source.Contribution > 0) - 1) * 10;
        return (condition.Value, RoadConditions.HasLevel(condition.Value) ? Math.Max(1, level) : 0,
            Math.Min(85, confidence));
    }

    private static int Level(double value, double step) => Math.Clamp((int)Math.Ceiling(value / step), 1, 5);

    private static double WrapDegrees(double degrees) => ((degrees % 360) + 540) % 360 - 180;

    private sealed record StopInterval(double Start, double? End, double Approach);

    /// <summary>Window features on a relative time axis (0 = window start, 30 = now).</summary>
    private sealed class RoadWindow
    {
        private readonly RoadLearningSample[] _samples;
        private readonly DateTimeOffset _start;

        internal RoadWindow(RoadLearningSample[] samples, LearningSource speedSource, DateTimeOffset now)
        {
            _samples = samples;
            _start = now - Window;
            LastT = Window.TotalSeconds;
            Speeds = [.. samples.Where(item => item.Source == speedSource && item.Speed is not null)];
            SpeedBins = [.. Speeds.GroupBy(item => (long)Math.Floor(T(item)))
                .OrderBy(group => group.Key)
                .Select(group => (T: T(group.Last()), V: group.Last().Speed!.Value))];
            MaxSpeed = SpeedBins.Length > 0 ? SpeedBins.Max(bin => bin.V) : 0;
            // Receiver speed jitters near zero; escalate damping until a clean standstill emerges.
            foreach ((int width, double enter, double exit) in StandstillDampingLevels)
            {
                (double T, double V)[] bins = width == 1 ? SpeedBins : Median(SpeedBins, width);
                List<StopInterval> stops = DetectStops(bins, enter, exit);
                StandstillDamping = width;
                StandstillThreshold = enter;
                CurrentStandstillSpeed = bins.Length > 0 ? bins[^1].V : null;
                Stops = stops;
                if (bins.Length == 0 || bins.Min(bin => bin.V) >= NearStandstillSpeed || stops.Count > 0)
                {
                    break;
                }
            }
        }

        /// <summary>
        ///  Median width with stop enter/exit speeds. Escalation runs only while no standstill is found at all,
        ///  so later levels tolerate a receiver whose standstill speed never reads below the raw stop speed.
        /// </summary>
        internal static readonly (int Width, double Enter, double Exit)[] StandstillDampingLevels =
            [(1, 0.6, 1.0), (3, 1.0, 1.4), (5, 1.0, 1.4)];
        private const double NearStandstillSpeed = 1.5;

        private static (double T, double V)[] Median((double T, double V)[] bins, int width)
        {
            // Full-width windows shifted inward at the edges, so the newest bin is not biased by a half window.
            width = Math.Min(width, bins.Length);
            return [.. bins.Select((bin, index) =>
            {
                int first = Math.Clamp(index - width / 2, 0, bins.Length - width);
                double[] neighbourhood = [.. bins[first..(first + width)].Select(item => item.V).Order()];
                return (bin.T, neighbourhood[neighbourhood.Length / 2]);
            })];
        }

        private static List<StopInterval> DetectStops((double T, double V)[] bins, double enter, double exit)
        {
            List<StopInterval> stops = [];
            bool stopped = false;
            double approach = 0;
            foreach ((double t, double v) in bins)
            {
                if (!stopped)
                {
                    approach = Math.Max(approach, v);
                    if (v < enter)
                    {
                        stopped = true;
                        stops.Add(new(t, null, approach));
                        approach = 0;
                    }
                }
                else if (v > exit)
                {
                    stopped = false;
                    stops[^1] = stops[^1] with { End = t };
                    approach = v;
                }
            }

            return stops;
        }

        /// <summary>Median width (1 = raw) needed to reveal a standstill in noisy speed.</summary>
        internal int StandstillDamping { get; private set; } = 1;
        /// <summary>Speed below which the (damped) vehicle counts as stopped.</summary>
        internal double StandstillThreshold { get; private set; } = 0.6;
        /// <summary>The latest speed after standstill damping.</summary>
        internal double? CurrentStandstillSpeed { get; private set; }

        internal RoadLearningSample[] Speeds { get; }
        internal (double T, double V)[] SpeedBins { get; }
        internal List<StopInterval> Stops { get; private set; } = [];
        internal double MaxSpeed { get; }
        internal double LastT { get; }

        private double T(RoadLearningSample sample) => (sample.MeasuredAt - _start).TotalSeconds;

        internal double MinSpeedBetween(double from, double to)
            => SpeedBins.Where(bin => bin.T >= from && bin.T < to).Select(bin => bin.V).DefaultIfEmpty(double.NaN).Min();

        internal double MaxSpeedBetween(double from, double to)
            => SpeedBins.Where(bin => bin.T >= from && bin.T < to).Select(bin => bin.V).DefaultIfEmpty(double.NaN).Max();

        internal double PeakDeceleration(double seconds)
        {
            double peak = 0;
            for (int index = 1; index < SpeedBins.Length; index++)
            {
                (double t0, double v0) = SpeedBins[index - 1];
                (double t1, double v1) = SpeedBins[index];
                if (t1 >= LastT - seconds && t1 - t0 is > 0.25 and <= 2.5)
                {
                    peak = Math.Max(peak, (v0 - v1) / (t1 - t0));
                }
            }

            return peak;
        }

        internal double? MeanPitch(double from, double to)
        {
            double[] values = [.. _samples.Where(item => item.Source == LearningSource.Internal
                && item.SupportedPitch is not null && T(item) >= from && T(item) <= to)
                .Select(item => item.SupportedPitch!.Value)];
            return values.Length >= 5 ? values.Average() : null;
        }

        internal double MaxLateral(double from)
            => _samples.Where(item => item.Source == LearningSource.Internal
                && item.SupportedLateralAcceleration is not null && T(item) >= from)
                .Select(item => Math.Abs(item.SupportedLateralAcceleration!.Value)).DefaultIfEmpty(0).Max();

        /// <summary>Net unwrapped GPS course change while moving; null without GPS course evidence.</summary>
        internal double? CourseChange(double from)
        {
            double[] courses = [.. _samples.Where(item => item.Source == LearningSource.Gps
                && item.GpsCourseDegrees is not null && item.Speed > 2 && T(item) >= from)
                .Select(item => item.GpsCourseDegrees!.Value)];
            return courses.Length < 2 ? null
                : courses.Zip(courses.Skip(1)).Sum(pair => WrapDegrees(pair.Second - pair.First));
        }

        /// <summary>Centripetal acceleration (m/s², right positive) from GPS course rate and speed.</summary>
        internal double? CourseLateral(double seconds)
        {
            RoadLearningSample[] gps = [.. _samples.Where(item => item.Source == LearningSource.Gps
                && item.GpsCourseDegrees is not null && item.Speed > 2 && T(item) >= LastT - seconds)];
            if (gps.Length < 2)
            {
                return null;
            }

            double dt = T(gps[^1]) - T(gps[0]);
            if (dt < 1)
            {
                return null;
            }

            double turn = gps.Zip(gps.Skip(1)).Sum(pair => WrapDegrees(pair.Second.GpsCourseDegrees!.Value - pair.First.GpsCourseDegrees!.Value));
            return turn / dt * Math.PI / 180 * gps.Average(item => item.Speed!.Value);
        }

        /// <summary>
        ///  Road grade in degrees from GPS altitude against travelled distance. The regression over many
        ///  seconds is deliberate damping: single altitude fixes are far too noisy for grade.
        /// </summary>
        internal double? AltitudeGrade(double from, double to)
        {
            RoadLearningSample[] gps = [.. _samples.Where(item => item.Source == LearningSource.Gps
                && item.GpsAltitudeMeters is not null && item.Speed is not null && T(item) >= from && T(item) <= to)];
            if (gps.Length < 5)
            {
                return null;
            }

            double[] distance = new double[gps.Length];
            for (int index = 1; index < gps.Length; index++)
            {
                double dt = T(gps[index]) - T(gps[index - 1]);
                distance[index] = distance[index - 1] + dt * (gps[index].Speed!.Value + gps[index - 1].Speed!.Value) / 2;
            }

            if (distance[^1] < 50)
            {
                return null;
            }

            double meanDistance = distance.Average();
            double meanAltitude = gps.Average(item => item.GpsAltitudeMeters!.Value);
            double denominator = distance.Sum(d => (d - meanDistance) * (d - meanDistance));
            double slope = distance.Zip(gps).Sum(pair => (pair.First - meanDistance)
                * (pair.Second.GpsAltitudeMeters!.Value - meanAltitude)) / denominator;
            return Math.Atan(slope) * 180 / Math.PI;
        }

        /// <summary>Speed magnitude rising while the vehicle-frame longitudinal acceleration is negative (or vice versa).</summary>
        internal bool ReverseBetween(double from, double to)
        {
            RoadLearningSample[] speeds = [.. Speeds.Where(item => T(item) >= from && T(item) <= to)];
            double[] longitudinal = [.. _samples.Where(item => item.Source == LearningSource.Internal
                && item.SupportedLongitudinalAcceleration is not null && T(item) >= from && T(item) <= to)
                .Select(item => item.SupportedLongitudinalAcceleration!.Value)];
            if (speeds.Length < 3 || longitudinal.Length < 3)
            {
                return false;
            }

            double meanSpeed = speeds.Average(item => item.Speed!.Value);
            double meanTime = speeds.Average(T);
            double denominator = speeds.Sum(item => Math.Pow(T(item) - meanTime, 2));
            if (meanSpeed is < 0.2 or > 4 || denominator <= 0)
            {
                return false;
            }

            double slope = speeds.Sum(item => (T(item) - meanTime) * (item.Speed!.Value - meanSpeed)) / denominator;
            double meanLongitudinal = longitudinal.Average();
            return Math.Abs(slope) > 0.2 && Math.Abs(meanLongitudinal) > 0.2
                && Math.Sign(slope) != Math.Sign(meanLongitudinal);
        }

        /// <summary>GPS course flips by more than 150 degrees across a standstill.</summary>
        internal bool CourseFlipAt(StopInterval stop)
        {
            if (stop.End is not { } end)
            {
                return false;
            }

            RoadLearningSample? before = _samples.LastOrDefault(item => item.Source == LearningSource.Gps
                && item.GpsCourseDegrees is not null && item.Speed > 0.8 && T(item) <= stop.Start);
            RoadLearningSample? after = _samples.FirstOrDefault(item => item.Source == LearningSource.Gps
                && item.GpsCourseDegrees is not null && item.Speed > 0.8 && T(item) >= end);
            return before is not null && after is not null
                && Math.Abs(WrapDegrees(after.GpsCourseDegrees!.Value - before.GpsCourseDegrees!.Value)) > 150;
        }

        internal bool ReversingNow(out bool viaCourse)
        {
            viaCourse = false;
            if (ReverseBetween(LastT - 4, LastT))
            {
                return true;
            }

            // Assumes the window began in forward motion; an odd number of course flips means reverse.
            int flips = Stops.Count(CourseFlipAt);
            viaCourse = flips % 2 == 1 && Stops.Count > 0 && Stops[^1].End is { } end && LastT - end <= 10;
            return viaCourse;
        }

        internal bool AnyReverse(out bool viaCourse)
        {
            viaCourse = false;
            for (double from = 0; from + 3 <= LastT; from += 1)
            {
                if (ReverseBetween(from, from + 3))
                {
                    return true;
                }
            }

            viaCourse = Stops.Any(CourseFlipAt);
            return viaCourse;
        }
    }
}