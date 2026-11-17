namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Edits the tablet font and toolbar icon size presets.</summary>
public sealed partial class AppearanceOptionsView : UserControl
{
    /// <summary>Creates the appearance options page.</summary>
    public AppearanceOptionsView() => InitializeComponent();

    internal void LoadOptions(FusionGaugesOptions options)
    {
        _icons.Items.AddRange(new object[] { "Small (24 px)", "Medium (36 px)", "Large (48 px)" });
        _fonts.Items.AddRange(new object[] { "Small (9 pt)", "Medium (10 pt)", "Large (12 pt)" });
        _icons.SelectedIndex = options.IconSize switch { 24 => 0, 48 => 2, _ => 1 };
        _fonts.SelectedIndex = options.FontSize switch { 9 => 0, 12 => 2, _ => 1 };
        _digits.Value = options.AccelerationDigits;
        _rings.Settings = options.Speedometer;
    }

    internal FusionGaugesOptions ReadOptions(FusionGaugesOptions options)
        => options with
        {
            IconSize = _icons.SelectedIndex switch { 0 => 24, 2 => 48, _ => 36 },
            FontSize = _fonts.SelectedIndex switch { 0 => 9, 2 => 12, _ => 10 },
            AccelerationDigits = (int)_digits.Value,
            Speedometer = _rings.Settings
        };
}
