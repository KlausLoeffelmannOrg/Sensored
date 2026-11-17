using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Presentation;

namespace Sensored;

/// <summary>Records independent comparison evidence and its provenance at an internal fusion boundary.</summary>
/// <remarks>
///  Bluetooth values remain device-frame raw measurements plus a declared display reference.
///  They are not certified ground truth and never enter the fusion engine. Host reception time
///  is not a synchronized Bluetooth measurement clock. Speed acceleration reuses the odometer's
///  timestamp-based differentiator and is unavailable until four distinct OBD measurements exist.
/// </remarks>
internal sealed record SensorComparisonReference(
    SensorDisplaySource SelectedSource,
    BluetoothSensorSnapshot? Bluetooth,
    SensorReferenceFrame InternalFrame,
    SensorReferenceFrame BluetoothFrame,
    double? ObdSpeedMetersPerSecond,
    DateTimeOffset? ObdSpeedTimestamp,
    double? SpeedDerivedAccelerationMetersPerSecondSquared)
{
    internal const double StandardGravity = 9.80665d;
}

/// <summary>Reuses the speedometer's source-time derivative without accessing a UI control.</summary>
internal sealed class SensorReferenceSpeed
{
    private readonly SpeedometerModel _model = new();

    internal double? Observe(VehicleSpeedMeasurement? speed, DateTimeOffset at)
    {
        bool fresh = speed?.IsFreshAt(at) == true;
        _model.SetSpeed(fresh ? speed!.MetersPerSecond : double.NaN, SpeedUnit.MetersPerSecond,
            fresh ? speed!.Timestamp : at);
        double acceleration = _model.ForwardAccelerationG * SensorComparisonReference.StandardGravity;

        return fresh && double.IsFinite(acceleration) ? acceleration : null;
    }
}
