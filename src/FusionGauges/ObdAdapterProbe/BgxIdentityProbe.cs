using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using WarpToolkit.Windows.Bluetooth;

namespace ObdAdapterProbe;

internal static class BgxIdentityProbe
{
    // Silicon Labs BGX Xpress Streaming Service, not a BlueDriver protocol definition.
    internal static readonly Guid ServiceUuid = new("331a36f5-2459-45ea-9d95-6142f0c4b307");
    internal static readonly Guid ReceiveUuid = new("a9da6040-0823-4995-94ec-9ce41ca28833");
    internal static readonly Guid TransmitUuid = new("a73e9a10-628f-4494-a099-12efaf72258f");
    internal static readonly Guid ModeUuid = new("75a9f022-af03-4e41-b4bc-9de90a47d50b");

    internal static async Task<string> RunAsync(BluetoothDeviceProfile profile, TextWriter log,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource connection = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        connection.CancelAfter(TimeSpan.FromSeconds(15));
        using BluetoothLEDevice device = await BluetoothLEDevice.FromIdAsync(profile.Device.Id)
            .AsTask(connection.Token)
            ?? throw new InvalidOperationException("BGX device could not be opened.");
        GattDeviceServicesResult services = await device.GetGattServicesForUuidAsync(ServiceUuid,
            BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(services.Status, "Opening BGX streaming service");
        if (services.Services.Count != 1)
        {
            foreach (GattDeviceService found in services.Services)
            {
                found.Dispose();
            }

            throw new InvalidOperationException("Expected exactly one BGX streaming service.");
        }

        using GattDeviceService service = services.Services[0];
        GattCharacteristicsResult characteristics = await service.GetCharacteristicsAsync(
            BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(characteristics.Status, "Reading BGX characteristics");
        GattCharacteristic rx = characteristics.Characteristics.Single(c => c.Uuid == ReceiveUuid);
        GattCharacteristic tx = characteristics.Characteristics.Single(c => c.Uuid == TransmitUuid);
        GattCharacteristic mode = characteristics.Characteristics.Single(c => c.Uuid == ModeUuid);
        GattReadResult read = await mode.ReadValueAsync(BluetoothCacheMode.Uncached).AsTask(connection.Token);
        Check(read.Status, "Reading BGX bus mode");
        CryptographicBuffer.CopyToByteArray(read.Value, out byte[] modeBytes);
        await log.WriteLineAsync($"BGX bus mode HEX: {Convert.ToHexString(modeBytes)}");
        if (modeBytes is not [1])
        {
            throw new InvalidOperationException("BGX is not in Streaming Mode (01). Probe will not change bus mode.");
        }

        ElmIdentityProbe parser = new();
        TaskCompletionSource<string> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Lock receiveLock = new();
        TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> handler = (_, e) =>
        {
            lock (receiveLock)
            {
                if (response.Task.IsCompleted)
                {
                    return;
                }

                CryptographicBuffer.CopyToByteArray(e.CharacteristicValue, out byte[] bytes);
                log.WriteLine($"RX HEX: {Convert.ToHexString(bytes)}");
                try
                {
                    if (parser.Append(bytes) is { } identity)
                    {
                        response.TrySetResult(identity);
                    }
                }
                catch (FormatException error)
                {
                    response.TrySetException(error);
                }
            }
        };
        tx.ValueChanged += handler;
        bool subscribed = false;
        try
        {
            Check(await tx.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify).AsTask(connection.Token),
                "Subscribing to BGX Peripheral Tx");
            subscribed = true;
            await log.WriteLineAsync("BGX connected; legacy flow control (Rx notifications disabled). TX ASCII: ATI\\r");
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            IBuffer buffer = CryptographicBuffer.CreateFromByteArray("ATI\r"u8.ToArray());
            GattWriteResult write = await rx.WriteValueWithResultAsync(buffer, GattWriteOption.WriteWithResponse)
                .AsTask(timeout.Token);
            Check(write.Status, "Writing ATI to BGX Peripheral Rx");
            try
            {
                return await response.Task.WaitAsync(timeout.Token);
            }
            catch (OperationCanceledException e) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("BGX connected but no complete ATI response within five seconds; BlueDriver protocol remains unverified.", e);
            }
        }
        finally
        {
            tx.ValueChanged -= handler;
            if (subscribed)
            {
                using CancellationTokenSource cleanup = new(TimeSpan.FromSeconds(3));
                try
                {
                    GattCommunicationStatus status = await tx.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None).AsTask(cleanup.Token);
                    if (status != GattCommunicationStatus.Success)
                    {
                        await log.WriteLineAsync($"BGX notification cleanup: {status}");
                    }
                }
                catch (OperationCanceledException) when (cleanup.IsCancellationRequested)
                {
                    await log.WriteLineAsync("BGX notification cleanup timed out; closing device handles.");
                }
            }
        }
    }

    private static void Check(GattCommunicationStatus status, string operation)
    {
        if (status != GattCommunicationStatus.Success)
        {
            throw new InvalidOperationException($"{operation}: {status}");
        }
    }
}
