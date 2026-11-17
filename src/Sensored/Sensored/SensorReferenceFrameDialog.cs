using WarpToolkit.Sensors;

namespace Sensored;

/// <summary>
///  Captures or edits one display reference without changing the fusion measured mount.
/// </summary>
/// <remarks>
///  <para>
///   The original manual-orientation wizard is not in this tree. This dialog restores that
///   workflow with the shared tablet pose viewer, capture of the current uncorrected pose,
///   manual offsets, and reset to the sensor's default mounting.
///  </para>
///  <para>
///   Raw gravity is apparent tilt and is affected by vehicle acceleration. Bluetooth yaw is
///   device-fused and is not a certified independent compass.
///  </para>
/// </remarks>
internal sealed partial class SensorReferenceFrameDialog : Form
{
    private readonly Func<SensorReferenceSample?>? _readSample;
    private readonly SensorReferenceFrame _resetFrame;

    /// <summary>
    ///  Initializes a new instance of the <see cref="SensorReferenceFrameDialog"/> class.
    /// </summary>
    /// <param name="frame">The finite reference to edit.</param>
    /// <param name="caption">The sensor collection named by the dialog.</param>
    /// <param name="readSample">Reads the current uncorrected pose, when a live source is connected.</param>
    /// <param name="dialogFont">The owner's font, applied before the autosized content is measured.</param>
    /// <param name="resetFrame">The default mounting reference for this sensor collection.</param>
    /// <param name="allowAxisSwap">Whether to show the Bluetooth display-axis swap option.</param>
    internal SensorReferenceFrameDialog(
        SensorReferenceFrame frame,
        string caption,
        Func<SensorReferenceSample?>? readSample = null,
        Font? dialogFont = null,
        SensorReferenceFrame resetFrame = default,
        bool allowAxisSwap = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(caption);
        if (!frame.IsFinite)
        {
            throw new ArgumentOutOfRangeException(nameof(frame), "Reference angles must be finite.");
        }

        _readSample = readSample;
        _resetFrame = resetFrame;
        InitializeComponent();
        Font = dialogFont ?? SystemFonts.MessageBoxFont!;
        Text = caption;
        _captionLabel.Text = caption;
        _resetButton.Text = "Reset mounting";
        _noteLabel.Text = "Adjust the mounting reference in 90-degree steps or capture the current pose as zero. "
            + "The picture previews the corrected pose, including heading when available. "
            + "These display offsets never change raw measurements or the fusion measured mount.";

        _yawUpDown.Value = ToEditorValue(frame.YawDegrees);
        _pitchUpDown.Value = ToEditorValue(frame.PitchDegrees);
        _rollUpDown.Value = ToEditorValue(frame.RollDegrees);
        _swapPitchAndRollCheckBox.Visible = allowAxisSwap;
        _swapPitchAndRollCheckBox.Checked = frame.SwapPitchAndRoll;
        UpDownHeightWorkaround.Attach(this);
        OnSampleTick(this, EventArgs.Empty);
    }

    /// <summary>
    ///  Gets the reference currently shown by the editor, with each angle normalized cyclically.
    /// </summary>
    internal SensorReferenceFrame Frame
        => new(
            NormalizeCyclicDegrees((double)_yawUpDown.Value),
            NormalizeCyclicDegrees((double)_pitchUpDown.Value),
            NormalizeCyclicDegrees((double)_rollUpDown.Value),
            _swapPitchAndRollCheckBox.Checked);

    /// <inheritdoc/>
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _sampleTimer.Start();
    }

    /// <inheritdoc/>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Rectangle workingArea = Screen.FromControl(Owner ?? this).WorkingArea;
        MinimumSize = new Size(
            Math.Min(MinimumSize.Width, workingArea.Width),
            Math.Min(MinimumSize.Height, workingArea.Height));
        MaximumSize = workingArea.Size;
        DialogBoundsPlacement.Restore(this, null, nameof(SensorReferenceFrameDialog));
    }

    /// <summary>
    ///  Normalizes a finite angle into the half-open range (-180, 180].
    /// </summary>
    /// <param name="degrees">The angle to keep. Non-finite values are rejected.</param>
    /// <returns>The cyclically equivalent angle.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The angle is not finite.</exception>
    internal static double NormalizeCyclicDegrees(double degrees)
    {
        if (!double.IsFinite(degrees))
        {
            throw new ArgumentOutOfRangeException(
                nameof(degrees),
                degrees,
                "Reference angle must be a finite number of degrees.");
        }

        double wrapped = degrees % 360d;
        if (wrapped > 180d)
        {
            wrapped -= 360d;
        }
        else if (wrapped <= -180d)
        {
            wrapped += 360d;
        }

        return wrapped;
    }

    /// <summary>
    ///  Converts a finite angle to an editor value without clamping it onto a smaller range.
    /// </summary>
    /// <param name="degrees">The angle to show.</param>
    /// <returns>The cyclically normalized angle.</returns>
    internal static decimal ToEditorValue(double degrees)
        => (decimal)NormalizeCyclicDegrees(degrees);

    private void OnSampleTick(object? sender, EventArgs e)
    {
        SensorReferenceSample? sample = _readSample?.Invoke();
        if (sample is not { } pose)
        {
            _captureButton.Enabled = false;
            _liveLabel.Text = _readSample is null
                ? "No live pose callback is connected. Capture is unavailable; adjust the mounting manually or reset it."
                : "No live pose is available. Capture will not change the offsets.";
            _liveLabel.Text += " Preview shows the offsets applied to a level example.";
            UpdatePreview(Frame.Correct(0d, 0d, 0d));

            return;
        }

        bool roll = double.IsFinite(pose.RollDegrees);
        bool pitch = double.IsFinite(pose.PitchDegrees);
        bool heading = double.IsFinite(pose.HeadingDegrees);
        _captureButton.Enabled = roll || pitch || heading;
        SensorAttitude corrected = Frame.Correct(pose.RollDegrees, pose.PitchDegrees, pose.HeadingDegrees);
        UpdatePreview(corrected);
        _liveLabel.Text =
            $"Live uncorrected pose: roll {FormatAxis(pose.RollDegrees, roll)}, "
            + $"pitch {FormatAxis(pose.PitchDegrees, pitch)}, "
            + $"heading {FormatAxis(pose.HeadingDegrees, heading)}."
            + Environment.NewLine
            + $"Normalized: roll {FormatAxis(corrected.RollDegrees, double.IsFinite(corrected.RollDegrees))}, "
            + $"pitch {FormatAxis(corrected.PitchDegrees, double.IsFinite(corrected.PitchDegrees))}, "
            + $"heading {FormatAxis(corrected.HeadingDegrees, double.IsFinite(corrected.HeadingDegrees))}.";
    }

    private void UpdatePreview(SensorAttitude corrected)
    {
        if (double.IsFinite(corrected.RollDegrees) && double.IsFinite(corrected.PitchDegrees))
        {
            _view.SetAttitude(corrected.RollDegrees, corrected.PitchDegrees,
                double.IsFinite(corrected.HeadingDegrees) ? corrected.HeadingDegrees : 0d);
        }
    }

    private void OnCaptureClick(object? sender, EventArgs e)
    {
        if (_readSample?.Invoke() is not { } pose)
        {
            _liveLabel.Text = "No live pose is available. Offsets were not changed.";
            return;
        }

        bool roll = double.IsFinite(pose.RollDegrees);
        bool pitch = double.IsFinite(pose.PitchDegrees);
        bool heading = double.IsFinite(pose.HeadingDegrees);
        if (!roll && !pitch && !heading)
        {
            _liveLabel.Text = "The live pose has no finite roll, pitch, or heading. Offsets were not changed.";
            return;
        }

        if (roll)
        {
            _rollUpDown.Value = ToEditorValue(pose.RollDegrees);
        }

        if (pitch)
        {
            _pitchUpDown.Value = ToEditorValue(pose.PitchDegrees);
        }

        if (heading)
        {
            _yawUpDown.Value = ToEditorValue(pose.HeadingDegrees);
        }

        _liveLabel.Text = heading
            ? "Captured the current pose as the display zero."
            : "Captured the available roll and pitch. Heading is unavailable, so the current yaw offset was retained.";
    }

    private void OnResetClick(object? sender, EventArgs e)
    {
        _yawUpDown.Value = ToEditorValue(_resetFrame.YawDegrees);
        _pitchUpDown.Value = ToEditorValue(_resetFrame.PitchDegrees);
        _rollUpDown.Value = ToEditorValue(_resetFrame.RollDegrees);
        _swapPitchAndRollCheckBox.Checked = _resetFrame.SwapPitchAndRoll;
        _liveLabel.Text = "Reset the display mounting reference. Fusion measured mount is unchanged.";
    }

    private void OnOffsetsChanged(object? sender, EventArgs e)
        => OnSampleTick(sender, e);

    private void OnYawQuarterTurnClick(object? sender, EventArgs e)
        => _yawUpDown.Value = ToEditorValue((double)_yawUpDown.Value + 90d);

    private void OnPitchQuarterTurnClick(object? sender, EventArgs e)
        => _pitchUpDown.Value = ToEditorValue((double)_pitchUpDown.Value + 90d);

    private void OnRollQuarterTurnClick(object? sender, EventArgs e)
        => _rollUpDown.Value = ToEditorValue((double)_rollUpDown.Value + 90d);

    private static string FormatAxis(double degrees, bool finite)
        => finite ? $"{degrees:0.0}°" : "unavailable";
}
