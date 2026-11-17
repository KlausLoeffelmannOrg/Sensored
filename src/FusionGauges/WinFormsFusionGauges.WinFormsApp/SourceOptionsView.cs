using System.IO.Ports;
using WarpToolkit.Sensors;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Edits Windows Location and serial NMEA configuration without opening a receiver.</summary>
public sealed partial class SourceOptionsView : UserControl
{
    /// <summary>Creates the source options page.</summary>
    public SourceOptionsView() => InitializeComponent();

    /// <summary>Gets the user's explicit request, which is not authorization until consent succeeds.</summary>
    internal bool ReceiverCommandsRequested => _authorize.Checked;

    internal void LoadOptions(FusionGaugesOptions options)
    {
        _sources.DataSource = Enum.GetValues<WindowsSensorSourceKind>();
        _sources.SelectedItem = options.SourceKind;
        _obdAdapter.DataSource = Enum.GetValues<ObdAdapterKind>();
        _obdAdapter.SelectedItem = options.ObdAdapter;
        _ports.Text = options.SerialPortName ?? "";
        _profiles.DataSource = Enum.GetValues<SerialGnssProfile>();
        _profiles.SelectedItem = options.GnssProfile;
        _interval.Value = options.SourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
            ? options.TargetFixIntervalMilliseconds ?? (int)options.GpsReportIntervalMilliseconds
            : (int)options.GpsReportIntervalMilliseconds;
        _authorize.Checked = options.AllowReceiverCommandsThisSession;
        UpdateSourceState();
    }

    internal FusionGaugesOptions ReadOptions(FusionGaugesOptions options)
    {
        int interval = (int)_interval.Value;
        var sourceKind = (WindowsSensorSourceKind)_sources.SelectedItem!;
        bool rateChanged = sourceKind == WindowsSensorSourceKind.ImuAndSerialNmeaGps
            && interval != (options.TargetFixIntervalMilliseconds ?? (int)options.GpsReportIntervalMilliseconds);
        var profile = (SerialGnssProfile)_profiles.SelectedItem!;

        return options with
        {
            SourceKind = sourceKind,
            ObdAdapter = (ObdAdapterKind)_obdAdapter.SelectedItem!,
            SerialPortName = string.IsNullOrWhiteSpace(_ports.Text) ? null : _ports.Text.Trim(),
            GnssProfile = profile == SerialGnssProfile.Passive && rateChanged
                ? SerialGnssProfile.Current : profile,
            GpsReportIntervalMilliseconds = (uint)interval,
            TargetFixIntervalMilliseconds = rateChanged ? interval : options.TargetFixIntervalMilliseconds,
            AllowReceiverCommandsThisSession = options.AllowReceiverCommandsThisSession && _authorize.Checked
        };
    }

    private void OnSourceChanged(object? sender, EventArgs e) => UpdateSourceState();

    private void OnAdapterFormat(object? sender, ListControlConvertEventArgs e)
        => e.Value = ObdAdapters.GetName((ObdAdapterKind)e.ListItem!);

    private void UpdateSourceState()
    {
        bool serial = _sources.SelectedItem is WindowsSensorSourceKind.ImuAndSerialNmeaGps;
        _ports.Enabled = _profiles.Enabled = _authorize.Enabled = _refreshPorts.Enabled = serial;
        _interval.Enabled = _sources.SelectedItem is WindowsSensorSourceKind.ImuAndGps
            or WindowsSensorSourceKind.GpsOnly or WindowsSensorSourceKind.ImuAndSerialNmeaGps;
    }

    private async void OnRefreshPorts(object? sender, EventArgs e)
    {
        _refreshPorts.Enabled = false;

        try
        {
            string[] ports = await Task.Run(SerialPort.GetPortNames);
            string selected = _ports.Text;
            _ports.Items.Clear();
            _ports.Items.AddRange(ports);
            _ports.Text = selected;
            _hint.Text = $"Found {ports.Length} COM ports. Discovery did not open a receiver. Blank selects automatic discovery.";
        }
        catch (Exception exception)
        {
            System.Diagnostics.Trace.TraceError(exception.ToString());
            _hint.Text = $"COM port discovery failed: {exception.Message}";
        }
        finally
        {
            UpdateSourceState();
        }
    }
}
