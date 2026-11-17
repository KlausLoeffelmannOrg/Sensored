using System.Globalization;
using System.Diagnostics;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;

namespace Sensored.TabViews;

/// <summary>
///  Displays the current GPS, attitude, and acceleration telemetry.
/// </summary>
public partial class TelemetryTabView : UserControl
{
    private static readonly TimeSpan DiagnosticUpdateInterval = TimeSpan.FromMilliseconds(200);
    private long _lastRawDiagnosticUpdate;
    private long _lastFusionDiagnosticUpdate;
    private long _lastFusedPositionUpdate;
    private string? _lastFusionTrust;
    private string? _lastRejection;
    private readonly bool _initialized;

    /// <summary>
    ///  Initializes a new instance of the <see cref="TelemetryTabView"/> class.
    /// </summary>
    public TelemetryTabView()
    {
        InitializeComponent();
        ApplyReadingFormats(DemoOptions.Default.ReadingFormats);
        _forwardAccelerationLabel.Value = double.NaN;
        _lateralAccelerationLabel.Value = double.NaN;
        _verticalAccelerationLabel.Value = double.NaN;
        ApplyGpsMotion(GpsMotionSnapshot.Empty);
        _initialized = true;
    }

    /// <summary>
    ///  Applies the configured decimal places to telemetry, keeping satellite counts integral.
    /// </summary>
    internal void ApplyReadingFormats(ReadingFormats formats)
    {
        ReadingFormats normalized = formats.Normalized();
        _forwardAccelerationLabel.DecimalPlaces = normalized.AccelerationDigits;
        _lateralAccelerationLabel.DecimalPlaces = normalized.AccelerationDigits;
        _verticalAccelerationLabel.DecimalPlaces = normalized.AccelerationDigits;
        _latitudeLabel.DecimalPlaces = normalized.GpsDigits;
        _longitudeLabel.DecimalPlaces = normalized.GpsDigits;
        _gpsSpeedLabel.DecimalPlaces = normalized.SpeedDigits;
        _gpsFourPointSpeedLabel.DecimalPlaces = normalized.SpeedDigits;
        _heightLabel.DecimalPlaces = normalized.RestDigits;
        _gpsCourseLabel.DecimalPlaces = normalized.RestDigits;
        _gpsHorizontalAccuracyLabel.DecimalPlaces = normalized.RestDigits;
        _gpsVerticalAccuracyLabel.DecimalPlaces = normalized.RestDigits;
        _rollLabel.DecimalPlaces = normalized.RestDigits;
        _pitchLabel.DecimalPlaces = normalized.RestDigits;
        _yawLabel.DecimalPlaces = normalized.RestDigits;
        _gpsSatelliteLabel.DecimalPlaces = 0;
    }

    /// <summary>
    ///  Applies vehicle-frame estimates while preserving separately labeled raw GPS readings.
    /// </summary>
    /// <param name="estimate">The pre-presentation fusion estimate.</param>
    /// <param name="gaugeReadings">Vehicle readings with the gauge's presentation-damped acceleration.</param>
    internal void ApplyFusion(FusionEstimate estimate, VehicleGaugeReadings gaugeReadings)
    {
        bool forwardKnown = estimate.IsAccelerationSupported;
        bool attitudeValid = estimate.IsAttitudeSupported;
        string rollCaption = forwardKnown ? "Vehicle roll" : "Provisional roll";
        string pitchCaption = forwardKnown ? "Vehicle pitch" : "Provisional pitch";

        if (_rollLabel.Caption != rollCaption
            && (attitudeValid || _rollLabel.Caption != "Raw device roll"))
        {
            _rollLabel.Caption = rollCaption;
        }

        if (_pitchLabel.Caption != pitchCaption
            && (attitudeValid || _pitchLabel.Caption != "Raw device pitch"))
        {
            _pitchLabel.Caption = pitchCaption;
        }

        _rollLabel.Value = gaugeReadings.RollDegrees;
        _pitchLabel.Value = gaugeReadings.PitchDegrees;
        _yawLabel.Caption = "Vehicle yaw";
        _yawLabel.Value = VehicleHeadingReadings.FromEstimate(estimate, null).YawDegrees;
        _forwardAccelerationLabel.Value = gaugeReadings.ForwardG;
        _lateralAccelerationLabel.Value = gaugeReadings.LateralG;
        _verticalAccelerationLabel.Value = gaugeReadings.VerticalG;
        string position = estimate.IsPositionSupported
            && estimate.Position is { } fix
            ? string.Create(CultureInfo.CurrentCulture, $"{fix.Latitude:F6}, {fix.Longitude:F6}")
            : "unavailable";
        string rejection = estimate.RejectedSample
            ? $" | Rejected: {estimate.Diagnostics.RejectionReason}"
            : string.Empty;
        string trust = !attitudeValid
            ? "ATTITUDE UNAVAILABLE"
            : !forwardKnown
                ? "GRAVITY ONLY — vehicle forward unknown"
                : estimate.CalibrationState == FusionCalibrationState.Calibrated
                    ? "CALIBRATED VEHICLE"
                    : "CONVERGING — provisional vehicle attitude";
        bool trustChanged = !string.Equals(_lastFusionTrust, trust, StringComparison.Ordinal);
        string uncertainty = estimate.PatternDiagnostics is { } pattern
            ? pattern.TiltErrorBoundDegrees is { } bound
                ? string.Create(CultureInfo.CurrentCulture, $"conditional tilt bound {bound:N1} deg")
                : "confidence not quantified"
            : string.Create(CultureInfo.CurrentCulture, $"confidence {estimate.Confidence:P0}");

        if (trustChanged
            || _lastFusionDiagnosticUpdate == 0
            || Stopwatch.GetElapsedTime(_lastFusionDiagnosticUpdate) >= DiagnosticUpdateInterval)
        {
            SetTextIfChanged(
                _fusionTrustDiagnostic,
                attitudeValid
                    ? string.Create(
                        CultureInfo.CurrentCulture,
                        $"{(forwardKnown ? "" : "Vehicle acceleration unavailable · ")}{trust} · {uncertainty}")
                    : (forwardKnown ? "" : "Vehicle acceleration unavailable. ")
                        + "Raw angles are device diagnostics, not a vehicle horizon. "
                        + WarpToolkit.WinForms.Sensors.DirectX.FusionReadinessSnapshot.ExplainReason(
                            estimate.Support?.BodyAttitude.Reason));
            _lastFusionTrust = trust;
            _lastFusionDiagnosticUpdate = Stopwatch.GetTimestamp();
        }

        if (trustChanged
            || !string.Equals(_lastRejection, rejection, StringComparison.Ordinal)
            || _lastFusedPositionUpdate == 0
            || Stopwatch.GetElapsedTime(_lastFusedPositionUpdate) >= DiagnosticUpdateInterval)
        {
            SetTextIfChanged(_gpsSourceLabel, string.Create(
                CultureInfo.CurrentCulture,
                $"Fused {position} | {trust}; {uncertainty}{rejection}"));
            _lastRejection = rejection;
            _lastFusedPositionUpdate = Stopwatch.GetTimestamp();
        }
    }

    /// <summary>
    ///  Shows raw device diagnostics separately from calibrated vehicle-frame outputs.
    /// </summary>
    /// <param name="imu">The latest raw inertial reading.</param>
    /// <param name="estimate">The outputs already supplied by fusion.</param>
    internal void ApplyRawImuFallback(
        RawImuPayload imu,
        FusionEstimate? estimate)
    {
        bool accelerationValid = estimate?.IsAccelerationSupported == true;
        if (estimate?.IsAttitudeSupported != true)
        {
            SetTextIfChanged(
                _fusionTrustDiagnostic,
                (accelerationValid
                    ? ""
                    : "Vehicle acceleration unavailable. ")
                    + "Raw angles are device diagnostics, not a vehicle horizon. "
                    + WarpToolkit.WinForms.Sensors.DirectX.FusionReadinessSnapshot.ExplainReason(
                        estimate?.Support?.BodyAttitude.Reason));

            if (_rollLabel.Caption != "Raw device roll")
            {
                _rollLabel.Caption = "Raw device roll";
            }

            if (_pitchLabel.Caption != "Raw device pitch")
            {
                _pitchLabel.Caption = "Raw device pitch";
            }

            _rollLabel.Value = imu.AbsoluteRollDegrees;
            _pitchLabel.Value = imu.AbsolutePitchDegrees;
        }

        VehicleHeadingReadings heading = VehicleHeadingReadings.FromEstimate(estimate, imu);
        _yawLabel.Caption = heading.IsDeviceYaw ? "Device yaw" : "Vehicle yaw";
        _yawLabel.Value = heading.YawDegrees;

        if (_lastRawDiagnosticUpdate == 0
            || Stopwatch.GetElapsedTime(_lastRawDiagnosticUpdate) >= DiagnosticUpdateInterval)
        {
            SetTextIfChanged(_rawAccelerationDiagnostic, string.Create(
                CultureInfo.CurrentCulture,
                $"Raw device (gravity included): X {imu.Acceleration.X:+0.000;-0.000;0.000}  Y {imu.Acceleration.Y:+0.000;-0.000;0.000}  Z {imu.Acceleration.Z:+0.000;-0.000;0.000} g"));
            _lastRawDiagnosticUpdate = Stopwatch.GetTimestamp();
        }

        if (!accelerationValid)
        {
            _forwardAccelerationLabel.Value = double.NaN;
            _lateralAccelerationLabel.Value = double.NaN;
            _verticalAccelerationLabel.Value = double.NaN;
        }
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

        _gpsHorizontalAccuracyLabel.Value = fix.HorizontalAccuracyMeters;
        _gpsVerticalAccuracyLabel.Value = fix.VerticalAccuracyMeters;
        _gpsSatelliteLabel.Value = fix.SatelliteCount ?? 0;

        SetTextIfChanged(_gpsSourceLabel, string.Create(
            CultureInfo.CurrentCulture,
            $"Source {fix.PositionSource}  ·  {fix.CoordinateTimestamp.LocalDateTime:HH:mm:ss}"));
    }

    /// <summary>Applies the full-rate GPS comparison, independently of dashboard sampling and fusion.</summary>
    internal void ApplyGpsMotion(GpsMotionSnapshot snapshot)
    {
        _gpsSpeedLabel.Value = snapshot.ReceiverSpeedMetersPerSecond * 3.6d;
        _gpsFourPointSpeedLabel.Value = snapshot.FourPoint.SpeedMetersPerSecond * 3.6d;
        _gpsCourseLabel.Value = snapshot.ReceiverCourseDegrees;
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

        _forwardAccelerationLabel.Value = double.NaN;
        _lateralAccelerationLabel.Value = double.NaN;
        _verticalAccelerationLabel.Value = double.NaN;
        _fusionTrustDiagnostic.Text = "Vehicle attitude unavailable.";
        _rawAccelerationDiagnostic.Text = "Raw device X/Y/Z (gravity included): —";
        _gpsSourceLabel.Text = "Waiting for a position.";
        _lastRawDiagnosticUpdate = 0;
        _lastFusionDiagnosticUpdate = 0;
        _lastFusedPositionUpdate = 0;
        _lastFusionTrust = null;
        _lastRejection = null;
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (!_initialized)
        {
            return;
        }

        using var disp = this.SuspendPainting(LayoutSuspendTraversal.TargetAndDescendants);
        foreach (AnimatedValueLabel label in EnumerateValueLabels())
        {
            label.Font = Font;
        }

        _gpsSourceLabel.Font = Font;
        _fusionTrustDiagnostic.Font = Font;
        _rawAccelerationDiagnostic.Font = Font;
    }

    private IEnumerable<AnimatedValueLabel> EnumerateValueLabels()
    {
        yield return _longitudeLabel;
        yield return _latitudeLabel;
        yield return _heightLabel;
        yield return _gpsSpeedLabel;
        yield return _gpsFourPointSpeedLabel;
        yield return _gpsCourseLabel;
        yield return _gpsHorizontalAccuracyLabel;
        yield return _gpsVerticalAccuracyLabel;
        yield return _gpsSatelliteLabel;
        yield return _rollLabel;
        yield return _pitchLabel;
        yield return _yawLabel;
        yield return _forwardAccelerationLabel;
        yield return _lateralAccelerationLabel;
        yield return _verticalAccelerationLabel;
    }

    private static void SetTextIfChanged(Label label, string text)
    {
        if (!string.Equals(label.Text, text, StringComparison.Ordinal))
        {
            label.Text = text;
        }
    }
}
