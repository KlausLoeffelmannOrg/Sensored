using Sensored;
using WarpToolkit.Sensors;

namespace FusionGauges.Tests;

public sealed class ResilientGpsSourceTests
{
    [Fact]
    public async Task HealthChangesWhileProbingDoNotDropTheReceiverItThenConnects()
    {
        int created = 0;
        FakeGpsSource primary = new() { ProbeHealthChanges = 3 };
        await using ResilientGpsSource source = new(_ => { created++; return primary; }, null,
            retryDelay: TimeSpan.FromMilliseconds(20));

        await source.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => source.Diagnostic.Contains("connected", StringComparison.Ordinal));
        await Task.Delay(200, TestContext.Current.CancellationToken);

        Assert.Equal(1, created);
        Assert.Same(primary, source.Primary);
        Assert.Equal(GpsAcquisitionStatus.Ready, source.AcquisitionStatus);
    }

    [Fact]
    public async Task FallbackFixesAreForwardedOnlyWhileThePrimaryIsStale()
    {
        ManualClock clock = new();
        FakeGpsSource primary = new();
        FakeGpsSource fallback = new();
        await using ResilientGpsSource source = new(_ => primary, () => fallback, clock, TimeSpan.FromHours(1));
        List<GpsSample> forwarded = [];
        source.FixReady += (_, sample) => forwarded.Add(sample);
        await source.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilAsync(() => ReferenceEquals(source.Primary, primary));

        fallback.Emit(1);
        Assert.Equal([1d], forwarded.Select(sample => sample.SpeedMetersPerSecond));
        primary.Emit(2);
        fallback.Emit(3);
        Assert.Equal([1d, 2d], forwarded.Select(sample => sample.SpeedMetersPerSecond));
        Assert.False(source.FallbackActive);

        clock.Advance(ResilientGpsSource.PrimaryFreshness + TimeSpan.FromSeconds(1));
        fallback.Emit(4);
        Assert.Equal([1d, 2d, 4d], forwarded.Select(sample => sample.SpeedMetersPerSecond));
        Assert.True(source.FallbackActive);
    }

    [Fact]
    public void MissingConfiguredPortProbesAllPortsWithoutReceiverWrites()
    {
        SerialNmeaGpsSourceOptions configured = new() { PortName = "COM6" };

        SerialNmeaGpsSourceOptions resolved = ResilientGpsSource.ResolveSerialOptions(configured, true, ["COM3", "COM4"]);

        Assert.Null(resolved.PortName);
        Assert.Equal(SerialGnssProfile.Passive, resolved.GnssProfile);
        Assert.False(resolved.AllowReceiverCommandsThisSession);
        Assert.Equal("COM6", ResilientGpsSource.ResolveSerialOptions(configured, true, ["com6"]).PortName);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 200 && !condition(); attempt++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }

        Assert.True(condition());
    }

    private sealed class ManualClock : TimeProvider
    {
        private long _ticks = 1;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        internal void Advance(TimeSpan by) => Interlocked.Add(ref _ticks, by.Ticks);
    }

    /// <summary>Mimics a serial source that raises health changes while probing, before it is Ready.</summary>
    private sealed class FakeGpsSource : IGpsSource
    {
        private volatile bool _ready;

        public event EventHandler<GpsSample>? FixReady;
        public event EventHandler? HealthChanged;
        public int ProbeHealthChanges { get; init; }
        public GpsAccessStatus AccessStatus => GpsAccessStatus.Allowed;
        public GpsAcquisitionStatus AcquisitionStatus => _ready ? GpsAcquisitionStatus.Ready : GpsAcquisitionStatus.NotInitialized;
        public GpsPositionSource? LastDroppedFixSource => null;
        public DateTimeOffset? LastDroppedFixUtc => null;
        public GpsStreamStatistics Statistics => new(0, 0, 0, 0, null);

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            for (int index = 0; index < ProbeHealthChanges; index++)
            {
                HealthChanged?.Invoke(this, EventArgs.Empty);
            }

            _ready = true;
            HealthChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _ready = false;
            return Task.CompletedTask;
        }

        internal void Emit(double speed)
            => FixReady?.Invoke(this, new(speed, 90, true, 2, DateTimeOffset.UtcNow, 47, -122,
                GpsPositionSource.Satellite, fixQuality: 1));
    }
}
