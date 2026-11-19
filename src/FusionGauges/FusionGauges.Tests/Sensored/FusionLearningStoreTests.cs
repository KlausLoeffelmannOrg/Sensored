using Sensored;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace Sensored.Tests;

/// <summary>Checks durable labels and original evidence without turning them into estimator truth.</summary>
public sealed class FusionLearningStoreTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("negative")]
    [InlineData("unknown")]
    public async Task ScenarioHistoryAcceptsLegacyFilesAndRejectsInvalidCountsWithoutOverwriting(string history)
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using (FusionLearningStore store = FusionLearningStore.Open(path))
            {
                store.BeginSession(null, "internal");
                await store.SaveAsync();
            }

            var state = System.Text.Json.Nodes.JsonNode.Parse(
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!;
            var profile = state["Profiles"]![0]!.AsObject();
            if (history == "missing")
            {
                profile.Remove("ScenarioCounts");
            }
            else
            {
                profile["ScenarioCounts"] = new System.Text.Json.Nodes.JsonObject
                {
                    [history == "unknown" ? "999" : "DynamicMotion"] = history == "negative" ? -1 : 1
                };
            }

            string original = state.ToJsonString();
            await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
            if (history == "missing")
            {
                using FusionLearningStore reopened = FusionLearningStore.Open(path);
                reopened.BeginSession(null, "internal");
                Assert.DoesNotContain("Scenario evidence:", reopened.Progress);
            }
            else
            {
                Assert.Throws<InvalidDataException>(() => FusionLearningStore.Open(path));
            }

            Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Fact]
    public async Task ScenarioEvidenceAccumulatesAcrossSessionsWithoutCreatingRoadTruthLabels()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using (FusionLearningStore store = FusionLearningStore.Open(path))
            {
                store.BeginSession(null, "internal");
                FusionPlausibilityAssessment assessment = new("test", At(4), "rough interval")
                {
                    Scenario = FusionPlausibilityScenario.DynamicMotion
                };
                store.ObserveAssessment(Observation(4, true), assessment, null);
                store.ObserveAssessment(Observation(4.1, true), assessment, null);
                store.ObserveAssessment(Observation(8, true), assessment with { Timestamp = At(8) }, null);
                Assert.Empty(store.PendingEpisodes);
                Assert.Contains("DynamicMotion: 2", store.Progress);
                await store.SaveAsync();
            }

            using FusionLearningStore reopened = FusionLearningStore.Open(path);
            reopened.BeginSession(null, "internal");
            Assert.Contains("DynamicMotion: 2", reopened.Progress);
            reopened.ObserveAssessment(Observation(4, true),
                new("test", At(4), "fresh trip") { Scenario = FusionPlausibilityScenario.DynamicMotion }, null);
            Assert.Contains("DynamicMotion: 3", reopened.Progress);
            Assert.Empty(reopened.PendingEpisodes);
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LegacyMixedStreamEndpointsArePreservedButNewReversedRangesAreRejected(bool canonical)
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using (FusionLearningStore store = FusionLearningStore.Open(path))
            {
                store.BeginSession(null, "internal");
                store.Observe(Observation(1, true), null);
                store.Observe(Observation(1.1, false), null);
                await store.SaveAsync();
            }

            var state = System.Text.Json.Nodes.JsonNode.Parse(
                await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!;
            var episode = state["Episodes"]![0]!;
            episode["StartSequence"] = 6200;
            episode["EndSequence"] = 1618;
            if (canonical)
            {
                episode["HasCanonicalSequenceRange"] = true;
            }
            else
            {
                episode.AsObject().Remove("HasCanonicalSequenceRange");
            }

            string original = state.ToJsonString();
            await File.WriteAllTextAsync(path, original, TestContext.Current.CancellationToken);
            if (canonical)
            {
                Assert.Throws<InvalidDataException>(() => FusionLearningStore.Open(path));
            }
            else
            {
                using FusionLearningStore reopened = FusionLearningStore.Open(path);
                FusionLearningEpisode retained = Assert.Single(reopened.PendingEpisodes);
                Assert.True(reopened.HasLegacySequenceRanges);
                Assert.Equal(6200, retained.StartSequence);
                Assert.Equal(1618, retained.EndSequence);
                await reopened.SaveAsync();
            }

            Assert.Equal(original, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Fact]
    public async Task FrameAndSessionChangesRetainLabelsAndKeepMountProfilesSeparate()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using (FusionLearningStore store = FusionLearningStore.Open(path))
            {
                store.BeginSession(new("car", 1, new(1, 0, 0), new(0, 0, 1)), "internal");
                store.Observe(Observation(1, true), null);
                store.Observe(Observation(1.1, true), null);
                store.Observe(Observation(1.2, false), "recording");
                FusionLearningEpisode first = Assert.Single(store.PendingEpisodes);
                Assert.Equal("recording", first.RecordingPath);
                Assert.Equal(1200, first.EndSequence);
                store.Answer(first.Id, new("medium", "light", true, true, "Remembered, not surveyed."));
                store.BeginSession(new("car", 2, new(0, 1, 0), new(0, 0, 1)), "internal");
                store.Observe(Observation(2, true), null);
                store.Observe(Observation(2.1, false), null);
                FusionLearningEpisode changed = Assert.Single(store.PendingEpisodes);
                Assert.NotEqual(first.ProfileId, changed.ProfileId);
                Assert.NotEqual(first.SessionId, changed.SessionId);
                await store.SaveAsync();
            }

            using FusionLearningStore reopened = FusionLearningStore.Open(path);
            Assert.Single(reopened.PendingEpisodes);
            Assert.Contains("Labels: 1 (1 saved)", reopened.Progress);
            reopened.BeginSession(new("car", 1, new(1, 0, 0), new(0, 0, 1)), "internal");
            Assert.Contains("2 sessions; 0.1s supported; 1 dropouts", reopened.Progress);
            Assert.Throws<ArgumentException>(() => reopened.Answer(reopened.PendingEpisodes[0].Id,
                new("invalid", "unknown", false, false, "")));
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Fact]
    public async Task AssessmentKeepsOriginalIntervalAndRecordingDespiteDelayedConsumption()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using FusionLearningStore store = FusionLearningStore.Open(path);
            store.BeginSession(null, "internal");
            FusionPlausibilityAssessment assessment = new("test", At(8), "incline-or-bias", 0, 1, 1)
            {
                WindowStart = At(6),
                WindowEnd = At(8),
                StartSequence = 6000,
                EndSequence = 8000,
                Epoch = 2,
                ImuSourceId = "original",
                ImuSourceGeneration = 3,
                RecordingPath = "original-recording"
            };
            store.ObserveAssessment(Observation(9, true), assessment, "later-recording");
            FusionLearningEpisode episode = Assert.Single(store.PendingEpisodes);
            Assert.Equal(At(6), episode.Start);
            Assert.Equal(At(8), episode.End);
            Assert.Equal(8000, episode.EndSequence);
            Assert.Equal("original", episode.SourceId);
            Assert.Equal(3, episode.SourceGeneration);
            Assert.Equal(2, episode.Epoch);
            Assert.Equal("original-recording", episode.RecordingPath);
            store.ObserveAssessment(Observation(10, true), assessment with { WindowEnd = At(10) }, null);
            Assert.Single(store.PendingEpisodes);
            store.ObserveAssessment(Observation(40, true),
                assessment with { WindowEnd = At(40), FusionAcceleration = 0.1, Residual = 0.1 }, null);
            Assert.Single(store.PendingEpisodes);
            await store.SaveAsync();
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Fact]
    public async Task InvalidFileIsNotOverwrittenAndFailedCheckpointCanBeRetried()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using (FusionLearningStore store = FusionLearningStore.Open(path))
            {
                store.BeginSession(null, "internal");
                store.Observe(Observation(1, true), null);
                store.Observe(Observation(1.1, false), null);
                using (FileStream blocker = new(path + ".writing", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    await Assert.ThrowsAsync<IOException>(store.SaveAsync);
                    Assert.Single(store.PendingEpisodes);
                    Assert.False(File.Exists(path));
                }

                await store.SaveAsync();
                Assert.True(File.Exists(path));
                Assert.Throws<IOException>(() => FusionLearningStore.Open(path));
                store.Answer(store.PendingEpisodes[0].Id, new("none", "light", false, false, ""));
                using (FileStream blocker = new(path + ".writing", FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                {
                    await Assert.ThrowsAsync<IOException>(store.SaveAsync);
                    Assert.Contains("Labels: 1 (0 saved)", store.Progress);
                }

                await store.SaveAsync();
                Assert.Contains("Labels: 1 (1 saved)", store.Progress);
            }

            const string invalid = "{\"Version\":99,\"Profiles\":[],\"Episodes\":[]}";
            await File.WriteAllTextAsync(path, invalid, TestContext.Current.CancellationToken);
            Assert.Throws<InvalidDataException>(() => FusionLearningStore.Open(path));
            Assert.Equal(invalid, await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    [Fact]
    public void CoverageDoesNotCountPausesDuplicatesOrSourceChanges()
    {
        string path = Path.Combine(Path.GetTempPath(), $"warp-fusion-learning-{Guid.NewGuid():N}", "evidence.json");
        try
        {
            using FusionLearningStore store = FusionLearningStore.Open(path);
            store.BeginSession(null, "internal");
            store.Observe(Observation(1, true), null);
            store.Observe(Observation(1.1, true), null);
            store.Observe(Observation(1.1, true), null);
            store.Observe(Observation(100, true), null);
            FusionObservation changed = Observation(100.1, true);
            store.Observe(changed with { Input = changed.Input with { SourceGeneration = 2 } }, null);
            Assert.Contains("0.1s supported", store.Progress);
        }
        finally
        {
            DeleteTestFiles(path);
        }
    }

    private static FusionObservation Observation(double time, bool supported)
        => new(Imu(time), new()
        {
            Sequence = (long)(time * 1000),
            Timestamp = At(time),
            Validity = supported ? FusionOutputValidity.Attitude | FusionOutputValidity.Acceleration : FusionOutputValidity.None
        }, 1, "test");

    private static DateTimeOffset At(double seconds)
        => DateTimeOffset.UnixEpoch.AddTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond));

    private static SensorEnvelope Imu(double seconds) => new()
    {
        Sequence = (long)Math.Round(seconds * 1000),
        Kind = SensorDataKind.Imu,
        SourceId = "synthetic-imu",
        SourceGeneration = 1,
        SourceTimestamp = At(seconds),
        ArrivalTimestamp = At(seconds),
        Imu = new(new Vec3(0, 0, 1), Vec3.Zero, false,
            double.NaN, 0, double.NaN, double.NaN, double.NaN, default)
    };

    private static void DeleteTestFiles(string path)
    {
        File.Delete(path);
        File.Delete(path + ".writing");
        File.Delete(path + ".lock");
        Directory.Delete(Path.GetDirectoryName(path)!);
    }
}
