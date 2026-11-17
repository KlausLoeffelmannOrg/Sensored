using System.Globalization;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;

namespace Sensored;

/// <summary>Formats compact status fields without presenting unavailable readings as real values.</summary>
internal static class DashboardStatusText
{
    /// <summary>Formats lifecycle state with measured arrival rates and numbered cleanup work.</summary>
    internal static string Session(SensorServiceState state, SensorRateSnapshot rates, string task, int item)
        => state switch
        {
            SensorServiceState.Starting => "Starting up...",
            SensorServiceState.Running => FormattableString.Invariant(
                $"Running at {rates.ImuHertz:0.0} HZ (GPS: {rates.GpsHertz:0.0} HZ)"),
            SensorServiceState.Stopping => $"Stopping...({task} [{item}])",
            _ => "Stopped"
        };

    /// <summary>Formats raw platform attitude and raw GPS speed, independently of fused/OBD speed.</summary>
    internal static string Raw(RawImuPayload? imu, double gpsMetersPerSecond)
        => $"RAW: P{Angle(imu?.AbsolutePitchDegrees)} R{Angle(imu?.AbsoluteRollDegrees)} "
            + $"Y{Angle(imu?.AbsoluteYawDegrees)} KM/H "
            + (double.IsFinite(gpsMetersPerSecond) && gpsMetersPerSecond >= 0
                ? (gpsMetersPerSecond * 3.6d).ToString("000", CultureInfo.InvariantCulture)
                : "---");

    private static string Angle(double? value)
        => value is { } number && double.IsFinite(number)
            ? (Math.Round(number, 2) == 0 ? 0 : number).ToString("+0.00;-0.00;+0.00", CultureInfo.InvariantCulture)
            : "--";
}
