using System.Diagnostics;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>Edits explicit Bluetooth roles without pairing or opening device sessions.</summary>
internal sealed partial class OptionsDialog
{
    private const int BluetoothScanSeconds = 20;
    private readonly Dictionary<string, BluetoothDeviceInfo> _discoveredDevices = [with(StringComparer.OrdinalIgnoreCase)];
    private readonly Dictionary<string, ListViewItem> _deviceRows = [with(StringComparer.OrdinalIgnoreCase)];
    private readonly IBluetoothDeviceScanner _bluetoothScanner;
    private BluetoothDeviceInfo? _selectedObdDevice;
    private BluetoothDeviceInfo? _selectedSensorDevice;
    private CancellationTokenSource? _bluetoothScanCancellation;
    private Task? _bluetoothScanTask;
    private long _bluetoothScanStartedAt;
    private bool _bluetoothScanStarted;
    private bool _updatingDeviceChoices;
    private bool _closingBluetoothOptions;
    private bool _scanStoppedByUser;

    private void LoadBluetoothOptions()
    {
        _selectedObdDevice = _options.ObdDevice;
        _selectedSensorDevice = _options.BluetoothSensorDevice;
        RefreshBluetoothChoices();
    }

    private void OnBluetoothSectionClick(object? sender, EventArgs e)
        => _tabControl.SelectedTab = _bluetoothPage;

    private async void OnScanBluetoothClick(object? sender, EventArgs e)
    {
        try
        {
            await StartBluetoothOptionsScanAsync();
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            SetBluetoothDiscoveryStatus($"Bluetooth discovery failed: {exception.Message}");
        }
    }

    private Task StartBluetoothOptionsScanAsync()
    {
        if (_bluetoothScanTask is { IsCompleted: false } || _closingBluetoothOptions)
        {
            return _bluetoothScanTask ?? Task.CompletedTask;
        }

        _bluetoothScanStarted = true;
        _scanStoppedByUser = false;
        _discoveredDevices.Clear();
        _deviceRows.Clear();
        _bluetoothDevices.Items.Clear();
        RefreshBluetoothChoices();
        _bluetoothScanCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(BluetoothScanSeconds));
        _bluetoothScanStartedAt = Stopwatch.GetTimestamp();
        _scanBluetoothButton.Enabled = false;
        _stopBluetoothScanButton.Enabled = true;
        _bluetoothScanProgress.Value = 0;
        _bluetoothScanProgress.Visible = true;
        _bluetoothScanTimer.Start();
        SetBluetoothDiscoveryStatus("Bluetooth: scanning paired and unpaired devices (0/20 s)...");
        _bluetoothScanTask = ScanBluetoothOptionsAsync(_bluetoothScanCancellation);

        return _bluetoothScanTask;
    }

    private async Task ScanBluetoothOptionsAsync(CancellationTokenSource cancellation)
    {
        string outcome = "Scan complete";
        try
        {
            await foreach (BluetoothDeviceInfo device in _bluetoothScanner.WatchAsync(
                BluetoothScanOptions.Default, cancellation.Token))
            {
                if (IsDisposed || Disposing)
                {
                    break;
                }

                ObserveBluetoothDevice(device);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            outcome = _scanStoppedByUser || _closingBluetoothOptions ? "Scan stopped" : "Scan complete";
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            outcome = $"Scan failed: {exception.Message}";
        }
        finally
        {
            cancellation.Dispose();
            _bluetoothScanCancellation = null;
            if (!IsDisposed && !Disposing)
            {
                _bluetoothScanTimer.Stop();
                _bluetoothScanProgress.Visible = false;
                _scanBluetoothButton.Enabled = true;
                _stopBluetoothScanButton.Enabled = false;
                SetBluetoothDiscoveryStatus($"Bluetooth: {outcome}; {_discoveredDevices.Count} devices. "
                    + "Not seen does not mean unpaired; wake the device and scan again.");
            }
        }
    }

    private void ObserveBluetoothDevice(BluetoothDeviceInfo device)
    {
        string key = BluetoothDeviceKey(device);
        bool choicesChanged = !_discoveredDevices.TryGetValue(key, out BluetoothDeviceInfo? previous)
            || previous.Name != device.Name || previous.IsPaired != device.IsPaired || previous.Id != device.Id;
        _discoveredDevices[key] = device;
        if (!_deviceRows.TryGetValue(key, out ListViewItem? row))
        {
            row = new ListViewItem();
            row.SubItems.AddRange(["", "", "", ""]);
            _deviceRows.Add(key, row);
            _bluetoothDevices.Items.Add(row);
        }

        row.Text = string.IsNullOrWhiteSpace(device.Name) ? "(unnamed device)" : device.Name;
        row.SubItems[1].Text = device.Address;
        row.SubItems[2].Text = device.Kind == BluetoothDeviceKind.LowEnergy ? "BLE" : "Classic";
        row.SubItems[3].Text = device.IsPaired ? "Paired" : "Unpaired";
        row.SubItems[4].Text = device.Rssi?.ToString(System.Globalization.CultureInfo.CurrentCulture) ?? "Unknown";
        if (choicesChanged)
        {
            RefreshBluetoothChoices();
        }

        OnBluetoothScanTick(this, EventArgs.Empty);
    }

    private void RefreshBluetoothChoices()
    {
        _updatingDeviceChoices = true;
        try
        {
            RefreshBluetoothChoice(_obdDeviceComboBox, _selectedObdDevice);
            RefreshBluetoothChoice(_sensorDeviceComboBox, _selectedSensorDevice);
            _selectedObdDevice = (_obdDeviceComboBox.SelectedItem as DeviceChoice)?.Device;
            _selectedSensorDevice = (_sensorDeviceComboBox.SelectedItem as DeviceChoice)?.Device;
        }
        finally
        {
            _updatingDeviceChoices = false;
        }
    }

    private void RefreshBluetoothChoice(ComboBox comboBox, BluetoothDeviceInfo? selected)
    {
        Dictionary<string, BluetoothDeviceInfo> choices = new(_discoveredDevices, StringComparer.OrdinalIgnoreCase);
        if (selected is not null)
        {
            choices.TryAdd(BluetoothDeviceKey(selected), selected);
        }

        DeviceChoice[] items =
        [
            new("Automatic (paired compatible devices)", null),
            .. choices.Values
                .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(device => device.Address, StringComparer.OrdinalIgnoreCase)
                .Select(device => new DeviceChoice(
                    $"{device.Name} | {device.Address} | {device.Kind} | "
                    + (_discoveredDevices.ContainsKey(BluetoothDeviceKey(device))
                        ? device.IsPaired ? "paired" : "unpaired"
                        : "saved; not seen this scan"), device))
        ];
        comboBox.DisplayMember = nameof(DeviceChoice.Caption);
        comboBox.DataSource = items;
        comboBox.SelectedItem = selected is null ? items[0]
            : items.First(item => item.Device is { } device
                && string.Equals(BluetoothDeviceKey(device), BluetoothDeviceKey(selected), StringComparison.OrdinalIgnoreCase));
    }

    private void OnBluetoothDeviceSelectionChanged(object? sender, EventArgs e)
    {
        if (!_updatingDeviceChoices)
        {
            _selectedObdDevice = (_obdDeviceComboBox.SelectedItem as DeviceChoice)?.Device;
            _selectedSensorDevice = (_sensorDeviceComboBox.SelectedItem as DeviceChoice)?.Device;
        }
    }

    private static string BluetoothDeviceKey(BluetoothDeviceInfo device)
        => $"{device.Kind}:{(string.IsNullOrWhiteSpace(device.Address) ? device.Id : device.Address)}";

    private void OnBluetoothScanTick(object? sender, EventArgs e)
    {
        int elapsed = Math.Clamp((int)Stopwatch.GetElapsedTime(_bluetoothScanStartedAt).TotalSeconds, 0, BluetoothScanSeconds);
        _bluetoothScanProgress.Value = elapsed;
        SetBluetoothDiscoveryStatus($"Bluetooth: scanning {elapsed}/{BluetoothScanSeconds} s; "
            + $"{_discoveredDevices.Count} paired/unpaired devices discovered.");
    }

    private void OnStopBluetoothScanClick(object? sender, EventArgs e)
    {
        _scanStoppedByUser = true;
        _stopBluetoothScanButton.Enabled = false;
        _bluetoothScanCancellation?.Cancel();
    }

    private void SetBluetoothDiscoveryStatus(string status)
    {
        _bluetoothScanStatus.Text = status.Replace('\r', ' ').Replace('\n', ' ');
        _bluetoothScanStatus.ToolTipText = status;
        _reportBluetoothDiscoveryStatus?.Invoke(status);
    }

    private async void OnBluetoothOptionsClosing(object? sender, FormClosingEventArgs e)
    {
        if (_bluetoothScanTask is not { IsCompleted: false } task)
        {
            return;
        }

        e.Cancel = true;
        if (_closingBluetoothOptions)
        {
            return;
        }

        _closingBluetoothOptions = true;
        DialogResult result = DialogResult;
        Enabled = false;
        try
        {
            _bluetoothScanCancellation?.Cancel();
            await task;
            DialogResult = result;
            Close();
        }
        catch (Exception exception)
        {
            Trace.TraceError(exception.ToString());
            SetBluetoothDiscoveryStatus($"Bluetooth discovery cleanup failed: {exception.Message}");
            _closingBluetoothOptions = false;
            Enabled = true;
        }
    }

    private void OnBluetoothOptionsDisposed(object? sender, EventArgs e)
        => _bluetoothScanCancellation?.Cancel();

    /// <summary>Associates a discovery caption with the selected device identity.</summary>
    private sealed record DeviceChoice(string Caption, BluetoothDeviceInfo? Device);
}
