using System.Globalization;
using WarpToolkit.WinForms.DirectX.Controls;

namespace WarpSensorsDemo.TabViews;

/// <summary>
///  Displays the current GPS, attitude, and acceleration telemetry.
/// </summary>
public partial class TelemetryTabView : UserControl
{
    private readonly bool _initialized;

    /// <summary>
    ///  Initializes a new instance of the <see cref="TelemetryTabView"/> class.
    /// </summary>
    public TelemetryTabView()
    {
        InitializeComponent();
        _initialized = true;
    }

    /// <summary>
    ///  Applies the latest inertial reading to the telemetry labels.
    /// </summary>
    /// <param name="imu">The inertial reading to display.</param>
    internal void ApplyRawImu(RawImuPayload imu)
    {
        _rollLabel.Value = imu.AbsoluteRollDegrees;
        _pitchLabel.Value = imu.AbsolutePitchDegrees;
        _forwardAccelerationLabel.Value = imu.Acceleration.X;
        _lateralAccelerationLabel.Value = imu.Acceleration.Y;
        _verticalAccelerationLabel.Value = imu.Acceleration.Z;
    }

    /// <summary>
    ///  Applies the latest geolocation reading to the telemetry labels.
    /// </summary>
    /// <param name="fix">The geolocation reading to display.</param>
    internal void ApplyGeolocation(GeolocationPayload fix)
    {
        _longitudeLabel.Value = fix.LongitudeDegrees;
        _latitudeLabel.Value = fix.LatitudeDegrees;
        _heightLabel.Value = fix.AltitudeMeters;

        _gpsSpeedLabel.Value = fix.SpeedValid && double.IsFinite(fix.SpeedMetersPerSecond)
            ? fix.SpeedMetersPerSecond * 3.6d
            : 0d;

        _gpsCourseLabel.Value = fix.CourseValid && double.IsFinite(fix.CourseDegrees)
            ? fix.CourseDegrees
            : 0d;

        _gpsHorizontalAccuracyLabel.Value = fix.HorizontalAccuracyMeters;
        _gpsVerticalAccuracyLabel.Value = fix.VerticalAccuracyMeters;
        _gpsSatelliteLabel.Value = fix.SatelliteCount ?? 0;

        _gpsSourceLabel.Text = string.Create(
            CultureInfo.CurrentCulture,
            $"Source {fix.PositionSource}  ·  {fix.CoordinateTimestamp.LocalDateTime:HH:mm:ss}");
    }

    /// <summary>
    ///  Applies the font used by the telemetry values.
    /// </summary>
    /// <param name="font">The font to apply without taking ownership of it.</param>
    internal void ApplyTelemetryFont(Font font)
    {
        ArgumentNullException.ThrowIfNull(font);

        Font = font;
    }

    /// <summary>
    ///  Resets all displayed telemetry values.
    /// </summary>
    internal void ResetValues()
    {
        foreach (AnimatedValueLabel label in EnumerateValueLabels())
        {
            label.Value = 0d;
            label.ResetPresentation();
        }

        _gpsSourceLabel.Text = "Waiting for a position.";
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (!_initialized)
        {
            return;
        }

        foreach (AnimatedValueLabel label in EnumerateValueLabels())
        {
            label.Font = Font;
        }

        _gpsSourceLabel.Font = Font;
    }

    private IEnumerable<AnimatedValueLabel> EnumerateValueLabels()
    {
        yield return _longitudeLabel;
        yield return _latitudeLabel;
        yield return _heightLabel;
        yield return _gpsSpeedLabel;
        yield return _gpsCourseLabel;
        yield return _gpsHorizontalAccuracyLabel;
        yield return _gpsVerticalAccuracyLabel;
        yield return _gpsSatelliteLabel;
        yield return _rollLabel;
        yield return _pitchLabel;
        yield return _forwardAccelerationLabel;
        yield return _lateralAccelerationLabel;
        yield return _verticalAccelerationLabel;
    }
}
