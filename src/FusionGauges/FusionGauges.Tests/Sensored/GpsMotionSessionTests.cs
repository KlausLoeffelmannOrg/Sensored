using System.Threading.Channels;
using Sensored;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;

namespace Sensored.Tests;

/// <summary>Checks the independent GPS reader's buffered drain, replay continuity, and fault ownership.</summary>
public sealed class GpsMotionSessionTests
{
    [Fact]
    public async Task Stop_DrainsEveryFixWithoutAnImuOrUiConsumer()
    {
        GpsMotionTracker tracker = new();
        TestSubscription subscription = new();
        GpsMotionSession session = new(subscription, tracker);

        for (int index = 0; index < 40; index++)
        {
            subscription.Publish(Fix(index));
        }

        await session.DisposeAsync();
        await session.DisposeAsync();
        Assert.True(session.Completion.IsCompletedSuccessfully);
        Assert.Equal(40, session.ProcessedCount);
        Assert.Equal(32, tracker.Latest.PointCount);
        Assert.Equal(1, subscription.DisposeCount);
        Assert.Equal(10d, tracker.Latest.FourPoint.SpeedMetersPerSecond, precision: 7);
        Assert.True(session.HasFreshFix(playbackRate: 1d));
    }

    [Fact]
    public async Task TransportGap_RestartsHistoryAndAllowsAnEarlierMeasurementTimeline()
    {
        GpsMotionTracker tracker = new();
        TestSubscription subscription = new();
        GpsMotionSession session = new(subscription, tracker);
        subscription.Publish(Fix(100));
        subscription.Publish(Fix(101));
        subscription.Publish(new SensorEnvelope
        {
            Sequence = 102,
            Kind = SensorDataKind.Gap,
            SourceTimestamp = DateTimeOffset.UnixEpoch,
            ArrivalTimestamp = DateTimeOffset.UnixEpoch,
            Gap = new SensorGapPayload(default, 3, 102, 104, "Test transport loss")
        });
        subscription.Publish(Fix(0));
        await session.DisposeAsync();
        Assert.Equal(3, session.ProcessedCount);
        Assert.Equal(1, tracker.Latest.PointCount);
        Assert.Equal(DateTimeOffset.UnixEpoch, tracker.Latest.Timestamp);
        Assert.Contains("transport gap", tracker.Latest.Diagnostic);
        Assert.True(double.IsNaN(tracker.Latest.FourPoint.SpeedMetersPerSecond));
    }

    [Fact]
    public async Task ContiguousReplayParts_PreserveTheBoundedTrackerAcrossReaders()
    {
        GpsMotionTracker tracker = new();
        TestSubscription first = new();
        GpsMotionSession firstSession = new(first, tracker);
        first.Publish(Fix(0));
        first.Publish(Fix(1));
        first.Publish(Fix(2));
        await firstSession.DisposeAsync();
        Assert.True(double.IsNaN(tracker.Latest.FourPoint.SpeedMetersPerSecond));
        TestSubscription second = new();
        GpsMotionSession secondSession = new(second, tracker);
        second.Publish(Fix(3));
        second.Publish(Fix(4));
        await secondSession.DisposeAsync();
        Assert.Equal(5, tracker.Latest.PointCount);
        Assert.Equal(10d, tracker.Latest.FourPoint.SpeedMetersPerSecond, precision: 7);
        Assert.Equal(4d, tracker.Latest.FullHistory.Duration.TotalSeconds);
    }

    [Fact]
    public async Task ReaderFault_IsObservableAndRepeatedDisposalDoesNotHideIt()
    {
        GpsMotionTracker tracker = new();
        TestSubscription subscription = new();
        GpsMotionSession session = new(subscription, tracker);
        InvalidOperationException failure = new("GPS motion test fault");
        subscription.Fail(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => session.Completion));
        Assert.Same(failure, session.Fault);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask());
        Assert.Equal(1, subscription.DisposeCount);
        Assert.False(session.HasFreshFix(1d));
    }

    [Fact]
    public async Task FailedDetach_StillCancelsAndJoinsTheOwnedReader()
    {
        GpsMotionTracker tracker = new();
        InvalidOperationException failure = new("Subscription detach test fault");
        TestSubscription subscription = new() { DisposalFailure = failure };
        GpsMotionSession session = new(subscription, tracker);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask()));
        Assert.True(session.Completion.IsCompleted);
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.DisposeAsync().AsTask());
        Assert.Equal(1, subscription.DisposeCount);
    }

    private static SensorEnvelope Fix(int second)
        => new()
        {
            Sequence = second,
            Kind = SensorDataKind.Geolocation,
            SourceTimestamp = DateTimeOffset.UnixEpoch.AddSeconds(second),
            ArrivalTimestamp = DateTimeOffset.UnixEpoch,
            Geolocation = default(GeolocationPayload) with
            {
                CoordinateTimestamp = DateTimeOffset.UnixEpoch.AddSeconds(second),
                LongitudeDegrees = second * 10d / 111_195.0802335329d,
                SpeedValid = true,
                SpeedMetersPerSecond = 12d,
                FixQuality = 1
            }
        };

    /// <summary>Provides a buffered single-reader subscription with controllable completion.</summary>
    private sealed class TestSubscription : ISensorSubscription
    {
        private readonly Channel<SensorEnvelope> _channel = Channel.CreateUnbounded<SensorEnvelope>();

        public SensorSubscriptionOptions Options { get; } = new();
        public SensorSubscriptionStatistics Statistics => default;
        public bool IsActive => DisposeCount == 0;
        internal int DisposeCount { get; private set; }
        internal Exception? DisposalFailure { get; init; }

        internal void Publish(SensorEnvelope envelope)
            => Assert.True(_channel.Writer.TryWrite(envelope));

        internal void Fail(Exception exception)
            => Assert.True(_channel.Writer.TryComplete(exception));

        public IAsyncEnumerable<SensorEnvelope> ReadAllAsync(CancellationToken cancellationToken = default)
            => _channel.Reader.ReadAllAsync(cancellationToken);

        public ValueTask DisposeAsync()
        {
            DisposeCount++;

            if (DisposalFailure is { } failure)
            {
                return ValueTask.FromException(failure);
            }

            _channel.Writer.TryComplete();

            return ValueTask.CompletedTask;
        }
    }
}
