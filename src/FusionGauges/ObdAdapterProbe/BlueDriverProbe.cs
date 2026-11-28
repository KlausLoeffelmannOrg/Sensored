using System.Globalization;
using System.Text;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace ObdAdapterProbe;

// Wire format per the community analysis at https://github.com/sjalkote/bluedriver-reverse-engineering
// (independently re-implemented; no vendor specification exists).
internal static class BlueDriverCodec
{
    internal const byte Key = 0x26;

    internal static byte[] Transform(ReadOnlySpan<byte> bytes)
    {
        byte[] result = new byte[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            byte value = bytes[i];
            result[i] = value is 0x0D or 0x3E ? value : (byte)(value ^ Key);
        }

        return result;
    }

    internal static byte[] EncodeCommand(string command)
        => Transform(Encoding.ASCII.GetBytes(command.TrimEnd('\r') + "\r"));
}

internal sealed class BlueDriverResponseFramer
{
    private readonly StringBuilder _response = new();

    /// <summary>Appends already-decoded bytes; returns the response text once the '&gt;' prompt arrives.</summary>
    internal string? Append(ReadOnlySpan<byte> decoded)
    {
        foreach (byte value in decoded)
        {
            if (value == '>')
            {
                string response = _response.ToString().Trim();
                _response.Clear();
                return response;
            }

            if (value > 127 || (value < 32 && value is not (9 or 10 or 13)))
            {
                throw new FormatException($"Decoded BlueDriver response contains non-ASCII byte 0x{value:X2}.");
            }

            if (_response.Length >= 16384)
            {
                throw new FormatException("BlueDriver response exceeded 16 KiB.");
            }

            _response.Append((char)value);
        }

        return null;
    }
}

internal static class BlueDriverPid
{
    internal static readonly string[] LivePids =
        ["0100", "0120", "0140", "010D", "010C", "014C", "0105", "012F", "0142", "0146", "0111", "0104", "015B"];

    internal static string Describe(string pid, string response)
    {
        string[] tokens = response.Split([' ', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        byte[] data = [];
        if (tokens.Length > 0 && tokens.All(t => t.Length == 2 && byte.TryParse(t,
            NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _)))
        {
            data = tokens.Select(t => byte.Parse(t, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture)).ToArray();
        }

        // Some firmware states echo the mode/PID (41 0D ..); strip it if present.
        if (data.Length >= 3 && data[0] == 0x41
            && data[1] == byte.Parse(pid.AsSpan(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture))
        {
            data = data[2..];
        }

        return (pid, data) switch
        {
            ("010D", [var a, ..]) => $"{a} km/h",
            ("010C", [var a, var b, ..]) => $"{((a << 8) | b) / 4.0:0.##} rpm",
            ("0105", [var a, ..]) => $"{a - 40} °C coolant",
            ("0142", [var a, var b, ..]) => $"{((a << 8) | b) / 1000.0:0.000} V module",
            _ => $"undecoded: '{response}'"
        };
    }
}

internal static class BlueDriverProbe
{
    internal static readonly Guid CommandUuid = BgxIdentityProbe.ReceiveUuid;
    internal static readonly Guid DataUuid = BgxIdentityProbe.TransmitUuid;
    internal static readonly Guid StatusUuid = BgxIdentityProbe.ModeUuid;

    // Documented official-app init order; standard AT commands after it reportedly break the firmware state.
    internal static readonly string[] InitSequence = ["LMD", "LML0", "LMI", "LMD", "LMDP", "LMI", "LMX1", "LMX0"];

    internal static async Task<object> RunAsync(string deviceId, bool live, TextWriter log,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource connection = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connection.CancelAfter(TimeSpan.FromSeconds(15));
        using BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(deviceId).AsTask(connection.Token)
            ?? throw new InvalidOperationException("BlueDriver could not be opened.");
        GattDeviceServicesResult services = await device.GetGattServicesForUuidAsync(BgxIdentityProbe.ServiceUuid,
            BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(services.Status, "Opening BGX streaming service");
        if (services.Services.Count != 1)
        {
            foreach (GattDeviceService found in services.Services)
            {
                found.Dispose();
            }

            throw new InvalidOperationException("Expected exactly one BGX streaming service; not a BlueDriver layout.");
        }

        using GattDeviceService service = services.Services[0];
        GattCharacteristicsResult characteristics = await service.GetCharacteristicsAsync(
            BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(characteristics.Status, "Reading BGX characteristics");
        GattCharacteristic cmd = characteristics.Characteristics.Single(c => c.Uuid == CommandUuid);
        GattCharacteristic data = characteristics.Characteristics.Single(c => c.Uuid == DataUuid);
        GattCharacteristic status = characteristics.Characteristics.Single(c => c.Uuid == StatusUuid);

        GattReadResult mode = await status.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(mode.Status, "Reading BGX bus mode");
        CryptographicBuffer.CopyToByteArray(mode.Value, out byte[] modeBytes);
        await log.WriteLineAsync($"BGX bus mode HEX: {Convert.ToHexString(modeBytes)}");
        if (modeBytes is not [1])
        {
            throw new InvalidOperationException("BGX is not in Streaming Mode (01). Probe will not change bus mode.");
        }

        BlueDriverResponseFramer framer = new();
        Lock receiveLock = new();
        TaskCompletionSource<string>? pending = null;
        TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> onData = (_, e) =>
        {
            CryptographicBuffer.CopyToByteArray(e.CharacteristicValue, out byte[] wire);
            // Credit-based flow control: every fragment must be acknowledged with 01 <len> 00.
            Task ack = AcknowledgeAsync(data, wire.Length, log);
            byte[] decoded = BlueDriverCodec.Transform(wire);
            lock (receiveLock)
            {
                log.WriteLine($"DATA RX HEX: {Convert.ToHexString(wire)} -> '{Printable(decoded)}'");
                try
                {
                    if (framer.Append(decoded) is { } response)
                    {
                        pending?.TrySetResult(response);
                    }
                }
                catch (FormatException error)
                {
                    pending?.TrySetException(error);
                }
            }
        };
        TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> onControl = (sender, e) =>
        {
            CryptographicBuffer.CopyToByteArray(e.CharacteristicValue, out byte[] bytes);
            log.WriteLine($"{(sender.Uuid == CommandUuid ? "CMD" : "STATUS")} RX HEX: {Convert.ToHexString(bytes)}");
        };

        data.ValueChanged += onData;
        cmd.ValueChanged += onControl;
        status.ValueChanged += onControl;
        List<GattCharacteristic> subscribed = [];
        try
        {
            foreach (GattCharacteristic characteristic in new[] { cmd, data, status })
            {
                Check(await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(connection.Token),
                    $"Subscribing to {characteristic.Uuid}");
                subscribed.Add(characteristic);
            }

            await log.WriteLineAsync("Opening 255-byte receive credit window: DATA TX HEX 00FF7F");
            await WriteAsync(data, [0x00, 0xFF, 0x7F], connection.Token);

            async Task<string> SendAsync(string command)
            {
                TaskCompletionSource<string> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
                lock (receiveLock)
                {
                    pending = response;
                }

                byte[] wire = BlueDriverCodec.EncodeCommand(command);
                await log.WriteLineAsync($"CMD TX '{command}' HEX: {Convert.ToHexString(wire)}");
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                await WriteAsync(cmd, wire, timeout.Token);
                try
                {
                    string text = await response.Task.WaitAsync(timeout.Token);
                    await log.WriteLineAsync($"  <- '{Printable(Encoding.ASCII.GetBytes(text))}'");
                    return text;
                }
                catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"No '>'-terminated response to {command} within five seconds.", e);
                }
            }

            string identity = await SendAsync("LMI");
            if (!identity.Contains("OSAPI", StringComparison.OrdinalIgnoreCase))
            {
                // The first LMI after connect is documented to answer '?'.
                identity = await SendAsync("LMI");
            }

            if (!identity.Contains("OSAPI", StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException($"LMI did not return an OSAPI identity: '{identity}'.");
            }

            if (!live)
            {
                return new { Identity = identity, ProtocolDecoded = true, VehicleTelemetryVerified = false };
            }

            Dictionary<string, string> init = [];
            foreach (string command in InitSequence)
            {
                init[command] = await SendAsync(command);
            }

            List<(string Pid, string Raw, string Value)> readings = [];
            for (int round = 0; round < 2; round++)
            {
                foreach (string pid in BlueDriverPid.LivePids)
                {
                    string response = await SendAsync(pid);
                    readings.Add((pid, response, BlueDriverPid.Describe(pid, response)));
                }
            }

            return new
            {
                Identity = identity,
                ObdProtocol = init["LMDP"],
                Readings = readings.Select(r => new { r.Pid, r.Raw, r.Value }).ToArray(),
                VehicleTelemetryVerified = readings.Any(r => !r.Value.StartsWith("undecoded", StringComparison.Ordinal))
            };
        }
        finally
        {
            data.ValueChanged -= onData;
            cmd.ValueChanged -= onControl;
            status.ValueChanged -= onControl;
            using CancellationTokenSource cleanup = new(TimeSpan.FromSeconds(3));
            foreach (GattCharacteristic characteristic in subscribed)
            {
                try
                {
                    await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None).AsTask(cleanup.Token);
                }
                catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException)
                {
                    await log.WriteLineAsync($"Notification cleanup for {characteristic.Uuid}: {e.GetType().Name}");
                }
            }
        }
    }

    private static async Task AcknowledgeAsync(GattCharacteristic data, int length, TextWriter log)
    {
        try
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            await WriteAsync(data, [0x01, (byte)length, 0x00], timeout.Token);
        }
        catch (Exception e) when (e is OperationCanceledException or InvalidOperationException or ObjectDisposedException)
        {
            log.WriteLine($"DATA ACK failed: {e.Message}");
        }
    }

    private static async Task WriteAsync(GattCharacteristic characteristic, byte[] bytes, CancellationToken token)
    {
        IBuffer buffer = CryptographicBuffer.CreateFromByteArray(bytes);
        GattWriteResult result = await characteristic.WriteValueWithResultAsync(buffer,
            GattWriteOption.WriteWithoutResponse).AsTask(token);
        Check(result.Status, $"Writing {characteristic.Uuid}");
    }

    private static string Printable(byte[] bytes)
        => Encoding.ASCII.GetString(bytes).Replace("\r", "\\r").Replace("\n", "\\n");

    private static void Check(GattCommunicationStatus status, string operation)
    {
        if (status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"{operation}: {status}");
        }
    }
}
