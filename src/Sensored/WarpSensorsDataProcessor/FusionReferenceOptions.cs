namespace WarpSensorsDataProcessor;

/// <summary>
///  Describes one <c>fusion-reference</c> invocation.
/// </summary>
/// <param name="RecordingFolder">The WARP recording folder.</param>
/// <param name="OutputDirectory">The new artifact directory.</param>
/// <param name="ScenarioPath">An optional scenario JSON path.</param>
/// <param name="ShowHelp">Whether usage information was requested.</param>
internal sealed record FusionReferenceOptions(
    string RecordingFolder,
    string OutputDirectory,
    string? ScenarioPath,
    bool ShowHelp)
{
    /// <summary>
    ///  Parses fusion-reference command arguments.
    /// </summary>
    internal static bool TryParse(
        IReadOnlyList<string> args,
        out FusionReferenceOptions? options,
        out string? error)
    {
        string? recordingFolder = null;
        string? outputDirectory = null;
        string? scenarioPath = null;
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
                    if (!CommandArgumentReader.TryReadValue(args, ref index, argument, out outputDirectory, out error))
                    {
                        options = null;
                        return false;
                    }

                    break;

                case "--scenario":
                    if (!CommandArgumentReader.TryReadValue(args, ref index, argument, out scenarioPath, out error))
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
            options = new FusionReferenceOptions(string.Empty, string.Empty, null, true);
            error = null;

            return true;
        }

        if (string.IsNullOrWhiteSpace(recordingFolder))
        {
            options = null;
            error = "--recording-folder is required.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            options = null;
            error = "--output is required.";

            return false;
        }

        options = new FusionReferenceOptions(
            Path.GetFullPath(recordingFolder),
            Path.GetFullPath(outputDirectory),
            string.IsNullOrWhiteSpace(scenarioPath) ? null : Path.GetFullPath(scenarioPath),
            false);
        error = null;

        return true;
    }
}

/// <summary>
///  Completion state of a published fusion-reference artifact.
/// </summary>
internal enum ArtifactCompletion
{
    Complete,
    Incomplete
}

/// <summary>
///  Summarizes one fusion-reference export.
/// </summary>
internal sealed record FusionReferenceResult(
    ArtifactCompletion Completion,
    string OutputPath,
    FusionReferenceManifest Manifest)
{
    public string? TraceSha256 => Manifest.TraceSha256;

    public long InputRows => Manifest.Counts.InputRows;

    public long ImuRows => Manifest.Counts.ImuRows;

    public long OutputRows => Manifest.Counts.OutputRows;

    public long RejectedRows => Manifest.Counts.RejectedRows;

    public double? FirstCalibratedSeconds => Manifest.Calibration.FirstCalibratedSeconds;

    public WarpToolkit.Sensors.Fusion.FusionCalibrationState FinalCalibration
        => Manifest.Calibration.FinalState;

    public double? OutputRateHz => Manifest.Cadence.OutputHz;

    public double? MaxIntervalSeconds => Manifest.Cadence.MaxIntervalSeconds;
}
