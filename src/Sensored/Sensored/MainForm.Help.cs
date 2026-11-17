using WarpToolkit.WinForms.Symbols;

namespace Sensored;

/// <summary>
///  Owns the single modeless help window and the dashboard status-strip help command.
/// </summary>
public partial class MainForm
{
    private HelpViewerForm? _helpViewer;
    private FontSymbol? _helpSymbol;

    /// <summary>
    ///  Opens or focuses the app-wide help window, replacing its current topic.
    /// </summary>
    /// <param name="topic">The local help document to show.</param>
    internal void ShowHelpTopic(HelpTopic topic)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (_helpViewer is null || _helpViewer.IsDisposed)
        {
            _helpViewer = new HelpViewerForm();
            _helpViewer.Disposed += OnHelpViewerDisposed;
            FormClosed += OnHelpOwnerClosed;
            Disposed += OnHelpOwnerDisposed;
        }

        _helpViewer.ShowTopic(topic);

        if (!_helpViewer.Visible)
        {
            _helpViewer.Show();
        }

        _helpViewer.Activate();
    }

    private void OnHelpStatusClick(object? sender, EventArgs e)
        => ShowHelpTopic(HelpTopic.Status);

    private void OnHelpStatusFontChanged(object? sender, EventArgs e)
    {
        FontSymbol symbol = FontSymbol.FromFluentSymbol(
            FluentSymbols.AllSymbols.Help,
            _statusStrip.Font.SizeInPoints * _statusStrip.DeviceDpi / 72f);
        FontSymbol? previous = _helpSymbol;
        _helpSymbol = symbol;
        _helpStatusButton.Symbol = symbol;
        _helpStatusButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
        previous?.SymbolFont?.Dispose();
    }

    private void OnHelpViewerDisposed(object? sender, EventArgs e)
    {
        FormClosed -= OnHelpOwnerClosed;
        Disposed -= OnHelpOwnerDisposed;
        _helpViewer = null;
    }

    private void OnHelpOwnerClosed(object? sender, FormClosedEventArgs e)
    {
        FormClosed -= OnHelpOwnerClosed;
        _helpViewer?.Dispose();
    }

    private void OnHelpOwnerDisposed(object? sender, EventArgs e)
    {
        FormClosed -= OnHelpOwnerClosed;
        Disposed -= OnHelpOwnerDisposed;
        _helpViewer?.Dispose();
        _helpViewer = null;
    }

    private void OnHelpStatusDisposed(object? sender, EventArgs e)
    {
        _helpStatusButton.Disposed -= OnHelpStatusDisposed;
        _helpSymbol?.SymbolFont?.Dispose();
        _helpSymbol = null;
    }
}
