using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Exports a deterministic golden fusion trace from a recording folder.
/// </summary>
internal static class FusionReferenceExporter
{
    internal const string TraceFileName = "golden.trace.csv";
    internal const string ManifestFileName = "manifest.json";
    internal const string WithheldFileName = "withheld-gps.csv";
    internal const string TransformedDirectoryName = "transformed";

    /// <summary>
    ///  Exports one recording through the supplied engine.
    /// </summary>
    internal static async Task<FusionReferenceResult> ExportAsync(
        FusionReferenceOptions options,
        IFusionEngine engine,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(engine);

        if (!Directory.Exists(options.RecordingFolder))
        {
            throw new DirectoryNotFoundException(
                $"Recording folder '{options.RecordingFolder}' does not exist.");
        }

        string[] partPaths = RecordingParts.ListSemicolonCsvFiles(options.RecordingFolder);

        if (partPaths.Length == 0)
        {
            throw new InvalidOperationException(
                "The recording folder contains no finalized semicolon-CSV parts.");
        }

        string outputDirectory = Path.GetFullPath(options.OutputDirectory);

        if (Directory.Exists(outputDirectory) || File.Exists(outputDirectory))
        {
            throw new IOException($"Output path '{outputDirectory}' already exists.");
        }

        string? parentDirectory = Path.GetDirectoryName(outputDirectory);

        if (parentDirectory is null)
        {
            throw new InvalidOperationException("The output directory has no parent directory.");
        }

        FusionScenario? scenario = options.ScenarioPath is { } scenarioPath
            ? FusionScenario.Load(scenarioPath)
            : null;
        List<FusionReferenceInputPart> inputParts = HashParts(partPaths);
        string inputSha256 = RecordingParts.ComputeCombinedSha256(
            inputParts.Select(static part => $"{part.FileName}:{part.Sha256}"));
        string? scenarioSha256 = options.ScenarioPath is { } path
            ? RecordingParts.ComputeSha256(path)
            : null;

        Directory.CreateDirectory(parentDirectory);
        string temporaryDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(outputDirectory)}.{Guid.NewGuid():N}.writing");
        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            FusionReferenceResult result = await RunExportAsync(
                options,
                engine,
                scenario,
                inputParts,
                inputSha256,
                scenarioSha256,
                temporaryDirectory,
                cancellationToken);
            Directory.Move(temporaryDirectory, outputDirectory);

            return result with { OutputPath = outputDirectory };
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            await WriteIncompleteAndMoveAsync(
                temporaryDirectory,
                outputDirectory,
                options,
                engine,
                scenario,
                inputParts,
                inputSha256,
                scenarioSha256,
                "canceled",
                exception);
            throw;
        }
        catch (Exception exception)
        {
            await WriteIncompleteAndMoveAsync(
                temporaryDirectory,
                outputDirectory,
                options,
                engine,
                scenario,
                inputParts,
                inputSha256,
                scenarioSha256,
                $"failed: {exception.Message}",
                exception);
            throw;
        }
    }

    private static async Task<FusionReferenceResult> RunExportAsync(
        FusionReferenceOptions options,
        IFusionEngine engine,
        FusionScenario? scenario,
        IReadOnlyList<FusionReferenceInputPart> inputParts,
        string inputSha256,
        string? scenarioSha256,
        string temporaryDirectory,
        CancellationToken cancellationToken)
    {
        engine.Reset();
        string tracePath = Path.Combine(temporaryDirectory, TraceFileName);
        string withheldPath = Path.Combine(temporaryDirectory, WithheldFileName);
        string? transformedPath = scenario is { ExportTransformedCopy: true }
            ? Path.Combine(temporaryDirectory, TransformedDirectoryName, "part-0000--merged.csv")
            : null;

        if (transformedPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(transformedPath)!);
        }

        ExportCounters counters = new();
        List<string> warnings = [];
        StreamWriter? withheldWriter = null;
        SemicolonCsvRecordingWriter? transformedWriter = null;

        try
        {
            await using FusionTraceWriter writer = new(tracePath);

            if (transformedPath is not null)
            {
                transformedWriter = new SemicolonCsvRecordingWriter(transformedPath);
                await transformedWriter.WriteHeaderAsync(cancellationToken);
            }

            await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadFolderAsync(
                options.RecordingFolder,
                SensorRecordingFormats.SemicolonCsv,
                cancellationToken))
            {
                await ProcessRowAsync(
                    row,
                    engine,
                    writer,
                    scenario,
                    counters,
                    () => withheldWriter ??= CreateWithheldWriter(withheldPath),
                    transformedWriter,
                    cancellationToken);
            }

            if (options.ScenarioPath is { } scenarioPath)
            {
                File.Copy(
                    scenarioPath,
                    Path.Combine(temporaryDirectory, "scenario.json"),
                    overwrite: false);
            }
        }
        finally
        {
            if (withheldWriter is not null)
            {
                await withheldWriter.DisposeAsync();
            }

            if (transformedWriter is not null)
            {
                await transformedWriter.DisposeAsync();
            }
        }

        if (counters.GpsWithheld == 0 && File.Exists(withheldPath))
        {
            File.Delete(withheldPath);
        }

        if (counters.ImuRows == 0)
        {
            throw new InvalidDataException("The recording contains no IMU rows.");
        }

        if (counters.OutputRows != counters.ImuRows)
        {
            throw new InvalidDataException(
                $"Output rows ({counters.OutputRows}) do not match IMU rows ({counters.ImuRows}).");
        }

        string traceSha256 = RecordingParts.ComputeSha256(tracePath);
        FusionReferenceManifest manifest = BuildManifest(
            ArtifactCompletion.Complete,
            completionReason: null,
            options,
            engine,
            scenario,
            inputParts,
            inputSha256,
            scenarioSha256,
            traceSha256,
            counters,
            warnings);
        await WriteManifestAsync(
            Path.Combine(temporaryDirectory, ManifestFileName),
            manifest,
            cancellationToken);

        return new FusionReferenceResult(
            ArtifactCompletion.Complete,
            temporaryDirectory,
            manifest);
    }

    private static async Task ProcessRowAsync(
        SensorEnvelopeRow row,
        IFusionEngine engine,
        FusionTraceWriter writer,
        FusionScenario? scenario,
        ExportCounters counters,
        Func<StreamWriter> withheldWriterFactory,
        SemicolonCsvRecordingWriter? transformedWriter,
        CancellationToken cancellationToken)
    {
        counters.Origin ??= row.Envelope.SourceTimestamp;
        DateTimeOffset origin = counters.Origin.Value;
        counters.InputRows++;
        SensorEnvelope envelope = row.Envelope;

        if (envelope.Kind == SensorDataKind.Imu)
        {
            counters.ImuRows++;
        }
        else if (envelope.Kind == SensorDataKind.Geolocation)
        {
            counters.GpsRows++;
        }

        if (scenario is not null && scenario.ShouldWithholdGps(envelope, origin))
        {
            counters.GpsWithheld++;
            await WriteWithheldAsync(withheldWriterFactory(), row, cancellationToken);

            return;
        }

        SensorEnvelope processed = scenario is null
            ? envelope
            : scenario.Transform(envelope, origin);

        if (envelope.Kind == SensorDataKind.Imu && processed.Imu is null)
        {
            throw new InvalidDataException(
                $"IMU row {row.Row} is missing an IMU payload.");
        }

        if (envelope.Kind == SensorDataKind.Geolocation)
        {
            counters.GpsFedToEngine++;
        }

        FusionEstimate? estimate = engine.Process(processed);

        if (envelope.Kind == SensorDataKind.Imu)
        {
            if (estimate is null)
            {
                throw new InvalidDataException(
                    $"IMU row {row.Row} did not produce a fusion estimate.");
            }
        }
        else if (estimate is not null)
        {
            throw new InvalidDataException(
                $"{envelope.Kind} row {row.Row} produced a fusion estimate.");
        }

        await writer.WriteAsync(processed, estimate, row.Row, cancellationToken);

        if (transformedWriter is not null)
        {
            await transformedWriter.WriteRowAsync(
                row with { Envelope = processed },
                cancellationToken);
        }

        if (estimate is null)
        {
            return;
        }

        counters.ObserveEstimate(estimate, origin);
    }

    private static StreamWriter CreateWithheldWriter(string path)
    {
        StreamWriter writer = new(
            new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                65536,
                FileOptions.Asynchronous | FileOptions.SequentialScan),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            NewLine = "\n"
        };
        writer.WriteLine("row;seq;t_source;lat;lon;speed_mps;course_deg");

        return writer;
    }

    private static async Task WriteWithheldAsync(
        StreamWriter writer,
        SensorEnvelopeRow row,
        CancellationToken cancellationToken)
    {
        GeolocationPayload? geolocation = row.Envelope.Geolocation;
        string line = string.Join(
            ';',
            row.Row.ToString(CultureInfo.InvariantCulture),
            row.Envelope.Sequence.ToString(CultureInfo.InvariantCulture),
            FusionTraceFormatting.FormatTimestamp(row.Envelope.SourceTimestamp),
            FormatOptionalNumber(geolocation?.LatitudeDegrees),
            FormatOptionalNumber(geolocation?.LongitudeDegrees),
            FormatOptionalNumber(geolocation?.SpeedMetersPerSecond),
            FormatOptionalNumber(geolocation?.CourseDegrees));
        await writer.WriteLineAsync(line.AsMemory(), cancellationToken);
    }

    private static string FormatOptionalNumber(double? value)
        => value is { } number
            ? number.ToString("R", CultureInfo.InvariantCulture)
            : string.Empty;

    private static object FormatEngineOptions(IFusionEngine engine)
        => engine is IFusionConfigurationSource configured
            ? configured.Configuration
            : "default";

    /// <summary>
    ///  Identifies the engine binary that produced a trace.
    /// </summary>
    /// <param name="Version">The assembly version.</param>
    /// <param name="FileVersion">The Win32 file version, when present.</param>
    /// <param name="Path">The loaded assembly path.</param>
    /// <param name="LastWriteUtc">The assembly file last-write time in UTC.</param>
    /// <param name="Sha256">The SHA-256 of the assembly file.</param>
    private sealed record EngineAssemblyIdentity(
        string Version,
        string? FileVersion,
        string? Path,
        string? LastWriteUtc,
        string? Sha256);

    private static EngineAssemblyIdentity GetEngineAssemblyIdentity(IFusionEngine engine)
    {
        System.Reflection.Assembly assembly = engine.GetType().Assembly;
        string version = assembly.GetName().Version?.ToString() ?? "unknown";
        string location = assembly.Location;

        if (string.IsNullOrWhiteSpace(location))
        {
            return new EngineAssemblyIdentity(version, null, null, null, null);
        }

        string? fileVersion = FileVersionInfo.GetVersionInfo(location).FileVersion;
        string lastWriteUtc = File.GetLastWriteTimeUtc(location)
            .ToString("o", CultureInfo.InvariantCulture);

        return new EngineAssemblyIdentity(
            version,
            fileVersion,
            location,
            lastWriteUtc,
            RecordingParts.ComputeSha256(location));
    }

    private static List<FusionReferenceInputPart> HashParts(IEnumerable<string> partPaths)
    {
        List<FusionReferenceInputPart> parts = [];

        foreach (string path in partPaths)
        {
            parts.Add(
                new FusionReferenceInputPart
                {
                    FileName = Path.GetFileName(path),
                    Sha256 = RecordingParts.ComputeSha256(path),
                    Length = new FileInfo(path).Length
                });
        }

        return parts;
    }

    private static FusionReferenceManifest BuildManifest(
        ArtifactCompletion completion,
        string? completionReason,
        FusionReferenceOptions options,
        IFusionEngine engine,
        FusionScenario? scenario,
        IReadOnlyList<FusionReferenceInputPart> inputParts,
        string inputSha256,
        string? scenarioSha256,
        string? traceSha256,
        ExportCounters counters,
        IReadOnlyList<string> warnings)
    {
        EngineAssemblyIdentity identity = GetEngineAssemblyIdentity(engine);

        return new()
        {
            Completion = completion,
            CompletionReason = completionReason,
            TraceSha256 = traceSha256,
            RecordingFolder = options.RecordingFolder,
            InputParts = inputParts,
            InputSha256 = inputSha256,
            ScenarioName = scenario?.Name,
            ScenarioPath = options.ScenarioPath,
            ScenarioSha256 = scenarioSha256,
            EngineType = engine.GetType().FullName ?? engine.GetType().Name,
            EngineAssemblyVersion = identity.Version,
            EngineAssemblySha256 = identity.Sha256,
            EngineAssemblyPath = identity.Path,
            EngineAssemblyFileVersion = identity.FileVersion,
            EngineAssemblyLastWriteUtc = identity.LastWriteUtc,
            EngineOptions = FormatEngineOptions(engine),
            Runtime = new FusionReferenceRuntime
            {
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture)
            },
            Counts = new FusionReferenceCounts
            {
                InputRows = counters.InputRows,
                ImuRows = counters.ImuRows,
                GpsRows = counters.GpsRows,
                GpsFedToEngine = counters.GpsFedToEngine,
                GpsWithheld = counters.GpsWithheld,
                OutputRows = counters.OutputRows,
                RejectedRows = counters.RejectedRows,
                RejectionRate = counters.OutputRows > 0
                    ? counters.RejectedRows / (double)counters.OutputRows
                    : null
            },
            Calibration = new FusionReferenceCalibration
            {
                FinalState = counters.FinalCalibration,
                FirstCalibratedSeconds = counters.FirstCalibratedSeconds,
                FirstCalibratedOutputSequence = counters.FirstCalibratedSequence,
                UncalibratedRows = counters.UncalibratedRows,
                ConvergingRows = counters.ConvergingRows,
                CalibratedRows = counters.CalibratedRows,
                CalibratedCoverage = counters.OutputRows > 0
                    ? counters.CalibratedRows / (double)counters.OutputRows
                    : null,
                CalibratedDurationSeconds = counters.CalibratedDurationSeconds
            },
            Cadence = new FusionReferenceCadence
            {
                OutputHz = counters.OutputHz,
                MinIntervalSeconds = counters.MinIntervalSeconds,
                MaxIntervalSeconds = counters.MaxIntervalSeconds,
                MeanIntervalSeconds = counters.MeanIntervalSeconds
            },
            Confidence = new FusionReferenceConfidence
            {
                Min = counters.ConfidenceMin,
                Max = counters.ConfidenceMax,
                Mean = counters.ConfidenceMean
            },
            Validity = new FusionReferenceValidity
            {
                Attitude = counters.AttitudeValidRows,
                Heading = counters.HeadingValidRows,
                Acceleration = counters.AccelerationValidRows,
                Navigation = counters.NavigationValidRows
            },
            Ranges = new FusionReferenceOutputRanges
            {
                PitchDegrees = counters.Pitch.ToRange(),
                RollDegrees = counters.Roll.ToRange(),
                HeadingDegrees = counters.Heading.ToRange(),
                LongitudinalAccelMps2 = counters.LongitudinalAccel.ToRange(),
                LateralAccelMps2 = counters.LateralAccel.ToRange(),
                VerticalAccelMps2 = counters.VerticalAccel.ToRange(),
                SpeedMps = counters.Speed.ToRange()
            },
            Warnings = warnings
        };
    }

    private static async Task WriteManifestAsync(
        string path,
        FusionReferenceManifest manifest,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            65536,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await JsonSerializer.SerializeAsync(stream, manifest, ProcessorJson.Options, cancellationToken);
    }

    private static async Task WriteIncompleteAndMoveAsync(
        string temporaryDirectory,
        string outputDirectory,
        FusionReferenceOptions options,
        IFusionEngine engine,
        FusionScenario? scenario,
        IReadOnlyList<FusionReferenceInputPart> inputParts,
        string inputSha256,
        string? scenarioSha256,
        string reason,
        Exception _)
    {
        if (!Directory.Exists(temporaryDirectory))
        {
            return;
        }

        try
        {
            string? traceSha256 = File.Exists(Path.Combine(temporaryDirectory, TraceFileName))
                ? RecordingParts.ComputeSha256(Path.Combine(temporaryDirectory, TraceFileName))
                : null;
            FusionReferenceManifest manifest = BuildManifest(
                ArtifactCompletion.Incomplete,
                reason,
                options,
                engine,
                scenario,
                inputParts,
                inputSha256,
                scenarioSha256,
                traceSha256,
                new ExportCounters(),
                [$"Export marked incomplete: {reason}"]);
            await WriteManifestAsync(
                Path.Combine(temporaryDirectory, ManifestFileName),
                manifest,
                CancellationToken.None);
        }
        catch (Exception)
        {
            // Best-effort incomplete marker; the original exception is rethrown by the caller.
        }

        string incompleteDirectory = outputDirectory + ".incomplete";

        try
        {
            if (Directory.Exists(incompleteDirectory))
            {
                Directory.Delete(incompleteDirectory, recursive: true);
            }

            if (File.Exists(incompleteDirectory))
            {
                File.Delete(incompleteDirectory);
            }

            Directory.Move(temporaryDirectory, incompleteDirectory);
        }
        catch (Exception)
        {
            try
            {
                Directory.Delete(temporaryDirectory, recursive: true);
            }
            catch (Exception)
            {
                // Ignore cleanup failures after a failed export.
            }
        }
    }

    /// <summary>
    ///  Streaming counters for one export.
    /// </summary>
    private sealed class ExportCounters
    {
        internal DateTimeOffset? Origin;
        internal long InputRows;
        internal long ImuRows;
        internal long GpsRows;
        internal long GpsFedToEngine;
        internal long GpsWithheld;
        internal long OutputRows;
        internal long RejectedRows;
        internal long CalibratedRows;
        internal long ConvergingRows;
        internal long UncalibratedRows;
        internal double CalibratedDurationSeconds;
        internal bool PreviousWasCalibrated;
        internal long AttitudeValidRows;
        internal long HeadingValidRows;
        internal long AccelerationValidRows;
        internal long NavigationValidRows;
        internal RangeAccumulator Pitch;
        internal RangeAccumulator Roll;
        internal RangeAccumulator Heading;
        internal RangeAccumulator LongitudinalAccel;
        internal RangeAccumulator LateralAccel;
        internal RangeAccumulator VerticalAccel;
        internal RangeAccumulator Speed;
        internal long IntervalCount;
        internal double IntervalSum;
        internal DateTimeOffset? PreviousTimestamp;
        internal DateTimeOffset? FirstTimestamp;
        internal DateTimeOffset? LastTimestamp;
        internal double? MinIntervalSeconds;
        internal double? MaxIntervalSeconds;
        internal double? FirstCalibratedSeconds;
        internal long? FirstCalibratedSequence;
        internal FusionCalibrationState FinalCalibration;
        internal double? ConfidenceMin;
        internal double? ConfidenceMax;
        internal double ConfidenceSum;

        internal double? MeanIntervalSeconds
            => IntervalCount > 0 ? IntervalSum / IntervalCount : null;

        internal double? ConfidenceMean
            => OutputRows > 0 ? ConfidenceSum / OutputRows : null;

        internal double? OutputHz
            => FirstTimestamp is { } first
                && LastTimestamp is { } last
                && OutputRows > 1
                && last > first
                    ? (OutputRows - 1) / (last - first).TotalSeconds
                    : null;

        internal void ObserveEstimate(FusionEstimate estimate, DateTimeOffset origin)
        {
            OutputRows++;
            FinalCalibration = estimate.CalibrationState;

            if (estimate.RejectedSample)
            {
                RejectedRows++;
            }

            switch (estimate.CalibrationState)
            {
                case FusionCalibrationState.Calibrated:
                    CalibratedRows++;
                    FirstCalibratedSeconds ??= (estimate.Timestamp - origin).TotalSeconds;
                    FirstCalibratedSequence ??= estimate.Sequence;
                    break;
                case FusionCalibrationState.Converging:
                    ConvergingRows++;
                    break;
                default:
                    UncalibratedRows++;
                    break;
            }

            FirstTimestamp ??= estimate.Timestamp;
            LastTimestamp = estimate.Timestamp;

            if (PreviousTimestamp is { } previous)
            {
                double interval = (estimate.Timestamp - previous).TotalSeconds;
                IntervalCount++;
                IntervalSum += interval;
                MinIntervalSeconds = MinIntervalSeconds is { } min
                    ? Math.Min(min, interval)
                    : interval;
                MaxIntervalSeconds = MaxIntervalSeconds is { } max
                    ? Math.Max(max, interval)
                    : interval;

                if (PreviousWasCalibrated)
                {
                    CalibratedDurationSeconds += Math.Max(0d, interval);
                }
            }

            PreviousTimestamp = estimate.Timestamp;
            PreviousWasCalibrated = estimate.CalibrationState == FusionCalibrationState.Calibrated;
            ConfidenceSum += estimate.Confidence;
            ConfidenceMin = ConfidenceMin is { } cMin
                ? Math.Min(cMin, estimate.Confidence)
                : estimate.Confidence;
            ConfidenceMax = ConfidenceMax is { } cMax
                ? Math.Max(cMax, estimate.Confidence)
                : estimate.Confidence;

            if (estimate.Validity.HasFlag(FusionOutputValidity.Attitude))
            {
                AttitudeValidRows++;
            }

            if (estimate.Validity.HasFlag(FusionOutputValidity.Heading))
            {
                HeadingValidRows++;
            }

            if (estimate.Validity.HasFlag(FusionOutputValidity.Acceleration))
            {
                AccelerationValidRows++;
            }

            if (estimate.Validity.HasFlag(FusionOutputValidity.Navigation))
            {
                NavigationValidRows++;
            }

            Pitch.Add(estimate.Pitch);
            Roll.Add(estimate.Roll);
            Heading.Add(estimate.Heading);
            LongitudinalAccel.Add(estimate.LongitudinalAccel);
            LateralAccel.Add(estimate.LateralAccel);
            VerticalAccel.Add(estimate.VerticalAccel);
            Speed.Add(estimate.Speed);
        }
    }

    /// <summary>
    ///  Finite min/max accumulator for one output field.
    /// </summary>
    private struct RangeAccumulator
    {
        private long _finite;
        private long _nonFinite;
        private double _min;
        private double _max;
        private bool _hasFinite;

        internal void Add(double value)
        {
            if (!double.IsFinite(value))
            {
                _nonFinite++;

                return;
            }

            _finite++;

            if (!_hasFinite)
            {
                _min = value;
                _max = value;
                _hasFinite = true;

                return;
            }

            if (value < _min)
            {
                _min = value;
            }

            if (value > _max)
            {
                _max = value;
            }
        }

        internal FusionReferenceRange ToRange()
            => new()
            {
                Min = _hasFinite ? _min : null,
                Max = _hasFinite ? _max : null,
                FiniteCount = _finite,
                NonFiniteCount = _nonFinite
            };
    }
}
