using System.Diagnostics;
using System.Runtime.ExceptionServices;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Presentation;

namespace Sensored;

/// <summary>Consumes unthrottled GPS fixes on a sequential background owner, independently of fusion and rendering.</summary>
internal sealed class GpsMotionSession : IAsyncDisposable
{
    private static readonly TimeSpan s_stopTimeout = TimeSpan.FromSeconds(10);
    private readonly ISensorSubscription _subscription;
    private readonly GpsMotionTracker _tracker;
    private readonly CancellationTokenSource _abort = new();
    private readonly CancellationToken _abortToken;
    private Task? _stopTask;
    private long _lastFixArrival;
    private long _processedCount;
    private long _generation;

    /// <summary>Starts consuming a subscription whose lifetime is transferred to this session.</summary>
    internal GpsMotionSession(ISensorSubscription subscription, GpsMotionTracker tracker)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        ArgumentNullException.ThrowIfNull(tracker);
        _subscription = subscription;
        _tracker = tracker;
        _abortToken = _abort.Token;
        Completion = Task.Run(ConsumeAsync);
    }

    /// <summary>Gets the background completion, including processing failures.</summary>
    internal Task Completion { get; }

    /// <summary>Gets the number of GPS envelopes inspected by the processing owner.</summary>
    internal long ProcessedCount => Interlocked.Read(ref _processedCount);

    /// <summary>Gets the generation of the sequentially processed GPS history.</summary>
    internal long Generation => Interlocked.Read(ref _generation);

    /// <summary>Gets the last processing fault without disguising it as a valid readout.</summary>
    internal Exception? Fault => Completion.Exception?.GetBaseException();

    /// <summary>Gets whether an accepted fix arrived recently, accounting for replay pacing.</summary>
    internal bool HasFreshFix(double playbackRate)
    {
        long arrival = Volatile.Read(ref _lastFixArrival);
        double sourceInterval = Math.Max(1d, _tracker.Latest.FixInterval.TotalSeconds);
        double limitSeconds = Math.Clamp(3d * sourceInterval / playbackRate, 3d, 120d);

        return arrival != 0 && Stopwatch.GetElapsedTime(arrival).TotalSeconds <= limitSeconds;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
        => new(_stopTask ??= StopAsync());

    private async Task ConsumeAsync()
    {
        await foreach (SensorEnvelope envelope in _subscription.ReadAllAsync(_abortToken).ConfigureAwait(false))
        {
            if (envelope.SourceTransition is { SourceId: "GPS" } transition && transition.Generation != Generation)
            {
                Interlocked.Exchange(ref _generation, transition.Generation);
                _tracker.Reset("GPS motion history reset after receiver handoff.");
                Volatile.Write(ref _lastFixArrival, 0);
            }
            else if (envelope.Gap is { DroppedCount: > 0 })
            {
                _tracker.Reset("GPS motion history reset after a transport gap.");
                Volatile.Write(ref _lastFixArrival, 0);
            }
            else if (envelope.Geolocation is { } fix)
            {
                if (envelope.SourceGeneration != Generation)
                {
                    Interlocked.Exchange(ref _generation, envelope.SourceGeneration);
                    _tracker.Reset("GPS motion history reset after receiver handoff.");
                    Volatile.Write(ref _lastFixArrival, 0);
                }
                Interlocked.Increment(ref _processedCount);

                if (_tracker.Add(fix))
                {
                    Volatile.Write(ref _lastFixArrival, Stopwatch.GetTimestamp());
                }
            }
        }
    }

    private async Task StopAsync()
    {
        List<Exception> failures = [];

        try
        {
            try
            {
                await _subscription.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                failures.Add(exception);
                await _abort.CancelAsync().ConfigureAwait(false);
            }

            try
            {
                await Completion.WaitAsync(s_stopTimeout).ConfigureAwait(false);
            }
            catch (TimeoutException exception)
            {
                failures.Add(exception);
                await _abort.CancelAsync().ConfigureAwait(false);

                try
                {
                    await Completion.WaitAsync(s_stopTimeout).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_abortToken.IsCancellationRequested)
                {
                }
                catch (Exception abortException)
                {
                    failures.Add(abortException);
                }
            }
            catch (OperationCanceledException) when (failures.Count > 0 && _abortToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }
        finally
        {
            _abort.Dispose();
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        if (failures.Count > 1)
        {
            throw new AggregateException("GPS motion detach and processing did not complete cleanly.", failures);
        }
    }
}
