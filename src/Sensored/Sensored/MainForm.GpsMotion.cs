using System.Diagnostics;
using Sensored.TabViews;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Owns full-rate GPS diagnostics and publishes their detached readouts to the dashboard.</summary>
public partial class MainForm
{
    private readonly GpsMotionTracker _gpsMotionTracker = new();
    private readonly GpsMotionTabView _gpsMotionTabHost = new();
    private GpsMotionSession? _gpsMotionSession;
    private GpsMotionSnapshot? _displayedGpsMotion;
    private FusionEstimate? _displayedGpsFusion;
    private Exception? _observedGpsMotionFault;
    private bool _displayedGpsFresh;
    private long _lastGpsMotionReadoutUpdate;
    private bool _completedReplayGpsFresh;

    private void StartGpsMotionInput(FusionContinuity continuity)
    {
        if (_gpsMotionSession is not null)
        {
            throw new InvalidOperationException("The previous GPS motion reader has not stopped.");
        }

        if (continuity != FusionContinuity.Continue)
        {
            _gpsMotionTracker.Reset();
        }

        _observedGpsMotionFault = null;
        ISensorSubscription subscription = _sensorService.Subscribe(new SensorSubscriptionOptions
        {
            Name = "Sensored independent GPS motion",
            Kinds = SensorDataKindFilter.Geolocation | SensorDataKindFilter.Gap | SensorDataKindFilter.SourceTransition,
            Capacity = 256
        });
        _gpsMotionSession = new GpsMotionSession(subscription, _gpsMotionTracker);
        _log.Information($"GPS motion reader started; retaining {_gpsMotionTracker.Capacity} distinct measurement-time positions.");
    }

    private async Task StopGpsMotionInputAsync()
    {
        if (_gpsMotionSession is not { } session)
        {
            return;
        }

        try
        {
            await session.DisposeAsync().ConfigureAwait(true);
            _log.Information($"GPS motion reader stopped; {session.ProcessedCount:N0} GPS envelopes processed.");
        }
        catch (Exception exception)
        {
            _terminalSourceStopFailure ??= exception;
            throw;
        }
        finally
        {
            _gpsMotionSession = null;
        }
    }

    private void ApplyGpsMotionReadouts()
    {
        if (_paused)
        {
            return;
        }

        if (_lease is not null)
        {
            ApplyAuxiliaryRawReadouts();
        }
        GpsMotionSession? session = _gpsMotionSession;

        if (session?.Fault is { } fault && !ReferenceEquals(fault, _observedGpsMotionFault))
        {
            _observedGpsMotionFault = fault;
            ShowError("Independent GPS motion processing stopped", fault);
        }

        bool preview = _lease is null && !_hasCompletedReplayPresentation
            && _sensorSourceFactory is { IsReplay: false };
        GpsMotionSnapshot snapshot = preview
            ? _sensorSourceFactory!.Preview.Dashboard.Motion : _gpsMotionTracker.Latest;
        FusionEstimate? estimate = _fusionSession?.Latest ?? _lastFusionEstimate;
        bool fresh = preview ? _sensorSourceFactory!.Preview.Capture().GpsFresh : snapshot.Timestamp is not null
            && session?.Fault is null
            && ((_hasCompletedReplayPresentation && _completedReplayGpsFresh)
                || (_replayPlayer is { IsPaused: true } && _displayedGpsFresh)
                || (session is { Fault: null }
                    && session.HasFreshFix(_replayPlayer is null ? 1d : _options.ReplaySpeedPercent / 100d)));
        if (_lease is not null && _sensorSourceFactory is { IsReplay: false } factory)
        {
            var live = factory.Preview.Capture();
            fresh &= live.GpsFresh && session?.Generation == live.Gps?.SourceGeneration;
        }

        if (ReferenceEquals(snapshot, _displayedGpsMotion)
            && fresh == _displayedGpsFresh
            && (ReferenceEquals(estimate, _displayedGpsFusion)
                || Stopwatch.GetElapsedTime(_lastGpsMotionReadoutUpdate) < TimeSpan.FromMilliseconds(200)))
        {
            return;
        }

        _displayedGpsMotion = snapshot;
        _displayedGpsFusion = estimate;
        _displayedGpsFresh = fresh;
        _lastGpsMotionReadoutUpdate = Stopwatch.GetTimestamp();
        GaugeDashboard.ApplyGps(snapshot, fresh, DateTimeOffset.UtcNow);
        _telemetryTabHost.ApplyGpsMotion(fresh ? snapshot : GpsMotionSnapshot.Empty);
        _gpsMotionTabHost.ApplySnapshot(
            snapshot,
            fresh,
            estimate?.IsSpeedSupported == true
                ? estimate.Speed : double.NaN,
            estimate?.IsHeadingSupported == true
                ? estimate.Heading : double.NaN);

    }
}
