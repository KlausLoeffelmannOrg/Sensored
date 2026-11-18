using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sensored;

internal sealed record RoadLearningAnswer(
    int Version, RoadLearningQuestion Question, int SelectedLevel,
    DateTimeOffset AnsweredAt, bool Retracted = false)
{
    public string LabelKind => SelectedLevel == 0 ? "rejected-guess" : "user-confirmed-condition";
}

/// <summary>One atomic, self-contained training example per answer; undo retains an explicit retraction.</summary>
internal sealed class RoadLearningJournal : IDisposable
{
    private static readonly JsonSerializerOptions s_json = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly string _folder;
    private readonly SemaphoreSlim _gate = new(1);
    private readonly List<(Guid Id, bool Retracted)> _answers = [];

    internal RoadLearningJournal(string root, Guid sessionId)
    {
        _folder = Path.Combine(root, sessionId.ToString("N"));
    }

    internal bool CanUndo => _answers.Any(answer => !answer.Retracted);

    internal async Task AnswerAsync(RoadLearningQuestion question, int level, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(level, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(level, 5);
        if (now < question.End || now - question.End > RoadLearningSession.QuestionLifetime)
        {
            throw new InvalidOperationException("This question is stale. Wait for a new condition.");
        }

        await _gate.WaitAsync().ConfigureAwait(true);
        try
        {
            if (_answers.Any(answer => answer.Id == question.Id))
            {
                throw new InvalidOperationException("This question has already been answered.");
            }

            RoadLearningAnswer answer = new(1, question, level, now);
            await WriteAsync(answer).ConfigureAwait(true);
            _answers.Add((question.Id, false));
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task UndoAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(true);
        try
        {
            int index = _answers.FindLastIndex(answer => !answer.Retracted);
            if (index < 0)
            {
                throw new InvalidOperationException("There is no previous answer to disregard.");
            }

            byte[] bytes = await File.ReadAllBytesAsync(AnswerPath(_answers[index].Id)).ConfigureAwait(true);
            RoadLearningAnswer original = JsonSerializer.Deserialize<RoadLearningAnswer>(bytes, s_json)
                ?? throw new InvalidDataException("The saved quiz answer is empty; it was not changed.");
            if (original.Version != 1 || original.Question.Id != _answers[index].Id)
            {
                throw new InvalidDataException("The saved quiz answer is incompatible; it was not changed.");
            }

            RoadLearningAnswer retracted = original with { Retracted = true };
            await WriteAsync(retracted).ConfigureAwait(true);
            _answers[index] = (retracted.Question.Id, true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteAsync(RoadLearningAnswer answer)
    {
        Directory.CreateDirectory(_folder);
        string path = AnswerPath(answer.Question.Id);
        string temporary = path + ".writing";
        await File.WriteAllBytesAsync(temporary, JsonSerializer.SerializeToUtf8Bytes(answer, s_json)).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }

    private string AnswerPath(Guid id) => Path.Combine(_folder, $"{id:N}.json");

    public void Dispose() => _gate.Dispose();
}
