using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <content>Alphabetic speed-source modes independent of the fusion input selection.</content>
public partial class MainForm
{
    private int _speedDisplayMode = 1;

    private bool PreferGpsInstrument => Volatile.Read(ref _speedDisplayMode) % 3 == 2;

    private void InitializeInstrumentModes()
    {
        int fallback = _userSettings?.Get("Speedometer.CompareGps", true) == false ? 0 : 1;
        int saved = _userSettings?.Get("Speedometer.DisplayMode", fallback) ?? fallback;
        Volatile.Write(ref _speedDisplayMode, saved is >= 0 and < 6 ? saved : 1);
        ApplySpeedModeOptions();
        _speedometer.DisplayModeChangeRequested += OnSpeedModeChangeRequested;
    }

    private void OnSpeedModeChangeRequested(object? sender, EventArgs e)
    {
        Volatile.Write(ref _speedDisplayMode, (_speedDisplayMode + 1) % 6);
        ApplySpeedModeOptions();
        ResetPerformanceTiming();
        PresentObd();
        _speedometer.ShowModeCaption(_speedometer.DisplayModeLabel);
        _userSettings?.Set("Speedometer.DisplayMode", _speedDisplayMode);
        _userSettings?.Flush();
    }

    private void ApplySpeedModeOptions()
    {
        _speedometer.DisplayUnit = _speedDisplayMode >= 3
            ? SpeedUnit.MilesPerHour : SpeedUnit.KilometersPerHour;
        _speedometer.ShowGpsSpeed = _speedDisplayMode % 3 == 1;
    }

    private void UpdateSpeedModeCaption(bool obdEffective)
    {
        bool miles = _speedDisplayMode >= 3;
        string primaryUnit = miles ? "mph" : "km/h";
        string otherUnit = miles ? "km/h" : "mph";
        string source = obdEffective ? "OBD2" : PreferGpsInstrument ? "GPS" : "GPS fallback";
        bool compare = _speedometer.ShowGpsSpeed && obdEffective;
        string secondaryUnit = compare ? primaryUnit : otherUnit;
        string secondarySource = compare ? "GPS" : source;
        _speedometer.SetDisplayModeLabel(
            $"{(char)('A' + _speedDisplayMode)}: Prim {primaryUnit} ({source}) / Sec {secondaryUnit} ({secondarySource})");
    }
}
