using System.Text;
using System.Text.Json;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <content>Automatic acceleration intervals, separated from fusion and presentation cadence.</content>
public partial class MainForm
{
    private readonly object _performanceGate = new();
    private readonly AccelerationRunTimer _performanceTimer = new();
    private GeolocationPayload? _performanceGps;
    private Exception? _performanceStorageFault;

    private void ObservePerformanceTiming(FusionObservation observation)
    {
        lock (_performanceGate)
        {
            SensorEnvelope input = observation.Input;
            if (_obdReplayActive || _replayPlayer is not null || _paused || _shutdownStarted
                || input.Kind is SensorDataKind.Gap or SensorDataKind.SourceTransition)
            {
                _performanceTimer.Reset();
                _performanceGps = null;

                return;
            }

            if (input.Geolocation is { } fix)
            {
                _performanceGps = fix.FixQuality is <= 0 ? null : fix;
            }

            SensorGeoPoint? position = _performanceGps is { } gpsPosition
                ? new(gpsPosition.CoordinateTimestamp, gpsPosition.LatitudeDegrees,
                    gpsPosition.LongitudeDegrees, gpsPosition.AltitudeMeters)
                : null;
            SpeedUnit unit = Volatile.Read(ref _speedDisplayMode) >= 3
                ? SpeedUnit.MilesPerHour : SpeedUnit.KilometersPerHour;
            VehicleSpeedMeasurement? obd = _obdSource?.Latest.Speed;
            bool useObd = !PreferGpsInstrument && obd?.IsFreshAt(input.ArrivalTimestamp) == true;
            if (useObd)
            {
                _performanceTimer.Observe(new(obd!.Timestamp, obd.MetersPerSecond,
                    AccelerationSpeedSource.Obd, unit, position));
            }
            else if (_performanceGps is { SpeedValid: true } gps
                && input.ArrivalTimestamp >= gps.CoordinateTimestamp
                && input.ArrivalTimestamp - gps.CoordinateTimestamp <= TimeSpan.FromSeconds(1.5))
            {
                _performanceTimer.Observe(new(gps.CoordinateTimestamp, gps.SpeedMetersPerSecond,
                    AccelerationSpeedSource.Gps, unit, position));
            }
            else
            {
                _performanceTimer.Reset();
            }

            if (observation.Estimate is { IsAccelerationSupported: true } estimate)
            {
                _performanceTimer.ObserveAcceleration(estimate.Timestamp, estimate.LongitudinalAccel);
            }
        }
    }

    private void ResetPerformanceTiming()
    {
        lock (_performanceGate)
        {
            _performanceTimer.Reset();
            _performanceGps = null;
        }

        _speedometer.SetRunDisplay(null);
    }

    private void PresentPerformanceTiming()
    {
        AccelerationRunDisplay? display;
        AccelerationRunResult? result;
        lock (_performanceGate)
        {
            display = _paused || _obdReplayActive ? null : _performanceTimer.GetDisplay(DateTimeOffset.UtcNow);
            result = _performanceTimer.PendingResult;
        }

        _speedometer.SetRunDisplay(display is null ? null
            : new(RunCaption(display.Kind), display.Elapsed, display.Running));
        if (_performanceStorageFault is not null)
        {
            return;
        }

        while (result is not null)
        {
            try
            {
                string folder = _options.RecordingRootFolder;
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "acceleration-runs.jsonl");
                File.AppendAllText(path, JsonSerializer.Serialize(result) + Environment.NewLine, Encoding.UTF8);
                _log.Information($"{RunCaption(result.Kind)}: {result.Elapsed.TotalSeconds:0.00} s; "
                    + $"{result.Source}; {result.FinishedAt:O}; saved to {path}.");
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                _performanceStorageFault = exception;
                ShowError("Acceleration result could not be saved; result remains pending", exception);

                return;
            }

            lock (_performanceGate)
            {
                _performanceTimer.AcknowledgeResult();
                result = _performanceTimer.PendingResult;
            }
        }
    }

    private static string RunCaption(AccelerationRunKind kind)
        => kind switch
        {
            AccelerationRunKind.ZeroToSixtyMph => "0–60 mph",
            AccelerationRunKind.ZeroToHundredKph => "0–100 km/h",
            AccelerationRunKind.EightyToOneTwentyKph => "80–120 km/h",
            AccelerationRunKind.QuarterMile => "1/4 mile",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
}
