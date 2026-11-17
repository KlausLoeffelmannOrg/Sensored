using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Adapts the library-owned paired OBD connection to speed and trip presentation.</summary>
internal sealed class ObdSpeedSource : IAsyncDisposable
{
    private readonly ObdLinkConnection _connection;
    private VehicleSpeedMeasurement _latest = VehicleSpeedMeasurement.Unavailable;
    private string _status = "OBD2: not started";
    private double _tripKilometers;

    internal ObdSpeedSource(IBluetoothDeviceScanner? scanner = null,
        IBluetoothDeviceInspector? inspector = null, IBluetoothTransportFactory? transports = null)
    {
        _connection = new(scanner: scanner, inspector: inspector, transports: transports);
        _connection.SpeedReceived += OnSpeedReceived;
        _connection.Changed += OnConnectionChanged;
        _connection.DiagnosticReceived += entry => DiagnosticReceived?.Invoke(entry);
        _connection.TelemetryReceived += readings =>
            TelemetryChanged?.Invoke(readings.ToSensorTelemetry(_connection.Latest.Connected, selectedForSpeed: false));
    }

    internal event Action<VehicleSpeedMeasurement>? ReadingChanged;
    internal event Action<VehicleTelemetryPayload>? TelemetryChanged;
    internal event Action<string>? StatusChanged;
    internal event Action<ObdDiagnosticEntry>? DiagnosticReceived;

    internal VehicleSpeedMeasurement Latest
        => _connection.Latest.Connected
            ? Volatile.Read(ref _latest) : Volatile.Read(ref _latest) with
            {
                MetersPerSecond = double.NaN,
                Timestamp = default
            };

    internal string Status => Volatile.Read(ref _status);
    internal ObdSpeedPollingStatistics? Statistics => _connection.Latest.Statistics;
    internal VehicleTelemetryPayload LatestTelemetry
        => _connection.Latest.Readings.ToSensorTelemetry(_connection.Latest.Connected, selectedForSpeed: false);

    internal void Start() => _connection.Start();
    internal void Start(ObdAdapterKind adapter) => _connection.Start(adapter);

    internal Task SetMercedesModeAsync(bool enabled, CancellationToken cancellationToken = default)
        => _connection.SetMercedesModeAsync(enabled, cancellationToken);

    private void OnConnectionChanged(ObdLinkConnectionSnapshot snapshot)
    {
        TelemetryChanged?.Invoke(snapshot.Readings.ToSensorTelemetry(snapshot.Connected, selectedForSpeed: false));

        if (!snapshot.Connected)
        {
            VehicleSpeedMeasurement unavailable = VehicleSpeedMeasurement.Unavailable with { TripKilometers = _tripKilometers };
            Volatile.Write(ref _latest, unavailable);
            ReadingChanged?.Invoke(unavailable);
        }

        if (Interlocked.Exchange(ref _status, snapshot.Status) != snapshot.Status)
        {
            StatusChanged?.Invoke(snapshot.Status);
        }
    }

    private void OnSpeedReceived(ObdMetric metric)
    {
        VehicleSpeedMeasurement measurement = metric is
            { Status: ObdReadingStatus.Valid, Value: { } value, Timestamp: { } timestamp }
            ? new(value / 3.6d, timestamp) : VehicleSpeedMeasurement.Unavailable;
        VehicleSpeedMeasurement previous = Volatile.Read(ref _latest);
        if (previous.IsFreshAt(measurement.Timestamp) && double.IsFinite(measurement.MetersPerSecond))
        {
            double seconds = (measurement.Timestamp - previous.Timestamp).TotalSeconds;
            _tripKilometers += (previous.MetersPerSecond + measurement.MetersPerSecond) * 0.5d * seconds / 1000d;
        }

        measurement = measurement with { TripKilometers = _tripKilometers };
        Volatile.Write(ref _latest, measurement);
        ReadingChanged?.Invoke(measurement);
    }

    public ValueTask DisposeAsync()
        => _connection.DisposeAsync();
}
