using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Separates raw device measurements and acquisition health from calibrated instruments.</summary>
public sealed partial class DiagnosticsView : UserControl
{
    /// <summary>Creates the scrollable diagnostic presentation.</summary>
    public DiagnosticsView() => InitializeComponent();

    /// <summary>Gets the full diagnostic snapshot, independently of selection or scroll position.</summary>
    internal string DiagnosticText => _details.Text;

    internal void Present(SensorObservationFrame frame, TabletFrameStatus calibration, SensorObservationCore core,
        MotionAxisCalibration? motionCalibration = null, bool isRunning = true, bool hasGpsPreview = false,
        FusionReadinessSnapshot? readiness = null)
    {
        string raw = frame.Imu?.Imu is { } imu
            ? $"RAW DEVICE FRAME (includes gravity)\r\n"
                + $"Acceleration G: X {imu.Acceleration.X:F4}   Y {imu.Acceleration.Y:F4}   Z {imu.Acceleration.Z:F4}\r\n"
                + $"Has angular rate: {imu.HasAngularRate}\r\n"
                + $"Angular rate deg/s: X {imu.AngularRate.X:F3}   Y {imu.AngularRate.Y:F3}   Z {imu.AngularRate.Z:F3}"
            : "IMU unavailable";
        string gps = frame.Gps?.Geolocation is { } fix
            ? $"Accepted GPS fix: {(frame.GpsFresh ? "fresh" : "STALE")}\r\n"
                + $"Latitude {fix.LatitudeDegrees:F7}   Longitude {fix.LongitudeDegrees:F7}\r\n"
                + $"Speed m/s {fix.SpeedMetersPerSecond:F3} (valid {fix.SpeedValid})\r\n"
                + $"Course deg {fix.CourseDegrees:F2} (valid {fix.CourseValid})\r\n"
                + $"Horizontal / vertical accuracy m: {fix.HorizontalAccuracyMeters:F2} / {fix.VerticalAccuracyMeters:F2}\r\n"
                + $"Provider: {fix.PositionSource}   Fix time: {frame.Gps.SourceTimestamp:O}"
            : "No accepted GPS fix";
        string session = isRunning
            ? ""
            : hasGpsPreview
                ? "SESSION STOPPED - IMU and fusion are historical. GPS is live preview; fix freshness is shown below.\r\n\r\n"
                : "SESSION STOPPED - frozen snapshot. Measurements below are historical, not live.\r\n\r\n";
        string history = isRunning ? "Retained 120 s" : "Frozen final history";
        _details.Text = $"{session}{raw}\r\n\r\n{gps}\r\n\r\n"
            + $"Arrival cadence: IMU {frame.ImuHz:F1} Hz / GPS {frame.GpsHz:F1} Hz\r\n"
            + $"Subscription drops: {frame.SubscriptionDropped}   Ingress gaps: {frame.IngressGaps}\r\n"
            + $"{history}: IMU {core.ImuHistory.Count}, GPS {core.GpsHistory.Count}, fusion {core.FusionHistory.Count}\r\n"
            + $"Sensor health: {frame.Health?.Health?.GpsAcquisitionStatus}   {frame.Health?.Health?.FaultMessage}\r\n"
            + $"Legacy down-only tracker (not vehicle calibration): {calibration.Quality} - {calibration.Message}\r\n"
            + $"Fusion: {frame.Fusion?.Engine} epoch {frame.Fusion?.Epoch}; validity {frame.Fusion?.Estimate?.Validity}\r\n"
            + $"{(readiness ?? FusionReadinessSnapshot.Create(frame.Fusion?.Estimate, null, isRunning, frame.FusionFresh)).Details}\r\n"
            + "A connected GPS transport is not evidence of a fresh accepted fix.";

        if (motionCalibration is not null)
        {
            _details.Text += $"\r\n\r\n{motionCalibration.Describe()}";

            if (frame.ImuFresh && frame.Imu?.Imu is { HasAngularRate: true } live)
            {
                var (pitch, roll, yaw) = motionCalibration.Project(live.AngularRate);
                _details.Text += $"\r\nMapped gyro deg/s: pitch {pitch:F3} / roll {roll:F3} / yaw {yaw:F3}";
            }
        }
    }
}
