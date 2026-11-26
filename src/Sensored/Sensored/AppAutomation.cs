using System.ComponentModel;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Sensored;

internal sealed record AutomationCommand(Guid Id, string Action, Guid? QuestionId = null, int? Level = null);

internal sealed class AppAutomation : IDisposable
{
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };
    private readonly FileStream _ownership;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly Func<AutomationCommand, Task> _execute;
    private readonly Func<object> _state;
    private readonly Func<IEnumerable<Form>> _windows;
    private readonly Action<string, Exception> _reportError;
    private readonly HashSet<Guid> _handled = [];
    private DateTimeOffset _nextCapture;
    private bool _busy;
    private bool _disposed;
    private bool _captureEnabled = true;
    private string _phase = "starting";
    private string? _error;
    private string[] _screenshots = [];

    internal AppAutomation(string directory, Func<AutomationCommand, Task> execute, Func<object> state,
        Func<IEnumerable<Form>> windows, Action<string, Exception> reportError)
    {
        DirectoryPath = Path.GetFullPath(directory);
        System.IO.Directory.CreateDirectory(DirectoryPath);
        if (System.IO.Directory.EnumerateFileSystemEntries(DirectoryPath).Any())
        {
            throw new IOException("Automation requires a new or empty run directory.");
        }

        _ownership = new FileStream(Path.Combine(DirectoryPath, "run.lock"), FileMode.CreateNew,
            FileAccess.Write, FileShare.None);
        _execute = execute;
        _state = state;
        _windows = windows;
        _reportError = reportError;
        _timer.Tick += OnTick;
    }

    internal string DirectoryPath { get; }
    internal string LabelsPath => Path.Combine(DirectoryPath, "quiz-labels");

    internal static string? ParseDirectory(string[] args)
    {
        if (args.Length == 0) { return null; }
        if (args.Length != 2 || args[0] != "--automation" || string.IsNullOrWhiteSpace(args[1]))
        {
            throw new ArgumentException("Usage: Sensored.exe [--automation <new-run-directory>]");
        }

        return Path.GetFullPath(args[1]);
    }

    internal void Start()
    {
        _phase = "ready";
        WriteStatus();
        _timer.Start();
    }

    private async void OnTick(object? sender, EventArgs e)
        => await TickAsync().ConfigureAwait(true);

    internal async Task TickAsync()
    {
        if (_busy || _disposed) { return; }
        _busy = true;
        try
        {
            string commandPath = Path.Combine(DirectoryPath, "command.json");
            if (File.Exists(commandPath))
            {
                string processingPath = Path.Combine(DirectoryPath, "processing.json");
                File.Move(commandPath, processingPath);
                AutomationCommand? command = null;
                try
                {
                    command = JsonSerializer.Deserialize<AutomationCommand>(File.ReadAllText(processingPath))
                        ?? throw new InvalidDataException("Empty automation command.");
                    if (command.Id == Guid.Empty || !_handled.Add(command.Id))
                    {
                        throw new InvalidOperationException("Command ID must be nonempty and unique.");
                    }

                    await _execute(command).ConfigureAwait(true);
                    _error = null;
                    WriteJson("result.json", new { command.Id, Success = true, Error = (string?)null });
                }
                catch (Exception exception)
                {
                    _error = exception.Message;
                    _reportError("Automation command failed", exception);
                    WriteJson("result.json", new { Id = command?.Id, Success = false, Error = exception.Message });
                }
                finally
                {
                    File.Delete(processingPath);
                }
            }

            if (_disposed) { return; }
            if (_captureEnabled && DateTimeOffset.UtcNow >= _nextCapture)
            {
                CaptureWindows();
                _nextCapture = DateTimeOffset.UtcNow.AddSeconds(10);
            }

            WriteStatus();
        }
        catch (Exception exception)
        {
            ReportFailure("Automation monitoring failed", exception);
        }
        finally
        {
            _busy = false;
        }
    }

    internal void ReportFailure(string message, Exception exception)
    {
        _error = $"{message}: {exception.Message}";
        _phase = "faulted";
        _captureEnabled = false;
        if (!_disposed) { _timer.Start(); }
        _reportError(message, exception);
        WriteStatus();
    }

    internal void RecordError(string message, Exception exception)
    {
        _error = $"{message}: {exception.Message}";
        WriteStatus();
    }

    private void CaptureWindows()
    {
        List<string> paths = [];
        foreach (Form window in _windows())
        {
            if (!window.Visible || window.WindowState == FormWindowState.Minimized)
            {
                throw new InvalidOperationException($"Cannot capture hidden or minimized window {window.Name}.");
            }

            string fileName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{window.Name}.png";
            string path = Path.Combine(DirectoryPath, fileName);
            using Bitmap bitmap = new(window.Width, window.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                nint hdc = graphics.GetHdc();
                try
                {
                    // Print only this HWND, never the desktop or overlapping applications.
                    if (!PrintWindow(window.Handle, hdc, 2))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), $"Could not capture {window.Name}.");
                    }
                }
                finally
                {
                    graphics.ReleaseHdc(hdc);
                }
            }

            bitmap.Save(path, ImageFormat.Png);
            paths.Add(path);
        }

        _screenshots = [.. paths];
    }

    private void WriteStatus()
        => WriteJson("status.json", new
        {
            ProcessId = Environment.ProcessId,
            UpdatedAt = DateTimeOffset.UtcNow,
            Phase = _phase,
            Error = _error,
            Screenshots = _screenshots,
            LabelsPath,
            State = _state()
        });

    private void WriteJson(string name, object value)
    {
        string path = Path.Combine(DirectoryPath, name);
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, s_jsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    public void Dispose()
    {
        if (_disposed) { return; }
        _disposed = true;
        _timer.Dispose();
        try
        {
            _phase = "closed";
            WriteStatus();
        }
        finally
        {
            _ownership.Dispose();
        }
    }

    [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PrintWindow(nint hwnd, nint hdc, uint flags);
}
