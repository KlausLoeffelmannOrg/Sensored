namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies subcommand dispatch without changing legacy flag-only parsing.
/// </summary>
public class ProcessorDispatcherTests
{
    [Fact]
    public async Task Help_lists_legacy_operations_and_new_commands()
    {
        StringWriter stdout = new();

        int exit = await ProcessorApplication.RunAsync(
            ["--help"],
            stdout,
            new StringWriter(),
            TestContext.Current.CancellationToken);

        string text = stdout.ToString();
        Assert.Equal(0, exit);
        Assert.Contains("--damping-ms", text);
        Assert.Contains("analyze --recording-folder", text);
        Assert.Contains("fusion-reference --recording-folder", text);
    }

    [Fact]
    public async Task Analyze_help_does_not_require_legacy_operations()
    {
        StringWriter stdout = new();

        int exit = await ProcessorApplication.RunAsync(
            ["analyze", "--help"],
            stdout,
            new StringWriter(),
            TestContext.Current.CancellationToken);

        Assert.Equal(0, exit);
        Assert.Contains("analyze --recording-folder", stdout.ToString());
        Assert.DoesNotContain("--damping-ms", stdout.ToString());
    }

    [Fact]
    public async Task Unknown_legacy_option_still_fails_as_before()
    {
        StringWriter stderr = new();

        int exit = await ProcessorApplication.RunAsync(
            ["--recording-folder", ".", "--not-an-option"],
            new StringWriter(),
            stderr,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, exit);
        Assert.Contains("Unknown option", stderr.ToString());
    }

    [Fact]
    public void Legacy_parser_still_requires_a_processing_operation()
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
