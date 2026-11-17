using System.Diagnostics;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>
///  Guides three parked-only physical rotations to identify raw landscape gyro axes and signs.
/// </summary>
internal sealed partial class MotionCalibrationDialog : Form
{
    private readonly MotionAxisCapture?[] _captures = new MotionAxisCapture?[3];
    private SensorHub? _hub;
    private CancellationTokenSource? _captureStop;
    private int _step;

    internal MotionCalibrationDialog()
    {
        InitializeComponent();
        ShowStep();
    }

    internal MotionCalibrationDialog(SensorHub hub, Font font) : this()
    {
        ArgumentNullException.ThrowIfNull(hub);
        _hub = hub;
        Font = font;
    }

    internal MotionAxisCalibration? Result { get; private set; }

    private void ShowStep()
    {
        string name = _step switch { 0 => "Pitch", 1 => "Roll", _ => "Yaw" };
        _heading.Text = $"Step {_step + 1} of 3: {name}";
        _instructions.Text = _step switch
        {
            0 => "Hold the tablet in landscape. First move the TOP edge TOWARD you, then away. "
                + "Repeat forwards/backwards about the same axis, without rolling or turning.",
            1 => "Return to the same landscape holding position. First bank the RIGHT edge DOWN, "
                + "then the left edge down. Keep facing the same direction; avoid pitching.",
            _ => "Return to the same landscape holding position. First turn RIGHT (clockwise viewed "
                + "from above), then left. Keep the tablet upright; avoid pitching or rolling."
        };
        _progress.Text = "While parked only. Move the whole tablet, not just its stand. "
            + "Use small, deliberate movements. Capture starts after a 3-second countdown.";
        _capture.Text = $"Capture {name.ToLowerInvariant()} (6 s)";
        _capture.Enabled = true;
        _next.Text = _step == 2 ? "Check all three axes" : "Next step";
        _next.Enabled = _captures[_step] is not null;
    }

    private async void OnCapture(object? sender, EventArgs e)
    {
        if (_captureStop is not null || _hub is null)
        {
            return;
        }

        using CancellationTokenSource stop = new();
        _captureStop = stop;
        _captures[_step] = null;
        _capture.Enabled = _next.Enabled = _restart.Enabled = false;

        try
        {
            for (int seconds = 3; seconds > 0; seconds--)
            {
                _progress.Text = $"Get ready: {seconds}...";
                await Task.Delay(1000, stop.Token);
            }

            _progress.Text = "MOVE NOW: make the instructed first stroke, then repeat back and forth.";
            Stopwatch clock = Stopwatch.StartNew();
            Task<MotionAxisCapture> capture = _hub.CaptureMotionAxisAsync(stop.Token);

            while (!capture.IsCompleted)
            {
                if (!IsDisposed && !Disposing && !stop.IsCancellationRequested)
                {
                    _progress.Text = $"Capturing: {Math.Max(0d, 6d - clock.Elapsed.TotalSeconds):F1} s remaining. "
                        + "Keep rotating only about this axis.";
                }

                await Task.WhenAny(capture, Task.Delay(100));
            }

            _captures[_step] = await capture;
            stop.Token.ThrowIfCancellationRequested();
            MotionAxisCapture measured = _captures[_step]!;
            _results.Text = $"Measured raw gyro XYZ direction: "
                + $"[{measured.Axis.X:F3}, {measured.Axis.Y:F3}, {measured.Axis.Z:F3}]\r\n"
                + $"{measured.Samples} samples; {measured.AngleDegrees:F1} deg total motion; "
                + $"{measured.Purity:P1} single-axis energy.\r\n"
                + "The positive sign is the first stroke described above. Repeat if you started in the opposite direction.";
            _progress.Text = "Capture accepted. Return to your starting posture before the next step.";
            _next.Enabled = true;
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested)
        {
            // Closing the dialog cancels its private subscription, never the sensor session.
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());

            if (!IsDisposed && !Disposing)
            {
                _results.Text = $"Capture rejected: {exception.Message}";
                _progress.Text = "Repeat this step after correcting the reported problem.";
            }
        }
        finally
        {
            _captureStop = null;

            if (!IsDisposed && !Disposing)
            {
                _capture.Enabled = _restart.Enabled = true;
            }
        }
    }

    private void OnNext(object? sender, EventArgs e)
    {
        if (_step == 3)
        {
            DialogResult = DialogResult.OK;

            return;
        }

        if (_captures[_step] is null)
        {
            return;
        }

        if (_step < 2)
        {
            _step++;
            ShowStep();

            return;
        }

        try
        {
            Result = MotionAxisCalibration.Create(_captures[0]!, _captures[1]!, _captures[2]!);
            _step = 3;
            _heading.Text = "Landscape motion-axis check complete";
            _instructions.Text = "Review the measured axes. Keep the result to see mapped angular rates "
                + "on Diagnostics. This does not silently change the vehicle fusion frame.";
            _results.Text = Result.Describe();
            _progress.Text = "Use Start over if the motions or first-stroke signs were incorrect.";
            _capture.Enabled = false;
            _next.Text = "Keep diagnostic mapping";
        }
        catch (InvalidDataException exception)
        {
            Trace.TraceError(exception.ToString());
            _results.Text = $"Calibration rejected: {exception.Message}";
        }
    }

    private void OnRestart(object? sender, EventArgs e)
    {
        Array.Clear(_captures);
        Result = null;
        _step = 0;
        _results.Clear();
        ShowStep();
    }

    /// <inheritdoc/>
    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _captureStop?.Cancel();
        base.OnFormClosing(e);
    }
}
