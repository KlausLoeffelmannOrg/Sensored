using System.Globalization;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Describes one invocation of the recorded sensor-data processor.
/// </summary>
/// <param name="RecordingFolder">The WARP recording folder.</param>
/// <param name="OutputPath">The output file or directory.</param>
/// <param name="DampingMilliseconds">The rounded damping time constant, or <see langword="null"/>.</param>
/// <param name="MergeParts">Whether all input parts are written to one output file.</param>
/// <param name="TrimStationaryEnd">Whether an excessive final stationary GPS run is trimmed.</param>
/// <param name="ShowHelp">Whether usage information was requested.</param>
internal sealed record CommandLineOptions(
    string RecordingFolder,
    string OutputPath,
    int? DampingMilliseconds,
    bool MergeParts,
    bool TrimStationaryEnd,
    bool ShowHelp)
{
    internal const int MinimumDampingMilliseconds = 100;
    internal const int MaximumDampingMilliseconds = 2000;
    internal const int DampingIncrementMilliseconds = 50;

    /// <summary>
    ///  Parses command line arguments.
    /// </summary>
    /// <param name="args">The arguments to parse.</param>
    /// <param name="options">The parsed options.</param>
    /// <param name="error">The validation error.</param>
    internal static bool TryParse(
        IReadOnlyList<string> args,
        out CommandLineOptions? options,
        out string? error)
    {
        string? recordingFolder = null;
        string? outputPath = null;
        int? dampingMilliseconds = null;
        bool mergeParts = false;
        bool trimStationaryEnd = false;
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
                    if (!TryReadValue(args, ref index, argument, out recordingFolder, out error))
                    {
                        options = null;
                        return false;
                    }

                    break;

                case "--output":
                    if (!TryReadValue(args, ref index, argument, out outputPath, out error))
                    {
                        options = null;
                        return false;
                    }

                    break;

                case "--damping-ms":
                    if (!TryReadValue(args, ref index, argument, out string? dampingText, out error))
                    {
                        options = null;
                        return false;
                    }

                    if (!double.TryParse(
                            dampingText,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out double requestedDamping)
                        || !double.IsFinite(requestedDamping)
                        || requestedDamping is < MinimumDampingMilliseconds
                            or > MaximumDampingMilliseconds)
                    {
                        options = null;
                        error =
                            $"--damping-ms must be between {MinimumDampingMilliseconds} and "
                            + $"{MaximumDampingMilliseconds}.";

                        return false;
                    }

                    dampingMilliseconds = (int)(
                        Math.Ceiling(requestedDamping / DampingIncrementMilliseconds)
                        * DampingIncrementMilliseconds);
                    break;

                case "--merge":
                    mergeParts = true;
                    break;

                case "--trim-stationary-end":
                    trimStationaryEnd = true;
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
            options = new CommandLineOptions(
                string.Empty,
                string.Empty,
                null,
                false,
                false,
                true);
            error = null;

            return true;
        }

        if (string.IsNullOrWhiteSpace(recordingFolder))
        {
            options = null;
            error = "--recording-folder is required.";

            return false;
        }

        if (dampingMilliseconds is null && !mergeParts && !trimStationaryEnd)
        {
            options = null;
            error =
                "Specify at least one processing option: --damping-ms, --merge, "
                + "or --trim-stationary-end.";

            return false;
        }

        string fullRecordingFolder = Path.GetFullPath(recordingFolder);
        string resolvedOutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(
                fullRecordingFolder,
                mergeParts ? "processed.csv" : "processed")
            : Path.GetFullPath(outputPath);
        options = new CommandLineOptions(
            fullRecordingFolder,
            resolvedOutputPath,
            dampingMilliseconds,
            mergeParts,
            trimStationaryEnd,
            false);
        error = null;

        return true;
    }

    private static bool TryReadValue(
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
