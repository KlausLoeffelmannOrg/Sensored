using System.Text.Json;
using System.Text.Json.Serialization;
using System.Globalization;
using Windows.Devices.Bluetooth;
using WarpToolkit.Windows.Bluetooth;

namespace ObdAdapterProbe;

internal static class Program
{
    private const string Usage = """
        ObdAdapterProbe list
        ObdAdapterProbe inspect <exact-name-or-address>
        ObdAdapterProbe probe <exact-name-or-address> <service-uuid> <write-uuid> <notify-uuid>
        ObdAdapterProbe bluedriver <exact-name-or-address> [--live]

        list/inspect discover and read service metadata only; they send no adapter commands.
        probe subscribes to the explicitly selected endpoints and sends only ATI\r.
        Use probe ONLY on confirmed diagnostic UART endpoints, never firmware-update services.
        A Bluetooth connection or ELM identity does not establish vehicle telemetry support.
        Address selectors use BLE directly, bypassing discovery. Name selectors and list
        use paired devices. BGX probes read bus mode and use legacy acknowledged writes.
        bluedriver uses the community-documented XOR/credit protocol and sends only LMI;
        --live additionally sends the official-app LM init sequence and read-only mode 01 PIDs
        (ignition/ACC on, parked).        Close other adapter apps first. Operate only while parked.
        """;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static async Task<int> Main(string[] args)
    {
        if (args is ["--help"] or ["-h"])
        {
            Console.WriteLine(Usage);
            return 0;
        }

        if (!ValidArguments(args))
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        using CancellationTokenSource stop = new(TimeSpan.FromSeconds(60));
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            stop.Cancel();
        };
        Console.CancelKeyPress += cancel;

        try
        {
            BluetoothDeviceInfo device;
            if (args.Length > 1 && TryParseAddress(args[1], out ulong address))
            {
                Console.Error.WriteLine("Opening BLE address directly; bypassing device discovery.");
                using BluetoothLEDevice opened = await BluetoothLEDevice.FromBluetoothAddressAsync(address)
                    .AsTask(stop.Token)
                    ?? throw new InvalidOperationException($"BLE device '{args[1]}' could not be opened.");
                device = new(opened.DeviceId, opened.Name, args[1], BluetoothDeviceKind.LowEnergy,
                    opened.DeviceInformation.Pairing.IsPaired, null, []);
            }
            else
            {
                Console.Error.WriteLine("Discovering paired devices.");
                BluetoothDeviceScanner scanner = new();
                IReadOnlyList<BluetoothDeviceInfo> devices = await scanner.ScanAsync(
                    new()
                    {
                        IncludeUnpaired = false,
                        IncludeClassic = args[0] == "list",
                        IncludeAdvertisements = false
                    }, stop.Token);
                if (args[0] == "list")
                {
                    Console.WriteLine(JsonSerializer.Serialize(devices, s_jsonOptions));
                    return 0;
                }

                device = SelectDevice(devices, args[1]);
            }

            if (args[0] == "bluedriver")
            {
                if (device.Kind != BluetoothDeviceKind.LowEnergy)
                {
                    throw new ArgumentException("BlueDriver uses Bluetooth LE; select its LowEnergy entry or BLE address.");
                }

                bool live = args is [_, _, "--live"];
                Console.Error.WriteLine(live
                    ? "BlueDriver live probe: LM init + read-only mode 01 PIDs. Parked, ignition/ACC on."
                    : "BlueDriver identity probe: sends only LMI.");
                object result = await BlueDriverProbe.RunAsync(device.Id, live, Console.Error, stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, s_jsonOptions));
                return 0;
            }

            Console.Error.WriteLine($"Inspecting {device.Name} ({device.Kind}); no adapter commands sent.");
            BluetoothDeviceProfile profile = await new BluetoothDeviceInspector().InspectAsync(device, stop.Token);
            Console.WriteLine(JsonSerializer.Serialize(profile, s_jsonOptions));
            Console.Error.WriteLine("TransportVerdict describes byte transport, NOT OBD protocol compatibility.");
            if (args[0] == "inspect")
            {
                return 0;
            }

            profile = SelectEndpoints(profile, Guid.Parse(args[2]), Guid.Parse(args[3]), Guid.Parse(args[4]));
            Console.Error.WriteLine($"Explicit ELM identity probe: {profile.GattServiceUuid} / "
                + $"{profile.GattWriteCharacteristicUuid} / {profile.GattNotifyCharacteristicUuid}");
            string identity;
            if (profile.GattServiceUuid == BgxIdentityProbe.ServiceUuid)
            {
                identity = await BgxIdentityProbe.RunAsync(profile, Console.Error, stop.Token);
            }
            else
            {
                await using IBluetoothTransport transport = new BluetoothTransportFactory().Create(profile);
                transport.ConnectionStateChanged += (_, e) =>
                    Console.Error.WriteLine($"Transport: {e.State} {e.Error?.Message}");
                using CancellationTokenSource connection = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
                connection.CancelAfter(TimeSpan.FromSeconds(15));
                await transport.ConnectAsync(connection.Token);
                identity = await ElmIdentityProbe.RunAsync(transport, Console.Error, stop.Token);
            }
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Device = device.Name,
                Identity = identity,
                VehicleTelemetryVerified = false
            }, s_jsonOptions));
            return 0;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or BluetoothException
            or TimeoutException or FormatException or OperationCanceledException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}");
            if (e.InnerException is { } inner)
            {
                Console.Error.WriteLine($"{inner.GetType().Name}: {inner.Message}");
            }

            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }

    internal static bool ValidArguments(string[] args)
        => args is ["list"]
            || (args is ["inspect", var selector] && !string.IsNullOrWhiteSpace(selector))
            || (args is ["bluedriver", _] or ["bluedriver", _, "--live"] && !string.IsNullOrWhiteSpace(args[1]))
            || (args is ["probe", var target, var service, var write, var notify]
                && !string.IsNullOrWhiteSpace(target)
                && Guid.TryParse(service, out Guid s) && s != Guid.Empty
                && Guid.TryParse(write, out Guid w) && w != Guid.Empty
                && Guid.TryParse(notify, out Guid n) && n != Guid.Empty);

    internal static bool TryParseAddress(string text, out ulong address)
    {
        string compact = text.Replace(":", "").Replace("-", "");
        address = 0;
        return compact.Length == 12
            && ulong.TryParse(compact, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out address);
    }

    internal static BluetoothDeviceInfo SelectDevice(IReadOnlyList<BluetoothDeviceInfo> devices, string selector)
    {
        BluetoothDeviceInfo[] matches = devices.Where(device =>
            string.Equals(device.Name, selector, StringComparison.OrdinalIgnoreCase)
            || string.Equals(device.Address, selector, StringComparison.OrdinalIgnoreCase)).ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"Device '{selector}' was not discovered. Run list with the adapter powered."),
            _ => throw new InvalidOperationException($"Device name '{selector}' is ambiguous. Use its address from list.")
        };
    }

    internal static BluetoothDeviceProfile SelectEndpoints(BluetoothDeviceProfile inspected,
        Guid serviceUuid, Guid writeUuid, Guid notifyUuid)
    {
        if (serviceUuid == BgxIdentityProbe.ServiceUuid
            && (writeUuid != BgxIdentityProbe.ReceiveUuid || notifyUuid != BgxIdentityProbe.TransmitUuid))
        {
            throw new ArgumentException("BGX requires writes to Peripheral Rx and data notifications from Peripheral Tx, not Rx flow control or Mode.");
        }

        BluetoothGattServiceInfo? service = inspected.GattServices.SingleOrDefault(s => s.Uuid == serviceUuid);
        const BluetoothGattCharacteristicProperties writable =
            BluetoothGattCharacteristicProperties.Write | BluetoothGattCharacteristicProperties.WriteWithoutResponse;
        const BluetoothGattCharacteristicProperties notifiable =
            BluetoothGattCharacteristicProperties.Notify | BluetoothGattCharacteristicProperties.Indicate;
        if (inspected.Device.Kind != BluetoothDeviceKind.LowEnergy
            || service is null
            || !service.Characteristics.Any(c => c.Uuid == writeUuid && (c.Properties & writable) != 0)
            || !service.Characteristics.Any(c => c.Uuid == notifyUuid && (c.Properties & notifiable) != 0))
        {
            throw new ArgumentException("The selected BLE service must contain the specified writable and notifiable endpoints.");
        }

        return inspected with
        {
            TransportVerdict = BluetoothTransportVerdict.LowEnergyUartLike,
            RfcommServiceUuid = null,
            GattServiceUuid = serviceUuid,
            GattWriteCharacteristicUuid = writeUuid,
            GattNotifyCharacteristicUuid = notifyUuid
        };
    }
}
