using System.Globalization;

using WarpToolkit.Sensors.Fusion;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Runs the <c>fusion-reference</c> subcommand.
/// </summary>
internal static class FusionReferenceApplication
{
    internal const string Usage =
        """
        WARP golden fusion-reference exporter

        Usage:
          WarpSensorsDataProcessor fusion-reference --recording-folder <folder> --output <directory> [--scenario <json>]

        Writes a new directory containing golden.trace.csv and manifest.json.
        The output directory must not already exist. Source recordings are not modified.
        Optional scenarios inject deterministic relative-time faults outside the engine.
        """;

    /// <summary>
    ///  Runs one fusion-reference invocation.
    /// </summary>
    internal static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        if (!FusionReferenceOptions.TryParse(args, out FusionReferenceOptions? options, out string? parseError))
        {
            await error.WriteLineAsync(parseError);
            await error.WriteLineAsync("Use fusion-reference --help for usage.");

            return 2;
        }

        if (options!.ShowHelp)
        {
            await output.WriteLineAsync(Usage);

            return 0;
        }

        try
        {
            IFusionEngine engine = FusionEngineFactory.CreateDefault();
            FusionReferenceResult result = await FusionReferenceExporter.ExportAsync(
                options,
                engine,
                cancellationToken);
            await WriteSummaryAsync(output, result);

            return result.Completion == ArtifactCompletion.Complete ? 0 : 1;
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

    private static async Task WriteSummaryAsync(TextWriter output, FusionReferenceResult result)
    {
        FusionReferenceManifest manifest = result.Manifest;
        await output.WriteLineAsync(
            result.Completion == ArtifactCompletion.Complete
                ? "Fusion reference export complete."
                : "Fusion reference export incomplete.");
        await output.WriteLineAsync($"Input rows: {result.InputRows:N0}");
        await output.WriteLineAsync(
            $"IMU rows / output rows: {result.ImuRows:N0} / {result.OutputRows:N0}");
        await output.WriteLineAsync(
            $"Rejected: {result.RejectedRows:N0} ({FormatRate(manifest.Counts.RejectionRate)})");
        await output.WriteLineAsync(
            $"Calibration: first {FormatOptional(result.FirstCalibratedSeconds)} s (seq {FormatOptional(manifest.Calibration.FirstCalibratedOutputSequence)}); final {result.FinalCalibration}; coverage {FormatRate(manifest.Calibration.CalibratedCoverage)}; duration {FormatOptional(manifest.Calibration.CalibratedDurationSeconds)} s");
        await output.WriteLineAsync(
            $"Calibration rows: Uncalibrated {manifest.Calibration.UncalibratedRows:N0}, Converging {manifest.Calibration.ConvergingRows:N0}, Calibrated {manifest.Calibration.CalibratedRows:N0}");
        await output.WriteLineAsync(
            $"Confidence: min {FormatOptional(manifest.Confidence.Min)}, mean {FormatOptional(manifest.Confidence.Mean)}, max {FormatOptional(manifest.Confidence.Max)}");
        await output.WriteLineAsync(
            $"Validity rows: Attitude {manifest.Validity.Attitude:N0}, Heading {manifest.Validity.Heading:N0}, Acceleration {manifest.Validity.Acceleration:N0}, Navigation {manifest.Validity.Navigation:N0}");
        await output.WriteLineAsync($"Pitch deg: {FormatRange(manifest.Ranges.PitchDegrees)}");
        await output.WriteLineAsync($"Roll deg: {FormatRange(manifest.Ranges.RollDegrees)}");
        await output.WriteLineAsync($"Heading deg: {FormatRange(manifest.Ranges.HeadingDegrees)}");
        await output.WriteLineAsync($"Accel long/lat/vert m/s^2: {FormatRange(manifest.Ranges.LongitudinalAccelMps2)} / {FormatRange(manifest.Ranges.LateralAccelMps2)} / {FormatRange(manifest.Ranges.VerticalAccelMps2)}");
        await output.WriteLineAsync($"Speed m/s: {FormatRange(manifest.Ranges.SpeedMps)}");
        await output.WriteLineAsync(
            $"Output cadence: {FormatOptional(result.OutputRateHz)} Hz (max interval {FormatOptional(result.MaxIntervalSeconds)} s)");
        await output.WriteLineAsync($"Trace SHA-256: {result.TraceSha256 ?? "n/a"}");
        await output.WriteLineAsync(
            $"Engine assembly: {manifest.EngineAssemblyPath ?? "n/a"} (file {manifest.EngineAssemblyFileVersion ?? manifest.EngineAssemblyVersion}, written {manifest.EngineAssemblyLastWriteUtc ?? "n/a"})");
        await output.WriteLineAsync($"Output: {result.OutputPath}");
    }

    private static string FormatOptional(double? value)
        => value is { } number
            ? number.ToString("0.###", CultureInfo.InvariantCulture)
            : "n/a";

    private static string FormatOptional(long? value)
        => value is { } number
            ? number.ToString("N0", CultureInfo.InvariantCulture)
            : "n/a";

    private static string FormatRate(double? value)
        => value is { } number
            ? (number * 100d).ToString("0.###", CultureInfo.InvariantCulture) + "%"
            : "n/a";

    private static string FormatRange(FusionReferenceRange range)
        => $"{FormatOptional(range.Min)} .. {FormatOptional(range.Max)} (finite {range.FiniteCount:N0}, nonfinite {range.NonFiniteCount:N0})";
}
