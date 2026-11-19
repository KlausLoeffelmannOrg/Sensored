using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Sensored;
using WarpToolkit.Windows.Bluetooth;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;

namespace Sensored.Tests;

/// <summary>Exercises paired discovery, reconnect, cancellation and one-shot terminal release using fake BLE transport.</summary>
public sealed class SensoredObdSourceTests
{
    [Fact]
    public async Task SelectedAd20IsPassedToTheSharedConnection()
    {
        Devices devices = new() { DeviceName = "AD20-13303" };
        await using ObdTelemetrySource source = new(devices, devices, devices, adapter: ObdAdapterKind.Ad20);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d);
        Assert.Equal("AD20-13303", source.Latest.DeviceName);
        Assert.Equal(1, devices.WatchCount);
        Assert.Single(devices.Transports);
    }

    [Fact]
    public async Task CancellingDiscoveryDoesNotCreateATransportAndDisposalIsIdempotent()
    {
        Devices devices = new() { DelayDiscovery = true };
        ObdTelemetrySource source = new(devices, devices, devices);
        await Task.WhenAll(source.DisposeAsync().AsTask(), source.DisposeAsync().AsTask());
        Assert.Empty(devices.Transports);
        Assert.Null(source.Fault);
    }

    [Fact]
    public async Task PairedSourceReconnectsAfterUnavailableSpeedWithoutDisablingPreference()
    {
        Devices devices = new();
        await using ObdTelemetrySource source = new(devices, devices, devices);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d);
        Assert.True(devices.LastOptions?.IncludeUnpaired == false);
        Assert.True(devices.LastOptions?.IncludeClassic == false);
        Transport first = Assert.Single(devices.Transports);
        first.StopReplies = true;
        await WaitUntilAsync(() => !source.Latest.Connected);
        Assert.True(source.Latest.Enabled);
        Assert.True(source.Latest.ModeEstablished);
        Assert.Null(source.Latest.Speed);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d && devices.Transports.Count == 2);
        Assert.Equal(10d, source.Latest.Speed!.MetersPerSecond);
        Assert.Equal(1, first.DisposeCount);
        Assert.Equal(1, devices.WatchCount);
    }

    [Fact]
    public async Task TerminalReleaseFailureIsRetainedAndCannotReconnectOnRepeatedDispose()
    {
        Devices devices = new();
        ObdTelemetrySource source = new(devices, devices, devices);
        await WaitUntilAsync(() => source.Latest.Speed is not null);
        Transport transport = Assert.Single(devices.Transports);
        transport.FailDispose = true;
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        Assert.IsType<IOException>(source.Fault);
        Assert.Single(devices.Transports);
        Assert.Equal(1, transport.DisposeCount);
    }

    [Fact]
    public async Task SearchWindowExpiryStopsDiscoveryWithoutCreatingATransport()
    {
        Devices devices = new() { DelayDiscovery = true };
        await using ObdTelemetrySource source = new(devices, devices, devices,
            searchDuration: TimeSpan.FromMilliseconds(80));
        await WaitUntilAsync(() => source.Latest.Status.Contains("window elapsed", StringComparison.Ordinal));
        Assert.False(source.Latest.Searching);
        Assert.False(source.Latest.Connected);
        Assert.Null(source.Latest.Speed);
        Assert.Empty(devices.Transports);
        Assert.Null(source.Fault);
    }

    [Fact]
    public void SearchWindowCannotExceedFifteenMinutesOrBeNonpositive()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObdTelemetrySource(searchDuration: TimeSpan.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObdTelemetrySource(searchDuration: TimeSpan.FromMinutes(16)));
    }

    [Fact]
    public async Task EstablishedConnectionReconnectsToTheSameAdapterAfterInitialSearchExpiry()
    {
        Devices devices = new();
        TimeSpan window = TimeSpan.FromSeconds(3);
        await using ObdTelemetrySource source = new(devices, devices, devices, searchDuration: window);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d);
        await Task.Delay(window, TestContext.Current.CancellationToken);
        Assert.True(source.Latest.Connected);
        Assert.NotNull(source.Latest.Speed);
        Transport transport = Assert.Single(devices.Transports);
        transport.StopReplies = true;
        await WaitUntilAsync(() => !source.Latest.Connected);
        Assert.True(source.Latest.Searching);
        Assert.True(source.Latest.ModeEstablished);
        Assert.Null(source.Latest.Speed);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d && devices.Transports.Count == 2);
        Assert.Equal(1, devices.WatchCount);
        Assert.Equal(1, transport.DisposeCount);
        Assert.Null(source.Fault);
    }

    [Fact]
    public async Task NormalReplyLatencyDoesNotForceReconnectOrRepeatDiscovery()
    {
        Devices devices = new() { SpeedReplyDelay = TimeSpan.FromMilliseconds(350) };
        await using ObdTelemetrySource source = new(devices, devices, devices);
        await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d);
        await Task.Delay(1200, TestContext.Current.CancellationToken);
        Assert.True(source.Latest.Connected);
        Assert.True(source.Latest.ModeEstablished);
        Assert.Equal(1, devices.WatchCount);
        Assert.Single(devices.Transports);
    }

    [Fact]
    public async Task LatePairedDeviceBecomesAuthoritativeDuringAnOpenRecordingAndReplaysTheSwitch()
    {
        TaskCompletionSource discovered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Devices devices = new() { DiscoveryGate = discovered.Task };
        await using ObdTelemetrySource source = new(devices, devices, devices);
        await using ObdRecordingCompanion journal = new();
        IFusionEngine engine = FusionEngineFactory.CreateDefault(
            allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);
        ITabletFrameController mounting = Assert.IsAssignableFrom<ITabletFrameController>(engine);
        IVehicleSpeedConsumer vehicle = Assert.IsAssignableFrom<IVehicleSpeedConsumer>(engine);
        mounting.BeginTabletFrameCapture();
        await WaitUntilAsync(() => devices.LastOptions is not null);
        Assert.True(source.Latest.Searching);
        string folder = Path.Combine(Path.GetTempPath(), $"Sensored-late-obd-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            SensorEnvelope Before(long sequence, DateTimeOffset time) => new()
            {
                Sequence = sequence,
                Kind = SensorDataKind.Imu,
                SourceTimestamp = time,
                ArrivalTimestamp = time,
                Imu = new(new(0, -1, 0), Vec3.Zero, true, double.NaN, 0,
                    double.NaN, double.NaN, double.NaN, DeviceYawSource.None)
            };

            SensorEnvelope first = Before(0, DateTimeOffset.UtcNow);
            journal.Publish(first, new(source.Latest, mounting.FrameStatus.Lock, mounting.FrameStatus.Phase));
            Assert.False(engine.Process(first)!.HasAuthoritativeSpeed);
            discovered.SetResult();
            await WaitUntilAsync(() => source.Latest.Speed?.MetersPerSecond == 10d && source.Latest.Hertz > 0);
            ObdTelemetrySnapshot connected = source.Latest;
            Assert.Equal(WellKnownBluetoothDevices.ObdLinkCx.Name, connected.DeviceName);
            SensorEnvelope next = Before(1, DateTimeOffset.UtcNow);
            vehicle.SetVehicleSpeed(connected.Speed);
            journal.Publish(next, new(connected, mounting.FrameStatus.Lock, mounting.FrameStatus.Phase));
            FusionEstimate live = engine.Process(next)!;
            Assert.True(live.HasAuthoritativeSpeed);
            Assert.Equal(10d, live.Speed);
            await journal.ExportAsync(folder, TestContext.Current.CancellationToken);
            using ObdReplayCursor replay = Assert.IsType<ObdReplayCursor>(ObdReplayCursor.Open(folder));
            IFusionEngine replayEngine = FusionEngineFactory.CreateDefault(
                allowGpsFallbackForSelectedObd: true, usePositionDerivedCourse: true);
            ObdRecordedInput unavailable = Assert.IsType<ObdRecordedInput>(replay.ReadNext());
            unavailable.State.Apply(replayEngine);
            Assert.False(replayEngine.Process(unavailable.Input)!.HasAuthoritativeSpeed);
            ObdRecordedInput available = Assert.IsType<ObdRecordedInput>(replay.ReadNext());
            available.State.Apply(replayEngine);
            Assert.True(replayEngine.Process(available.Input)!.HasAuthoritativeSpeed);
            Assert.Equal(connected.DeviceName, available.State.Telemetry.DeviceName);
            Assert.Equal(connected.Hertz, available.State.Telemetry.Hertz);
            Assert.Null(replay.ReadNext());
        }
        finally
        {
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName));
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName + ".writing"));
            Directory.Delete(folder);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10d));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>Supplies a selected paired adapter with the required GATT services.</summary>
    private sealed class Devices : IBluetoothDeviceScanner, IBluetoothDeviceInspector, IBluetoothTransportFactory
    {
        internal bool DelayDiscovery { get; init; }
        internal string DeviceName { get; init; } = "OBDLink CX";
        internal Task? DiscoveryGate { get; init; }
        internal TimeSpan SpeedReplyDelay { get; init; }
        private int _watchCount;
        internal int WatchCount => Volatile.Read(ref _watchCount);
        internal BluetoothScanOptions? LastOptions { get; private set; }
        internal System.Collections.Concurrent.ConcurrentQueue<Transport> Transports { get; } = new();

        public Task<IReadOnlyList<BluetoothDeviceInfo>> ScanAsync(BluetoothScanOptions options, CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<BluetoothDeviceInfo> WatchAsync(BluetoothScanOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _watchCount);
            LastOptions = options;
            if (DiscoveryGate is { } gate)
            {
                await gate.WaitAsync(cancellationToken);
            }

            if (DelayDiscovery)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            yield return new(DeviceName, DeviceName, "00:00:00:00:00:01", BluetoothDeviceKind.LowEnergy, true, null, []);
        }

        public Task<BluetoothDeviceProfile> InspectAsync(BluetoothDeviceInfo device, CancellationToken cancellationToken)
        {
            WellKnownBluetoothDevice known = WellKnownBluetoothDevices.ObdLinkCx;
            BluetoothGattServiceInfo service = new(known.ServiceUuid,
                [new(known.WriteCharacteristicUuid, BluetoothGattCharacteristicProperties.Write),
                 new(known.NotifyCharacteristicUuid, BluetoothGattCharacteristicProperties.Notify)], true);

            return Task.FromResult(new BluetoothDeviceProfile(device, [], [service], BluetoothTransportVerdict.Unknown));
        }

        public IBluetoothTransport Create(BluetoothDeviceProfile profile)
        {
            Transport transport = new() { SpeedReplyDelay = SpeedReplyDelay };
            Transports.Enqueue(transport);

            return transport;
        }
    }

    /// <summary>Serializes ELM replies and makes missing replies/release faults observable.</summary>
    private sealed class Transport : BluetoothTransportBase
    {
        private readonly Channel<ReadOnlyMemory<byte>> _receive = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        internal volatile bool StopReplies;
        internal bool FailDispose { get; set; }
        internal TimeSpan SpeedReplyDelay { get; init; }
        internal int DisposeCount { get; private set; }

        public override Task ConnectAsync(CancellationToken cancellationToken)
        {
            TransitionTo(BluetoothConnectionState.Connected);

            return Task.CompletedTask;
        }

        public override async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string command = Encoding.ASCII.GetString(data.Span).Trim();
            if (StopReplies && command == "010D")
            {
                return;
            }

            if (command == "010D" && SpeedReplyDelay > TimeSpan.Zero)
            {
                await Task.Delay(SpeedReplyDelay, cancellationToken);
            }

            string response = command switch
            {
                "ATI" => "ELM327 v2.2",
                "STDI" => "OBDLink CX test",
                "0100" => "7E8 06 41 00 00 08 00 00",
                "0120" or "0140" => "NO DATA",
                "010D" => "7E8 03 41 0D 24",
                _ => "OK"
            };
            if (!_receive.Writer.TryWrite(Encoding.ASCII.GetBytes(response + "\r>")))
            {
                throw new IOException("Test transport is closed.");
            }
        }

        public override IAsyncEnumerable<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
            => _receive.Reader.ReadAllAsync(cancellationToken);

        public override ValueTask DisposeAsync()
        {
            DisposeCount++;
            _receive.Writer.TryComplete();
            TransitionTo(BluetoothConnectionState.Disconnected);

            return FailDispose ? ValueTask.FromException(new IOException("Release failed.")) : ValueTask.CompletedTask;
        }
    }
}
