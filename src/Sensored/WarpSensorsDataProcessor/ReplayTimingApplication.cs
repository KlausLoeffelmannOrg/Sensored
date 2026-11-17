using System.Diagnostics;
using System.Text.Json;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace WarpSensorsDataProcessor;

/// <summary>Runs an independently timed, bounded replay through the real sensor service.</summary>
internal static class ReplayTimingApplication
{
    private const string Usage =
        """
        WARP replay timing observer

        Usage:
          WarpSensorsDataProcessor replay-timing --recording-folder <folder> --output <json-path>
            [--speed <multiplier>] [--start-seconds <offset>] [--duration-seconds <window>] [--fusion]

        Defaults: speed 1.0, start 0, duration 60 recorded seconds, transport-only.
        --fusion adds the actual full-rate Golden engine; no GUI or hardware is used.
        Records callback/consumer wall time without modifying source timestamps or recordings.
        Exit 0: complete evidence within 5% of selected speed; exit 1: failed timing/run; exit 2: bad arguments.
        Existing outputs are never overwritten. Fast offline fusion-reference export is a separate command.
        """;

    internal static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!ReplayTimingOptions.TryParse(args, out ReplayTimingOptions? options, out string? parseError))
        {
            await error.WriteLineAsync(parseError);
            return 2;
        }

        if (options!.ShowHelp)
        {
            await output.WriteLineAsync(Usage);
            return 0;
        }

        try
        {
            if (File.Exists(options.OutputPath) || Directory.Exists(options.OutputPath))
            {
                throw new IOException($"Output path '{options.OutputPath}' already exists.");
            }

            ReplayTimingReport report = await MeasureAsync(options, cancellationToken);
            await WriteReportAsync(options.OutputPath, report, cancellationToken);
            await output.WriteLineAsync(
                $"Replay timing: {report.Timing.RecordedSeconds:F3} recorded s / "
                + $"{report.Timing.WallSeconds:F3} wall s = {report.Timing.ObservedRate:F4}x; "
                + $"relative error {report.Timing.RelativeRateError:P2}");
            await output.WriteLineAsync(
                $"Rows: {report.ObservedRows}/{report.ExpectedRows}; consumed {report.ConsumedRows}; "
                + $"late p95/max {report.Timing.Lateness.P95:F4}/{report.Timing.Lateness.Maximum:F4} s");
            await output.WriteLineAsync($"Output: {options.OutputPath}");

            if (!report.Timing.Passed)
            {
                await error.WriteLineAsync("Replay timing failed the 5% selected-rate acceptance gate.");
                return 1;
            }

            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync("Replay timing canceled; no complete timing evidence was published.");
            return 1;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Replay timing failed: {exception.Message}");
            return 1;
        }
    }

    internal static async Task<ReplayTimingReport> MeasureAsync(
        ReplayTimingOptions options,
        CancellationToken cancellationToken)
    {
        string[] parts = RecordingParts.ListSemicolonCsvFiles(options.RecordingFolder);
        IReadOnlyList<FusionReferenceInputPart> provenance =
        [
            .. parts.Select(path => new FusionReferenceInputPart
            {
                FileName = Path.GetFileName(path),
                Length = new FileInfo(path).Length,
                Sha256 = RecordingParts.ComputeSha256(path)
            })
        ];
        IReadOnlyList<ReplayExpectedRow> expected = await ReadWindowAsync(options, cancellationToken);
        ReplayTimingObserver observer = new(expected);
        SensorReplaySourceFactory replayFactory = new(
            options.RecordingFolder, SensorRecordingFormats.SemicolonCsv, true,
            TimeSpan.FromSeconds(options.StartSeconds), options.Speed);
        DelegateSensorSourceFactory sourceFactory = new(replayFactory.ConfigurationKey, serviceOptions =>
        {
            SensorSourceSet sources = replayFactory.CreateSources(serviceOptions);
            observer.AttachBeforePipeline(replayFactory.CurrentPlayer!);
            return sources;
        });
        await using SensorService service = new(sourceFactory);
        long started = Stopwatch.GetTimestamp();
        await using ISensorServiceSessionLease lease = await service.AcquireSessionAsync(
            SensorServiceOptions.Default with
            {
                Classification = SensorClassificationOptions.Default with { Enabled = false }
            },
            cancellationToken);
        SensorReplayPlayer player = replayFactory.CurrentPlayer!;
        observer.AttachAfterPipeline(player);
        await using ISensorSubscription subscription = service.Subscribe(new SensorSubscriptionOptions
        {
            Name = "Independent replay timing",
            Kinds = SensorDataKindFilter.Imu | SensorDataKindFilter.Geolocation | SensorDataKindFilter.Gap,
            Capacity = 8192
        });
        IFusionEngine? engine = options.Fusion ? FusionEngineFactory.CreateDefault() : null;
        await using SensorFusionSession? fusion = engine is null
            ? null
            : new SensorFusionSession(new ReplayTimingObserver.ObservedEngine(observer, engine));
        Task consumer;

        if (fusion is not null)
        {
            fusion.Attach(subscription);
            consumer = fusion.Completion;
        }
        else
        {
            consumer = ConsumeAsync(subscription, observer, cancellationToken);
        }

        long stopping;

        try
        {
            TimeSpan timeout = TimeSpan.FromSeconds((options.DurationSeconds / options.Speed * 2d) + 30d);
            Task completed = await Task.WhenAny(observer.Completion, player.Completion, consumer)
                .WaitAsync(timeout, cancellationToken);
            await completed;

            if (!observer.Completion.IsCompletedSuccessfully)
            {
                throw new InvalidDataException("Replay ended before the selected timing window completed.");
            }
        }
        finally
        {
            stopping = Stopwatch.GetTimestamp();
            await lease.DisposeAsync();
        }

        await consumer.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        SensorCompletedSessionDiagnostics terminal = lease.CompletedSession
            ?? throw new InvalidDataException("No terminal acquisition diagnostics were available.");
        SensorSubscriptionStatistics statistics = subscription.Statistics;

        if (fusion is not null)
        {
            await fusion.FinishInputAsync(terminal);
            await fusion.CompleteAsync();
        }

        double drainSeconds = Stopwatch.GetElapsedTime(stopping).TotalSeconds;
        bool complete = terminal.Drained && !terminal.Faults.HasFault
            && terminal.Statistics.IngressDropped == 0 && statistics.Dropped == 0 && statistics.Throttled == 0;
        IReadOnlyList<ReplayTimingObservation> observations = observer.CreateObservations();
        ReplayTimingSegment[] segments = [new(0d, 0d, options.Speed)];
        ReplayTimingAssessment assessment = ReplayTimingAssessment.Evaluate(observations, segments, complete);

        for (int index = 0; index < parts.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (RecordingParts.ComputeSha256(parts[index]) != provenance[index].Sha256)
            {
                throw new InvalidDataException("An input recording changed while timing evidence was collected.");
            }
        }

        return new ReplayTimingReport
        {
            Mode = engine is null ? "transport" : "fusion",
            InputParts = provenance,
            InputSha256 = RecordingParts.ComputeCombinedSha256(
                provenance.Select(static part => $"{part.FileName}:{part.Sha256}")),
            Runtime = new FusionReferenceRuntime
            {
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture)
            },
            EngineType = engine?.GetType().FullName,
            EngineAssemblyVersion = engine?.GetType().Assembly.GetName().Version?.ToString(),
            EngineAssemblySha256 = engine is null ? null : RecordingParts.ComputeSha256(engine.GetType().Assembly.Location),
            EngineOptions = (engine as IFusionConfigurationSource)?.Configuration,
            ClockFrequency = Stopwatch.Frequency,
            RequestedStartSeconds = options.StartSeconds,
            RequestedDurationSeconds = options.DurationSeconds,
            StartupSeconds = Stopwatch.GetElapsedTime(started, observer.FirstEmission).TotalSeconds,
            DrainSeconds = drainSeconds,
            TraceWritingEnabled = false,
            ExpectedRows = expected.Count,
            ObservedRows = observer.ObservedCount,
            ConsumedRows = observer.ConsumedCount,
            FusionOutputRows = engine is null ? null : observer.OutputCount,
            IngressDropped = terminal.Statistics.IngressDropped,
            SubscriptionDropped = statistics.Dropped,
            SubscriptionThrottled = statistics.Throttled,
            Complete = complete,
            Timing = assessment,
            ConsumerLatency = ReplayTimingDistribution.From(observations.Select(
                static row => row.ConsumedWallSeconds - row.ObservedWallSeconds)),
            ProcessingDuration = engine is null ? null : ReplayTimingDistribution.From(
                observations.Select(static row => row.ProcessingSeconds)),
            GpsClock = observer.GpsEvidence(),
            Segments = segments,
            Observations = observations
        };
    }

    private static async Task ConsumeAsync(
        ISensorSubscription subscription,
        ReplayTimingObserver observer,
        CancellationToken cancellationToken)
    {
        await foreach (SensorEnvelope envelope in subscription.ReadAllAsync(cancellationToken))
        {
            observer.Process(envelope, engine: null);
        }
    }

    private static async Task<IReadOnlyList<ReplayExpectedRow>> ReadWindowAsync(
        ReplayTimingOptions options,
        CancellationToken cancellationToken)
    {
        List<ReplayExpectedRow> rows = [];
        DateTimeOffset? origin = null;
        DateTimeOffset maximum = default;
        TimeSpan start = TimeSpan.FromSeconds(options.StartSeconds);
        TimeSpan end = start + TimeSpan.FromSeconds(options.DurationSeconds);

        await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadFolderAsync(
            options.RecordingFolder, SensorRecordingFormats.SemicolonCsv, cancellationToken))
        {
            if (!(row.Envelope.Kind == SensorDataKind.Imu && row.Envelope.Imu is not null)
                && !(row.Envelope.Kind == SensorDataKind.Geolocation && row.Envelope.Geolocation is not null))
            {
                continue;
            }

            origin ??= row.Envelope.ArrivalTimestamp;
            maximum = row.Envelope.ArrivalTimestamp > maximum ? row.Envelope.ArrivalTimestamp : maximum;
            TimeSpan position = maximum - origin.Value;

            if (position < start)
            {
                continue;
            }

            rows.Add(new(row, position));

            if (position >= end)
            {
                if (rows.Count < 2 || rows[^1].Position <= rows[0].Position)
                {
                    throw new InvalidDataException("The timing window needs at least two distinct arrival times.");
                }

                return rows;
            }
        }

        throw new InvalidDataException("The recording does not cover the requested timing window.");
    }

    private static async Task WriteReportAsync(
        string outputPath,
        ReplayTimingReport report,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        string temporary = outputPath + $".{Guid.NewGuid():N}.writing";

        try
        {
            await using (FileStream stream = new(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                await JsonSerializer.SerializeAsync(stream, report, ProcessorJson.Options, cancellationToken);
            }

            File.Move(temporary, outputPath);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }
}
