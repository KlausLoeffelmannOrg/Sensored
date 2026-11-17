using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Sensored;
using WarpToolkit.Windows.Bluetooth;

namespace FusionGauges.Tests;

/// <summary>Exercises bounded paired sensor discovery and terminal ownership without live hardware.</summary>
public sealed class SensoredBluetoothSourceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitSensorUsesItsIdentityWithoutPairedNameDiscovery(bool paired)
    {
        FakeHardware hardware = new();
        BluetoothDeviceInfo selected = new("chosen-sensor", "Renamed sensor", "00:11:22:33:44:66",
            BluetoothDeviceKind.LowEnergy, paired, null, []);
        await using BluetoothSensorSource source = new(TimeSpan.FromSeconds(3), hardware, hardware, hardware,
            selectedDevice: selected);
        await WaitUntilAsync(() => source.Latest.IsFreshAt(DateTimeOffset.UtcNow));
        Assert.Equal(0, hardware.Scans);
        Assert.Same(selected, hardware.InspectedDevice);
        Assert.Equal(1, source.Latest.Reading!.AccelerationZ);
        Assert.Single(hardware.Transports).Disconnect();
        await WaitUntilAsync(() => hardware.Transports.Count == 2 && source.Latest.Connected);
        Assert.Equal(0, hardware.Scans);
        Assert.Same(selected, hardware.InspectedDevice);
    }

    [Fact]
    public async Task ExplicitIncompatibleSensorReportsFailureAndNeverFallsBackToAnotherDevice()
    {
        FakeHardware hardware = new() { UnsupportedServices = true };
        BluetoothDeviceInfo selected = new("incompatible", "Other BLE device", "00:11:22:33:44:66",
            BluetoothDeviceKind.LowEnergy, false, null, []);
        await using BluetoothSensorSource source = new(TimeSpan.FromMilliseconds(100), hardware, hardware, hardware,
            selectedDevice: selected);
        await WaitUntilAsync(() => source.IsCompleted);
        Assert.False(source.Latest.Connected);
        Assert.Equal(0, hardware.Scans);
        Assert.Empty(hardware.Transports);
        Assert.Same(selected, hardware.InspectedDevice);
        Assert.Contains("WT901BLE67", source.Latest.Status);
    }

    [Fact]
    public async Task DiscoveryExpiresWithoutConnectingUnpairedDevices()
    {
        FakeHardware hardware = new() { Paired = false };
        await using BluetoothSensorSource source = new(TimeSpan.FromMilliseconds(100), hardware, hardware, hardware);
        await WaitUntilAsync(() => source.IsCompleted);
        Assert.False(source.Latest.Connected);
        Assert.False(source.Latest.Searching);
        Assert.Contains("elapsed", source.Latest.Status);
        Assert.Empty(hardware.Transports);
        Assert.Equal(0, hardware.Inspections);
        Assert.False(hardware.Options!.IncludeUnpaired);
        Assert.False(hardware.Options.IncludeClassic);
    }

    [Fact]
    public async Task CancelDuringDiscoveryAwaitsWatchAndIsIdempotent()
    {
        FakeHardware hardware = new() { Paired = false };
        BluetoothSensorSource source = new(TimeSpan.FromMinutes(3), hardware, hardware, hardware);
        await WaitUntilAsync(() => hardware.Scans == 1);
        await Task.WhenAll(source.DisposeAsync().AsTask(), source.DisposeAsync().AsTask());
        Assert.True(hardware.WatchEnded);
        Assert.True(source.IsCompleted);
        Assert.False(source.Latest.Searching);
        Assert.Empty(hardware.Transports);
    }

    [Fact]
    public async Task EstablishedReferenceInvalidatesOnDisconnectAndReconnectsOnlyTheSameDevice()
    {
        FakeHardware hardware = new();
        await using BluetoothSensorSource source = new(TimeSpan.FromSeconds(3), hardware, hardware, hardware);
        await WaitUntilAsync(() => source.Latest.IsFreshAt(DateTimeOffset.UtcNow));
        Assert.Equal(1, source.Latest.Reading!.AccelerationZ);
        FakeTransport first = Assert.Single(hardware.Transports);
        first.Disconnect();
        await WaitUntilAsync(() => !source.Latest.Connected);
        Assert.False(source.Latest.IsFreshAt(DateTimeOffset.UtcNow));
        Assert.Null(source.Latest.Reading);
        await WaitUntilAsync(() => hardware.Transports.Count == 2 && source.Latest.Connected);
        Assert.Equal(1, hardware.Scans);
        Assert.Equal(2, hardware.Inspections);
        await source.DisposeAsync();
        Assert.All(hardware.Transports, transport => Assert.Equal(1, transport.Releases));
        Assert.Null(source.Latest.Reading);
    }

    [Fact]
    public async Task FailedReleaseRemainsTerminalAndCannotCreateAnotherSession()
    {
        FakeHardware hardware = new() { FailRelease = true };
        BluetoothSensorSource source = new(TimeSpan.FromSeconds(3), hardware, hardware, hardware);
        await WaitUntilAsync(() => source.Latest.Connected);
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        Assert.IsType<IOException>(source.Fault);
        Assert.Contains("failed", source.Latest.Status);
        Assert.False(source.Latest.Connected);
        Assert.Equal(1, Assert.Single(hardware.Transports).Releases);
    }

    private static async Task WaitUntilAsync(Func<bool> predicate)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        while (!predicate())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>Supplies paired discovery and inspected protocol endpoints deterministically.</summary>
    private sealed class FakeHardware : IBluetoothDeviceScanner, IBluetoothDeviceInspector, IBluetoothTransportFactory
    {
        internal bool Paired { get; init; } = true;
        internal bool FailRelease { get; init; }
        internal bool UnsupportedServices { get; init; }
        internal BluetoothDeviceInfo? InspectedDevice;
        internal int Scans;
        internal int Inspections;
        internal bool WatchEnded;
        internal BluetoothScanOptions? Options;
        internal ConcurrentQueue<FakeTransport> Transports { get; } = new();

        public Task<IReadOnlyList<BluetoothDeviceInfo>> ScanAsync(BluetoothScanOptions options,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<BluetoothDeviceInfo> WatchAsync(BluetoothScanOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Scans);
            Options = options;
            try
            {
                yield return new("paired-wt901", "WT901BLE67", "00:11:22:33:44:55",
                    BluetoothDeviceKind.LowEnergy, Paired, null, []);
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            finally
            {
                WatchEnded = true;
            }
        }

        public Task<BluetoothDeviceProfile> InspectAsync(BluetoothDeviceInfo device, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref Inspections);
            InspectedDevice = device;
            WellKnownBluetoothDevice known = WellKnownBluetoothDevices.Wt901Ble67;

            return Task.FromResult(new BluetoothDeviceProfile(device, [],
                UnsupportedServices ? [] : [new(known.ServiceUuid,
                    [new(known.WriteCharacteristicUuid, BluetoothGattCharacteristicProperties.Write),
                        new(known.NotifyCharacteristicUuid, BluetoothGattCharacteristicProperties.Notify)], true)],
                BluetoothTransportVerdict.LowEnergyUartLike));
        }

        public IBluetoothTransport Create(BluetoothDeviceProfile profile)
        {
            FakeTransport transport = new() { FailRelease = FailRelease };
            Transports.Enqueue(transport);

            return transport;
        }
    }

    /// <summary>Emits a confirmed volatile rate and a complete standard sensor frame.</summary>
    private sealed class FakeTransport : BluetoothTransportBase
    {
        private readonly Channel<ReadOnlyMemory<byte>> _received = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        internal bool FailRelease { get; init; }
        internal int Releases;

        public override Task ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TransitionTo(BluetoothConnectionState.Connected);

            return Task.CompletedTask;
        }

        public override Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (data.Span[2] == 0x27)
            {
                byte[] rate = new byte[20];
                rate[0] = 0x55;
                rate[1] = 0x71;
                rate[2] = 3;
                rate[4] = 8;
                byte[] sample = new byte[20];
                sample[0] = 0x55;
                sample[1] = 0x61;
                sample[7] = 8;
                _received.Writer.TryWrite(rate);
                _received.Writer.TryWrite(sample);
            }

            return Task.CompletedTask;
        }

        public override IAsyncEnumerable<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
            => ReadChannelAsync(_received.Reader, cancellationToken);

        internal void Disconnect()
        {
            TransitionTo(BluetoothConnectionState.Disconnected);
            _received.Writer.TryComplete();
        }

        public override ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref Releases);
            Disconnect();

            return FailRelease ? ValueTask.FromException(new IOException("Fake transport release failed"))
                : ValueTask.CompletedTask;
        }
    }
}
