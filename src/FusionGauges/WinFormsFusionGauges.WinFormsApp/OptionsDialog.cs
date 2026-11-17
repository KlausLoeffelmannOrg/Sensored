namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Edits parked-only configuration on four WARP Fluent option pages.</summary>
internal sealed partial class OptionsDialog : Form
{
    private FusionGaugesOptions _options;

    internal OptionsDialog(FusionGaugesOptions options, Font font)
    {
        _options = options;
        InitializeComponent();
        Font = font;
        _appearance.LoadOptions(options);
        _sources.LoadOptions(options);
        _recording.LoadOptions(options);
        _mirror.LoadOptions(options);
        _tabs.AddTab("Appearance", _appearance);
        _tabs.AddTab("Sources / GPS", _sources);
        _tabs.AddTab("Recording", _recording);
        _tabs.AddTab("ZIP / OneDrive", _mirror);
    }

    internal FusionGaugesOptions Options => _options;

    private void OnAccept(object? sender, EventArgs e)
    {
        try
        {
            FusionGaugesOptions options = _appearance.ReadOptions(_options);
            options = _sources.ReadOptions(options);
            options = _recording.ReadOptions(options);
            options = _mirror.ReadOptions(options);
            options.Validate();
            if (options.SourceKind == WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps
                && options.GnssProfile != WarpToolkit.Sensors.SerialGnssProfile.Passive
                && _sources.ReceiverCommandsRequested && !_options.AllowReceiverCommandsThisSession)
            {
                if (!WarpToolkit.WinForms.Sensors.DirectX.GpsReceiverCommandConsent.Confirm(this,
                    options.CreateSourceOptions().SerialGpsOptions.WithoutReceiverCommands()))
                {
                    return;
                }

                options = options with { AllowReceiverCommandsThisSession = true };
            }

            _options = options;
            DialogResult = DialogResult.OK;
        }
        catch (ArgumentException exception)
        {
            _error.Text = exception.Message;
        }
    }
}
