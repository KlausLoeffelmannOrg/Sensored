using WarpToolkit.WinForms.DirectX.Markdown;

namespace Sensored;

/// <summary>
///  Displays one locally supplied help topic at a time in a modeless Markdown window.
/// </summary>
internal sealed partial class HelpViewerForm : Form
{
    internal HelpViewerForm()
    {
        InitializeComponent();
        _markdownPreview.LinkOpening += OnLinkOpening;
    }

    internal void ShowTopic(HelpTopic topic)
    {
        string title = HelpContent.GetTitle(topic);
        Text = title;
        _markdownPreview.AccessibleName = title;
        _markdownPreview.Markdown = HelpContent.GetMarkdown(topic);
    }

    private static void OnLinkOpening(object? sender, MarkdownLinkOpeningEventArgs e)
    {
        if (!e.Link.IsFragment)
        {
            e.Cancel = true;
        }
    }
}
