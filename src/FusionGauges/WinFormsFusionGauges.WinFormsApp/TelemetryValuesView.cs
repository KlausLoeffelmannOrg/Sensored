using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.DirectX.Controls;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Displays independent raw, receiver, calculated and fusion channels in labeled groups.</summary>
public sealed partial class TelemetryValuesView : UserControl
{
    private Font? _valueFont;
    private bool _initialized;

    /// <summary>Creates telemetry controls without acquiring sensors.</summary>
    public TelemetryValuesView()
    {
        InitializeComponent();
        _initialized = true;
        Disposed += OnDisposed;
        ApplyPrecision(2);
        UpdateValueFont();
    }

    internal IEnumerable<AnimatedValueLabel> Values
        => [_fusionKmh, _receiverKmh, _calculatedKmh, _fusionMph, _receiverMph,
            _tripKm, _tripMiles, _latitude, _longitude, _altitude, _heading, _accuracy,
            _x, _y, _z, _pitch, _roll, _yaw];

    internal void ApplyPrecision(int digits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(digits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(digits, 5);
        _x.DecimalPlaces = _y.DecimalPlaces = _z.DecimalPlaces = digits;
    }

    internal void Present(SensorObservationFrame frame, GpsDashboardSnapshot gps, bool gpsFresh,
        VehicleSpeedMeasurement? vehicleSpeed = null)
    {
        FusionEstimate? estimate = frame.FusionFresh ? frame.Fusion?.Estimate : null;
        double fused = estimate?.IsSpeedSupported == true
            ? estimate.Speed : double.NaN;
        double receiver = gpsFresh ? gps.Motion.ReceiverSpeedMetersPerSecond : double.NaN;
        if (vehicleSpeed is not null)
        {
            fused = vehicleSpeed.IsFreshAt(DateTimeOffset.UtcNow) ? vehicleSpeed.MetersPerSecond : double.NaN;
            receiver = double.NaN;
        }

        _fusionKmh.Caption = vehicleSpeed is null ? "KM/H - Fusion:" : "KM/H - OBD2:";
        _fusionMph.Caption = vehicleSpeed is null ? "MP/H - Fusion:" : "MP/H - OBD2:";
        _fusionKmh.AccessibleName = _fusionKmh.Caption;
        _fusionMph.AccessibleName = _fusionMph.Caption;
        _fusionKmh.Value = fused * 3.6d;
        _fusionMph.Value = fused * 2.2369362920544d;
        _receiverKmh.Value = receiver * 3.6d;
        _receiverMph.Value = receiver * 2.2369362920544d;
        _calculatedKmh.Value = vehicleSpeed is null && gpsFresh
            ? gps.Motion.FourPoint.SpeedMetersPerSecond * 3.6d : double.NaN;
        double trip = vehicleSpeed is null ? gps.TripKilometers : vehicleSpeed.TripKilometers ?? 0;
        _tripKm.Value = trip;
        _tripMiles.Value = trip / 1.609344d;
        GeolocationPayload? fix = gpsFresh ? gps.Fix : null;
        _latitude.Value = fix?.LatitudeDegrees ?? double.NaN;
        _longitude.Value = fix?.LongitudeDegrees ?? double.NaN;
        _altitude.Value = fix?.AltitudeMeters ?? double.NaN;
        _heading.Value = gpsFresh ? gps.Motion.ReceiverCourseDegrees : double.NaN;
        _accuracy.Value = fix?.HorizontalAccuracyMeters ?? double.NaN;
        string source = vehicleSpeed is null
            ? $"Source: {fix?.PositionSource.ToString() ?? "Unavailable"}"
            : vehicleSpeed.IsFreshAt(DateTimeOffset.UtcNow)
                ? "Speed: OBD2 (100%); no fusion" : "Speed: OBD2 unavailable; no GPS fallback";

        if (_source.Text != source)
        {
            _source.Text = source;
        }

        RawImuPayload? imu = frame.ImuFresh ? frame.Imu?.Imu : null;
        _x.Value = imu?.Acceleration.X ?? double.NaN;
        _y.Value = imu?.Acceleration.Y ?? double.NaN;
        _z.Value = imu?.Acceleration.Z ?? double.NaN;
        _pitch.Value = imu?.AbsolutePitchDegrees ?? double.NaN;
        _roll.Value = imu?.AbsoluteRollDegrees ?? double.NaN;
        _yaw.Value = imu?.AbsoluteYawDegrees ?? double.NaN;
    }

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_initialized)
        {
            UpdateValueFont();
        }
    }

    private void UpdateValueFont()
    {
        Font? previous = _valueFont;
        _valueFont = WarpToolkit.WinForms.Sensors.DirectX.SensorTelemetryAppearance.CreateFont(Font);

        foreach (AnimatedValueLabel label in Values)
        {
            label.Font = _valueFont;
            label.MinimumSize = Size.Empty;
            label.MinimumSize = label.GetPreferredSize(Size.Empty);
        }

        _source.Font = _yawAccuracy.Font = _valueFont;
        MinimumSize = new Size(Math.Max(Values.Max(label => label.PreferredSize.Width),
            Math.Max(_source.PreferredWidth, _yawAccuracy.PreferredWidth)) + 32, 0);
        previous?.Dispose();
        PerformLayout();
    }

    /// <inheritdoc/>
    protected override void OnDpiChangedAfterParent(EventArgs e)
    {
        base.OnDpiChangedAfterParent(e);
        UpdateValueFont();
    }

    private void OnDisposed(object? sender, EventArgs e)
        => _valueFont?.Dispose();
}
