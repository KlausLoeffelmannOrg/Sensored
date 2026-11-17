using System.Security.Cryptography;
using System.Text.Json;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Stores user-declared road observations, never measured calibration or road truth.</summary>
internal sealed record FusionRoadFeedback(
    string Acceleration, string Incline, bool HardTurn, bool Braking, string Notes);

/// <summary>Identifies an original episode independently of the current frame or recording.</summary>
internal sealed record FusionLearningEpisode(
    Guid Id, Guid SessionId, string ProfileId, DateTimeOffset Start, DateTimeOffset End,
    long StartSequence, long EndSequence, int Epoch, string? SourceId, long SourceGeneration,
    string Reason, double? OdometerAcceleration, double? FusionAcceleration,
    string? RecordingPath, FusionRoadFeedback? Feedback = null)
{
    /// <summary>Gets whether both endpoints identify the same canonical IMU interval.</summary>
    /// <remarks>Legacy files mixed OBD and IMU endpoints; their original values are retained, not repaired.</remarks>
    public bool HasCanonicalSequenceRange { get; init; }
}

/// <summary>Accumulates evidence coverage, not a confidence or calibration score.</summary>
internal sealed record FusionLearningProfile(
    string Id, VehicleMountReference? Mount, string SourceConfiguration,
    int Sessions = 0, double SupportedSeconds = 0, int SupportLosses = 0, int MovingRenewals = 0)
{
    public Dictionary<FusionPlausibilityScenario, int> ScenarioCounts { get; init; } = [];
}

/// <summary>Persists evidence independently of assembly-version settings and estimator resets.</summary>
internal sealed class FusionLearningStore : IDisposable
{
    private const int MaximumPendingEpisodes = 100;
    private static readonly TimeSpan EpisodeInterval = TimeSpan.FromSeconds(30);
    private readonly object _gate = new();
    private readonly SemaphoreSlim _saveGate = new(1);
    private readonly string _path;
    private readonly FileStream _ownership;
    private readonly LearningState _state;
    private string? _profileId;
    private Guid _sessionId;
    private FusionObservation? _previous;
    private DateTimeOffset? _lastEpisode;
    private DateTimeOffset? _lastScenarioAssessment;
    private bool _movingReference;
    private long _revision;
    private long _savedRevision;
    private int _persistedAnswers;

    private FusionLearningStore(string path, FileStream ownership, LearningState state)
    {
        _path = path;
        _ownership = ownership;
        _state = state;
        _persistedAnswers = state.Episodes.Count(episode => episode.Feedback is not null);
    }

    /// <summary>Opens a single-writer store; corrupt or incompatible evidence is never replaced.</summary>
    internal static FusionLearningStore Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        FileStream ownership = new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        try
        {
            LearningState state = File.Exists(path)
                ? JsonSerializer.Deserialize<LearningState>(File.ReadAllBytes(path))
                    ?? throw new InvalidDataException("The fusion evidence file contains no state.")
                : new();
            if (state.Version != 1 || state.Profiles is null || state.Episodes is null
                || state.Profiles.Any(profile => profile is null || string.IsNullOrWhiteSpace(profile.Id)
                    || profile.Sessions < 0 || profile.SupportLosses < 0 || profile.MovingRenewals < 0
                    || profile.ScenarioCounts is null
                    || profile.ScenarioCounts.Any(entry => !Enum.IsDefined(entry.Key) || entry.Value < 0)
                    || !double.IsFinite(profile.SupportedSeconds) || profile.SupportedSeconds < 0)
                || state.Episodes.Any(episode => episode is null || episode.End < episode.Start
                    || episode.Id == Guid.Empty || episode.SessionId == Guid.Empty
                    || episode.StartSequence < 0 || episode.EndSequence < 0
                    || (episode.HasCanonicalSequenceRange && episode.EndSequence < episode.StartSequence)
                    || string.IsNullOrWhiteSpace(episode.Reason))
                || state.Profiles.Select(profile => profile.Id).Distinct().Count() != state.Profiles.Count
                || state.Episodes.Select(episode => episode.Id).Distinct().Count() != state.Episodes.Count
                || state.Episodes.Any(episode => !state.Profiles.Any(profile => profile.Id == episode.ProfileId)))
            {
                throw new InvalidDataException("The fusion evidence file is incompatible or invalid; it was not overwritten.");
            }

            return new(path, ownership, state);
        }
        catch
        {
            ownership.Dispose();
            throw;
        }
    }

    /// <summary>Starts fresh navigation while retaining all previously collected evidence.</summary>
    internal void BeginSession(VehicleMountReference? mount, string sourceConfiguration)
    {
        string id = Convert.ToHexString(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(new { Mount = mount, Source = sourceConfiguration })));
        lock (_gate)
        {
            _profileId = id;
            _sessionId = Guid.NewGuid();
            _previous = null;
            _lastEpisode = null;
            _lastScenarioAssessment = null;
            _movingReference = false;
            int index = _state.Profiles.FindIndex(profile => profile.Id == id);
            FusionLearningProfile profile = index >= 0
                ? _state.Profiles[index] : new(id, mount, sourceConfiguration);
            if (index < 0)
            {
                _state.Profiles.Add(profile);
                index = _state.Profiles.Count - 1;
            }

            _state.Profiles[index] = profile with { Sessions = profile.Sessions + 1 };
            _revision++;
        }
    }

    /// <summary>Observes full-rate support without performing storage or UI work on the fusion owner.</summary>
    internal void Observe(FusionObservation observation, string? recordingPath)
    {
        if (observation.Input.Kind != SensorDataKind.Imu || observation.Estimate is not { } estimate)
        {
            return;
        }

        lock (_gate)
        {
            if (_profileId is null)
            {
                return;
            }

            int index = _state.Profiles.FindIndex(profile => profile.Id == _profileId);
            FusionLearningProfile profile = _state.Profiles[index];
            bool supported = IsSupported(estimate);
            bool previousSupported = _previous?.Estimate is { } previous && IsSupported(previous);
            double elapsed = _previous is { } prior ? (estimate.Timestamp - prior.Estimate!.Timestamp).TotalSeconds : 0;
            bool continuous = _previous is { } preceding && elapsed is > 0 and <= 0.5
                && preceding.Epoch == observation.Epoch
                && preceding.Input.SourceId == observation.Input.SourceId
                && preceding.Input.SourceGeneration == observation.Input.SourceGeneration;
            bool moving = supported && estimate.PatternDiagnostics?.GravityReason.StartsWith(
                "conditional-moving-odometer-gravity;", StringComparison.Ordinal) == true;
            bool loss = previousSupported && !supported;
            profile = profile with
            {
                SupportedSeconds = profile.SupportedSeconds + (continuous && previousSupported && supported ? elapsed : 0),
                SupportLosses = profile.SupportLosses + (loss ? 1 : 0),
                MovingRenewals = profile.MovingRenewals + (moving && !_movingReference ? 1 : 0)
            };
            _state.Profiles[index] = profile;
            _movingReference = moving;
            if (loss)
            {
                QueueEpisode(observation, estimate.Support?.BodyAttitude.Reason ?? "body-support-lost",
                    null, recordingPath);
            }

            _previous = observation;
            _revision++;
        }
    }

    /// <summary>Queues a matched ambiguity, not an automatically asserted incline.</summary>
    internal void ObserveAssessment(FusionObservation observation, FusionPlausibilityAssessment assessment,
        string? recordingPath)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(Enum.IsDefined(assessment.Scenario), true, nameof(assessment));
        lock (_gate)
        {
            DateTimeOffset evidenceTime = assessment.WindowEnd ?? assessment.Timestamp;
            if (_profileId is not null && assessment.Scenario != FusionPlausibilityScenario.Unknown
                && (_lastScenarioAssessment is null || evidenceTime > _lastScenarioAssessment))
            {
                _lastScenarioAssessment = evidenceTime;
                int index = _state.Profiles.FindIndex(profile => profile.Id == _profileId);
                FusionLearningProfile profile = _state.Profiles[index];
                Dictionary<FusionPlausibilityScenario, int> counts = [with(profile.ScenarioCounts)];
                counts.TryGetValue(assessment.Scenario, out int count);
                counts[assessment.Scenario] = count < int.MaxValue ? count + 1 : count;
                _state.Profiles[index] = profile with { ScenarioCounts = counts };
                _revision++;
            }
        }

        if (assessment.OdometerAcceleration is null
            || (assessment.FusionAcceleration is not null && Math.Abs(assessment.Residual ?? 0) < 0.5))
        {
            return;
        }

        lock (_gate)
        {
            QueueEpisode(observation, assessment.Reason, assessment, recordingPath);
        }
    }

    private void QueueEpisode(FusionObservation observation, string reason,
        FusionPlausibilityAssessment? assessment, string? recordingPath)
    {
        DateTimeOffset end = assessment?.WindowEnd ?? observation.Input.SourceTimestamp;
        if (_profileId is null || (_lastEpisode is { } last && end - last < EpisodeInterval)
            || _state.Episodes.Count(episode => episode.Feedback is null) >= MaximumPendingEpisodes)
        {
            return;
        }

        _state.Episodes.Add(new(Guid.NewGuid(), _sessionId, _profileId,
            assessment?.WindowStart ?? end, end,
            assessment?.StartSequence ?? observation.Input.Sequence,
            assessment?.EndSequence ?? observation.Input.Sequence, assessment?.Epoch ?? observation.Epoch,
            assessment is null ? observation.Input.SourceId : assessment.ImuSourceId,
            assessment?.ImuSourceGeneration ?? observation.Input.SourceGeneration, reason,
            assessment?.OdometerAcceleration, assessment?.FusionAcceleration,
            assessment is null ? recordingPath : assessment.RecordingPath)
        {
            HasCanonicalSequenceRange = true
        });
        _lastEpisode = end;
        _revision++;
    }

    /// <summary>Returns immutable snapshots of unanswered episodes across all installations.</summary>
    internal FusionLearningEpisode[] PendingEpisodes
    {
        get
        {
            lock (_gate)
            {
                return [.. _state.Episodes.Where(episode => episode.Feedback is null).OrderByDescending(episode => episode.End)];
            }
        }
    }

    /// <summary>Gets whether retained legacy episodes have incomparable sequence endpoints.</summary>
    internal bool HasLegacySequenceRanges
    {
        get
        {
            lock (_gate)
            {
                return _state.Episodes.Any(episode => !episode.HasCanonicalSequenceRange);
            }
        }
    }

    /// <summary>Describes retained evidence without claiming that the estimator is trained.</summary>
    internal string Progress
    {
        get
        {
            lock (_gate)
            {
                FusionLearningProfile? profile = _state.Profiles.FirstOrDefault(profile => profile.Id == _profileId);
                int answered = _state.Episodes.Count(episode => episode.Feedback is not null);
                int pending = _state.Episodes.Count - answered;
                string coverage = profile is null ? "No live profile yet."
                    : $"This mount/source: {profile.Sessions} sessions; {profile.SupportedSeconds:0.0}s supported; "
                        + $"{profile.SupportLosses} dropouts; {profile.MovingRenewals} moving renewals.";

                string scenarios = profile is null || profile.ScenarioCounts.Count == 0 ? ""
                    : "\r\nScenario evidence: " + string.Join("; ", profile.ScenarioCounts
                        .OrderByDescending(entry => entry.Value)
                        .ThenBy(entry => entry.Key)
                        .Select(entry => $"{entry.Key}: {entry.Value}"));

                return $"{coverage}{scenarios}\r\nLabels: {answered} ({_persistedAnswers} saved); pending review: {pending}"
                    + (pending >= MaximumPendingEpisodes ? " (queue full; review before more episodes can be added)" : "")
                    + ". Evidence retained, not calibrated accuracy.";
            }
        }
    }

    /// <summary>Attaches a user's multi-category answer without changing sensor support or mounting.</summary>
    internal void Answer(Guid episodeId, FusionRoadFeedback feedback)
    {
        ArgumentNullException.ThrowIfNull(feedback);
        if (feedback.Acceleration is not ("unknown" or "none" or "slight" or "medium" or "full-throttle")
            || feedback.Incline is not ("unknown" or "none" or "light" or "medium" or "steep")
            || feedback.Notes is null || feedback.Notes.Length > 2000)
        {
            throw new ArgumentException("Feedback contains an invalid category or note.", nameof(feedback));
        }

        lock (_gate)
        {
            int index = _state.Episodes.FindIndex(episode => episode.Id == episodeId);
            if (index < 0 || _state.Episodes[index].Feedback is not null)
            {
                throw new InvalidOperationException("The episode is absent or has already been answered.");
            }

            _state.Episodes[index] = _state.Episodes[index] with { Feedback = feedback };
            _revision++;
        }
    }

    /// <summary>Atomically checkpoints dirty evidence; failed writes leave both the old file and in-memory data intact.</summary>
    internal async Task SaveAsync()
    {
        await _saveGate.WaitAsync().ConfigureAwait(false);
        try
        {
            byte[] bytes;
            long revision;
            int answers;
            lock (_gate)
            {
                revision = _revision;
                if (revision == _savedRevision)
                {
                    return;
                }

                bytes = JsonSerializer.SerializeToUtf8Bytes(_state);
                answers = _state.Episodes.Count(episode => episode.Feedback is not null);
            }

            string temporary = _path + ".writing";
            await File.WriteAllBytesAsync(temporary, bytes).ConfigureAwait(false);
            File.Move(temporary, _path, overwrite: true);
            lock (_gate)
            {
                _savedRevision = revision;
                _persistedAnswers = answers;
            }
        }
        finally
        {
            _saveGate.Release();
        }
    }

    private static bool IsSupported(FusionEstimate estimate)
        => !estimate.RejectedSample && estimate.IsAttitudeSupported && estimate.IsAccelerationSupported;

    /// <inheritdoc/>
    public void Dispose()
    {
        _ownership.Dispose();
        _saveGate.Dispose();
    }

    /// <summary>Versioned on-disk evidence, containing no reusable gravity or navigation state.</summary>
    private sealed record LearningState
    {
        /// <summary>Gets the evidence schema version.</summary>
        [System.Text.Json.Serialization.JsonRequired]
        public int Version { get; init; } = 1;
        /// <summary>Gets installation/source-specific coverage summaries.</summary>
        [System.Text.Json.Serialization.JsonRequired]
        public List<FusionLearningProfile> Profiles { get; init; } = [];
        /// <summary>Gets original episodes and their optional retrospective annotations.</summary>
        [System.Text.Json.Serialization.JsonRequired]
        public List<FusionLearningEpisode> Episodes { get; init; } = [];
    }
}
