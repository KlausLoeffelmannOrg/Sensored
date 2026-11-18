using System.Text.Json;
using Sensored;

namespace FusionGauges.Tests;

public sealed class RoadLearningTests
{
    private static readonly DateTimeOffset s_start = DateTimeOffset.UnixEpoch;

    [Fact]
    public void ThirtySecondWindowDebouncesAndAsksOnlyDifferentConditions()
    {
        RoadLearningSession session = new("mount/source-test");
        Feed(session, 0, 29, seconds => 8 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(29), null));
        Feed(session, 29.25, 30, seconds => 8 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(30), null));
        Feed(session, 30.25, 32, seconds => 8 + seconds);
        RoadLearningQuestion question = Assert.IsType<RoadLearningQuestion>(session.TryQuestion(s_start.AddSeconds(32), "recording"));
        Assert.Equal(RoadCondition.Acceleration, question.Condition);
        Assert.Equal(s_start.AddSeconds(2), question.Start);
        Assert.All(question.Samples, sample => Assert.InRange(sample.ReceivedAt, question.Start, question.End));
        Assert.Equal(1d, question.Sources[(int)LearningSource.Vehicle].Acceleration!.Value, 5);
        Assert.Equal(0, question.Sources[(int)LearningSource.Gps].Contribution);
        Assert.Equal(100, question.Sources[(int)LearningSource.Vehicle].Contribution);
        Assert.True(session.IsCurrent(question, question.End));
        Feed(session, 32.25, 35, seconds => 8 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(35), null));
        Feed(session, 35.25, 40, seconds => 43 - (seconds - 35));
        Assert.Null(session.TryQuestion(s_start.AddSeconds(40), null));
        Feed(session, 40.25, 42, seconds => 43 - (seconds - 35));
        RoadLearningQuestion braking = Assert.IsType<RoadLearningQuestion>(session.TryQuestion(s_start.AddSeconds(42), null));
        Assert.Equal(RoadCondition.Braking, braking.Condition);
        Assert.Equal(121, question.Samples.Length);
        session.BreakContinuity();
        Assert.False(session.IsCurrent(braking, braking.End));
    }

    [Fact]
    public void PauseEndAndDiscontinuitiesSuppressQuestionsWithoutLabels()
    {
        RoadLearningSession session = new("profile");
        Feed(session, 0, 30, seconds => 5 + seconds);
        session.Pause(s_start.AddSeconds(30), 1);
        Feed(session, 30.25, 35, seconds => 5 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(35), null));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Pause(s_start, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => session.Pause(s_start, 21));
        Feed(session, 35.25, 90, seconds => 5 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(90), null));
        Feed(session, 90.25, 92, seconds => 5 + seconds);
        Assert.NotNull(session.TryQuestion(s_start.AddSeconds(92), null));
        session.BreakContinuity();
        Feed(session, 92.25, 100, seconds => 150 - seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(100), null));
        session.End();
        Feed(session, 100.25, 140, seconds => 150 - seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(140), null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SourceGenerationOrEpochChangeRequiresANewWindow(bool generation)
    {
        RoadLearningSession session = new("profile");
        Feed(session, 0, 30, seconds => 5 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(30), null));
        for (int tick = 121; tick <= 140; tick++)
        {
            double seconds = tick / 4d;
            DateTimeOffset time = s_start.AddSeconds(seconds);
            session.Observe(new(time, time, tick, generation ? 0 : 1, "vehicle",
                generation ? 2 : 1, LearningSource.Vehicle, Speed: 5 + seconds));
        }

        Assert.Null(session.TryQuestion(s_start.AddSeconds(35), null));
    }

    [Fact]
    public void MissingStaleOrConfoundedSensorsNeverBecomeSlopeOrSemanticTruth()
    {
        RoadLearningSession session = new("profile");
        for (int tick = 0; tick <= 128; tick++)
        {
            double seconds = tick / 4d;
            DateTimeOffset time = s_start.AddSeconds(seconds);
            session.Observe(new(time, time, tick, 0, "imu", 1, LearningSource.Internal,
                AccelerationMagnitude: 1, SupportedPitch: seconds / 2));
            if (tick == 120) { Assert.Null(session.TryQuestion(time, null)); }
        }

        Assert.Null(session.TryQuestion(s_start.AddSeconds(32), null));
        Feed(session, 32.25, 64, _ => 0);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(64), null));
        Feed(session, 64.25, 66, _ => 0);
        RoadLearningQuestion stopped = Assert.IsType<RoadLearningQuestion>(session.TryQuestion(s_start.AddSeconds(66), null));
        Assert.Equal(RoadCondition.Stopped, stopped.Condition);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(70), null));
        Assert.Equal(0, stopped.Sources[(int)LearningSource.Internal].Contribution);
    }

    [Fact]
    public async Task AnswersPreserveFrozenEvidenceAndUndoPersistsRetraction()
    {
        string root = Path.Combine(Path.GetTempPath(), $"sensored-road-quiz-{Guid.NewGuid():N}");
        RoadLearningSession session = new("mount/source-profile");
        Feed(session, 0, 30, seconds => 5 + seconds);
        Assert.Null(session.TryQuestion(s_start.AddSeconds(30), null));
        Feed(session, 30.25, 32, seconds => 5 + seconds);
        session.Observe(new(s_start.AddSeconds(32), s_start.AddSeconds(32), 128, 0,
            "internal-imu", 1, LearningSource.Internal, AccelerationMagnitude: 1,
            AccelerationVectorG: new(0, 0, 1), AngularRateVectorDegreesPerSecond: new(0.1, 0.2, 0.3)));
        RoadLearningQuestion question = session.TryQuestion(s_start.AddSeconds(32), "recording")!;
        try
        {
            using RoadLearningJournal journal = new(root, session.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => journal.AnswerAsync(question, 3, question.End.AddSeconds(11)));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => journal.AnswerAsync(question, 6, question.End));
            Assert.False(journal.CanUndo);
            await journal.AnswerAsync(question, 0, question.End.AddSeconds(1));
            string path = Directory.GetFiles(Path.Combine(root, session.Id.ToString("N")), "*.json").Single();
            using (JsonDocument saved = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken)))
            {
                Assert.Equal("rejected-guess", saved.RootElement.GetProperty("LabelKind").GetString());
                Assert.Equal("Acceleration", saved.RootElement.GetProperty("Question").GetProperty("Condition").GetString());
                Assert.Equal(122, saved.RootElement.GetProperty("Question").GetProperty("Samples").GetArrayLength());
                Assert.False(saved.RootElement.GetProperty("Retracted").GetBoolean());
            }

            await Assert.ThrowsAsync<InvalidOperationException>(() => journal.AnswerAsync(question, 2, question.End));
            Assert.True(journal.CanUndo);
            await journal.UndoAsync();
            Assert.False(journal.CanUndo);
            using JsonDocument retracted = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
            Assert.True(retracted.RootElement.GetProperty("Retracted").GetBoolean());
            Assert.Equal(0, retracted.RootElement.GetProperty("SelectedLevel").GetInt32());
            Assert.Equal(1d, retracted.RootElement.GetProperty("Question").GetProperty("Samples")[121]
                .GetProperty("AccelerationVectorG").GetProperty("Z").GetDouble());
            await Assert.ThrowsAsync<InvalidOperationException>(journal.UndoAsync);
        }
        finally
        {
            if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); }
        }
    }

    [Fact]
    public async Task FailedWriteDoesNotCountAsAnAnswer()
    {
        string blocker = Path.Combine(Path.GetTempPath(), $"sensored-quiz-blocked-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(blocker, "not a directory", TestContext.Current.CancellationToken);
        RoadLearningSession session = new("profile");
        Feed(session, 0, 30, seconds => 5 + seconds);
        session.TryQuestion(s_start.AddSeconds(30), null);
        Feed(session, 30.25, 32, seconds => 5 + seconds);
        RoadLearningQuestion question = session.TryQuestion(s_start.AddSeconds(32), null)!;
        try
        {
            using RoadLearningJournal journal = new(blocker, session.Id);
            await Assert.ThrowsAnyAsync<IOException>(() => journal.AnswerAsync(question, 2, question.End));
            Assert.False(journal.CanUndo);
        }

        finally
        {
            File.Delete(blocker);
        }
    }

    [Fact]
    public void ConflictingSpeedFamiliesAbstainAndStaleOrDuplicateMeasurementsDoNotCreateAcceleration()
    {
        RoadLearningSession session = new("profile");
        for (int tick = 0; tick <= 128; tick++)
        {
            double seconds = tick / 4d;
            DateTimeOffset time = s_start.AddSeconds(seconds);
            session.Observe(new(time, time, tick, 0, "vehicle", 1, LearningSource.Vehicle, Speed: 10 + seconds));
            session.Observe(new(time, time, tick, 0, "gps", 1, LearningSource.Gps, Speed: 20 + seconds));
            if (tick >= 120) { Assert.Null(session.TryQuestion(time, null)); }
        }

        RoadLearningSession duplicates = new("profile");
        for (int tick = 0; tick <= 128; tick++)
        {
            DateTimeOffset time = s_start.AddSeconds(tick / 4d);
            duplicates.Observe(new(time, s_start, tick, 0, "vehicle", 1, LearningSource.Vehicle, Speed: 10 + tick));
            if (tick >= 120) { Assert.Null(duplicates.TryQuestion(time, null)); }
        }
    }

    [Theory]
    [InlineData(5, (int)RoadCondition.RoughRoad)]
    [InlineData(0.15, (int)RoadCondition.WavyRoad)]
    public void VibrationRulesRetainSensorProvenance(double frequency, int expected)
    {
        RoadLearningSession session = new("vibration-profile");
        RoadLearningQuestion? question = null;
        for (int tick = 0; tick <= 640; tick++)
        {
            double seconds = tick / 20d;
            DateTimeOffset time = s_start.AddSeconds(seconds);
            session.Observe(new(time, time, tick, 0, "vehicle", 1, LearningSource.Vehicle, Speed: 10));
            session.Observe(new(time, time, tick, 0, "accelerometer", 1, LearningSource.Internal,
                AccelerationMagnitude: 1 + 0.15 * Math.Sin(2 * Math.PI * frequency * seconds)));
            if (tick >= 600 && tick % 10 == 0)
            {
                question ??= session.TryQuestion(time, null);
            }
        }

        Assert.NotNull(question);
        Assert.Equal((RoadCondition)expected, question.Condition);
        Assert.True(question.Sources[(int)LearningSource.Internal].Contribution > 0);
        Assert.Equal(0, question.Sources[(int)LearningSource.Bluetooth].Contribution);
    }

    [Theory]
    [InlineData("level-to-uphill", (int)RoadCondition.LevelToUphill)]
    [InlineData("downhill-to-uphill", (int)RoadCondition.DownhillToUphill)]
    [InlineData("uphill-start", (int)RoadCondition.UphillStart)]
    [InlineData("downhill-braking-stop", (int)RoadCondition.DownhillBrakingToStandstill)]
    [InlineData("traffic-light", (int)RoadCondition.WaitingAtTrafficLight)]
    [InlineData("stop-sign", (int)RoadCondition.WaitingAtStopSign)]
    [InlineData("intersection", (int)RoadCondition.IntersectionStop)]
    [InlineData("reversing", (int)RoadCondition.Reversing)]
    [InlineData("parallel-parking", (int)RoadCondition.ParallelParking)]
    [InlineData("enter-highway", (int)RoadCondition.EnterHighway)]
    [InlineData("exit-highway", (int)RoadCondition.ExitHighway)]
    [InlineData("highway", (int)RoadCondition.HighwayDriving)]
    [InlineData("stop-and-go", (int)RoadCondition.StopAndGo)]
    [InlineData("emergency", (int)RoadCondition.EmergencyBraking)]
    [InlineData("uphill-right-curve", (int)RoadCondition.UphillRightCurve)]
    public void CatalogueScenariosAreRecognized(string scenario, int expected)
    {
        Func<double, (double V, double Pitch, double Lat, double Lon, double? Course)> drive = scenario switch
        {
            "level-to-uphill" => t => (15, t < 40 ? 0 : 5, 0, 0, null),
            "downhill-to-uphill" => t => (15, t < 40 ? -4 : 4, 0, 0, null),
            "uphill-start" => t => (t < 35 ? 0 : Math.Min(10, (t - 35) * 1.5), 6, 0, 0, null),
            "downhill-braking-stop" => t => (t < 35 ? 12 : Math.Max(0, 12 - 3 * (t - 35)), -5, 0, 0, null),
            "traffic-light" => t => (t < 20 ? 10 : Math.Max(0, 10 - 2.5 * (t - 20)), 0, 0, 0, null),
            "stop-sign" => t => (StopCycle(t), 0, 0, 0, 90),
            "intersection" => t => (StopCycle(t), 0, 0, 0, t < 38 ? 90 : Math.Min(180, 90 + (t - 38) * 25)),
            "reversing" => t => (t < 32 ? 0 : Math.Min(2, (t - 32) * 0.5), 0, 0, t < 32 ? 0 : -0.5, null),
            "parallel-parking" => Parking,
            "enter-highway" => t => (t < 35 ? 14 : Math.Min(28, 14 + 2 * (t - 35)), 0, t is > 25 and < 33 ? 2 : 0, 0, null),
            "exit-highway" => t => (t < 35 ? 30 : Math.Max(12, 30 - 2 * (t - 35)), 0, t is > 36 and < 44 ? 2 : 0, 0, null),
            "highway" => _ => (30, 0, 0, 0, null),
            "stop-and-go" => t => (StopAndGo(t % 10), 0, 0, 0, null),
            "emergency" => t => (t < 40 ? 25 : Math.Max(0, 25 - 8 * (t - 40)), 0, 0, 0, null),
            "uphill-right-curve" => t => (15, 4, t < 35 ? 0 : 2, 0, null),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        HashSet<RoadCondition> asked = Drive(drive, 70);
        Assert.Contains((RoadCondition)expected, asked);
        RoadCondition? lookAlike = scenario switch
        {
            "stop-sign" => RoadCondition.IntersectionStop,
            "intersection" => RoadCondition.WaitingAtStopSign,
            "highway" => RoadCondition.EnterHighway,
            "level-to-uphill" => RoadCondition.LevelToDownhill,
            "parallel-parking" => RoadCondition.StopAndGo,
            _ => null
        };
        if (lookAlike is { } other)
        {
            Assert.DoesNotContain(other, asked);
        }
    }

    [Fact]
    public void ConditionsWithoutCatalogueLevelReportLevelZero()
    {
        Assert.False(RoadConditions.HasLevel(RoadCondition.DownhillToUphill));
        Assert.Equal("Downhill to uphill", RoadConditions.Headline(RoadCondition.DownhillToUphill, 0));
        Assert.Equal("Level to uphill - Level 3", RoadConditions.Headline(RoadCondition.LevelToUphill, 3));
    }

    private static double StopCycle(double t) => t switch
    {
        < 30 => 10,
        < 34 => 10 - 2.5 * (t - 30),
        < 37 => 0,
        _ => Math.Min(10, 1.5 * (t - 37))
    };

    private static double StopAndGo(double phase) => phase switch
    {
        < 4 => 1.5 * phase,
        < 7 => 6 - 2 * (phase - 4),
        _ => 0
    };

    private static (double V, double Pitch, double Lat, double Lon, double? Course) Parking(double t)
    {
        // 8 s cycle: forward leg, stop, reverse leg (longitudinal acceleration opposes speed slope), stop.
        double phase = t % 8;
        bool reverse = phase >= 4;
        double leg = reverse ? phase - 4 : phase;
        (double v, double slope) = leg switch
        {
            < 2.5 => (0.6 * leg, 0.6),
            < 3 => (1.5 - 3 * (leg - 2.5), -3),
            _ => (0, 0)
        };
        return (v, 0, 0.3, reverse ? -slope : slope, null);
    }

    private static HashSet<RoadCondition> Drive(
        Func<double, (double V, double Pitch, double Lat, double Lon, double? Course)> drive, double duration)
    {
        RoadLearningSession session = new("scenario");
        HashSet<RoadCondition> asked = [];
        for (int tick = 0; tick <= duration * 4; tick++)
        {
            double seconds = tick / 4d;
            DateTimeOffset time = s_start.AddSeconds(seconds);
            (double v, double pitch, double lateral, double longitudinal, double? course) = drive(seconds);
            session.Observe(new(time, time, tick, 0, "vehicle", 1, LearningSource.Vehicle, Speed: v));
            session.Observe(new(time, time, tick, 0, "imu", 1, LearningSource.Internal, AccelerationMagnitude: 1,
                SupportedPitch: pitch, SupportedLateralAcceleration: lateral, SupportedLongitudinalAcceleration: longitudinal));
            if (course is { } heading)
            {
                session.Observe(new(time, time, tick, 0, "gps", 1, LearningSource.Gps, Speed: v, GpsCourseDegrees: heading));
            }

            if (tick % 2 == 0 && session.TryQuestion(time, null) is { } question)
            {
                asked.Add(question.Condition);
            }
        }

        return asked;
    }

    private static void Feed(RoadLearningSession session, double from, double through, Func<double, double> speed)
    {
        for (double seconds = from; seconds <= through; seconds += 0.25)
        {
            DateTimeOffset time = s_start.AddSeconds(seconds);
            session.Observe(new(time, time, (long)(seconds * 4), 0, "vehicle", 1,
                LearningSource.Vehicle, Speed: speed(seconds)));
        }
    }
}
