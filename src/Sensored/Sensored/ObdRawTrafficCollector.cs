using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Collects raw OBD traffic and owns bounded-duration files on a separate worker.</summary>
/// <remarks>The UI reads a bounded tail only while visible. Recordings never use that lossy tail.</remarks>
internal sealed class ObdRawTrafficCollector : IAsyncDisposable
{
    private const int TailCapacity = 500;
    private readonly Channel<Work> _queue = Channel.CreateBounded<Work>(new BoundedChannelOptions(4096)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait
    });
    private readonly Lock _gate = new();
    private readonly Queue<ObdDiagnosticEntry> _tail = new();
    private readonly TimeSpan _maximumDuration;
    private readonly Task _worker;
    private StreamWriter? _writer;
    private System.Threading.Timer? _timer;
    private long _recordingStarted;
    private long _recordingId;
    private long _version;
    private string? _filePath;
    private bool _recording;
    private Exception? _fault;
    private int _closing;

    internal ObdRawTrafficCollector(TimeSpan? maximumDuration = null)
    {
        _maximumDuration = maximumDuration ?? TimeSpan.FromMinutes(1);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(_maximumDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(_maximumDuration, TimeSpan.FromMinutes(1));
        _worker = Task.Run(RunAsync);
    }

    internal Exception? Fault => Volatile.Read(ref _fault);

    internal void Publish(ObdDiagnosticEntry entry)
    {
        if (!_queue.Writer.TryWrite(new(entry)))
        {
            Interlocked.CompareExchange(ref _fault,
                new IOException("Raw OBD queue overflowed or closed; capture is incomplete."), null);
        }
    }

    internal Task StartRecordingAsync(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return SendAsync(new(null, path, Completion: new(TaskCreationOptions.RunContinuationsAsynchronously)));
    }

    internal Task StopRecordingAsync()
        => SendAsync(new(null, Stop: true, Completion: new(TaskCreationOptions.RunContinuationsAsynchronously)));

    internal ObdRawTrafficSnapshot ReadSnapshot()
    {
        lock (_gate)
        {
            return new(_version, _tail.ToArray(), _recording, _filePath);
        }
    }

    private async Task SendAsync(Work work)
    {
        if (Fault is { } fault)
        {
            throw new IOException("Raw OBD collection failed.", fault);
        }

        await _queue.Writer.WriteAsync(work).ConfigureAwait(false);
        await work.Completion!.Task.ConfigureAwait(false);
    }

    private async Task RunAsync()
    {
        Work? current = null;
        try
        {
            await foreach (Work work in _queue.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                current = work;
                if (Fault is { } fault)
                {
                    throw new IOException("Raw OBD capture is incomplete.", fault);
                }

                if (work.Path is { } path)
                {
                    if (_writer is not null)
                    {
                        throw new InvalidOperationException("A raw OBD recording is already active.");
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
                    _writer = new StreamWriter(new FileStream(path, FileMode.CreateNew,
                        FileAccess.Write, FileShare.Read, 4096, useAsync: true));
                    _recordingStarted = Stopwatch.GetTimestamp();
                    long id = ++_recordingId;
                    lock (_gate)
                    {
                        _filePath = path;
                        _recording = true;
                        _version++;
                    }

                    _timer = new System.Threading.Timer(_ => QueueAutomaticStop(id), null,
                        _maximumDuration, Timeout.InfiniteTimeSpan);
                }
                else if (work.Stop)
                {
                    if (work.RecordingId == 0 || work.RecordingId == _recordingId)
                    {
                        await CloseRecordingAsync().ConfigureAwait(false);
                    }
                }
                else if (work.Entry is { } entry)
                {
                    // Enforce the deadline even if a busy queue delays the automatic stop command.
                    if (_writer is not null && Stopwatch.GetElapsedTime(_recordingStarted) >= _maximumDuration)
                    {
                        await CloseRecordingAsync().ConfigureAwait(false);
                    }

                    if (_writer is { } writer)
                    {
                        await writer.WriteLineAsync(JsonSerializer.Serialize(entry)).ConfigureAwait(false);
                    }

                    lock (_gate)
                    {
                        if (_tail.Count == TailCapacity)
                        {
                            _tail.Dequeue();
                        }

                        _tail.Enqueue(entry);
                        _version++;
                    }
                }

                work.Completion?.TrySetResult();
                current = null;
            }
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _fault, exception, null);
            Trace.TraceError($"Raw OBD collection failed: {exception}");
            _queue.Writer.TryComplete(exception);
            current?.Completion?.TrySetException(exception);
            while (_queue.Reader.TryRead(out Work? pending))
            {
                pending.Completion?.TrySetException(exception);
            }
        }
        finally
        {
            try
            {
                await CloseRecordingAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Interlocked.CompareExchange(ref _fault, exception, null);
                Trace.TraceError($"Raw OBD recording finalization failed: {exception}");
            }
        }
    }

    private void QueueAutomaticStop(long id)
    {
        if (!_queue.Writer.TryWrite(new(null, Stop: true, RecordingId: id))
            && Volatile.Read(ref _closing) == 0)
        {
            Interlocked.CompareExchange(ref _fault,
                new IOException("Raw OBD automatic stop could not be queued; capture is incomplete."), null);
        }
    }

    private async Task CloseRecordingAsync()
    {
        _timer?.Dispose();
        _timer = null;
        if (_writer is { } writer)
        {
            _writer = null;
            try
            {
                await writer.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                lock (_gate)
                {
                    _recording = false;
                    _version++;
                }
            }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _closing, 1);
        _queue.Writer.TryComplete();
        await _worker.ConfigureAwait(false);
        if (Fault is { } fault)
        {
            throw new IOException("Raw OBD capture failed; any retained file may be incomplete.", fault);
        }
    }

    /// <summary>Serializes capture commands with incoming adapter messages.</summary>
    private sealed record Work(ObdDiagnosticEntry? Entry, string? Path = null, bool Stop = false,
        long RecordingId = 0, TaskCompletionSource? Completion = null);
}

/// <summary>Contains the bounded display tail and current recording state.</summary>
internal sealed record ObdRawTrafficSnapshot(
    long Version, ObdDiagnosticEntry[] Entries, bool Recording, string? FilePath);
