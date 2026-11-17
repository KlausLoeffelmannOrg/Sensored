using System.Globalization;
using System.Text;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

/// <summary>
///  Displays the bounded exception history without blocking the dashboard.
/// </summary>
internal sealed partial class ExceptionHistoryForm : Form
{
    private readonly ExceptionHistory _history;

    /// <summary>Initializes the exception-history window.</summary>
    /// <param name="history">The history to display.</param>
    /// <param name="toolbarSize">The main dashboard toolbar symbol size.</param>
    internal ExceptionHistoryForm(
        ExceptionHistory history,
        DashboardToolbarSize toolbarSize)
    {
        ArgumentNullException.ThrowIfNull(history);

        _history = history;
        InitializeComponent();
        ApplyToolbarSize(toolbarSize);
        UpdateEntries();
    }

    /// <summary>Applies the main dashboard toolbar symbol size.</summary>
    /// <param name="toolbarSize">The symbol size.</param>
    internal void ApplyToolbarSize(DashboardToolbarSize toolbarSize)
    {
        int symbolSize = (int)toolbarSize;
        SymbolToolStripAppearance.Apply(_toolStrip, new Size(symbolSize, symbolSize));
        _closeButton.ConfigureItem(
            FluentSymbols.AllSymbols.Cancel,
            (OnCloseCommand, true),
            "Close exception history",
            symbolSize);
    }

    /// <summary>Refreshes the displayed snapshot.</summary>
    internal void UpdateEntries()
    {
        IReadOnlyList<ExceptionHistoryEntry> entries = _history.GetSnapshot();

        if (entries.Count == 0)
        {
            _historyTextBox.Text = "No exceptions have been recorded.";
            return;
        }

        StringBuilder text = new();

        foreach (ExceptionHistoryEntry entry in entries.Reverse())
        {
            text.AppendLine(
                CultureInfo.CurrentCulture,
                $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] {entry.Context}");
            text.AppendLine(entry.Details);
            text.AppendLine();
        }

        _historyTextBox.Text = text.ToString();
    }

    private void OnCloseCommand(object? sender, EventArgs e)
        => Close();
}
