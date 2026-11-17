namespace WarpSensorsDataProcessor;

/// <summary>
///  Coordinates command-line parsing, processing, and user-facing diagnostics.
/// </summary>
internal static class ProcessorApplication
{
    private const string Usage =
        """
        WARP recorded sensor-data processor

        Usage:
          WarpSensorsDataProcessor --recording-folder <folder> [operations] [--output <path>]
          WarpSensorsDataProcessor analyze --recording-folder <folder> --output <json-path>
          WarpSensorsDataProcessor fusion-reference --recording-folder <folder> --output <directory> [--scenario <json>]
          WarpSensorsDataProcessor replay-timing --recording-folder <folder> --output <json-path> [--speed <multiplier>] [--fusion]

        Operations:
          --damping-ms <100-2000>   Exponentially dampen IMU and GPS measurements.
                                    Values are rounded up to the next 50 ms.
          --merge                   Write all CSV parts to one output CSV file.
          --trim-stationary-end     Keep only the first minute of a final stationary GPS run.

        Output:
          With --merge, --output names a CSV file (default: <folder>\processed.csv).
          Without --merge, --output names a new directory containing processed parts
          (default: <folder>\processed).
          analyze and fusion-reference require a new output path and never overwrite sources.

        Options:
          --output <path>           Override the output file or directory.
          --help, -h, /?            Show this help.
        """;

    /// <summary>
    ///  Runs one processor invocation.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <param name="output">The standard output writer.</param>
    /// <param name="error">The standard error writer.</param>
    /// <param name="cancellationToken">A token used to cancel processing.</param>
    internal static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (args.Count > 0
            && string.Equals(args[0], "replay-timing", StringComparison.OrdinalIgnoreCase))
        {
            return await ReplayTimingApplication.RunAsync(
                SkipCommand(args), output, error, cancellationToken);
        }

        if (args.Count > 0
            && string.Equals(args[0], "analyze", StringComparison.OrdinalIgnoreCase))
        {
            return await AnalyzeApplication.RunAsync(
                SkipCommand(args),
                output,
                error,
                cancellationToken);
        }

        if (args.Count > 0
            && string.Equals(args[0], "fusion-reference", StringComparison.OrdinalIgnoreCase))
        {
            return await FusionReferenceApplication.RunAsync(
                SkipCommand(args),
                output,
                error,
                cancellationToken);
        }

        if (!CommandLineOptions.TryParse(args, out CommandLineOptions? options, out string? parseError))
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync("Use --help for usage.");

            return 2;
        }

        if (options!.ShowHelp)
        {
            await output.WriteLineAsync(Usage);

            return 0;
        }

        try
        {
            RecordingProcessingResult result = await RecordingProcessor.ProcessAsync(
                options,
                cancellationToken);
            await output.WriteLineAsync($"Input rows: {result.InputRows:N0}");
            await output.WriteLineAsync($"Output rows: {result.OutputRows:N0}");

            if (result.TrimmedRows > 0)
            {
                await output.WriteLineAsync($"Trimmed rows: {result.TrimmedRows:N0}");
            }

            if (options.DampingMilliseconds is { } dampingMilliseconds)
            {
                await output.WriteLineAsync($"Damping: {dampingMilliseconds} ms");
            }

            await output.WriteLineAsync($"Output: {result.OutputPath}");

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

    private static IReadOnlyList<string> SkipCommand(IReadOnlyList<string> args)
    {
        if (args.Count <= 1)
        {
            return [];
        }

        string[] rest = new string[args.Count - 1];

        for (int index = 1; index < args.Count; index++)
        {
            rest[index - 1] = args[index];
        }

        return rest;
    }
}
