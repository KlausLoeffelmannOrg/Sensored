namespace WarpSensorsDemo.TabViews;

/// <summary>
///  Displays the sensor dashboard diagnostic log.
/// </summary>
public partial class DebugTabView : UserControl
{
    /// <summary>
    ///  Initializes a new instance of the <see cref="DebugTabView"/> class.
    /// </summary>
    public DebugTabView()
    {
        InitializeComponent();
    }

    /// <summary>
    ///  Attaches the application log to the view's console.
    /// </summary>
    /// <param name="log">The log to attach.</param>
    internal void Attach(DemoLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.Attach(_debugConsole);
    }
}
