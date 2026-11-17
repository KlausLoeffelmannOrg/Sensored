namespace Sensored;

/// <summary>Integrates one measurement-time trip independently of speed source and render cadence.</summary>
internal sealed class VehicleTripAccumulator
{
    private DateTimeOffset _previousTime;
    private double _previousSpeed = double.NaN;
    private double _kilometers;

    internal double Kilometers => Volatile.Read(ref _kilometers);

    internal void Observe(DateTimeOffset time, double speed)
    {
        if (time <= _previousTime)
        {
            return;
        }

        double seconds = (time - _previousTime).TotalSeconds;
        if (double.IsFinite(speed) && speed >= 0d && double.IsFinite(_previousSpeed)
            && seconds <= 2d)
        {
            Volatile.Write(ref _kilometers, _kilometers + (_previousSpeed + speed) * 0.5d * seconds / 1000d);
        }

        _previousTime = time;
        _previousSpeed = double.IsFinite(speed) && speed >= 0d ? speed : double.NaN;
    }
}
