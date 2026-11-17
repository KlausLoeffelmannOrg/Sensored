using System.IO.Compression;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Configures opt-in ZIP publication to a locally synchronized OneDrive folder.</summary>
public sealed partial class MirrorOptionsView : UserControl
{
    /// <summary>Creates the ZIP mirror options page.</summary>
    public MirrorOptionsView() => InitializeComponent();

    internal void LoadOptions(FusionGaugesOptions options)
    {
        _enabled.Checked = options.MirrorEnabled;
        _folder.Text = options.MirrorFolder;
        _compression.DataSource = Enum.GetValues<CompressionLevel>();
        _compression.SelectedItem = options.CompressionLevel;
    }

    internal FusionGaugesOptions ReadOptions(FusionGaugesOptions options)
        => options with
        {
            MirrorEnabled = _enabled.Checked,
            MirrorFolder = _folder.Text.Trim(),
            CompressionLevel = (CompressionLevel)_compression.SelectedItem!
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
