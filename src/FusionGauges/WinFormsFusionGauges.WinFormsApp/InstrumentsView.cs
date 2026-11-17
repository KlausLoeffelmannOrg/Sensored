using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Displays only supported vehicle-frame estimates and fresh accepted GPS fixes.</summary>
public sealed partial class InstrumentsView : UserControl
{
    private int _epoch = -1;
    private readonly SensorGaugeDashboard _dashboard;
    private bool _initialized;

    /// <summary>Creates four existing WARP gauges without starting acquisition.</summary>
    public InstrumentsView()
    {
        InitializeComponent();
        _canLog.Font = CreateDiagnosticFont();
        Disposed += OnMercedesDisposed;
        _dashboard = new SensorGaugeDashboard(_speed, _horizon, _acceleration, _compass);
        _initialized = true;
        UpdateLayoutSizing();
    }

    internal void ApplyOptions(FusionGaugesOptions options)
    {
        _speed.Rings.Apply(options.Speedometer);
        _telemetry.ApplyPrecision(options.AccelerationDigits);
        ApplyReadiness(FusionReadinessSnapshot.Create(null, options.VehicleMount, isRunning: false));
        UpdateLayoutSizing();
    }

    internal void ResetSession()
    {
        _speed.Reset();
        ResetGauges();
        _epoch = -1;
    }

    internal void Present(SensorObservationFrame frame, TabletFrameStatus calibration,
        GpsDashboardSnapshot? gpsDashboard = null, VehicleSpeedMeasurement? vehicleSpeed = null,
        FusionReadinessSnapshot? readiness = null)
    {
        GpsDashboardSnapshot gps = gpsDashboard ?? GpsDashboardSnapshot.Empty;
        FusionObservation? observation = frame.FusionFresh ? frame.Fusion : null;
        FusionEstimate? estimate = observation?.Estimate;

        if (observation is not null && observation.Epoch != _epoch)
        {
            ResetGauges();
            _epoch = observation.Epoch;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        bool gpsAvailable = frame.GpsFresh && gps.Motion.Timestamp is not null
            && now >= gps.AcceptedArrival && now - gps.AcceptedArrival <= TimeSpan.FromSeconds(3);
        _dashboard.ApplyFusion(estimate, frame.ImuFresh ? frame.Imu?.Imu : null, now,
            applySpeed: vehicleSpeed is null);
        _dashboard.ApplyGps(gps.Motion, gpsAvailable, now);
        _speed.SetGpsHistory(gps);
        if (vehicleSpeed is not null)
        {
            bool fresh = vehicleSpeed.IsFreshAt(now);
            _speed.SetVehicleSpeed(fresh ? vehicleSpeed.MetersPerSecond : double.NaN,
                fresh ? vehicleSpeed.Timestamp : now, vehicleSpeed.TripKilometers);
        }

        _horizon.SetAltitudeTrace(_speed.CaptureAltitudeTrace());
        _telemetry.Present(frame, gps, gpsAvailable, vehicleSpeed);
        ApplyReadiness(readiness ?? FusionReadinessSnapshot.Create(estimate, null,
            isRunning: true, fusionFresh: frame.FusionFresh));
    }

    private void ApplyReadiness(FusionReadinessSnapshot readiness)
    {
        _calibrationLabel.Text = readiness.Summary;
        _calibrationLabel.AccessibleDescription = readiness.Details;
        _calibrationLabel.ForeColor = TabletFrameStatusColors.GetColor(
            readiness.BodyOutputsSupported ? TabletFrameQuality.Green : TabletFrameQuality.Yellow,
            Application.IsDarkModeEnabled);
    }

    private void ResetGauges()
    {
        _horizon.ResetPresentation();
        _acceleration.ResetPresentation();
        _compass.ResetPresentation();
        InvalidateSpeed(DateTimeOffset.UtcNow);
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_initialized)
        {
            UpdateLayoutSizing();
        }
    }

    private void UpdateLayoutSizing()
    {
        Size gauge = _speed.GetPreferredSize(Size.Empty);
        _layout.MinimumSize = new Size(
            (gauge.Width * 2) + _telemetry.MinimumSize.Width + 64,
            (gauge.Height * 2) + _calibrationLabel.PreferredHeight + 48);
        AutoScrollMinSize = _layout.MinimumSize;
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateLayoutSizing();
    }

    private void InvalidateSpeed(DateTimeOffset timestamp)
    {
        // Speedometer.ResetPresentation clears damping, not measurement validity or trip history.
        _speed.SetSpeed(double.NaN, SpeedUnit.MetersPerSecond, timestamp);
        _speed.ResetPresentation();
    }
}
