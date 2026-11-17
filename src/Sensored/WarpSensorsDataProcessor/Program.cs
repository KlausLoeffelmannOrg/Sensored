namespace WarpSensorsDataProcessor;

/// <summary>
///  Hosts the recorded sensor-data processing command line.
/// </summary>
internal static class Program
{
    /// <summary>
    ///  Runs the command line application.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    private static async Task<int> Main(string[] args)
    {
        using CancellationTokenSource cancellation = new();

        ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        Console.CancelKeyPress += cancelHandler;

        try
        {
            return await ProcessorApplication.RunAsync(
                args,
                Console.Out,
                Console.Error,
                cancellation.Token);
        }
        finally
        {
            Console.CancelKeyPress -= cancelHandler;
        }
    }
}
