namespace WarpSensorsDataProcessor;

/// <summary>
///  Describes one <c>analyze</c> invocation.
/// </summary>
/// <param name="RecordingFolder">The WARP recording folder.</param>
/// <param name="OutputPath">The new JSON report path.</param>
/// <param name="ShowHelp">Whether usage information was requested.</param>
internal sealed record AnalyzeOptions(
    string RecordingFolder,
    string OutputPath,
    bool ShowHelp)
{
    /// <summary>
    ///  Parses analyze command arguments.
    /// </summary>
    internal static bool TryParse(
        IReadOnlyList<string> args,
        out AnalyzeOptions? options,
        out string? error)
    {
        string? recordingFolder = null;
        string? outputPath = null;
        bool showHelp = false;
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < args.Count; index++)
        {
            string argument = args[index];

            if (!seen.Add(argument))
            {
                options = null;
                error = $"Option '{argument}' was specified more than once.";

                return false;
            }

            switch (argument)
            {
                case "--recording-folder":
                    if (!CommandArgumentReader.TryReadValue(args, ref index, argument, out recordingFolder, out error))
                    {
                        options = null;
                        return false;
                    }

                    break;

                case "--output":
                    if (!CommandArgumentReader.TryReadValue(args, ref index, argument, out outputPath, out error))
                    {
                        options = null;
                        return false;
                    }

                    break;

                case "--help":
                case "-h":
                case "/?":
                    showHelp = true;
                    break;

                default:
                    options = null;
                    error = $"Unknown option '{argument}'.";

                    return false;
            }
        }

        if (showHelp)
        {
            options = new AnalyzeOptions(string.Empty, string.Empty, true);
            error = null;

            return true;
        }

        if (string.IsNullOrWhiteSpace(recordingFolder))
        {
            options = null;
            error = "--recording-folder is required.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            options = null;
            error = "--output is required.";

            return false;
        }

        options = new AnalyzeOptions(
            Path.GetFullPath(recordingFolder),
            Path.GetFullPath(outputPath),
            false);
        error = null;

        return true;
    }
}

/// <summary>
///  Shared option-value reader for the new subcommands.
/// </summary>
internal static class CommandArgumentReader
{
    /// <summary>
    ///  Reads the value that follows an option name.
    /// </summary>
    internal static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string option,
        out string? value,
        out string? error)
    {
        if (index + 1 >= args.Count || args[index + 1].StartsWith('-'))
        {
            value = null;
            error = $"Option '{option}' requires a value.";

            return false;
        }

        index++;
        value = args[index];
        error = null;

        return true;
    }
}
