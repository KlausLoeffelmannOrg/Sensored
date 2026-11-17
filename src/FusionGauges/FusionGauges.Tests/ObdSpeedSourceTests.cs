using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Bluetooth;
using WinFormsFusionGauges.WinFormsApp;

namespace FusionGauges.Tests;

/// <summary>Exercises background discovery, source selection, trip integration and terminal cleanup without hardware.</summary>
public sealed class ObdSpeedSourceTests
{
    [Theory]
    [InlineData("ELM327 v2.2")]
    [InlineData("AD20 v1.0")]
    public async Task SelectedAd20SkipsCxAndUsesStandardElmSetupWithoutStnIdentity(string version)
    {
        FakeBluetooth hardware = new()
        {
            DeviceName = "AD20-13303", PrecedingDeviceName = "OBDLink CX",
            CreateFreshTransports = true, Version = version
        };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start(ObdAdapterKind.Ad20);
        await WaitAsync(() => source.Statistics?.Samples >= 2);
        FakeBluetooth first = Assert.Single(hardware.Transports);
        Assert.Contains("ATI", first.Commands);
        Assert.Contains("ATTP0", first.Commands);
        Assert.DoesNotContain("STDI", first.Commands);
        Assert.Equal(["AD20-13303"], hardware.InspectedNames);
        Assert.Equal(20, source.Latest.MetersPerSecond);
        first.Disconnect();
        await WaitAsync(() => hardware.Transports.Count == 2 && source.Statistics?.Samples >= 2, timeoutSeconds: 12);
        Assert.Equal(1, hardware.Scans);
        Assert.Equal(["AD20-13303", "AD20-13303"], hardware.InspectedNames);
        Assert.Throws<InvalidOperationException>(() => source.Start(ObdAdapterKind.ObdLinkCx));
    }

    [Theory]
    [InlineData("OBDLink CX", true)]
    [InlineData("AD20-13303", false)]
    public async Task Ad20SelectionNeverFallsBackToCxOrAnUnpairedDevice(string name, bool paired)
    {
        FakeBluetooth hardware = new() { DeviceName = name, Paired = paired };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start(ObdAdapterKind.Ad20);
        await WaitAsync(() => source.Status.Contains("not found"));
        Assert.Equal(0, hardware.Inspections);
        Assert.Equal(0, hardware.Connections);
    }

    [Fact]
    public async Task CxStillRequiresItsStnIdentity()
    {
        FakeBluetooth hardware = new() { Identity = "?" };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start();
        await WaitAsync(() => source.Status.Contains("Expected an OBDLink CX"));
        Assert.Contains("STDI", hardware.Commands);
        Assert.True(double.IsNaN(source.Latest.MetersPerSecond));
    }

    [Fact]
    public async Task Ad20RejectsAnUnknownProtocolIdentityWithoutPublishingSpeed()
    {
        FakeBluetooth hardware = new() { DeviceName = "AD20-13303", Version = "?" };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start(ObdAdapterKind.Ad20);
        await WaitAsync(() => source.Status.Contains("Unrecognized OBD adapter identity"));
        Assert.DoesNotContain("STDI", hardware.Commands);
        Assert.DoesNotContain("010D", hardware.Commands);
        Assert.True(double.IsNaN(source.Latest.MetersPerSecond));
    }

    [Fact]
    public async Task StartupUsesPairedProfileAndRunsIndependentlyOfSensorStartAndUi()
    {
        FakeBluetooth hardware = new();
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        await using SensorHub hub = new(obd: source);
        hub.StartBluetooth();
        hub.StartBluetooth();
        await WaitAsync(() => source.Statistics?.Samples >= 10);
        Assert.False(hub.IsRunning);
        Assert.False(hub.UseObdSpeed);
        Assert.Null(hub.VehicleSpeed);
        Assert.Equal(1, hardware.Connections);
        Assert.Equal(1, hardware.Scans);
        Assert.NotNull(hardware.Options);
        Assert.False(hardware.Options.IncludeUnpaired);
        Assert.False(hardware.Options.IncludeClassic);
        hub.SelectObdSpeed(true);
        VehicleSpeedMeasurement reading = Assert.IsType<VehicleSpeedMeasurement>(hub.VehicleSpeed);
        Assert.Equal(20, reading.MetersPerSecond);
        Assert.True(reading.IsFreshAt(DateTimeOffset.UtcNow));
        Assert.True(reading.TripKilometers > 0);
        Assert.True(source.Statistics!.SamplesPerSecond >= 10);
        await WaitAsync(() => source.LatestTelemetry.Rpm.State == WarpToolkit.Sensors.SensorReadingState.Valid);
        Assert.Equal(2000, source.LatestTelemetry.Rpm.Value);
        hub.SelectObdSpeed(false);
        Assert.Null(hub.VehicleSpeed);
        Assert.True(source.Latest.IsFreshAt(DateTimeOffset.UtcNow));
        await hub.StopBluetoothAsync();
        await hub.StopBluetoothAsync();
        Assert.Equal(1, hardware.Releases);
        Assert.True(double.IsNaN(source.Latest.MetersPerSecond));
    }

    [Fact]
    public async Task UnpairedDiscoveryDoesNotInspectConnectOrPair()
    {
        FakeBluetooth hardware = new() { Paired = false };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start();
        await WaitAsync(() => source.Status.Contains("not found"));
        Assert.Equal(0, hardware.Inspections);
        Assert.Equal(0, hardware.Connections);
        Assert.True(double.IsNaN(source.Latest.MetersPerSecond));
    }

    [Fact]
    public async Task CloseDuringDiscoveryCancelsAndAwaitsWatch()
    {
        FakeBluetooth hardware = new() { WaitForCancellation = true };
        ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start();
        await WaitAsync(() => hardware.Scans == 1);
        await source.DisposeAsync();
        Assert.True(hardware.WatchEnded);
        Assert.Equal(0, hardware.Connections);
        await source.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(source.Start);
    }

    [Fact]
    public async Task RemoteDisconnectInvalidatesSpeedWithoutGpsFallback()
    {
        FakeBluetooth hardware = new();
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start();
        await WaitAsync(() => source.Statistics?.Samples >= 2);
        hardware.Disconnect();
        await WaitAsync(() => source.Status.Contains("unavailable"));
        Assert.True(double.IsNaN(source.Latest.MetersPerSecond));
        Assert.NotNull(source.Latest.TripKilometers);
        await source.DisposeAsync();
        Assert.Equal(1, hardware.Releases);
    }

    [Fact]
    public async Task FailedReleaseRemainsTerminalAndVisible()
    {
        FakeBluetooth hardware = new() { FailRelease = true };
        ObdSpeedSource source = new(hardware, hardware, hardware);
        source.Start();
        await WaitAsync(() => source.Statistics?.Samples >= 2);
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<IOException>(() => source.DisposeAsync().AsTask());
        Assert.Equal(1, hardware.Releases);
        Assert.Contains("cleanup failed", source.Status);
    }

    [Fact]
    public async Task InvestigationIsConsumedOnceAndDoesNotRestartOnReconnect()
    {
        FakeBluetooth hardware = new() { CreateFreshTransports = true };
        await using ObdSpeedSource source = new(hardware, hardware, hardware);
        ConcurrentQueue<ObdDiagnosticEntry> entries = new();
        source.DiagnosticReceived += entries.Enqueue;
        await source.SetMercedesModeAsync(true, TestContext.Current.CancellationToken);
        source.Start();
        await WaitAsync(() => entries.Any(entry => entry.Message.Contains("Standard OBD configuration restored")));
        FakeBluetooth first = Assert.Single(hardware.Transports);
        Assert.Equal(3, first.MonitorRequests);
        await source.SetMercedesModeAsync(true, TestContext.Current.CancellationToken);
        long samples = source.Statistics?.Samples ?? 0;
        await WaitAsync(() => source.Statistics?.Samples > samples + 2);
        Assert.Equal(3, first.MonitorRequests);
        first.Disconnect();
        await WaitAsync(() => hardware.Transports.Count == 2 && source.Statistics?.Samples >= 3, timeoutSeconds: 12);
        FakeBluetooth second = hardware.Transports.Last();
        Assert.Equal(0, second.MonitorRequests);
        Assert.Single(entries, entry => entry.Message.Contains("Read-only CAN capture queued"));
        await source.SetMercedesModeAsync(false, TestContext.Current.CancellationToken);
        await source.SetMercedesModeAsync(true, TestContext.Current.CancellationToken);
        await WaitAsync(() => second.MonitorRequests == 3 && entries.Count(entry =>
            entry.Message.Contains("Standard OBD configuration restored")) == 2);
    }

    private static async Task WaitAsync(Func<bool> predicate, int timeoutSeconds = 5)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(timeoutSeconds));
        while (!predicate())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>Provides discovery and a verified protocol stream while recording ownership operations.</summary>
    private sealed class FakeBluetooth : BluetoothTransportBase,
        IBluetoothDeviceScanner, IBluetoothDeviceInspector, IBluetoothTransportFactory
    {
        private readonly Channel<ReadOnlyMemory<byte>> _received = Channel.CreateUnbounded<ReadOnlyMemory<byte>>();
        internal bool Paired { get; init; } = true;
        internal bool WaitForCancellation { get; init; }
        internal bool FailRelease { get; init; }
        internal bool CreateFreshTransports { get; init; }
        internal string DeviceName { get; init; } = "OBDLink CX";
        internal string? PrecedingDeviceName { get; init; }
        internal string Identity { get; init; } = "OBDLink CX r1.7.1";
        internal string Version { get; init; } = "ELM327 v2.2";
        internal ConcurrentQueue<string> InspectedNames { get; } = new();
        internal ConcurrentQueue<string> Commands { get; } = new();
        internal ConcurrentQueue<FakeBluetooth> Transports { get; } = new();
        internal int MonitorRequests;
        internal bool WatchEnded { get; private set; }
        internal int Scans { get; private set; }
        internal int Inspections { get; private set; }
        internal int Connections { get; private set; }
        internal int Releases { get; private set; }
        internal BluetoothScanOptions? Options { get; private set; }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> ScanAsync(BluetoothScanOptions options, CancellationToken token)
            => throw new InvalidOperationException("Startup must use streaming discovery.");

        public async IAsyncEnumerable<BluetoothDeviceInfo> WatchAsync(BluetoothScanOptions options,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Options = options;
            Scans++;
            try
            {
                if (WaitForCancellation)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }

                if (PrecedingDeviceName is { } preceding)
                {
                    yield return new("other-obd", preceding, "00:00:00:00:00:02",
                        BluetoothDeviceKind.LowEnergy, true, null, []);
                }

                yield return new("fake-obd", DeviceName, "00:00:00:00:00:01",
                    BluetoothDeviceKind.LowEnergy, Paired, null, []);
            }
            finally
            {
                WatchEnded = true;
            }
        }

        public Task<BluetoothDeviceProfile> InspectAsync(BluetoothDeviceInfo device, CancellationToken cancellationToken)
        {
            Inspections++;
            InspectedNames.Enqueue(device.Name);
            WellKnownBluetoothDevice known = WellKnownBluetoothDevices.ObdLinkCx;

            return Task.FromResult(new BluetoothDeviceProfile(device, [],
                [new(known.ServiceUuid,
                    [new(known.WriteCharacteristicUuid, BluetoothGattCharacteristicProperties.WriteWithoutResponse),
                     new(known.NotifyCharacteristicUuid, BluetoothGattCharacteristicProperties.Notify)], true)],
                BluetoothTransportVerdict.LowEnergyUartLike));
        }

        public IBluetoothTransport Create(BluetoothDeviceProfile profile)
        {
            if (!CreateFreshTransports)
            {
                return this;
            }

            FakeBluetooth transport = new() { Version = Version };
            Transports.Enqueue(transport);

            return transport;
        }

        public override Task ConnectAsync(CancellationToken cancellationToken)
        {
            Connections++;
            TransitionTo(BluetoothConnectionState.Connected);

            return Task.CompletedTask;
        }

        public override Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string command = Encoding.ASCII.GetString(data.Span).Trim();
            Commands.Enqueue(command);
            if (command == "STM")
            {
                Interlocked.Increment(ref MonitorRequests);
            }

            string response = command switch
            {
                "ATI" => Version,
                "STDI" => Identity,
                "0100" => "7E8 06 41 00 08 18 00 00",
                "010D" => "7E8 03 41 0D 48",
                "010C" => "7E8 04 41 0C 1F 40",
                "0105" => "7E8 03 41 05 64",
                "STM" => "105 09 C4",
                _ => "OK"
            };
            _received.Writer.TryWrite(Encoding.ASCII.GetBytes(response + "\r>"));

            return Task.CompletedTask;
        }

        public override IAsyncEnumerable<ReadOnlyMemory<byte>> ReceiveAsync(CancellationToken cancellationToken)
            => _received.Reader.ReadAllAsync(cancellationToken);

        internal void Disconnect() => _received.Writer.TryComplete();

        public override ValueTask DisposeAsync()
        {
            Releases++;
            _received.Writer.TryComplete();
            TransitionTo(BluetoothConnectionState.Disconnected);

            return FailRelease ? ValueTask.FromException(new IOException("Release failed.")) : ValueTask.CompletedTask;
        }
    }
}
