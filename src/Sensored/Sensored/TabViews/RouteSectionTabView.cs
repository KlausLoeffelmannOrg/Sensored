using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored.TabViews;

/// <summary>
///  Displays classified route sections.
/// </summary>
public partial class RouteSectionTabView : UserControl
{
    /// <summary>
    ///  Initializes a new instance of the <see cref="RouteSectionTabView"/> class.
    /// </summary>
    public RouteSectionTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    ///  Appends a classified route section to the view.
    /// </summary>
    /// <param name="entry">The route section to append.</param>
    internal void Append(RoadSectionClassificationEntry entry)
        => _routeSectionGrid.Append(entry);

    /// <summary>
    ///  Clears all classified route sections.
    /// </summary>
    internal void Clear()
        => _routeSectionGrid.Rows.Clear();
}
