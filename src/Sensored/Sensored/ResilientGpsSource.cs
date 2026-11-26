using System.IO.Ports;
using WarpToolkit.Sensors;

namespace Sensored;

/// <summary>
///  Keeps one stable GPS stream alive by preferring a serial NMEA receiver and falling back to the
///  tablet's Windows location provider while the receiver is missing, renumbered or silent.
/// </summary>
/// <remarks>
///  <para>
///   Serial recovery happens inside this source, so the shared acquisition owner keeps one generation
///   and does not publish a retry transition every few seconds. Fixes from the fallback are forwarded
///   only while the serial receiver has not delivered a fix within <see cref="PrimaryFreshness"/>.
///  </para>
///  <para>
///   When the configured COM port is absent, available ports are probed read-only. Receiver commands
///   are only ever sent on the first attempt to the explicitly configured port.
///  </para>
/// </remarks>
internal sealed class ResilientGpsSource : IGpsSource, IAsyncDisposable, IGpsReleaseDiagnostics
{
    internal static readonly TimeSpan PrimaryFreshness = TimeSpan.FromSeconds(3);
    internal static readonly TimeSpan PrimaryRetryDelay = TimeSpan.FromSeconds(10);

    private readonly Lock _gate = new();
    private readonly Func<bool, IGpsSource> _createPrimary;
    private readonly Func<IGpsSource>? _createFallback;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _retryDelay;
    private CancellationTokenSource? _stop;
    private Task _primaryLoop = Task.CompletedTask;
    private IGpsSource? _primary;
    private IGpsSource? _fallback;
    private long _lastPrimaryFix;
    private GpsAcquisitionStatus _reported = GpsAcquisitionStatus.NotInitialized;
    private Exception? _releaseFault;
    private Exception? _lastFault;
    private long _received, _accepted, _invalid, _dropped;

    /// <summary>Creates a source from explicit factories; the primary factory receives "first attempt".</summary>
    internal ResilientGpsSource(Func<bool, IGpsSource> createPrimary, Func<IGpsSource>? createFallback,
        TimeProvider? clock = null, TimeSpan? retryDelay = null)
    {
        _createPrimary = createPrimary ?? throw new ArgumentNullException(nameof(createPrimary));
        _createFallback = createFallback;
        _clock = clock ?? TimeProvider.System;
        _retryDelay = retryDelay ?? PrimaryRetryDelay;
    }

    /// <summary>Creates the production pairing of a serial receiver and the Windows location provider.</summary>
    internal static ResilientGpsSource CreateSerialWithTabletFallback(SerialNmeaGpsSourceOptions serial,
        Func<IGpsSource>? tabletGps)
    {
        ArgumentNullException.ThrowIfNull(serial);
        return new(firstAttempt => new SerialNmeaGpsSource(ResolveSerialOptions(serial, firstAttempt,
            SerialPort.GetPortNames())), tabletGps);
    }

    /// <summary>Probes all ports, without receiver writes, when the configured port is not present.</summary>
    internal static SerialNmeaGpsSourceOptions ResolveSerialOptions(SerialNmeaGpsSourceOptions configured,
        bool firstAttempt, IReadOnlyCollection<string> presentPorts)
    {
        bool present = string.IsNullOrWhiteSpace(configured.PortName)
            || presentPorts.Contains(configured.PortName, StringComparer.OrdinalIgnoreCase);
        if (present)
        {
            return firstAttempt ? configured : configured.WithoutReceiverCommands();
        }

        SerialNmeaGpsSourceOptions safe = configured.WithoutReceiverCommands();
        return new()
        {
            PortName = null,
            BaudRates = safe.BaudRates,
            ProbeWindow = safe.ProbeWindow,
            OverallProbeTimeout = safe.OverallProbeTimeout,
            PortDisposeTimeout = safe.PortDisposeTimeout,
            ReceiverWriteTimeout = safe.ReceiverWriteTimeout,
            ReceiverStopTimeout = safe.ReceiverStopTimeout,
            ReadBufferSize = safe.ReadBufferSize,
            GnssProfile = SerialGnssProfile.Passive,
            AllowReceiverCommandsThisSession = false
        };
    }

    public event EventHandler<GpsSample>? FixReady;
    public event EventHandler? HealthChanged;

    /// <summary>Gets the serial receiver currently owned, if any.</summary>
    internal IGpsSource? Primary => Volatile.Read(ref _primary);

    /// <summary>Gets whether fixes are currently forwarded from the tablet fallback.</summary>
    internal bool FallbackActive => !PrimaryFresh && Volatile.Read(ref _fallback) is { AcquisitionStatus: GpsAcquisitionStatus.Ready };

    private bool PrimaryFresh
    {
        get
        {
            long last = Interlocked.Read(ref _lastPrimaryFix);
            return last != 0 && _clock.GetElapsedTime(last) <= PrimaryFreshness;
        }
    }

    private IGpsSource? Effective
    {
        get
        {
            IGpsSource? primary = Volatile.Read(ref _primary);
            IGpsSource? fallback = Volatile.Read(ref _fallback);
            if (primary is { AcquisitionStatus: GpsAcquisitionStatus.Ready } && (PrimaryFresh || fallback is not { AcquisitionStatus: GpsAcquisitionStatus.Ready }))
            {
                return primary;
            }

            return fallback is { AcquisitionStatus: GpsAcquisitionStatus.Ready } ? fallback : primary ?? fallback;
        }
    }

    public GpsAccessStatus AccessStatus => Effective?.AccessStatus ?? GpsAccessStatus.Allowed;

    public GpsAcquisitionStatus AcquisitionStatus
        => Primary is { AcquisitionStatus: GpsAcquisitionStatus.Ready }
            || Volatile.Read(ref _fallback) is { AcquisitionStatus: GpsAcquisitionStatus.Ready }
            ? GpsAcquisitionStatus.Ready
            : Effective?.AcquisitionStatus ?? GpsAcquisitionStatus.NotAvailable;

    public GpsPositionSource? LastDroppedFixSource => Effective?.LastDroppedFixSource;
    public DateTimeOffset? LastDroppedFixUtc => Effective?.LastDroppedFixUtc;
    public GpsFixDiagnostics FixDiagnostics => Effective?.FixDiagnostics ?? default;
    public Exception? ReleaseFault => Volatile.Read(ref _releaseFault);

    public GpsStreamStatistics Statistics
    {
        get
        {
            lock (_gate)
            {
                GpsStreamStatistics p = _primary?.Statistics ?? default;
                GpsStreamStatistics f = _fallback?.Statistics ?? default;
                return new(_received + p.ReceivedFixCount + f.ReceivedFixCount,
                    _accepted + p.AcceptedFixCount + f.AcceptedFixCount,
                    _invalid + p.InvalidFixCount + f.InvalidFixCount,
                    _dropped + p.DroppedFixCount + f.DroppedFixCount,
                    AcquisitionStatus == GpsAcquisitionStatus.Ready ? null
                        : p.LastSourceFault ?? f.LastSourceFault ?? Volatile.Read(ref _lastFault));
            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_stop is not null)
            {
                return;
            }

            _stop = new();
            CancellationToken token = _stop.Token;
            _primaryLoop = Task.Run(() => RunPrimaryAsync(token), CancellationToken.None);
        }

        if (_createFallback is not null)
        {
            try
            {
                IGpsSource fallback = _createFallback();
                fallback.FixReady += OnFallbackFix;
                fallback.HealthChanged += OnMemberHealth;
                Volatile.Write(ref _fallback, fallback);
                await fallback.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The tablet provider is optional; the serial receiver may still work.
                Volatile.Write(ref _lastFault, exception);
            }
        }

        PublishHealth();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        CancellationTokenSource? stop;
        Task loop;
        IGpsSource? fallback;
        lock (_gate)
        {
            stop = _stop;
            _stop = null;
            loop = _primaryLoop;
            fallback = _fallback;
            _fallback = null;
        }

        if (stop is null)
        {
            return;
        }

        await stop.CancelAsync().ConfigureAwait(false);
        try
        {
            await loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            stop.Dispose();
        }

        if (fallback is not null)
        {
            fallback.FixReady -= OnFallbackFix;
            fallback.HealthChanged -= OnMemberHealth;
            await ReleaseAsync(fallback).ConfigureAwait(false);
        }

        PublishHealth();
    }

    public ValueTask DisposeAsync() => new(StopAsync());

    private async Task RunPrimaryAsync(CancellationToken token)
    {
        bool firstAttempt = true;
        while (!token.IsCancellationRequested)
        {
            IGpsSource? primary = null;
            TaskCompletionSource lost = new(TaskCreationOptions.RunContinuationsAsynchronously);
            bool armed = false;
            void OnFix(object? sender, GpsSample sample) => OnPrimaryFix(primary!, sample);
            void OnHealth(object? sender, EventArgs args)
            {
                // Probing raises health changes before the port is Ready; only a confirmed connection can be lost.
                if (Volatile.Read(ref armed) && primary!.AcquisitionStatus != GpsAcquisitionStatus.Ready)
                {
                    lost.TrySetResult();
                }

                PublishHealth();
            }

            try
            {
                SetDiagnostic("Serial receiver: opening or probing ports.");
                primary = _createPrimary(firstAttempt);
                firstAttempt = false;
                primary.FixReady += OnFix;
                primary.HealthChanged += OnHealth;
                Volatile.Write(ref _primary, primary);
                await primary.StartAsync(token).ConfigureAwait(false);
                if (primary.AcquisitionStatus == GpsAcquisitionStatus.Ready)
                {
                    Volatile.Write(ref armed, true);
                    SetDiagnostic($"Serial receiver connected{(primary is SerialNmeaGpsSource { ActivePort: { } port } ? $" on {port.PortName} @ {port.BaudRate}" : "")}.");
                    PublishHealth();
                    if (primary.AcquisitionStatus != GpsAcquisitionStatus.Ready)
                    {
                        lost.TrySetResult();
                    }

                    await lost.Task.WaitAsync(token).ConfigureAwait(false);
                    SetDiagnostic("Serial receiver lost; using fallback while retrying.");
                }
                else
                {
                    Exception? fault = primary.Statistics.LastSourceFault;
                    Volatile.Write(ref _lastFault, fault);
                    SetDiagnostic($"Serial receiver unavailable: {fault?.Message ?? "no NMEA port found"}.");
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                // Any receiver failure is recoverable here; the fallback keeps the stream alive.
                Volatile.Write(ref _lastFault, exception);
                SetDiagnostic($"Serial receiver failed: {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (primary is not null)
                {
                    primary.FixReady -= OnFix;
                    primary.HealthChanged -= OnHealth;
                    Volatile.Write(ref _primary, null);
                    await ReleaseAsync(primary).ConfigureAwait(false);
                }

                PublishHealth();
            }

            try
            {
                await Task.Delay(_retryDelay, _clock, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private string _diagnostic = "Not started.";

    /// <summary>Gets a human-readable description of the receiver/fallback state.</summary>
    internal string Diagnostic
        => $"{Volatile.Read(ref _diagnostic)} {(FallbackActive ? "Tablet location supplies fixes."
            : PrimaryFresh ? "Serial fixes are live." : "No fresh fixes.")}";

    private void SetDiagnostic(string text) => Volatile.Write(ref _diagnostic, text);

    private void OnPrimaryFix(IGpsSource primary, GpsSample sample)
    {
        if (!ReferenceEquals(primary, Volatile.Read(ref _primary)))
        {
            return;
        }

        if (sample.SpeedValid || double.IsFinite(sample.LatitudeDegrees))
        {
            Interlocked.Exchange(ref _lastPrimaryFix, _clock.GetTimestamp());
        }

        FixReady?.Invoke(this, sample);
    }

    private void OnFallbackFix(object? sender, GpsSample sample)
    {
        if (!PrimaryFresh)
        {
            FixReady?.Invoke(this, sample);
        }
    }

    private void OnMemberHealth(object? sender, EventArgs args) => PublishHealth();

    // Only aggregate changes are published; member churn must not look like a new acquisition episode.
    private void PublishHealth()
    {
        GpsAcquisitionStatus status = AcquisitionStatus;
        lock (_gate)
        {
            if (status == _reported)
            {
                return;
            }

            _reported = status;
        }

        HealthChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task ReleaseAsync(IGpsSource source)
    {
        try
        {
            await source.StopAsync(CancellationToken.None).ConfigureAwait(false);
            if (source is IAsyncDisposable disposable)
            {
                await disposable.DisposeAsync().ConfigureAwait(false);
            }

            if (source is IGpsReleaseDiagnostics { ReleaseFault: { } fault })
            {
                Volatile.Write(ref _releaseFault, fault);
            }
        }
        catch (Exception exception)
        {
            Volatile.Write(ref _releaseFault, exception);
        }
        finally
        {
            GpsStreamStatistics statistics = source.Statistics;
            lock (_gate)
            {
                _received += statistics.ReceivedFixCount;
                _accepted += statistics.AcceptedFixCount;
                _invalid += statistics.InvalidFixCount;
                _dropped += statistics.DroppedFixCount;
            }
        }
    }
}
