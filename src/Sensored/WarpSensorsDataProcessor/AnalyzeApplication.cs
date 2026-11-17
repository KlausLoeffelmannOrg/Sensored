using System.Text.Json;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Runs the <c>analyze</c> subcommand.
/// </summary>
internal static class AnalyzeApplication
{
    internal const string Usage =
        """
        WARP recorded sensor-data analyzer

        Usage:
          WarpSensorsDataProcessor analyze --recording-folder <folder> --output <json-path>

        Writes a new invariant JSON report. The output path must not already exist
        and must not replace an input recording part. Source files are not modified.
        """;

    /// <summary>
    ///  Runs one analyze invocation.
    /// </summary>
    internal static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!AnalyzeOptions.TryParse(args, out AnalyzeOptions? options, out string? parseError))
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync("Use analyze --help for usage.");

            return 2;
        }

        if (options!.ShowHelp)
        {
            await output.WriteLineAsync(Usage);

            return 0;
        }

        try
        {
            AnalyzeReport report = await AnalyzeRecordingAsync(options, cancellationToken);
            await WriteReportAsync(options, report, cancellationToken);
            await WriteSummaryAsync(output, report, options.OutputPath);

            return 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await error.WriteLineAsync("Processing canceled.");

            return 1;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"Processing failed: {exception.Message}");

            return 1;
        }
    }

    /// <summary>
    ///  Streams one recording into an analysis report.
    /// </summary>
    internal static async Task<AnalyzeReport> AnalyzeRecordingAsync(
        AnalyzeOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

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

        RecordingAnalyzer analyzer = new(options.RecordingFolder);

        foreach (string partPath in partPaths)
        {
            await analyzer.ObservePartAsync(partPath, cancellationToken);
        }

        return analyzer.Complete();
    }

    private static async Task WriteReportAsync(
        AnalyzeOptions options,
        AnalyzeReport report,
        CancellationToken cancellationToken)
    {
        string outputPath = Path.GetFullPath(options.OutputPath);

        if (File.Exists(outputPath) || Directory.Exists(outputPath))
        {
            throw new IOException($"Output path '{outputPath}' already exists.");
        }

        string[] partPaths = RecordingParts.ListSemicolonCsvFiles(options.RecordingFolder);

        if (partPaths.Any(
                path => string.Equals(
                    Path.GetFullPath(path),
                    outputPath,
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new IOException("The output file cannot replace an input recording part.");
        }

        string? parentDirectory = Path.GetDirectoryName(outputPath);

        if (parentDirectory is null)
        {
            throw new InvalidOperationException("The output file has no parent directory.");
        }

        Directory.CreateDirectory(parentDirectory);
        string temporaryPath = outputPath + $".{Guid.NewGuid():N}.writing";

        try
        {
            await using (FileStream stream = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.Read,
                65536,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    report,
                    ProcessorJson.Options,
                    cancellationToken);
            }

            File.Move(temporaryPath, outputPath);
        }
        catch
        {
            File.Delete(temporaryPath);
            throw;
        }
    }

    private static async Task WriteSummaryAsync(
        TextWriter output,
        AnalyzeReport report,
        string outputPath)
    {
        await output.WriteLineAsync($"Parts: {report.Parts.Count}");
        await output.WriteLineAsync(
            $"Rows: {report.Counts.Rows:N0} (IMU {report.Counts.Imu:N0}, GPS {report.Counts.Geolocation:N0}, Classification {report.Counts.Classification:N0})");
        await output.WriteLineAsync(
            $"Duration: {FormatOptional(report.Timing.DurationSeconds)} s");
        await output.WriteLineAsync(
            $"IMU rate: {FormatOptional(report.Timing.Imu.RateHz)} Hz (median positive interval {FormatOptional(report.Timing.Imu.MedianPositiveIntervalSeconds)} s)");
        await output.WriteLineAsync(
            $"GPS rate: {FormatOptional(report.Timing.Geolocation.RateHz)} Hz");
        await output.WriteLineAsync(
            $"Gyro available: {report.Availability.ImuGyroAvailable:N0}; compass finite: {report.Availability.ImuCompassFinite:N0}");
        await output.WriteLineAsync(
            $"Future GPS: {report.FutureGps.Count:N0} (max lead {FormatOptional(report.FutureGps.MaxLeadSeconds)} s)");
        await output.WriteLineAsync(
            $"First speed > 2 m/s: {FormatOptional(report.Motion.FirstSpeedAbove2MpsSeconds)} s");
        await output.WriteLineAsync(
            $"Max GPS gap: {FormatOptional(report.Excitation.MaxGpsGapSeconds)} s");
        await output.WriteLineAsync(
            $"Speed-change intervals > 0.5 m/s: {report.Excitation.GpsSpeedChangeAboveHalfMps:N0}; turn intervals: {report.Excitation.TurnIntervalsAtSpeed:N0}");
        await output.WriteLineAsync($"Output: {outputPath}");
    }

    private static string FormatOptional(double? value)
        => value is { } number
            ? number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            : "n/a";
}
