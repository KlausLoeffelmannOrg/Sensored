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
    double? GpsCourseDegrees = null, double? SupportedLongitudinalAcceleration = null);

internal sealed record RoadSourceEvidence(
    string Abbreviation, string Sensors, int Samples, double? Speed, double? Acceleration,
    double? Rpm, double? Vibration, double? Pitch, double? AngularRate,
    double Contribution, string Explanation);

internal sealed record RoadLearningQuestion(
    Guid Id, Guid SessionId, DateTimeOffset Start, DateTimeOffset End,
    RoadCondition Condition, int Level, int Confidence, string ModelVersion,
    string Profile, string? RecordingPath, RoadSourceEvidence[] Sources, RoadLearningSample[] Samples,
    long Continuity);

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
    private DateTimeOffset? _startedWindow;
    private DateTimeOffset? _lastReceived;
    private int? _epoch;
    private RoadCondition? _lastAsked;
    private RoadCondition? _candidate;
    private DateTimeOffset _candidateSince;
    private DateTimeOffset _pausedUntil;
    private bool _active = true;
    private long _continuity;

    internal RoadLearningSession(string profile)
    {
        Profile = profile;
    }

    internal Guid Id { get; } = Guid.NewGuid();
    internal string Profile { get; }
    internal bool Active { get { lock (_gate) { return _active; } } }
    internal DateTimeOffset PausedUntil { get { lock (_gate) { return _pausedUntil; } } }
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
            ResetWindow();
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

    internal void BreakContinuity()
    {
        lock (_gate) { ResetWindow(); }
    }

    private void ResetWindow()
    {
        _continuity++;
        _samples.Clear();
        _identities.Clear();
        _lastSample.Clear();
        _startedWindow = null;
        _lastReceived = null;
        _epoch = null;
        _candidate = null;
    }

    internal void Observe(RoadLearningSample sample)
    {
        lock (_gate)
        {
            if (!_active)
            {
                return;
            }

            if ((_epoch is { } epoch && epoch != sample.Epoch)
                || (_lastReceived is { } last && (sample.ReceivedAt < last
                    || sample.ReceivedAt - last > TimeSpan.FromSeconds(3)))
                || (_identities.TryGetValue(sample.Source, out var identity)
                    && identity != (sample.SourceId, sample.Generation)))
            {
                ResetWindow();
            }

            _epoch = sample.Epoch;
            _identities[sample.Source] = (sample.SourceId, sample.Generation);
            _lastReceived = sample.ReceivedAt;
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
            _startedWindow ??= sample.ReceivedAt;
            _samples.Enqueue(sample);
            while (_samples.TryPeek(out RoadLearningSample? first)
                && sample.ReceivedAt - first.ReceivedAt > Window)
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

            RoadLearningSample[] samples = [.. _samples.Where(item => now - item.ReceivedAt <= Window
                && now >= item.MeasuredAt && now - item.MeasuredAt <= Window)];
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
        if (speeds.Length < 4 || (speeds[^1].MeasuredAt - speeds[0].MeasuredAt).TotalSeconds < 1)
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

        if (gps.Speed is { } gpsSpeed && vehicle.Speed is { } vehicleSpeed
            && (Math.Abs(gpsSpeed - vehicleSpeed) > 3
                || (gps.Acceleration is { } gpsAcceleration && vehicle.Acceleration is { } vehicleAcceleration
                    && Math.Abs(gpsAcceleration - vehicleAcceleration) > 1.5)))
        {
            return null;
        }

        RoadWindow w = new(samples, speedSource, now);
        RoadLearningSample[] recent = [.. w.Speeds.Where(item => now - item.MeasuredAt <= TimeSpan.FromSeconds(8))];
        double? earlierSpeed = recent.Length > 0 ? recent[0].Speed : null;
        double? pitch = w.MeanPitch(Window.TotalSeconds - 5, Window.TotalSeconds);
        RoadLearningSample? lastImu = samples.LastOrDefault(item => item.Source == LearningSource.Internal
            && now - item.MeasuredAt <= TimeSpan.FromSeconds(2));
        double? lateral = lastImu?.SupportedLateralAcceleration;
        StopInterval? last = w.Stops.LastOrDefault();
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
        else if (speed < 0.6 && earlierSpeed > 2)
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
        else if (last is { End: null } stopped && speed < 0.6)
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

        if (condition is null && speed > 3 && pitch is { } late && w.MeanPitch(0, 10) is { } early)
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

        bool usesImu = usesVibration || usesLateral || usesPitch || usesLongitudinal;
        sources[(int)speedSource] = speedEvidence with
        {
            Contribution = usesImu ? 1 : 2,
            Explanation = "Measured speed and source-time speed slope; not fused speed."
        };
        if (speedSource == LearningSource.Vehicle && (usesCourse
            || (gps.Acceleration is { } ga && vehicle.Acceleration is { } va && Math.Abs(ga - va) < 0.5)))
        {
            sources[(int)LearningSource.Gps] = gps with
            {
                Contribution = 1,
                Explanation = usesCourse ? "GPS course change/reversal over the window."
                    : "GPS speed slope agrees with vehicle speed slope (correlated evidence)."
            };
        }
        else if (usesCourse)
        {
            sources[(int)LearningSource.Gps] = sources[(int)LearningSource.Gps] with
            {
                Contribution = 3,
                Explanation = "GPS speed plus GPS course change/reversal over the window."
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
            bool stopped = false;
            double approach = 0;
            foreach ((double t, double v) in SpeedBins)
            {
                if (!stopped)
                {
                    approach = Math.Max(approach, v);
                    if (v < 0.6)
                    {
                        stopped = true;
                        Stops.Add(new(t, null, approach));
                        approach = 0;
                    }
                }
                else if (v > 1.0)
                {
                    stopped = false;
                    Stops[^1] = Stops[^1] with { End = t };
                    approach = v;
                }
            }
        }

        internal RoadLearningSample[] Speeds { get; }
        internal (double T, double V)[] SpeedBins { get; }
        internal List<StopInterval> Stops { get; } = [];
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