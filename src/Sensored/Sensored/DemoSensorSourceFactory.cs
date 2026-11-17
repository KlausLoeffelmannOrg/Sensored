using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.Sensors.Presentation;

namespace Sensored;

/// <summary>
///  Applies the app's current source selection when the shared sensor service starts a session.
/// </summary>
public sealed class DemoSensorSourceFactory : ISensorSourceFactory, IAsyncDisposable
{
    private readonly object _syncObject = new();
    private ISensorSourceFactory _sourceFactory =
        new WindowsSensorSourceFactory(WindowsSensorSourceOptions.Default);
    private SensorReplayPlayer? _activeReplayPlayer;
    private readonly GpsAcquisitionCoordinator _gpsOwner = new();
    private readonly GpsPreviewSession _preview;
    private volatile bool _replaying;
    private long _configurationVersion;

    /// <summary>Creates the application-root GPS owner and independent preview interest.</summary>
    public DemoSensorSourceFactory() => _preview = new(_gpsOwner.CreateSourceView());

    internal SerialNmeaGpsSource? ActiveSerialSource => _gpsOwner.CurrentSource as SerialNmeaGpsSource;
    internal GpsPreviewSession Preview => _preview;
    internal bool IsReplay => _replaying;
    internal WindowsSensorSourceOptions? LiveOptions { get; private set; }
    internal Exception? GpsFault => _gpsOwner.Fault;
    internal Task StartPreviewAsync() => _preview.StartAsync();

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => _gpsOwner.DisposeAsync();

    public string ConfigurationKey
    {
        get
        {
            lock (_syncObject)
            {
                return _sourceFactory.ConfigurationKey;
            }
        }
    }

    internal Task ConfigureAsync(WindowsSensorSourceOptions options, bool allowReceiverWrites = false)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (!allowReceiverWrites)
        {
            options = options with
            {
                SerialGpsOptions = options.SerialGpsOptions.WithoutReceiverCommands()
            };
        }
        lock (_syncObject)
        {
            long version = ++_configurationVersion;
            WindowsSensorSourceFactory live = new(options, _gpsOwner.CreateSourceView());
            return CompleteLiveConfigurationAsync(version, live,
                _gpsOwner.ConfigureAsync(live.CreateGpsSelection()));
        }
    }

    private async Task CompleteLiveConfigurationAsync(long version, WindowsSensorSourceFactory live, Task configuring)
    {
        await configuring.ConfigureAwait(false);
        Task resume;
        lock (_syncObject)
        {
            if (version != _configurationVersion)
            {
                return;
            }
            resume = _gpsOwner.SetSuspendedAsync(false);
        }
        await resume.ConfigureAwait(false);
        lock (_syncObject)
        {
            if (version != _configurationVersion)
            {
                return;
            }
            LiveOptions = live.Options;
            _sourceFactory = live;
            _activeReplayPlayer = null;
            _replaying = false;
        }
    }

    internal Task ConfigureReplayAsync(string path, TimeSpan initialPosition = default, double playbackSpeed = 1d)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialPosition, TimeSpan.Zero);

        string fullPath = Path.GetFullPath(path);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The sensor replay file does not exist.", fullPath);
        }

        SensorRecordingFormats format = SensorRecordingReader.DetectFormat(fullPath);
        if (format == SensorRecordingFormats.None)
        {
            throw new ArgumentException(
                "Select a WARP compact JSON, JSON-lines or legacy semicolon-CSV journal, not a V2 snapshot or fusion CSV.",
                nameof(path));
        }

        lock (_syncObject)
        {
            _replaying = true;
            long version = ++_configurationVersion;
            SensorReplaySourceFactory replay = new(fullPath, format,
                realTime: true, initialPosition: initialPosition, playbackSpeed: playbackSpeed);
            return CompleteReplayConfigurationAsync(version, replay, _gpsOwner.SetSuspendedAsync(true));
        }
    }

    private async Task CompleteReplayConfigurationAsync(long version, ISensorSourceFactory replay, Task suspend)
    {
        await suspend.ConfigureAwait(false);
        lock (_syncObject)
        {
            if (version != _configurationVersion)
            {
                return;
            }
            _sourceFactory = replay;
            _activeReplayPlayer = null;
        }
    }

    internal SensorReplayPlayer? ActiveReplayPlayer
    {
        get
        {
            lock (_syncObject)
            {
                return _activeReplayPlayer;
            }
        }
    }

    public SensorSourceSet CreateSources(SensorServiceOptions options)
    {
        ISensorSourceFactory sourceFactory;

        lock (_syncObject)
        {
            sourceFactory = _sourceFactory;
        }

        SensorSourceSet sources = sourceFactory.CreateSources(options);

        lock (_syncObject)
        {
            _activeReplayPlayer = sourceFactory is SensorReplaySourceFactory replayFactory
                ? replayFactory.CurrentPlayer
                : null;
        }

        return sources;
    }
}
