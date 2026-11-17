namespace Sensored;

/// <summary>
///  Stores a bounded, thread-safe history of application and sensor exceptions.
/// </summary>
internal sealed class ExceptionHistory
{
    private const int Capacity = 100;
    private readonly Lock _syncObject = new();
    private readonly Queue<ExceptionHistoryEntry> _entries = new();

    /// <summary>Gets the process-wide exception history.</summary>
    internal static ExceptionHistory Shared { get; } = new();

    /// <summary>Gets the latest exception, or <see langword="null"/>.</summary>
    internal ExceptionHistoryEntry? Latest
    {
        get
        {
            lock (_syncObject)
            {
                return _entries.Count == 0 ? null : _entries.Last();
            }
        }
    }

    /// <summary>Adds an exception to the history.</summary>
    /// <param name="context">The operation or subsystem reporting the exception.</param>
    /// <param name="exception">The reported exception.</param>
    /// <param name="diagnosticContext">Additional state captured where the exception was handled.</param>
    internal void Add(
        string context,
        Exception exception,
        string? diagnosticContext = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentNullException.ThrowIfNull(exception);

        lock (_syncObject)
        {
            _entries.Enqueue(new ExceptionHistoryEntry(
                DateTimeOffset.Now,
                context,
                exception.Message,
                FormatDetails(exception, diagnosticContext)));

            while (_entries.Count > Capacity)
            {
                _entries.Dequeue();
            }
        }
    }

    private static string FormatDetails(
        Exception exception,
        string? diagnosticContext)
        => string.IsNullOrWhiteSpace(diagnosticContext)
            ? exception.ToString()
            : $"{diagnosticContext}{Environment.NewLine}{Environment.NewLine}{exception}";

    /// <summary>Returns a stable snapshot ordered from oldest to newest.</summary>
    internal IReadOnlyList<ExceptionHistoryEntry> GetSnapshot()
    {
        lock (_syncObject)
        {
            return [.. _entries];
        }
    }
}

/// <summary>
///  Describes one exception captured by the sensor dashboard.
/// </summary>
/// <param name="Timestamp">The local time at which the exception was captured.</param>
/// <param name="Context">The operation or subsystem that reported the exception.</param>
/// <param name="Message">The exception message.</param>
/// <param name="Details">The complete exception text.</param>
internal sealed record ExceptionHistoryEntry(
    DateTimeOffset Timestamp,
    string Context,
    string Message,
    string Details);
