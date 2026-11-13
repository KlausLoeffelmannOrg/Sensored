using System.Globalization;
using System.Text;
using System.Text.Json;
using WarpToolkit.Sensors;

namespace WarpSensorsDemo;

/// <summary>
///  Last-chance handling for exceptions that nothing else catches.
/// </summary>
/// <remarks>
///  <para>
///   The service-owned recording writes through a buffered file stream on its own writer thread. A
///   process that disappears without flushing it loses exactly the part of the recording that
///   explains why it disappeared, which makes an in-vehicle failure unreproducible. The guard
///   therefore flushes (or stops) the active recording with a bounded wait and writes a plain text
///   crash report so a failure that happens before - or instead of - recording is still visible.
///  </para>
///  <para>
///   Every path here is defensive: a last-chance handler that throws replaces a diagnosable crash
///   with an undiagnosable one.
///  </para>
/// </remarks>
internal sealed class CrashGuard
{
    private const string CrashReportDirectoryName = "CrashReports";
#if DEBUG
    private const string BuildConfiguration = "Debug";
#else
    private const string BuildConfiguration = "Release";
#endif

    /// <summary>
    ///  How long the guard waits for the log writers to drain. A crashing process must not hang.
    /// </summary>
    private static readonly TimeSpan FlushTimeout = TimeSpan.FromSeconds(3);

    private readonly string _runId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
    private string _stage = "installing";
    private int _reportInProgress;
    private int _fatalReported;

    private CrashGuard()
    {
    }

    /// <summary>
    ///  Installs the guard for the lifetime of the process.
    /// </summary>
    public static CrashGuard Install()
    {
        CrashGuard guard = new();

        try
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        }
        catch (InvalidOperationException)
        {
            // A window already exists; the current mode stays in effect.
        }

        Application.ThreadException += guard.OnThreadException;
        AppDomain.CurrentDomain.UnhandledException += guard.OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += guard.OnUnobservedTaskException;
        guard.WriteLifecycleEvent("run-start");

        return guard;
    }

    /// <summary>
    ///  Records the current startup or runtime stage in the durable lifecycle log.
    /// </summary>
    /// <param name="stage">The stable stage name.</param>
    public void SetStage(string stage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stage);
        _stage = stage;
        WriteLifecycleEvent("stage");
    }

    /// <summary>
    ///  Records an exception escaping the application's synchronous main path.
    /// </summary>
    /// <param name="exception">The fatal exception.</param>
    public void ReportFatal(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Interlocked.Exchange(ref _fatalReported, 1);
        Report("unhandled-main-exception", exception, stopRecording: true);
    }

    /// <summary>
    ///  Removes the guard's handlers, used when the application shuts down normally.
    /// </summary>
    public void Uninstall()
    {
        WriteLifecycleEvent(
            Volatile.Read(ref _fatalReported) == 0
                ? "run-stop"
                : "run-stop-after-fatal");
        Application.ThreadException -= OnThreadException;
        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnThreadException(object? sender, ThreadExceptionEventArgs e)
    {
        Report("unhandled-ui-exception", e.Exception, stopRecording: false);
        ShowFailure(e.Exception);
    }

    private void OnAppDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        Exception? exception = e.ExceptionObject as Exception;

        if (e.IsTerminating)
        {
            Interlocked.Exchange(ref _fatalReported, 1);
        }

        // A terminating exception gives no second chance, so the logs are closed rather than only
        // flushed: the file must be complete and released by the time the process is gone.
        Report(
            e.IsTerminating ? "unhandled-terminating-exception" : "unhandled-background-exception",
            exception,
            stopRecording: e.IsTerminating);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Report("unobserved-task-exception", e.Exception, stopRecording: false);

        // The failure is now recorded, so it must not additionally tear the process down.
        e.SetObserved();
    }

    /// <summary>
    ///  Records the exception, flushes (and optionally closes) both sensor logs, and writes a crash
    ///  report file.
    /// </summary>
    /// <param name="kind">The kind discriminator written into the diagnostics log.</param>
    /// <param name="exception">The exception, when one is available.</param>
    /// <param name="stopRecording">Whether the logs should be closed rather than only flushed.</param>
    private void Report(string kind, Exception? exception, bool stopRecording)
    {
        // Re-entrancy is real here: flushing or reporting can itself fail on a dying process.
        if (Interlocked.Exchange(ref _reportInProgress, 1) == 1)
        {
            return;
        }

        try
        {
            if (exception is not null)
            {
                ExceptionHistory.Shared.Add(kind, exception);
            }

            ISensorRecordingSession? recording = TryGet(static () => DemoRecordingRegistry.Active);

            if (stopRecording)
            {
                Bounded(() => recording?.StopAsync() ?? Task.CompletedTask);
            }
            else
            {
                Bounded(() => recording?.FlushAsync() ?? Task.CompletedTask);
            }

            WriteCrashReport(kind, exception, recording?.FolderPath);
            WriteLifecycleEvent(kind, exception, recording?.FolderPath);
        }
        catch (Exception)
        {
            // A last-chance handler never throws.
        }
        finally
        {
            Volatile.Write(ref _reportInProgress, 0);
        }
    }

    private static T? TryGet<T>(Func<T?> accessor)
    {
        try
        {
            return accessor();
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>
    ///  Runs an asynchronous cleanup step, waiting no longer than <see cref="FlushTimeout"/>.
    /// </summary>
    /// <param name="operation">The step to run.</param>
    private static void Bounded(Func<Task> operation)
    {
        try
        {
            Task task = operation();
            _ = task.Wait(FlushTimeout);
        }
        catch (Exception)
        {
            // Intentionally ignored on the crash path.
        }
    }

    private void WriteCrashReport(
        string kind,
        Exception? exception,
        string? recordingFolderPath)
    {
        try
        {
            string directory = GetCrashReportDirectory();
            Directory.CreateDirectory(directory);

            string timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            string path = Path.Combine(directory, $"warp-crash-{timestamp}.txt");

            StringBuilder report = new();
            report.AppendLine(CultureInfo.InvariantCulture, $"Run:         {_runId}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Kind:        {kind}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Occurred at: {DateTimeOffset.Now:O}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Stage:       {_stage}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Build:       {BuildConfiguration}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Process:     {Environment.ProcessPath ?? "(unknown)"}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Process ID:  {Environment.ProcessId}");
            report.AppendLine(CultureInfo.InvariantCulture, $"Recording:   {recordingFolderPath ?? "(none)"}");
            report.AppendLine();
            report.AppendLine(exception?.ToString() ?? "(no exception object was supplied)");

            WriteTextDurably(path, report.ToString(), append: false);
        }
        catch (Exception)
        {
            // Intentionally ignored on the crash path.
        }
    }

    private void WriteLifecycleEvent(
        string kind,
        Exception? exception = null,
        string? recordingFolderPath = null)
    {
        try
        {
            string directory = GetCrashReportDirectory();
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "warp-lifecycle.jsonl");
            string line = JsonSerializer.Serialize(new
            {
                SchemaVersion = 1,
                Kind = kind,
                RunId = _runId,
                OccurredAt = DateTimeOffset.Now,
                Stage = _stage,
                BuildConfiguration,
                ProcessId = Environment.ProcessId,
                ProcessPath = Environment.ProcessPath,
                RecordingFolderPath = recordingFolderPath,
                ExceptionType = exception?.GetType().FullName,
                ExceptionMessage = exception?.Message
            });

            WriteTextDurably(path, $"{line}{Environment.NewLine}", append: true);
        }
        catch (Exception)
        {
            // Durable diagnostics must never become the cause of a failure.
        }
    }

    private static string GetCrashReportDirectory()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string root = string.IsNullOrWhiteSpace(localAppData)
            ? Environment.CurrentDirectory
            : localAppData;

        return Path.Combine(root, "WARP", "WarpSensorsDemo", CrashReportDirectoryName);
    }

    private static void WriteTextDurably(string path, string contents, bool append)
    {
        using FileStream stream = new(
            path,
            append ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        using StreamWriter writer = new(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(contents);
        writer.Flush();
        stream.Flush(flushToDisk: true);
    }

    private static void ShowFailure(Exception exception)
    {
        try
        {
            MessageBox.Show(
                $"An unexpected error occurred and was written to the crash report and the diagnostics log.{Environment.NewLine}{Environment.NewLine}{exception.Message}",
                "WARP sensors demo",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception)
        {
            // Intentionally ignored on the crash path.
        }
    }
}
