using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;

namespace WarpSensorsDemo;

/// <summary>
///  Applies the app's current source selection when the shared sensor service starts a session.
/// </summary>
public sealed class DemoSensorSourceFactory : ISensorSourceFactory
{
    private readonly object _syncObject = new();
    private WindowsSensorSourceOptions _options = WindowsSensorSourceOptions.Default;

    public string ConfigurationKey
    {
        get
        {
            lock (_syncObject)
            {
                return new WindowsSensorSourceFactory(_options).ConfigurationKey;
            }
        }
    }

    internal void Configure(WindowsSensorSourceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        lock (_syncObject)
        {
            _options = options;
        }
    }

    public SensorSourceSet CreateSources(SensorServiceOptions options)
    {
        WindowsSensorSourceOptions sourceOptions;

        lock (_syncObject)
        {
            sourceOptions = _options;
        }

        return new WindowsSensorSourceFactory(sourceOptions).CreateSources(options);
    }
}
