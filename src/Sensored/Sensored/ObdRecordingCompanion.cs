using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Contains the independent vehicle input and reference at a fusion owner boundary.</summary>
internal sealed record ObdFusionState(
    ObdTelemetrySnapshot Telemetry, SessionDownLock? Frame, TabletFramePhase? Phase = TabletFramePhase.Idle)
{
    /// <summary>Gets optional comparison evidence, which is deliberately not applied to the estimator.</summary>
    public SensorComparisonReference? Comparison { get; init; }

    /// <summary>Gets the exact optional correction submitted at this owner boundary, including its original support.</summary>
    public FusionPlausibilityFeedback? Plausibility { get; init; }

    internal void Apply(IFusionEngine engine)
    {
        if (Plausibility is { } feedback)
        {
            if (engine is not IFusionPlausibilityController controller)
            {
                throw new InvalidOperationException("The current engine cannot restore recorded plausibility feedback.");
            }

            controller.PostPlausibility(feedback);
        }

        if (Phase is not null && engine is ITabletFrameController mount)
        {
            if (Phase == TabletFramePhase.Reading && mount.FrameStatus.Phase != Phase)
            {
                mount.BeginTabletFrameCapture();
            }

            if (Frame is { } frame && mount.FrameStatus.Lock != frame)
            {
                mount.ApplySessionDownLock(frame);
            }
            else if (Frame is null && mount.HasSessionDownLock)
            {
                mount.ClearSessionDownLock();
            }
        }
        else if (Frame is not null || Phase is not null)
        {
            throw new InvalidOperationException("The current engine cannot restore recorded mounting controls.");
        }

        if (engine is IVehicleSpeedConsumer speed)
        {
            speed.SetVehicleSpeed(Telemetry.Speed);
        }
        else if (Telemetry.Speed is not null)
        {
            throw new InvalidOperationException("The current engine cannot restore recorded vehicle speed.");
        }
    }
}

/// <summary>Associates an exact original envelope with the state consumed before processing it.</summary>
internal sealed record ObdRecordedInput(SensorEnvelope Input, ObdFusionState State);

/// <summary>
/// Keeps a temporary disk-backed input journal from session start. Recording exports include
/// the causal prefix, permitting exact warm-up even when raw recording begins mid-drive.
/// </summary>
internal sealed class ObdRecordingCompanion : IAsyncDisposable
{
    internal const string FileName = "obd-inputs.csv";
    internal const string Header = "warp-obd-inputs;3;sequence;kind;source_ticks;arrival_ticks;state_base64;input_base64";
    private const string LegacyHeader = "warp-obd-inputs;2;sequence;kind;source_ticks;arrival_ticks;state_base64;input_base64";
    internal static readonly JsonSerializerOptions Json = new()
    {
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };
    private readonly Channel<JournalEntry> _inputs = Channel.CreateBounded<JournalEntry>(
        new BoundedChannelOptions(8192) { SingleReader = true });
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"Sensored-obd-{Guid.NewGuid():N}.writing");
    private readonly Task _writer;
    private Exception? _fault;
    private long _lastSequence = -1;

    internal ObdRecordingCompanion()
    {
        FileStream file = new(_path, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        _writer = Task.Run(() => WriteAsync(file));
    }

    internal Exception? Fault => Volatile.Read(ref _fault) ?? _writer.Exception?.GetBaseException();
    internal long LastSequence => Interlocked.Read(ref _lastSequence);
    internal static bool IsLegacyHeader(string? header)
        => header == LegacyHeader;

    internal void Publish(SensorEnvelope input, ObdFusionState state)
    {
        if (Fault is { } fault)
        {
            throw new IOException("OBD input journal failed; deterministic recording is unavailable.", fault);
        }

        if (!_inputs.Writer.TryWrite(new(new(input, state), null)))
        {
            IOException overflow = new("OBD input journal overflowed; no complete companion can be published.");
            Interlocked.CompareExchange(ref _fault, overflow, null);
            throw overflow;
        }

        Interlocked.Exchange(ref _lastSequence, input.Sequence);
    }

    internal async Task ExportAsync(string folder, CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<long> flushed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await _inputs.Writer.WriteAsync(new(null, flushed), cancellationToken).ConfigureAwait(false);
        long length = await flushed.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        if (Fault is { } fault)
        {
            throw new IOException("OBD input journal cannot be exported.", fault);
        }

        string destination = Path.Combine(folder, FileName);
        await using (FileStream source = new(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        await using (FileStream target = new(destination + ".writing", FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            byte[] buffer = new byte[65536];
            while (length > 0)
            {
                int count = await source.ReadAsync(buffer.AsMemory(0, (int)Math.Min(length, buffer.Length)),
                    cancellationToken).ConfigureAwait(false);
                if (count == 0)
                {
                    throw new EndOfStreamException("The OBD journal ended before its committed boundary.");
                }

                await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
                length -= count;
            }
        }

        File.Move(destination + ".writing", destination);
        File.Delete(destination + ".pending");
    }

    private async Task WriteAsync(FileStream file)
    {
        TaskCompletionSource<long>? flushing = null;
        try
        {
            await using StreamWriter writer = new(file);
            await writer.WriteLineAsync(Header).ConfigureAwait(false);
            ObdFusionState? previous = null;
            await foreach (JournalEntry entry in _inputs.Reader.ReadAllAsync().ConfigureAwait(false))
            {
                if (entry.Flush is { } flush)
                {
                    flushing = flush;
                    await writer.FlushAsync().ConfigureAwait(false);
                    flush.TrySetResult(file.Position);
                    flushing = null;
                    continue;
                }

                ObdRecordedInput recorded = entry.Recorded!;
                SensorEnvelope input = recorded.Input;
                string state = recorded.State == previous ? string.Empty
                    : Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(recorded.State, Json));
                string payload = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(input, Json));
                await writer.WriteLineAsync(FormattableString.Invariant(
                    $"{input.Sequence};{(int)input.Kind};{input.SourceTimestamp.UtcTicks};{input.ArrivalTimestamp.UtcTicks};{state};{payload}"))
                    .ConfigureAwait(false);
                previous = recorded.State;
            }
        }
        catch (Exception exception)
        {
            Interlocked.CompareExchange(ref _fault, exception, null);
            flushing?.TrySetException(exception);
            _inputs.Writer.TryComplete(exception);
            while (_inputs.Reader.TryRead(out JournalEntry? pending))
            {
                pending.Flush?.TrySetException(exception);
            }

            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _inputs.Writer.TryComplete();
        try
        {
            await _writer.ConfigureAwait(false);
        }
        finally
        {
            File.Delete(_path);
        }
    }

    /// <summary>Orders owner observations and export flushes through the same bounded queue.</summary>
    private sealed record JournalEntry(ObdRecordedInput? Recorded, TaskCompletionSource<long>? Flush);
}

/// <summary>Streams original inputs in order with bounded memory, including duplicates and causal warm-up.</summary>
internal sealed class ObdReplayCursor : IDisposable
{
    private readonly StreamReader _reader;
    private ObdFusionState? _state;
    private long _lastSequence = -1;

    private ObdReplayCursor(StreamReader reader, bool includesCanonicalInputs)
    {
        _reader = reader;
        IncludesCanonicalInputs = includesCanonicalInputs;
    }

    internal bool IncludesCanonicalInputs { get; }

    internal static ObdReplayCursor? Open(string folder)
    {
        string path = Path.Combine(folder, ObdRecordingCompanion.FileName);
        if (File.Exists(path + ".writing") || File.Exists(path + ".pending"))
        {
            throw new InvalidDataException("The OBD companion is incomplete (.writing).");
        }

        if (!File.Exists(path))
        {
            return null;
        }

        StreamReader reader = new(path);
        string? header = reader.ReadLine();
        if (header != ObdRecordingCompanion.Header && !ObdRecordingCompanion.IsLegacyHeader(header))
        {
            reader.Dispose();
            throw new InvalidDataException("Unsupported OBD companion header.");
        }

        return new(reader, header == ObdRecordingCompanion.Header);
    }

    internal static async Task<long> FindMinimumSequenceAsync(
        IAsyncEnumerable<SensorEnvelopeRow> rows, TimeSpan position)
    {
        DateTimeOffset? firstArrival = null;
        DateTimeOffset timeline = default;
        await foreach (SensorEnvelopeRow row in rows.ConfigureAwait(false))
        {
            firstArrival ??= row.Envelope.ArrivalTimestamp;
            if (row.Envelope.ArrivalTimestamp > timeline)
            {
                timeline = row.Envelope.ArrivalTimestamp;
            }

            if (timeline - firstArrival.Value >= position)
            {
                return row.Envelope.Sequence;
            }
        }

        return long.MaxValue;
    }

    internal ObdRecordedInput? ReadNext()
    {
        if (_reader.ReadLine() is not { } line)
        {
            return null;
        }

        string[] columns = line.Split(';');
        if (columns.Length != 6
            || !long.TryParse(columns[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long sequence)
            || sequence <= _lastSequence
            || !int.TryParse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int kind)
            || !Enum.IsDefined((SensorDataKind)kind)
            || !long.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out long source)
            || !long.TryParse(columns[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out long arrival))
        {
            throw new InvalidDataException("Invalid or unordered OBD companion row.");
        }

        if (columns[4].Length > 0)
        {
            _state = JsonSerializer.Deserialize<ObdFusionState>(Convert.FromBase64String(columns[4]),
                ObdRecordingCompanion.Json) ?? throw new InvalidDataException("Missing OBD companion state.");
        }

        SensorEnvelope input = JsonSerializer.Deserialize<SensorEnvelope>(Convert.FromBase64String(columns[5]),
            ObdRecordingCompanion.Json) ?? throw new InvalidDataException("Missing OBD companion input.");
        if (_state is null || input.Sequence != sequence || (int)input.Kind != kind
            || input.SourceTimestamp.UtcTicks != source || input.ArrivalTimestamp.UtcTicks != arrival)
        {
            throw new InvalidDataException("OBD companion input does not match its index.");
        }

        _lastSequence = sequence;

        return new(input, _state);
    }

    internal ObdRecordedInput Resolve(SensorEnvelope target, long minimumSequence,
        IFusionEngine engine, Action<FusionEstimate> warmup, Action<SensorEnvelope>? prepareInput = null)
    {
        while (ReadNext() is { } recorded)
        {
            if (recorded.Input.Sequence >= minimumSequence
                && recorded.Input.SourceTimestamp == target.SourceTimestamp && recorded.Input.Kind == target.Kind)
            {
                return recorded;
            }

            prepareInput?.Invoke(recorded.Input);
            recorded.State.Apply(engine);
            if (engine.Process(recorded.Input) is { } estimate)
            {
                warmup(estimate);
            }
        }

        throw new InvalidDataException("Replay input is missing from its OBD companion.");
    }

    public void Dispose() => _reader.Dispose();
}
