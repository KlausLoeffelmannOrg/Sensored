namespace Sensored.TabViews;

/// <summary>
///  Displays the sensor dashboard diagnostic log.
/// </summary>
public partial class DebugTabView : UserControl
{
    private Font? _consoleFont;
    /// <summary>
    ///  Initializes a new instance of the <see cref="DebugTabView"/> class.
    /// </summary>
    public DebugTabView()
    {
        InitializeComponent();
        ApplyConsoleFont();
        Disposed += OnDisposed;
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

    /// <inheritdoc/>
    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);

        if (_debugConsole is not null)
        {
            ApplyConsoleFont();
        }
    }

    private void ApplyConsoleFont()
    {
        Font? previous = _consoleFont;
        _consoleFont = new Font("Consolas", Font.SizeInPoints, Font.Style, GraphicsUnit.Point);
        _debugConsole.Font = _consoleFont;
        previous?.Dispose();
    }

    private void OnDisposed(object? sender, EventArgs e)
    {
        _consoleFont?.Dispose();
        _consoleFont = null;
    }
}
