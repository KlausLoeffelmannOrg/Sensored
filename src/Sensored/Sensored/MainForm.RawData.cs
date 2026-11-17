using Windows.Devices.Sensors;

namespace Sensored;

/// <summary>Reads optional live diagnostics without altering sensor report intervals or fusion input.</summary>
public partial class MainForm
{
    private Magnetometer? _rawMagnetometer;
    private Altimeter? _rawAltimeter;
    private bool _rawAuxiliaryDiscovered;
    private bool _rawAuxiliaryFaulted;

    private void ApplyAuxiliaryRawReadouts()
    {
        if (_obdReplayActive || _lease is null)
        {
            _gpsMotionTabHost.ApplyAuxiliaryReadings(double.NaN, double.NaN, double.NaN, double.NaN);

            return;
        }

        if (_rawAuxiliaryFaulted)
        {
            return;
        }

        try
        {
            if (!_rawAuxiliaryDiscovered)
            {
                _rawMagnetometer = Magnetometer.GetDefault();
                _rawAltimeter = Altimeter.GetDefault();
                _rawAuxiliaryDiscovered = true;
            }

            MagnetometerReading? magnetic = _rawMagnetometer?.GetCurrentReading();
            AltimeterReading? altitude = _rawAltimeter?.GetCurrentReading();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            bool magneticFresh = magnetic is not null
                && (now - magnetic.Timestamp).Duration() <= TimeSpan.FromSeconds(2);
            _gpsMotionTabHost.ApplyAuxiliaryReadings(
                magneticFresh ? magnetic!.MagneticFieldX : double.NaN,
                magneticFresh ? magnetic!.MagneticFieldY : double.NaN,
                magneticFresh ? magnetic!.MagneticFieldZ : double.NaN,
                altitude is not null && (now - altitude.Timestamp).Duration() <= TimeSpan.FromSeconds(2)
                    ? altitude.AltitudeChangeInMeters : double.NaN);
        }
        catch (Exception exception)
        {
            _rawAuxiliaryFaulted = true;
            _gpsMotionTabHost.ApplyAuxiliaryReadings(double.NaN, double.NaN, double.NaN, double.NaN);
            ShowError("Raw magnetometer / altimeter diagnostics failed", exception);
        }
    }
}
