using WarpToolkit.Sensors;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Shows durable local recording and separate best-effort cloud-folder publication status.</summary>
public sealed partial class RecordingView : UserControl
{
    /// <summary>Creates the recording status page.</summary>
    public RecordingView() => InitializeComponent();

    /// <summary>Gets the full recording log text, independently of selection or scroll position.</summary>
    internal string DiagnosticText => _details.Text;

    internal void Present(string message, SensorRecordingStatus? recording, SensorRecordingZipMirrorStatus mirror)
    {
        _details.Text = $"{message}\r\n\r\n"
            + $"Raw rows: {recording?.RowsWritten ?? 0:N0}   Lost rows: {recording?.RowsDropped ?? 0:N0}\r\n"
            + $"Finalized parts: {recording?.FinalizedParts.Count ?? 0}\r\n"
            + $"Local writer fault: {recording?.LastFaultMessage ?? "none"}\r\n\r\n"
            + $"ZIPs pending: {mirror.Pending}   Published to sync folder: {mirror.Published}\r\n"
            + $"ZIP fault: {mirror.LastError ?? "none"}\r\n\r\n"
            + "Raw JSONL, compact JSON and semicolon CSV retain the existing WARP formats.\r\n"
            + "Fusion and markers are companion files. Incomplete companions are explicitly named.\r\n"
            + "ZIPs contain finalized local bytes. OneDrive's client owns remote upload.\r\n"
            + "Pending archives survive shutdown in the session-local retry journal.\r\n\r\n"
            + "Use Tools / Options while parked to configure folders, formats and automatic naming.";
    }
}
