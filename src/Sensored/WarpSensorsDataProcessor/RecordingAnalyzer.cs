using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Streams a recording once and accumulates analysis statistics.
/// </summary>
internal sealed class RecordingAnalyzer
{
    private const int MaxIntervalBucketExclusive = 100;
    private const int MaxFutureGpsExamples = 20;
    private const double ParkedSpeedMetersPerSecond = 0.5d;
    private const double MovingSpeedMetersPerSecond = 2d;
    private const double SpeedChangeThresholdMps = 0.5d;
    private const double TurnCourseDegrees = 5d;

    private readonly string _recordingFolder;
    private readonly List<AnalyzePartSummary> _parts = [];
    private readonly Dictionary<string, RangeAccumulator> _ranges;
    private readonly RangeAccumulator _imuInterval = new();
    private readonly RangeAccumulator _gpsInterval = new();
    private readonly RangeAccumulator _imuLag = new();
    private readonly RangeAccumulator _gpsLag = new();
    private readonly RangeAccumulator _compassConfidence = new();
    private readonly RangeAccumulator _gpsSpeed = new();
    private readonly RangeAccumulator _gyroMagnitude = new();
    private readonly RangeAccumulator _accelMagnitude = new();
    private readonly RangeAccumulator _speedDerivative = new();
    private readonly RangeAccumulator _courseRate = new();
    private readonly long[] _imuIntervalBuckets = new long[MaxIntervalBucketExclusive];
    private readonly long[] _gpsIntervalBuckets = new long[MaxIntervalBucketExclusive];
    private readonly long[] _compassTenths = new long[11];
    private readonly List<AnalyzeFutureGpsExample> _futureGpsExamples = [];

    private long _rows;
    private long _imu;
    private long _geolocation;
    private long _health;
    private long _gap;
    private long _classification;
    private long? _previousRow;
    private long _rowRegressions;
    private long _rowGaps;
    private long _duplicateRows;
    private long? _previousSequence;
    private long _sequenceRegressions;
    private DateTimeOffset? _previousSource;
    private DateTimeOffset? _previousArrival;
    private long _sourceRegressions;
    private long _arrivalRegressions;
    private DateTimeOffset? _previousImuSource;
    private DateTimeOffset? _previousGpsSource;
    private long _duplicateImuSource;
    private long _duplicateGpsSource;
    private DateTimeOffset? _firstSource;
    private DateTimeOffset? _lastSource;
    private long _imuIntervalOverflow;
    private long _gpsIntervalOverflow;
    private long _imuGyroAvailable;
    private long _imuGyroMissing;
    private long _imuCompassFinite;
    private long _imuCompassMissing;
    private long _gpsSpeedValid;
    private long _gpsSpeedInvalid;
    private long _gpsCourseValid;
    private long _gpsCourseInvalid;
    private long _gpsPositionFinite;
    private long _futureGpsCount;
    private double? _maxFutureLead;
    private DateTimeOffset? _origin;
    private double? _firstSpeedAbove2Seconds;
    private bool? _stationary;
    private DateTimeOffset? _motionWindowStart;
    private DateTimeOffset? _lastGpsTime;
    private long _stationaryWindows;
    private long _movingWindows;
    private double _stationaryDuration;
    private double _movingDuration;
    private double? _longestStationary;
    private DateTimeOffset? _previousGpsFixTime;
    private double? _previousGpsSpeed;
    private double? _previousGpsCourse;
    private long _gpsSpeedChangeIntervals;
    private long _gpsSpeedChangeAboveHalf;
    private long _turnIntervals;
    private double? _maxAbsSpeedDerivative;
    private double? _maxAbsCourseRate;
    private double? _maxGpsGap;
    private long _gpsGapsOverTwoSeconds;

    /// <summary>
    ///  Initializes a streaming analyzer.
    /// </summary>
    /// <param name="recordingFolder">The recording folder being read.</param>
    internal RecordingAnalyzer(string recordingFolder)
    {
        _recordingFolder = recordingFolder;
        _ranges = new Dictionary<string, RangeAccumulator>(StringComparer.Ordinal)
        {
            ["imuAx"] = new(),
            ["imuAy"] = new(),
            ["imuAz"] = new(),
            ["imuGx"] = new(),
            ["imuGy"] = new(),
            ["imuGz"] = new(),
            ["imuMagDeg"] = new(),
            ["gpsLat"] = new(),
            ["gpsLon"] = new(),
            ["gpsSpeedMps"] = new(),
            ["gpsCourseDeg"] = new()
        };
    }

    /// <summary>
    ///  Observes every row of one part file.
    /// </summary>
    internal async Task ObservePartAsync(string partPath, CancellationToken cancellationToken)
    {
        long partRows = 0;
        long? firstRow = null;
        long? lastRow = null;
        DateTimeOffset? firstSource = null;
        DateTimeOffset? lastSource = null;

        await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadPartAsync(
            partPath,
            cancellationToken))
        {
            Observe(row);
            partRows++;
            firstRow ??= row.Row;
            lastRow = row.Row;
            firstSource ??= row.Envelope.SourceTimestamp;
            lastSource = row.Envelope.SourceTimestamp;
        }

        _parts.Add(
            new AnalyzePartSummary
            {
                FileName = Path.GetFileName(partPath),
                RowCount = partRows,
                FirstRow = firstRow,
                LastRow = lastRow,
                FirstSourceTimestamp = FormatTimestamp(firstSource),
                LastSourceTimestamp = FormatTimestamp(lastSource)
            });
    }

    /// <summary>
    ///  Builds the invariant report.
    /// </summary>
    internal AnalyzeReport Complete()
    {
        CloseMotionWindow(_lastGpsTime);

        return new AnalyzeReport
        {
            RecordingFolder = _recordingFolder,
            Counts = new AnalyzeCounts
            {
                Rows = _rows,
                Imu = _imu,
                Geolocation = _geolocation,
                Health = _health,
                Gap = _gap,
                Classification = _classification
            },
            Continuity = new AnalyzeContinuity
            {
                RowContiguous = _rowRegressions == 0 && _rowGaps == 0 && _duplicateRows == 0,
                RowRegressions = _rowRegressions,
                RowGaps = _rowGaps,
                DuplicateRows = _duplicateRows,
                SequenceContiguous = _sequenceRegressions == 0,
                SequenceRegressions = _sequenceRegressions,
                SourceTimestampRegressions = _sourceRegressions,
                ArrivalTimestampRegressions = _arrivalRegressions,
                DuplicateImuSourceTimestamps = _duplicateImuSource,
                DuplicateGpsSourceTimestamps = _duplicateGpsSource
            },
            Parts = _parts,
            Timing = new AnalyzeTiming
            {
                FirstSourceTimestamp = FormatTimestamp(_firstSource),
                LastSourceTimestamp = FormatTimestamp(_lastSource),
                DurationSeconds = DurationSeconds(_firstSource, _lastSource),
                Imu = BuildSourceTiming(
                    _imu,
                    _imuInterval,
                    _imuLag,
                    _imuIntervalBuckets,
                    _imuIntervalOverflow),
                Geolocation = BuildSourceTiming(
                    _geolocation,
                    _gpsInterval,
                    _gpsLag,
                    _gpsIntervalBuckets,
                    _gpsIntervalOverflow)
            },
            Availability = new AnalyzeAvailability
            {
                ImuGyroAvailable = _imuGyroAvailable,
                ImuGyroMissing = _imuGyroMissing,
                ImuCompassFinite = _imuCompassFinite,
                ImuCompassMissing = _imuCompassMissing,
                GpsSpeedValid = _gpsSpeedValid,
                GpsSpeedInvalid = _gpsSpeedInvalid,
                GpsCourseValid = _gpsCourseValid,
                GpsCourseInvalid = _gpsCourseInvalid,
                GpsPositionFinite = _gpsPositionFinite
            },
            CompassConfidence = new AnalyzeCompassConfidence
            {
                Missing = _imuCompassMissing,
                Values = _compassConfidence.ToStats(),
                TenthsBuckets = BuildTenths()
            },
            Ranges = _ranges.ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value.ToStats(),
                StringComparer.Ordinal),
            FutureGps = new AnalyzeFutureGps
            {
                Count = _futureGpsCount,
                MaxLeadSeconds = _maxFutureLead,
                Examples = _futureGpsExamples
            },
            Motion = new AnalyzeMotion
            {
                FirstSpeedAbove2MpsSeconds = _firstSpeedAbove2Seconds,
                StationaryWindowCount = _stationaryWindows,
                StationaryDurationSeconds = _stationaryDuration,
                LongestStationarySeconds = _longestStationary,
                MovingWindowCount = _movingWindows,
                MovingDurationSeconds = _movingDuration,
                GpsSpeedMetersPerSecond = _gpsSpeed.ToStats()
            },
            Excitation = new AnalyzeExcitation
            {
                GyroMagnitudeDegreesPerSecond = _gyroMagnitude.ToStats(),
                AccelerometerMagnitudeG = _accelMagnitude.ToStats(),
                GpsSpeedChangeIntervals = _gpsSpeedChangeIntervals,
                GpsSpeedChangeAboveHalfMps = _gpsSpeedChangeAboveHalf,
                TurnIntervalsAtSpeed = _turnIntervals,
                MaxAbsGpsSpeedDerivativeMps2 = _maxAbsSpeedDerivative,
                MaxAbsCourseRateDegreesPerSecond = _maxAbsCourseRate,
                MaxGpsGapSeconds = _maxGpsGap,
                GpsGapsOverTwoSeconds = _gpsGapsOverTwoSeconds
            },
            GpsDerivatives = new AnalyzeGpsDerivatives
            {
                SpeedDerivativeMps2 = _speedDerivative.ToStats(),
                CourseRateDegreesPerSecond = _courseRate.ToStats()
            }
        };
    }

    private void Observe(SensorEnvelopeRow row)
    {
        SensorEnvelope envelope = row.Envelope;
        _rows++;
        _origin ??= envelope.SourceTimestamp;
        _firstSource ??= envelope.SourceTimestamp;
        _lastSource = envelope.SourceTimestamp;
        ObserveContinuity(row);

        switch (envelope.Kind)
        {
            case SensorDataKind.Imu:
                _imu++;
                ObserveImu(envelope);
                break;
            case SensorDataKind.Geolocation:
                _geolocation++;
                ObserveGps(row);
                break;
            case SensorDataKind.Health:
                _health++;
                break;
            case SensorDataKind.Gap:
                _gap++;
                break;
            case SensorDataKind.Classification:
                _classification++;
                break;
        }
    }

    private void ObserveContinuity(SensorEnvelopeRow row)
    {
        if (_previousRow is { } previousRow)
        {
            if (row.Row < previousRow)
            {
                _rowRegressions++;
            }
            else if (row.Row == previousRow)
            {
                _duplicateRows++;
            }
            else if (row.Row > previousRow + 1)
            {
                _rowGaps++;
            }
        }

        _previousRow = row.Row;

        if (_previousSequence is { } previousSequence && row.Envelope.Sequence < previousSequence)
        {
            _sequenceRegressions++;
        }

        _previousSequence = row.Envelope.Sequence;

        if (_previousSource is { } previousSource && row.Envelope.SourceTimestamp < previousSource)
        {
            _sourceRegressions++;
        }

        _previousSource = row.Envelope.SourceTimestamp;

        if (_previousArrival is { } previousArrival && row.Envelope.ArrivalTimestamp < previousArrival)
        {
            _arrivalRegressions++;
        }

        _previousArrival = row.Envelope.ArrivalTimestamp;
    }

    private void ObserveImu(SensorEnvelope envelope)
    {
        if (envelope.Imu is not { } imu)
        {
            return;
        }

        if (_previousImuSource is { } previous)
        {
            if (envelope.SourceTimestamp == previous)
            {
                _duplicateImuSource++;
            }

            ObserveInterval(
                envelope.SourceTimestamp - previous,
                _imuInterval,
                _imuIntervalBuckets,
                ref _imuIntervalOverflow);
        }

        _previousImuSource = envelope.SourceTimestamp;
        _imuLag.Add((envelope.ArrivalTimestamp - envelope.SourceTimestamp).TotalSeconds);
        _ranges["imuAx"].Add(imu.Acceleration.X);
        _ranges["imuAy"].Add(imu.Acceleration.Y);
        _ranges["imuAz"].Add(imu.Acceleration.Z);
        _ranges["imuGx"].Add(imu.AngularRate.X);
        _ranges["imuGy"].Add(imu.AngularRate.Y);
        _ranges["imuGz"].Add(imu.AngularRate.Z);
        _ranges["imuMagDeg"].Add(imu.MagneticHeadingDegrees);
        _accelMagnitude.Add(imu.Acceleration.Magnitude);

        if (imu.HasAngularRate)
        {
            _imuGyroAvailable++;
            _gyroMagnitude.Add(imu.AngularRate.Magnitude);
        }
        else
        {
            _imuGyroMissing++;
        }

        if (double.IsFinite(imu.MagneticHeadingDegrees))
        {
            _imuCompassFinite++;
            _compassConfidence.Add(imu.MagneticHeadingConfidence);
            int bucket = (int)Math.Clamp(Math.Round(imu.MagneticHeadingConfidence * 10d), 0d, 10d);
            _compassTenths[bucket]++;
        }
        else
        {
            _imuCompassMissing++;
        }
    }

    private void ObserveGps(SensorEnvelopeRow row)
    {
        SensorEnvelope envelope = row.Envelope;

        if (envelope.Geolocation is not { } geolocation)
        {
            return;
        }

        DateTimeOffset measurementTime = FusionTraceFormatting.GetGpsMeasurementTime(
            geolocation,
            envelope.SourceTimestamp);

        if (_previousGpsSource is { } previousSource && envelope.SourceTimestamp == previousSource)
        {
            _duplicateGpsSource++;
        }

        if (_previousGpsFixTime is { } previousFix)
        {
            TimeSpan gap = measurementTime - previousFix;
            ObserveInterval(gap, _gpsInterval, _gpsIntervalBuckets, ref _gpsIntervalOverflow);
            double gapSeconds = gap.TotalSeconds;

            if (double.IsFinite(gapSeconds) && gapSeconds > 0d)
            {
                _maxGpsGap = _maxGpsGap is { } currentMax
                    ? Math.Max(currentMax, gapSeconds)
                    : gapSeconds;

                if (gapSeconds > 2d)
                {
                    _gpsGapsOverTwoSeconds++;
                }

                ObserveGpsDerivatives(geolocation, gapSeconds);
            }
        }

        _previousGpsSource = envelope.SourceTimestamp;
        _previousGpsFixTime = measurementTime;
        _gpsLag.Add((envelope.ArrivalTimestamp - envelope.SourceTimestamp).TotalSeconds);
        _ranges["gpsLat"].Add(geolocation.LatitudeDegrees);
        _ranges["gpsLon"].Add(geolocation.LongitudeDegrees);
        _ranges["gpsSpeedMps"].Add(geolocation.SpeedMetersPerSecond);
        _ranges["gpsCourseDeg"].Add(geolocation.CourseDegrees);

        if (geolocation.SpeedValid && double.IsFinite(geolocation.SpeedMetersPerSecond))
        {
            _gpsSpeedValid++;
            _gpsSpeed.Add(geolocation.SpeedMetersPerSecond);
            ObserveMotion(measurementTime, geolocation.SpeedMetersPerSecond);
        }
        else
        {
            _gpsSpeedInvalid++;
        }

        if (geolocation.CourseValid)
        {
            _gpsCourseValid++;
        }
        else
        {
            _gpsCourseInvalid++;
        }

        if (double.IsFinite(geolocation.LatitudeDegrees) && double.IsFinite(geolocation.LongitudeDegrees))
        {
            _gpsPositionFinite++;
        }

        if (_previousImuSource is { } imuTime
            && measurementTime != default
            && measurementTime > imuTime)
        {
            double lead = (measurementTime - imuTime).TotalSeconds;
            _futureGpsCount++;
            _maxFutureLead = _maxFutureLead is { } currentLead
                ? Math.Max(currentLead, lead)
                : lead;

            if (_futureGpsExamples.Count < MaxFutureGpsExamples)
            {
                _futureGpsExamples.Add(
                    new AnalyzeFutureGpsExample
                    {
                        Row = row.Row,
                        LeadSeconds = lead,
                        GpsMeasurementTimestamp = FusionTraceFormatting.FormatTimestamp(measurementTime),
                        PrecedingImuTimestamp = FusionTraceFormatting.FormatTimestamp(imuTime)
                    });
            }
        }

        if (geolocation.SpeedValid)
        {
            _previousGpsSpeed = geolocation.SpeedMetersPerSecond;
        }

        if (geolocation.CourseValid)
        {
            _previousGpsCourse = geolocation.CourseDegrees;
        }
    }

    private void ObserveGpsDerivatives(GeolocationPayload geolocation, double gapSeconds)
    {
        if (geolocation.SpeedValid
            && double.IsFinite(geolocation.SpeedMetersPerSecond)
            && _previousGpsSpeed is { } previousSpeed
            && double.IsFinite(previousSpeed))
        {
            double delta = geolocation.SpeedMetersPerSecond - previousSpeed;
            double derivative = delta / gapSeconds;
            _speedDerivative.Add(derivative);
            _gpsSpeedChangeIntervals++;
            _maxAbsSpeedDerivative = _maxAbsSpeedDerivative is { } current
                ? Math.Max(current, Math.Abs(derivative))
                : Math.Abs(derivative);

            if (Math.Abs(delta) > SpeedChangeThresholdMps)
            {
                _gpsSpeedChangeAboveHalf++;
            }
        }

        if (geolocation.CourseValid
            && double.IsFinite(geolocation.CourseDegrees)
            && _previousGpsCourse is { } previousCourse
            && double.IsFinite(previousCourse)
            && geolocation.SpeedValid
            && geolocation.SpeedMetersPerSecond >= MovingSpeedMetersPerSecond)
        {
            double courseDelta = WrapDegrees(geolocation.CourseDegrees - previousCourse);
            double rate = courseDelta / gapSeconds;
            _courseRate.Add(rate);
            _maxAbsCourseRate = _maxAbsCourseRate is { } currentRate
                ? Math.Max(currentRate, Math.Abs(rate))
                : Math.Abs(rate);

            if (Math.Abs(courseDelta) >= TurnCourseDegrees)
            {
                _turnIntervals++;
            }
        }
    }

    private void ObserveMotion(DateTimeOffset timestamp, double speed)
    {
        if (_origin is { } origin
            && _firstSpeedAbove2Seconds is null
            && speed > MovingSpeedMetersPerSecond)
        {
            _firstSpeedAbove2Seconds = (timestamp - origin).TotalSeconds;
        }

        bool stationary = speed <= ParkedSpeedMetersPerSecond;

        if (_stationary is null)
        {
            _stationary = stationary;
            _motionWindowStart = timestamp;
            _lastGpsTime = timestamp;

            return;
        }

        if (stationary != _stationary)
        {
            CloseMotionWindow(timestamp);
            _stationary = stationary;
            _motionWindowStart = timestamp;
        }

        _lastGpsTime = timestamp;
    }

    private void CloseMotionWindow(DateTimeOffset? end)
    {
        if (_stationary is not { } stationary
            || _motionWindowStart is not { } start
            || end is not { } endTime)
        {
            return;
        }

        double duration = Math.Max(0d, (endTime - start).TotalSeconds);

        if (stationary)
        {
            _stationaryWindows++;
            _stationaryDuration += duration;
            _longestStationary = _longestStationary is { } longest
                ? Math.Max(longest, duration)
                : duration;
        }
        else
        {
            _movingWindows++;
            _movingDuration += duration;
        }
    }

    private static void ObserveInterval(
        TimeSpan interval,
        RangeAccumulator accumulator,
        long[] buckets,
        ref long overflow)
    {
        double seconds = interval.TotalSeconds;
        accumulator.Add(seconds);

        if (!double.IsFinite(seconds) || seconds < 0d)
        {
            return;
        }

        int millisecond = (int)Math.Round(seconds * 1000d);

        if (millisecond >= 0 && millisecond < buckets.Length)
        {
            buckets[millisecond]++;
        }
        else
        {
            overflow++;
        }
    }

    private static AnalyzeSourceTiming BuildSourceTiming(
        long count,
        RangeAccumulator intervals,
        RangeAccumulator lag,
        long[] buckets,
        long overflow)
    {
        AnalyzeRangeStats intervalStats = intervals.ToStats();
        double? duration = intervalStats.Mean is { } mean && count > 1
            ? mean * (count - 1)
            : null;

        return new AnalyzeSourceTiming
        {
            RateHz = duration is > 0d ? (count - 1) / duration : null,
            IntervalSeconds = intervalStats,
            ArrivalLagSeconds = lag.ToStats(),
            IntervalMillisecondBuckets = BuildBuckets(buckets),
            IntervalOverflowBuckets = overflow,
            MedianPositiveIntervalSeconds = MedianPositive(buckets),
            ZeroMillisecondIntervals = buckets.Length > 0 ? buckets[0] : 0
        };
    }

    private static IReadOnlyDictionary<string, long> BuildBuckets(long[] buckets)
    {
        Dictionary<string, long> map = new(StringComparer.Ordinal);

        for (int index = 0; index < buckets.Length; index++)
        {
            if (buckets[index] > 0)
            {
                map[index.ToString(System.Globalization.CultureInfo.InvariantCulture)] = buckets[index];
            }
        }

        return map;
    }

    private IReadOnlyDictionary<string, long> BuildTenths()
    {
        Dictionary<string, long> map = new(StringComparer.Ordinal);

        for (int index = 0; index < _compassTenths.Length; index++)
        {
            if (_compassTenths[index] > 0)
            {
                map[(index / 10d).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)] =
                    _compassTenths[index];
            }
        }

        return map;
    }

    private static double? MedianPositive(long[] buckets)
    {
        long positive = 0;

        for (int index = 1; index < buckets.Length; index++)
        {
            positive += buckets[index];
        }

        if (positive == 0)
        {
            return null;
        }

        long midpoint = (positive + 1) / 2;
        long seen = 0;

        for (int index = 1; index < buckets.Length; index++)
        {
            seen += buckets[index];

            if (seen >= midpoint)
            {
                return index / 1000d;
            }
        }

        return null;
    }

    private static string? FormatTimestamp(DateTimeOffset? value)
        => value is { } timestamp
            ? FusionTraceFormatting.FormatTimestamp(timestamp)
            : null;

    private static double? DurationSeconds(DateTimeOffset? first, DateTimeOffset? last)
        => first is { } start && last is { } end
            ? (end - start).TotalSeconds
            : null;

    private static double WrapDegrees(double delta)
    {
        double wrapped = delta;

        while (wrapped > 180d)
        {
            wrapped -= 360d;
        }

        while (wrapped < -180d)
        {
            wrapped += 360d;
        }

        return wrapped;
    }

    /// <summary>
    ///  Welford mean and variance for finite samples.
    /// </summary>
    private sealed class RangeAccumulator
    {
        private long _finite;
        private long _nonFinite;
        private double _mean;
        private double _m2;
        private double _min = double.PositiveInfinity;
        private double _max = double.NegativeInfinity;

        internal void Add(double value)
        {
            if (!double.IsFinite(value))
            {
                _nonFinite++;

                return;
            }

            _finite++;
            double delta = value - _mean;
            _mean += delta / _finite;
            _m2 += delta * (value - _mean);

            if (value < _min)
            {
                _min = value;
            }

            if (value > _max)
            {
                _max = value;
            }
        }

        internal AnalyzeRangeStats ToStats()
            => new()
            {
                Min = _finite > 0 ? _min : null,
                Max = _finite > 0 ? _max : null,
                Mean = _finite > 0 ? _mean : null,
                StdDev = _finite > 1 ? Math.Sqrt(_m2 / _finite) : null,
                FiniteCount = _finite,
                NonFiniteCount = _nonFinite
            };
    }
}
