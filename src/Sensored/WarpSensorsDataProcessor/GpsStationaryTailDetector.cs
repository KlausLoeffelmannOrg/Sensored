using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Detects a final GPS run whose fixes remain within a small jitter radius.
/// </summary>
internal sealed class GpsStationaryTailDetector
{
    internal const double StationaryRadiusMeters = 15d;
    internal static readonly TimeSpan RetainedStationaryDuration = TimeSpan.FromMinutes(1);
    private const double EarthRadiusMeters = 6_371_008.8d;

    private double _anchorLatitude;
    private double _anchorLongitude;
    private DateTimeOffset _stationaryStart;
    private DateTimeOffset _lastTimestamp;
    private int _fixCount;

    /// <summary>
    ///  Observes one geolocation row.
    /// </summary>
    /// <param name="geolocation">The geolocation payload.</param>
    /// <param name="timestamp">The row source timestamp.</param>
    internal void Observe(GeolocationPayload geolocation, DateTimeOffset timestamp)
    {
        if (!AreValidCoordinates(
                geolocation.LatitudeDegrees,
                geolocation.LongitudeDegrees))
        {
            return;
        }

        if (_fixCount == 0
            || DistanceMeters(
                _anchorLatitude,
                _anchorLongitude,
                geolocation.LatitudeDegrees,
                geolocation.LongitudeDegrees) > StationaryRadiusMeters)
        {
            _anchorLatitude = geolocation.LatitudeDegrees;
            _anchorLongitude = geolocation.LongitudeDegrees;
            _stationaryStart = timestamp;
            _fixCount = 1;
        }
        else
        {
            _fixCount++;
        }

        _lastTimestamp = timestamp;
    }

    /// <summary>
    ///  Gets the cutoff timestamp when the final stationary run exceeds one minute.
    /// </summary>
    internal DateTimeOffset? GetCutoffTimestamp()
        => _fixCount >= 2
            && _lastTimestamp - _stationaryStart > RetainedStationaryDuration
                ? _stationaryStart + RetainedStationaryDuration
                : null;

    private static bool AreValidCoordinates(double latitude, double longitude)
        => double.IsFinite(latitude)
            && latitude is >= -90d and <= 90d
            && double.IsFinite(longitude)
            && longitude is >= -180d and <= 180d;

    private static double DistanceMeters(
        double firstLatitude,
        double firstLongitude,
        double secondLatitude,
        double secondLongitude)
    {
        double latitude1 = DegreesToRadians(firstLatitude);
        double latitude2 = DegreesToRadians(secondLatitude);
        double latitudeDelta = latitude2 - latitude1;
        double longitudeDelta = DegreesToRadians(secondLongitude - firstLongitude);
        double sinLatitude = Math.Sin(latitudeDelta / 2d);
        double sinLongitude = Math.Sin(longitudeDelta / 2d);
        double a = (sinLatitude * sinLatitude)
            + (Math.Cos(latitude1) * Math.Cos(latitude2) * sinLongitude * sinLongitude);
        double centralAngle = 2d
            * Math.Atan2(Math.Sqrt(a), Math.Sqrt(Math.Max(0d, 1d - a)));

        return EarthRadiusMeters * centralAngle;
    }

    private static double DegreesToRadians(double degrees)
        => degrees * Math.PI / 180d;
}
