using System.Globalization;

namespace WarpSensorsDataProcessor;

/// <summary>Describes a bounded, independently observed replay timing run.</summary>
internal sealed record ReplayTimingOptions(
    string RecordingFolder,
    string OutputPath,
    double Speed,
    double StartSeconds,
    double DurationSeconds,
    bool Fusion,
    bool ShowHelp)
{
    /// <summary>Parses the replay-timing command without changing source recordings.</summary>
    internal static bool TryParse(
        IReadOnlyList<string> args,
        out ReplayTimingOptions? options,
        out string? error)
    {
        string? folder = null;
        string? output = null;
        double speed = 1d;
        double start = 0d;
        double duration = 60d;
        bool fusion = false;
        bool help = false;
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        options = null;
        error = null;

        for (int index = 0; index < args.Count; index++)
        {
            string argument = args[index];

            if (!seen.Add(argument))
            {
                error = $"Option '{argument}' was specified more than once.";
                return false;
            }

            if (argument is "--help" or "-h" or "/?")
            {
                help = true;
                continue;
            }

            if (argument == "--fusion")
            {
                fusion = true;
                continue;
            }

            if (argument is not ("--recording-folder" or "--output" or "--speed" or "--start-seconds" or "--duration-seconds"))
            {
                error = $"Unknown option '{argument}'.";
                return false;
            }

            if (!CommandArgumentReader.TryReadValue(args, ref index, argument, out string? value, out error))
            {
                return false;
            }

            switch (argument)
            {
                case "--recording-folder":
                    folder = value;
                    break;
                case "--output":
                    output = value;
                    break;
                default:
                    if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)
                        || !double.IsFinite(number)
                        || (argument == "--start-seconds" ? number < 0d : number <= 0d))
                    {
                        error = $"Invalid value for {argument}: '{value}'.";
                        return false;
                    }

                    switch (argument)
                    {
                        case "--speed": speed = number; break;
                        case "--start-seconds": start = number; break;
                        case "--duration-seconds": duration = number; break;
                    }

                    break;
            }
        }

        if (!help && (string.IsNullOrWhiteSpace(folder) || string.IsNullOrWhiteSpace(output)))
        {
            error = "--recording-folder and --output are required.";
            return false;
        }

        double deadlineSeconds = (duration / speed * 2d) + 30d;

        if (!double.IsFinite(start + duration)
            || start + duration > TimeSpan.MaxValue.TotalSeconds
            || !double.IsFinite(deadlineSeconds)
            || deadlineSeconds * 1000d > uint.MaxValue - 1d)
        {
            error = "The requested recording window or playback timeout is too large.";
            return false;
        }

        options = new(
            help ? string.Empty : Path.GetFullPath(folder!),
            help ? string.Empty : Path.GetFullPath(output!),
            speed, start, duration, fusion, help);

        return true;
    }
}
