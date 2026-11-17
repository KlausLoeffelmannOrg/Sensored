using WarpToolkit.Sensors;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Collects an optional parked-only session prefix while preserving unique output names.</summary>
internal sealed partial class SessionNameDialog : Form
{
    internal SessionNameDialog(string prefix, Font font)
    {
        InitializeComponent();
        Font = font;
        _name.Text = prefix;
    }

    internal string SessionName => _name.Text.Trim();

    private void OnAccept(object? sender, EventArgs e)
    {
        try
        {
            new SensorRecordingRequest { RootFolderPath = ".", NamePrefix = SessionName }.Validate();
            DialogResult = DialogResult.OK;
        }
        catch (ArgumentException exception)
        {
            _hint.Text = exception.Message;
        }
    }
}
