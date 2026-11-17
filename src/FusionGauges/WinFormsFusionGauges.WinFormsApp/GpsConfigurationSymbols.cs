using WarpToolkit.WinForms.Drawing.Symbols;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Provides theme-tinted SVG command variations for receiver configuration.</summary>
internal static class GpsConfigurationSymbols
{
    internal static SvgSymbol Configuration { get; } = Create(
        "M12 2 V6 M12 18 V22 M2 12 H6 M18 12 H22 M5 5 L8 8 M16 16 L19 19 M5 19 L8 16 M16 8 L19 5 M8 8 H16 V16 H8 Z");
    internal static SvgSymbol Windows { get; } = Create("M3 4 L21 2 V22 L3 20 Z M11 3 V21 M3 12 H21");
    internal static SvgSymbol Serial { get; } = Create(
        "M3 5 H21 V15 H3 Z M7 15 V20 H17 V15 M7 8 V11 M12 8 V11 M17 8 V11");
    internal static SvgSymbol Port { get; } = Create("M5 3 H19 V15 H5 Z M8 15 V21 M12 15 V21 M16 15 V21 M8 6 H16 V11 H8 Z");
    internal static SvgSymbol Clock { get; } = Create("M5 3 H19 V21 H5 Z M12 6 V12 L17 15");

    internal static SvgSymbol Constellation(int index)
    {
        int x = 4 + index * 3;

        return Create($"M2 21 L12 3 L22 21 Z M{x} 8 V14 M{x - 3} 11 H{x + 3}");
    }

    private static SvgSymbol Create(string path)
        => SvgSymbol.FromMarkup(
            $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><title>GPS configuration command</title><path d="{path}" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round"/></svg>""");
}
