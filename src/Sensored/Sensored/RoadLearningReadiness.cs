using System.Text;
using System.Text.Json;

namespace Sensored;

internal enum RoadLearningStage { Collecting, Promising, Ready }

internal sealed record RoadConditionTally(RoadCondition Condition, int Confirmed, int Rejected, int Sessions)
{
    internal double RejectionRate => Confirmed + Rejected == 0 ? 0 : (double)Rejected / (Confirmed + Rejected);
    internal bool Trainable => Confirmed >= RoadLearningReadiness.MinConfirmedPerCondition
        && Sessions >= RoadLearningReadiness.MinSessionsPerCondition;
}

internal sealed record RoadLearningReadinessReport(
    int Sessions, int Days, int Confirmed, int Rejected, int Retracted, int Unreadable,
    RoadConditionTally[] Conditions, int SessionAnswers, TimeSpan SessionSpan)
{
    internal RoadConditionTally[] TrainableConditions => [.. Conditions.Where(item => item.Trainable)];

    internal RoadLearningStage Stage
        => TrainableConditions.Length >= RoadLearningReadiness.MinTrainableConditions
            && Sessions >= RoadLearningReadiness.MinSessions && Days >= RoadLearningReadiness.MinDays
            && Confirmed >= RoadLearningReadiness.MinConfirmed ? RoadLearningStage.Ready
        : TrainableConditions.Length > 0 && Sessions >= 2 ? RoadLearningStage.Promising
        : RoadLearningStage.Collecting;
}

/// <summary>
/// Summarizes all saved quiz labels to advise when a first offline training run is worthwhile.
/// Thresholds are heuristics: enough examples per class, spread over whole sessions so one can be held out.
/// </summary>
internal static class RoadLearningReadiness
{
    internal const int MinConfirmedPerCondition = 20;
    internal const int MinSessionsPerCondition = 3;
    internal const int MinTrainableConditions = 5;
    internal const int MinSessions = 4;
    internal const int MinDays = 3;
    internal const int MinConfirmed = 150;
    internal const int MeaningfulSessionAnswers = 8;
    internal static readonly TimeSpan MeaningfulSessionSpan = TimeSpan.FromMinutes(15);
    private const string StateFile = "readiness-state.json";

    internal static RoadLearningReadinessReport Assess(string root, Guid currentSession)
    {
        Dictionary<RoadCondition, (int Confirmed, int Rejected, HashSet<Guid> Sessions)> tallies = [];
        HashSet<Guid> sessions = [];
        HashSet<DateOnly> days = [];
        int confirmed = 0, rejected = 0, retracted = 0, unreadable = 0, sessionAnswers = 0;
        DateTimeOffset? first = null, last = null;
        IEnumerable<string> files = Directory.Exists(root)
            ? Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                .Where(path => !string.Equals(Path.GetFileName(path), StateFile, StringComparison.OrdinalIgnoreCase))
            : [];
        foreach (string path in files)
        {
            try
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(path));
                JsonElement answer = document.RootElement;
                JsonElement question = answer.GetProperty("Question");
                if (answer.GetProperty("Version").GetInt32() != 1
                    || !Enum.TryParse(question.GetProperty("Condition").GetString(), out RoadCondition condition))
                {
                    unreadable++;
                    continue;
                }

                if (answer.TryGetProperty("Retracted", out JsonElement flag) && flag.GetBoolean())
                {
                    retracted++;
                    continue;
                }

                Guid session = question.GetProperty("SessionId").GetGuid();
                int level = answer.GetProperty("SelectedLevel").GetInt32();
                DateTimeOffset answeredAt = answer.GetProperty("AnsweredAt").GetDateTimeOffset();
                sessions.Add(session);
                days.Add(DateOnly.FromDateTime(answeredAt.LocalDateTime));
                var tally = tallies.TryGetValue(condition, out var existing) ? existing : (0, 0, new HashSet<Guid>());
                tally.Item3.Add(session);
                if (level == 0)
                {
                    rejected++;
                    tally.Item2++;
                }
                else
                {
                    confirmed++;
                    tally.Item1++;
                }

                tallies[condition] = tally;
                if (session == currentSession)
                {
                    sessionAnswers++;
                    DateTimeOffset start = question.GetProperty("Start").GetDateTimeOffset();
                    DateTimeOffset end = question.GetProperty("End").GetDateTimeOffset();
                    first = first is { } f && f < start ? f : start;
                    last = last is { } l && l > end ? l : end;
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
                or JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                unreadable++;
            }
        }

        return new(sessions.Count, days.Count, confirmed, rejected, retracted, unreadable,
            [.. tallies.Select(pair => new RoadConditionTally(pair.Key, pair.Value.Confirmed,
                pair.Value.Rejected, pair.Value.Sessions.Count))
                .OrderByDescending(item => item.Confirmed).ThenBy(item => item.Condition)],
            sessionAnswers, first is { } a && last is { } b ? b - a : TimeSpan.Zero);
    }

    /// <summary>Short errands stay silent; a substantial session or a newly reached stage triggers the reminder.</summary>
    internal static bool ShouldRemind(RoadLearningReadinessReport report, RoadLearningStage lastReminded)
        => report.Stage > lastReminded
            || (report.SessionAnswers >= MeaningfulSessionAnswers && report.SessionSpan >= MeaningfulSessionSpan);

    internal static RoadLearningStage ReadLastStage(string root)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root, StateFile)));
            return Enum.TryParse(document.RootElement.GetProperty("LastRemindedStage").GetString(),
                out RoadLearningStage stage) ? stage : RoadLearningStage.Collecting;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException
            or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return RoadLearningStage.Collecting;
        }
    }

    internal static void WriteLastStage(string root, RoadLearningStage stage)
    {
        Directory.CreateDirectory(root);
        string path = Path.Combine(root, StateFile);
        File.WriteAllText(path + ".writing", JsonSerializer.Serialize(new { LastRemindedStage = stage.ToString() }));
        File.Move(path + ".writing", path, overwrite: true);
    }

    internal static string Format(RoadLearningReadinessReport report)
    {
        StringBuilder text = new();
        text.AppendLine($"This session: {report.SessionAnswers} answers over {report.SessionSpan.TotalMinutes:0} min.");
        text.AppendLine($"All sessions: {report.Confirmed} confirmed, {report.Rejected} rejected guesses "
            + $"({report.Retracted} disregarded) from {report.Sessions} sessions on {report.Days} days.");
        if (report.Unreadable > 0)
        {
            text.AppendLine($"{report.Unreadable} saved file(s) could not be read and were skipped.");
        }

        text.AppendLine();
        text.AppendLine($"Conditions with enough data ({MinConfirmedPerCondition}+ confirmed in {MinSessionsPerCondition}+ sessions):");
        RoadConditionTally[] trainable = report.TrainableConditions;
        text.AppendLine(trainable.Length == 0 ? "  none yet"
            : string.Join(Environment.NewLine, trainable.Select(item => $"  {RoadConditions.Title(item.Condition)}: "
                + $"{item.Confirmed} in {item.Sessions} sessions")));
        RoadConditionTally[] close = [.. report.Conditions.Where(item => !item.Trainable && item.Confirmed > 0).Take(5)];
        if (close.Length > 0)
        {
            text.AppendLine("Closest to enough:");
            text.AppendLine(string.Join(Environment.NewLine, close.Select(item => $"  {RoadConditions.Title(item.Condition)}: "
                + $"{item.Confirmed}/{MinConfirmedPerCondition} in {item.Sessions}/{MinSessionsPerCondition} sessions")));
        }

        RoadConditionTally[] weak = [.. report.Conditions.Where(item => item.Confirmed + item.Rejected >= 10
            && item.RejectionRate >= 0.5)];
        if (weak.Length > 0)
        {
            text.AppendLine("Often wrong so far (good learning targets): " + string.Join(", ", weak.Select(item
                => $"{RoadConditions.Title(item.Condition)} ({item.RejectionRate:P0} rejected)")));
        }

        text.AppendLine();
        text.AppendLine(report.Stage switch
        {
            RoadLearningStage.Ready => "Advice: a first offline training run is worthwhile now. Hold out at least one whole "
                + "session for evaluation and compare against the current rules before trusting the result.",
            RoadLearningStage.Promising => "Advice: not yet. Keep collecting on different routes and days; "
                + $"aim for {MinTrainableConditions} conditions with enough data, {MinSessions}+ sessions on {MinDays}+ days "
                + $"and {MinConfirmed}+ confirmed answers in total.",
            _ => "Advice: keep collecting. Longer drives on varied roads add the most useful examples."
        });
        return text.ToString();
    }
}
