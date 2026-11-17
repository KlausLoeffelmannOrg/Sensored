namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Shows a selectable snapshot without starting another investigative capture.</summary>
internal sealed partial class CanDiagnosticResultsDialog : Form
{
    /// <summary>Creates the Designer-compatible result list.</summary>
    public CanDiagnosticResultsDialog() => InitializeComponent();

    /// <summary>Creates a list containing command replies, absent frames and decoded candidate values.</summary>
    internal CanDiagnosticResultsDialog(IEnumerable<CanDiagnosticResult> results) : this()
    {
        foreach (CanDiagnosticResult result in results)
        {
            _results.Rows.Add(result.Command, result.Reading, result.Result, result.Attempts,
                result.Samples, result.Response, result.RawValue, result.Value, result.Unit,
                result.Timestamp?.ToLocalTime().ToString("HH:mm:ss.fff") ?? "");
        }
    }
}
