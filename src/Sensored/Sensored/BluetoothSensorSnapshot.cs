using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Contains detached Bluetooth reference data, never a primary fusion input.</summary>
internal sealed record BluetoothSensorSnapshot(
    bool Connected, bool Searching, string Status, Wt901Reading? Reading = null)
{
    internal bool IsFreshAt(DateTimeOffset now)
        => Connected && Reading is { } reading
            && now >= reading.Timestamp
            && now - reading.Timestamp <= TimeSpan.FromSeconds(2);
}
