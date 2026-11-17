using System.Globalization;
using WarpToolkit.Sensors;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Summarizes one completed recording-processing operation.
/// </summary>
/// <param name="InputRows">The number of rows read.</param>
/// <param name="OutputRows">The number of rows written.</param>
/// <param name="TrimmedRows">The number of rows removed by stationary-tail trimming.</param>
/// <param name="OutputPath">The created output file or directory.</param>
internal sealed record RecordingProcessingResult(
    long InputRows,
    long OutputRows,
    long TrimmedRows,
    string OutputPath);

/// <summary>
///  Processes WARP semicolon-CSV recording parts without modifying the source recording.
/// </summary>
internal static class RecordingProcessor
{
    /// <summary>
    ///  Processes one recording folder.
    /// </summary>
    /// <param name="options">The processing options.</param>
    /// <param name="cancellationToken">A token used to cancel processing.</param>
    internal static async Task<RecordingProcessingResult> ProcessAsync(
        CommandLineOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!Directory.Exists(options.RecordingFolder))
        {
            throw new DirectoryNotFoundException(
                $"Recording folder '{options.RecordingFolder}' does not exist.");
        }

        string[] partPaths = GetPartPaths(options.RecordingFolder);

        if (partPaths.Length == 0)
        {
            throw new InvalidOperationException(
                "The recording folder contains no finalized semicolon-CSV parts.");
        }

        DateTimeOffset? cutoffTimestamp = options.TrimStationaryEnd
            ? await FindStationaryTailCutoffAsync(partPaths, cancellationToken)
            : null;

        return options.MergeParts
            ? await WriteMergedAsync(
                options,
                partPaths,
                cutoffTimestamp,
                cancellationToken)
            : await WritePartsAsync(
                options,
                partPaths,
                cutoffTimestamp,
                cancellationToken);
    }

    private static async Task<DateTimeOffset?> FindStationaryTailCutoffAsync(
        IEnumerable<string> partPaths,
        CancellationToken cancellationToken)
    {
        GpsStationaryTailDetector detector = new();

        foreach (string partPath in partPaths)
        {
            await foreach (SensorEnvelopeRow row in SensorRecordingReader
                .ReadPartAsync(partPath, cancellationToken))
            {
                if (row.Envelope.Kind == SensorDataKind.Geolocation
                    && row.Envelope.Geolocation is { } geolocation)
                {
                    detector.Observe(geolocation, row.Envelope.SourceTimestamp);
                }
            }
        }

        return detector.GetCutoffTimestamp();
    }

    private static async Task<RecordingProcessingResult> WriteMergedAsync(
        CommandLineOptions options,
        IEnumerable<string> partPaths,
        DateTimeOffset? cutoffTimestamp,
        CancellationToken cancellationToken)
    {
        string outputPath = Path.GetFullPath(options.OutputPath);
        EnsureOutputFileIsAvailable(outputPath, partPaths);
        Directory.CreateDirectory(
            Path.GetDirectoryName(outputPath)
                ?? throw new InvalidOperationException("The output file has no parent directory."));
        string temporaryPath = outputPath + $".{Guid.NewGuid():N}.writing";

        try
        {
            ProcessingCounts counts;

            await using (SemicolonCsvRecordingWriter writer = new(temporaryPath))
            {
                await writer.WriteHeaderAsync(cancellationToken);
                counts = await WriteRowsAsync(
                    partPaths,
                    cutoffTimestamp,
                    options.DampingMilliseconds,
                    writer,
                    cancellationToken);
            }

            File.Move(temporaryPath, outputPath);

            return new RecordingProcessingResult(
                counts.InputRows,
                counts.OutputRows,
                counts.TrimmedRows,
                outputPath);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    private static async Task<RecordingProcessingResult> WritePartsAsync(
        CommandLineOptions options,
        IReadOnlyList<string> partPaths,
        DateTimeOffset? cutoffTimestamp,
        CancellationToken cancellationToken)
    {
        string outputDirectory = Path.GetFullPath(options.OutputPath);

        if (Directory.Exists(outputDirectory) || File.Exists(outputDirectory))
        {
            throw new IOException($"Output path '{outputDirectory}' already exists.");
        }

        string? parentDirectory = Path.GetDirectoryName(outputDirectory);

        if (parentDirectory is null)
        {
            throw new InvalidOperationException("The output directory has no parent directory.");
        }

        Directory.CreateDirectory(parentDirectory);
        string temporaryDirectory = Path.Combine(
            parentDirectory,
            $".{Path.GetFileName(outputDirectory)}.{Guid.NewGuid():N}.writing");
        Directory.CreateDirectory(temporaryDirectory);
        SensorDataDamper? damper = options.DampingMilliseconds is { } dampingMilliseconds
            ? new SensorDataDamper(dampingMilliseconds)
            : null;
        long inputRows = 0;
        long outputRows = 0;
        long trimmedRows = 0;

        try
        {
            foreach (string partPath in partPaths)
            {
                string outputPartPath = Path.Combine(
                    temporaryDirectory,
                    Path.GetFileName(partPath));
                await using SemicolonCsvRecordingWriter writer = new(outputPartPath);
                await writer.WriteHeaderAsync(cancellationToken);

                await foreach (SensorEnvelopeRow row in SensorRecordingReader
                    .ReadPartAsync(partPath, cancellationToken))
                {
                    inputRows++;

                    if (cutoffTimestamp is { } cutoff
                        && row.Envelope.SourceTimestamp > cutoff)
                    {
                        trimmedRows++;
                        continue;
                    }

                    await writer.WriteRowAsync(
                        damper?.Apply(row) ?? row,
                        cancellationToken);
                    outputRows++;
                }
            }

            Directory.Move(temporaryDirectory, outputDirectory);

            return new RecordingProcessingResult(
                inputRows,
                outputRows,
                trimmedRows,
                outputDirectory);
        }
        catch
        {
            Directory.Delete(temporaryDirectory, recursive: true);
            throw;
        }
    }

    private static async Task<ProcessingCounts> WriteRowsAsync(
        IEnumerable<string> partPaths,
        DateTimeOffset? cutoffTimestamp,
        int? dampingMilliseconds,
        SemicolonCsvRecordingWriter writer,
        CancellationToken cancellationToken)
    {
        SensorDataDamper? damper = dampingMilliseconds is { } value
            ? new SensorDataDamper(value)
            : null;
        long inputRows = 0;
        long outputRows = 0;
        long trimmedRows = 0;

        foreach (string partPath in partPaths)
        {
            await foreach (SensorEnvelopeRow row in SensorRecordingReader
                .ReadPartAsync(partPath, cancellationToken))
            {
                inputRows++;

                if (cutoffTimestamp is { } cutoff
                    && row.Envelope.SourceTimestamp > cutoff)
                {
                    trimmedRows++;
                    continue;
                }

                await writer.WriteRowAsync(damper?.Apply(row) ?? row, cancellationToken);
                outputRows++;
            }
        }

        return new ProcessingCounts(inputRows, outputRows, trimmedRows);
    }

    private static string[] GetPartPaths(string recordingFolder)
        =>
        [
            .. Directory
                .EnumerateFiles(recordingFolder, "part-*.csv", SearchOption.TopDirectoryOnly)
                .Where(static path => !path.Contains("--writing", StringComparison.Ordinal))
                .OrderBy(GetPartIndex)
                .ThenBy(static path => path, StringComparer.Ordinal)
        ];

    private static long GetPartIndex(string path)
    {
        ReadOnlySpan<char> fileName = Path.GetFileName(path);
        const string Prefix = "part-";

        if (!fileName.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return long.MaxValue;
        }

        ReadOnlySpan<char> indexText = fileName[Prefix.Length..];
        int separator = indexText.IndexOf('-');

        if (separator >= 0)
        {
            indexText = indexText[..separator];
        }

        return long.TryParse(
            indexText,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long index)
                ? index
                : long.MaxValue;
    }

    private static void EnsureOutputFileIsAvailable(
        string outputPath,
        IEnumerable<string> partPaths)
    {
        if (File.Exists(outputPath) || Directory.Exists(outputPath))
        {
            throw new IOException($"Output path '{outputPath}' already exists.");
        }

        if (partPaths.Any(
                path => string.Equals(
                    Path.GetFullPath(path),
                    outputPath,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new IOException("The output file cannot replace an input recording part.");
        }
    }

    /// <summary>
    ///  Tracks processing row counts.
    /// </summary>
    private readonly record struct ProcessingCounts(
        long InputRows,
        long OutputRows,
        long TrimmedRows);
}
