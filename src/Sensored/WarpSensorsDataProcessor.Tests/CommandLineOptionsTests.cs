namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies command-line parsing and damping normalization.
/// </summary>
public class CommandLineOptionsTests
{
    [Theory]
    [InlineData("100", 100)]
    [InlineData("101", 150)]
    [InlineData("149.9", 150)]
    [InlineData("1951", 2000)]
    [InlineData("2000", 2000)]
    public void Damping_is_rounded_up_to_the_next_fifty_milliseconds(
        string requested,
        int expected)
    {
        bool parsed = CommandLineOptions.TryParse(
            ["--recording-folder", ".", "--damping-ms", requested],
            out CommandLineOptions? options,
            out string? error);

        Assert.True(parsed, error);
        Assert.Equal(expected, options!.DampingMilliseconds);
    }

    [Theory]
    [InlineData("99.9")]
    [InlineData("2000.1")]
    [InlineData("not-a-number")]
    public void Damping_outside_the_supported_range_is_rejected(string requested)
    {
        bool parsed = CommandLineOptions.TryParse(
            ["--recording-folder", ".", "--damping-ms", requested],
            out CommandLineOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("--damping-ms", error);
    }

    [Fact]
    public void At_least_one_processing_operation_is_required()
    {
        bool parsed = CommandLineOptions.TryParse(
            ["--recording-folder", "."],
            out CommandLineOptions? options,
            out string? error);

        Assert.False(parsed);
        Assert.Null(options);
        Assert.Contains("at least one processing option", error);
    }
}
