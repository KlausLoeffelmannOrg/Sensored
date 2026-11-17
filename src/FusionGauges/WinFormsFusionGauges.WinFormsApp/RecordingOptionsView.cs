using WarpToolkit.Sensors;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Configures interoperable local formats, folders and filename-free driving operations.</summary>
public sealed partial class RecordingOptionsView : UserControl
{
    /// <summary>Creates the recording options page.</summary>
    public RecordingOptionsView() => InitializeComponent();

    internal void LoadOptions(FusionGaugesOptions options)
    {
        _folder.Text = options.RecordingFolder;
        _prefix.Text = options.NamePrefix;
        _marker.Text = options.MarkerLabel;
        _automatic.Checked = options.AutomaticNaming;
        _autostart.Checked = options.AutoRecording;
        _jsonl.Checked = options.Formats.HasFlag(SensorRecordingFormats.VerboseJsonLines);
        _compact.Checked = true;
        _csv.Checked = true;
        _rawRate.Value = options.RawSnapshotsPerSecond;
    }

    internal FusionGaugesOptions ReadOptions(FusionGaugesOptions options)
        => options with
        {
            RecordingFolder = _folder.Text.Trim(),
            NamePrefix = _prefix.Text.Trim(),
            MarkerLabel = _marker.Text.Trim(),
            AutomaticNaming = _automatic.Checked,
            AutoRecording = _autostart.Checked,
            RawSnapshotsPerSecond = (int)_rawRate.Value,
            Formats = (_jsonl.Checked ? SensorRecordingFormats.VerboseJsonLines : SensorRecordingFormats.None)
                | SensorRecordingFormats.CompactJson
        };

    private void OnBrowse(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new() { InitialDirectory = _folder.Text };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dialog.SelectedPath;
        }
    }
}
