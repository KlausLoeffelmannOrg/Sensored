using WarpToolkit.WinForms.DirectX.Controls;

namespace WarpSensorsDemo;

/// <summary>
///  The severity of a demo log message.
/// </summary>
internal enum DemoLogLevel
{
    /// <summary>An informational message.</summary>
    Information,

    /// <summary>A warning.</summary>
    Warning,

    /// <summary>An error.</summary>
    Error
}

/// <summary>
///  Routes the demo's informational, warning and error messages to the debug console.
/// </summary>
/// <remarks>
///  <para>
///   The log is deliberately a thin, UI-thread-safe adapter: it owns no history of its own. The
///   structured record of failures stays in <see cref="ExceptionHistory"/>; this type only makes
///   the same events visible while the application runs.
///  </para>
///  <para>
///   Messages produced before a console is attached are buffered, so nothing that happened during
///   start-up is lost.
///  </para>
/// </remarks>
internal sealed class DemoLog
{
    private const int PendingCapacity = 200;
    private const string ResetSequence = "\u001b[0m";

    private readonly Queue<(DemoLogLevel Level, string Message)> _pending = new();
    private readonly Lock _syncObject = new();

    private D2DConsole? _console;

    /// <summary>
    ///  Attaches the console that renders the log and flushes everything buffered so far.
    /// </summary>
    /// <param name="console">The console to write to.</param>
    internal void Attach(D2DConsole console)
    {
        ArgumentNullException.ThrowIfNull(console);

        (DemoLogLevel Level, string Message)[] pending;

        lock (_syncObject)
        {
            _console = console;
            pending = [.. _pending];
            _pending.Clear();
        }

        foreach ((DemoLogLevel level, string message) in pending)
        {
            Write(level, message);
        }
    }

    /// <summary>
    ///  Detaches the console, for example while the form is closing.
    /// </summary>
    internal void Detach()
    {
        lock (_syncObject)
        {
            _console = null;
        }
    }

    /// <summary>
    ///  Logs an informational message.
    /// </summary>
    /// <param name="message">The message.</param>
    internal void Information(string message)
        => Write(DemoLogLevel.Information, message);

    /// <summary>
    ///  Logs a warning.
    /// </summary>
    /// <param name="message">The message.</param>
    internal void Warning(string message)
        => Write(DemoLogLevel.Warning, message);

    /// <summary>
    ///  Logs an error.
    /// </summary>
    /// <param name="message">The message.</param>
    internal void Error(string message)
        => Write(DemoLogLevel.Error, message);

    /// <summary>
    ///  Logs an error together with the exception that caused it.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="exception">The exception.</param>
    internal void Error(string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        Write(DemoLogLevel.Error, $"{message}: {exception.Message}");
    }

    /// <summary>
    ///  Formats one log line.
    /// </summary>
    /// <param name="timestamp">The time the message was produced.</param>
    /// <param name="level">The severity.</param>
    /// <param name="message">The message.</param>
    /// <returns>The formatted line.</returns>
    internal static string FormatLine(DateTimeOffset timestamp, DemoLogLevel level, string message)
    {
        string tag = level switch
        {
            DemoLogLevel.Error => "ERR",
            DemoLogLevel.Warning => "WRN",
            _ => "INF"
        };

        return $"[{timestamp.LocalDateTime:HH:mm:ss.fff}] {tag}  {message}";
    }

    private void Write(DemoLogLevel level, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        D2DConsole? console;

        lock (_syncObject)
        {
            console = _console;

            if (console is null)
            {
                if (_pending.Count >= PendingCapacity)
                {
                    _pending.Dequeue();
                }

                _pending.Enqueue((level, message));

                return;
            }
        }

        if (console.IsDisposed)
        {
            return;
        }

        string line = string.Concat(
            GetColorSequence(level),
            FormatLine(DateTimeOffset.Now, level, message),
            ResetSequence);

        try
        {
            // WriteLineAsync serializes concurrent producers and marshals to the UI thread itself.
            ValueTask pending = console.WriteLineAsync(line);

            if (!pending.IsCompletedSuccessfully)
            {
                _ = pending.AsTask();
            }
        }
        catch (ObjectDisposedException)
        {
            // The console went away between the check and the write; losing a log line while the
            // window closes is not worth failing the caller for.
        }
    }

    private static string GetColorSequence(DemoLogLevel level)
        => level switch
        {
            DemoLogLevel.Error => "\u001b[91m",
            DemoLogLevel.Warning => "\u001b[93m",
            _ => "\u001b[37m"
        };
}
