using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Contains detached OBD measurements and connection state at a fusion input boundary.</summary>
internal sealed record ObdTelemetrySnapshot(bool Enabled, bool Connected, ObdReadings Readings, string Status)
{
    /// <summary>Gets the selected device's advertised name.</summary>
    public string? DeviceName { get; init; }

    /// <summary>Gets the measured valid speed-report cadence, not the polling target.</summary>
    public double Hertz { get; init; }

    /// <summary>Gets whether initial discovery or reconnect to the established adapter is pending.</summary>
    public bool Searching { get; init; }

    /// <summary>Gets whether the selected adapter was established before a temporary outage.</summary>
    public bool HasConnected { get; init; }

    /// <summary>Gets whether the OBD dashboard stays established, independently of fresh telemetry.</summary>
    internal bool ModeEstablished => Enabled && (Connected || HasConnected);

    internal static ObdTelemetrySnapshot Disabled { get; } = new(false, false,
        new(ObdMetric.Pending, ObdMetric.Pending, ObdMetric.Pending, ObdMetric.Pending), "OBD2 off");

    internal VehicleSpeedMeasurement? Speed
        => Enabled && Connected && Readings.Speed is
        { Status: ObdReadingStatus.Valid, Value: { } value, Timestamp: { } time }
            ? new(value / 3.6d, time) : null;
}

/// <summary>Owns bounded selected-adapter discovery, persistent targeted reconnect, and terminal cleanup.</summary>
internal sealed class ObdTelemetrySource : IAsyncDisposable
{
    private readonly ObdLinkConnection _connection;
    private ObdTelemetrySnapshot _latest = ObdTelemetrySnapshot.Disabled with { Enabled = true, Status = "OBD2 searching" };

    internal ObdTelemetrySource(IBluetoothDeviceScanner? scanner = null,
        IBluetoothDeviceInspector? inspector = null, IBluetoothTransportFactory? transports = null,
        TimeSpan? searchDuration = null, ObdAdapterKind adapter = ObdAdapterKind.ObdLinkCx,
        Action<ObdDiagnosticEntry>? rawTraffic = null, BluetoothDeviceInfo? selectedDevice = null)
    {
        _connection = new(searchDuration: searchDuration, scanner: scanner, inspector: inspector, transports: transports);
        _connection.RawTrafficReceived += rawTraffic;
        _connection.Changed += OnConnectionChanged;
        _connection.TelemetryReceived += readings =>
        {
            ObdTelemetrySnapshot snapshot = Latest with { Readings = readings };
            ReadingChanged?.Invoke(snapshot);
        };
        _connection.Start(adapter, selectedDevice);
    }

    internal ObdTelemetrySnapshot Latest => Volatile.Read(ref _latest);
    internal Exception? Fault => _connection.Fault;
    internal event Action<ObdTelemetrySnapshot>? ReadingChanged;

    private void OnConnectionChanged(ObdLinkConnectionSnapshot snapshot)
    {
        Volatile.Write(ref _latest, new(true, snapshot.Connected, snapshot.Readings, snapshot.Status)
        {
            DeviceName = snapshot.DeviceName,
            Hertz = snapshot.Statistics?.SamplesPerSecond ?? 0,
            Searching = snapshot.Searching,
            HasConnected = snapshot.HasConnected
        });
        ReadingChanged?.Invoke(Latest);
    }

    public ValueTask DisposeAsync()
        => _connection.DisposeAsync();
}
