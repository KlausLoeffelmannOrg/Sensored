using System.Diagnostics;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Owns selected or automatic WT901 discovery, bounded reconnect and awaited terminal release.</summary>
internal sealed class BluetoothSensorSource : IAsyncDisposable
{
    private readonly IBluetoothDeviceScanner _scanner;
    private readonly IBluetoothDeviceInspector _inspector;
    private readonly IBluetoothTransportFactory _transports;
    private readonly TimeSpan _searchDuration;
    private readonly BluetoothDeviceInfo? _selectedDevice;
    private readonly CancellationTokenSource _stop = new();
    private readonly object _gate = new();
    private readonly Task _worker;
    private Task? _dispose;
    private BluetoothSensorSnapshot _latest = new(false, true, "Bluetooth sensors: searching for paired WT901BLE67");

    internal BluetoothSensorSource(TimeSpan searchDuration,
        IBluetoothDeviceScanner? scanner = null, IBluetoothDeviceInspector? inspector = null,
        IBluetoothTransportFactory? transports = null, BluetoothDeviceInfo? selectedDevice = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(searchDuration, TimeSpan.Zero);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(searchDuration, TimeSpan.FromMinutes(15));
        _searchDuration = searchDuration;
        _selectedDevice = selectedDevice;
        if (selectedDevice is not null)
        {
            _latest = new(false, true, $"Bluetooth sensors: connecting to {selectedDevice.Name}");
        }

        _scanner = scanner ?? new BluetoothDeviceScanner();
        _inspector = inspector ?? new BluetoothDeviceInspector();
        _transports = transports ?? new BluetoothTransportFactory();
        _worker = Task.Run(RunAsync);
    }

    internal BluetoothSensorSnapshot Latest => Volatile.Read(ref _latest);
    internal Exception? Fault => _worker.Exception?.GetBaseException();
    internal bool IsCompleted => _worker.IsCompleted;

    private async Task RunAsync()
    {
        long searchStarted = Stopwatch.GetTimestamp();
        BluetoothDeviceInfo? pairedDevice = _selectedDevice;
        BluetoothDeviceInfo? establishedDevice = null;
        HashSet<string> rejectedDevices = [];
        bool failed = false;
        try
        {
            while (!_stop.IsCancellationRequested)
            {
                TimeSpan remaining = _searchDuration - Stopwatch.GetElapsedTime(searchStarted);
                if (remaining <= TimeSpan.Zero)
                {
                    Publish(new(false, false, $"Bluetooth sensors: search window elapsed. Last status: {Latest.Status}"));

                    return;
                }

                Wt901Session? session = null;
                bool established = false;
                try
                {
                    using CancellationTokenSource attempt = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
                    attempt.CancelAfter(remaining < TimeSpan.FromSeconds(20) ? remaining : TimeSpan.FromSeconds(20));
                    if (pairedDevice is null)
                    {
                        await foreach (BluetoothDeviceInfo device in _scanner.WatchAsync(new BluetoothScanOptions
                        {
                            IncludeClassic = false,
                            IncludeUnpaired = false
                        }, attempt.Token).ConfigureAwait(false))
                        {
                            if (device.IsPaired && !rejectedDevices.Contains(device.Id)
                                && WellKnownBluetoothDevices.Wt901Ble67.Matches(device))
                            {
                                pairedDevice = device;
                                break;
                            }
                        }
                    }

                    if (pairedDevice is null)
                    {
                        throw new BluetoothDeviceUnreachableException("No paired WT901BLE67 was found.");
                    }

                    Publish(new(false, true, $"Bluetooth sensors: connecting to {pairedDevice.Name}"));
                    BluetoothDeviceProfile profile = await _inspector.InspectAsync(pairedDevice, attempt.Token)
                        .ConfigureAwait(false);
                    session = new(_transports.Create(WellKnownBluetoothDevices.Wt901Ble67.SelectProfile(profile)));
                    session.ReadingReceived += OnReading;
                    await session.StartAsync(attempt.Token).ConfigureAwait(false);
                    established = true;
                    establishedDevice = pairedDevice;
                    Publish(Latest with { Connected = true, Searching = false, Status = "Bluetooth sensors: connected" });
                    long connectedAt = Stopwatch.GetTimestamp();
                    while (session.IsConnected && !_stop.IsCancellationRequested)
                    {
                        if (session.Latest is { } reading
                            ? DateTimeOffset.UtcNow - reading.Timestamp > TimeSpan.FromSeconds(2)
                            : Stopwatch.GetElapsedTime(connectedAt) > TimeSpan.FromSeconds(2))
                        {
                            throw new BluetoothDeviceUnreachableException("WT901 stopped reporting sensor frames.");
                        }

                        await Task.Delay(50, _stop.Token).ConfigureAwait(false);
                    }

                    _stop.Token.ThrowIfCancellationRequested();
                    throw new BluetoothDeviceUnreachableException(
                        $"WT901 disconnected: {session.Error?.Message ?? "transport closed"}");
                }
                catch (OperationCanceledException) when (_stop.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception) when (exception is BluetoothException
                    or OperationCanceledException or TimeoutException)
                {
                    Trace.TraceWarning($"Bluetooth reference acquisition: {exception}");
                    Publish(new(false, true, $"Bluetooth sensors unavailable: {exception.Message}; retrying"));
                    if (establishedDevice is null)
                    {
                        if (pairedDevice is not null)
                        {
                            rejectedDevices.Add(pairedDevice.Id);
                        }
                        else
                        {
                            rejectedDevices.Clear();
                        }

                        pairedDevice = _selectedDevice;
                    }
                }
                finally
                {
                    if (session is not null)
                    {
                        session.ReadingReceived -= OnReading;
                        await session.DisposeAsync().ConfigureAwait(false);
                    }
                }

                if (established)
                {
                    searchStarted = Stopwatch.GetTimestamp();
                }

                remaining = _searchDuration - Stopwatch.GetElapsedTime(searchStarted);
                if (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining < TimeSpan.FromSeconds(1) ? remaining : TimeSpan.FromSeconds(1),
                        _stop.Token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (_stop.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            failed = true;
            Publish(new(false, false, $"Bluetooth sensors failed: {exception.Message}"));
            Trace.TraceError(exception.ToString());
            throw;
        }
        finally
        {
            Publish(Latest with
            {
                Connected = false,
                Searching = false,
                Reading = null,
                Status = _stop.IsCancellationRequested && !failed ? "Bluetooth sensors: stopped" : Latest.Status
            });
        }
    }

    private void OnReading(Wt901Reading reading)
        => Publish(new(true, false, "Bluetooth sensors: connected", reading));

    private void Publish(BluetoothSensorSnapshot snapshot)
        => Volatile.Write(ref _latest, snapshot);

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            return new(_dispose ??= DisposeCoreAsync());
        }
    }

    private async Task DisposeCoreAsync()
    {
        await _stop.CancelAsync().ConfigureAwait(false);
        try
        {
            await _worker.ConfigureAwait(false);
        }
        finally
        {
            _stop.Dispose();
        }
    }
}
