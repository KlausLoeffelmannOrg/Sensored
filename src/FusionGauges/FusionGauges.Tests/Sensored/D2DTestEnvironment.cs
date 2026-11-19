using System.Runtime.ExceptionServices;

namespace Sensored.Tests;

/// <summary>Hosts WinForms tests on an STA thread and captures message-dispatch failures.</summary>
internal static class D2DTestEnvironment
{
    /// <summary>Creates an offscreen form owned by the caller.</summary>
    internal static Form CreateOffscreenForm()
        => new()
        {
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.Manual,
            Location = new Point(-32000, -32000),
            Opacity = 0.01,
            Size = new Size(400, 300),
        };

    /// <summary>Runs a test on an STA thread and rethrows captured failures.</summary>
    /// <param name="body">The test body to execute.</param>
    internal static void RunOnSta(Action body)
    {
        ArgumentNullException.ThrowIfNull(body);

        Exception? captured = null;
        Thread thread = new(() =>
        {
            List<Exception> failures = [];
            void CaptureMessageFailure(object? sender, ThreadExceptionEventArgs e) => failures.Add(e.Exception);
            try
            {
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException, threadScope: true);
                Application.ThreadException += CaptureMessageFailure;
                body();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
            finally
            {
                Application.ThreadException -= CaptureMessageFailure;
                captured = failures.Count switch
                {
                    0 => null,
                    1 => failures[0],
                    _ => new AggregateException("STA test and message-dispatch failures.", failures)
                };
            }
        })
        {
            Name = "SensoredTests.Sta",
            IsBackground = true
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (captured is not null)
        {
            ExceptionDispatchInfo.Capture(captured).Throw();
        }
    }
}
