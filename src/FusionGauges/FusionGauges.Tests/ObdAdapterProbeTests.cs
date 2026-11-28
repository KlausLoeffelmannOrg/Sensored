using ObdAdapterProbe;
using WarpToolkit.Windows.Bluetooth;

namespace FusionGauges.Tests;

public sealed class ObdAdapterProbeTests
{
    [Fact]
    public void ProbeRequiresExplicitValidEndpoints()
    {
        Assert.False(Program.ValidArguments(["probe", "BlueDriver"]));
        Assert.False(Program.ValidArguments(["probe", "BlueDriver", "bad", "bad", "bad"]));
        Assert.True(Program.ValidArguments(["inspect", "BlueDriver-E1D8"]));
    }

    [Fact]
    public void DuplicateNamesRequireAddressSelection()
    {
        BluetoothDeviceInfo first = Device("first");
        BluetoothDeviceInfo second = Device("second");
        Assert.Throws<InvalidOperationException>(() => Program.SelectDevice([first, second], first.Name));
        Assert.Same(second, Program.SelectDevice([first, second], "second"));
    }

    [Fact]
    public void EndpointsMustBelongToTheSelectedServiceAndSupportTheirOperations()
    {
        Guid service = Guid.NewGuid(), write = Guid.NewGuid(), notify = Guid.NewGuid();
        BluetoothDeviceProfile profile = new(Device("address"), [],
            [new(service, [new(write, BluetoothGattCharacteristicProperties.Write),
                new(notify, BluetoothGattCharacteristicProperties.Read)], false)],
            BluetoothTransportVerdict.Unknown);
        Assert.Throws<ArgumentException>(() => Program.SelectEndpoints(profile, service, write, notify));
        profile = profile with
        {
            GattServices = [new(service, [new(write, BluetoothGattCharacteristicProperties.WriteWithoutResponse),
                new(notify, BluetoothGattCharacteristicProperties.Indicate)], false)]
        };
        Assert.Throws<ArgumentException>(() => Program.SelectEndpoints(profile, Guid.NewGuid(), write, notify));
        Assert.Equal(notify, Program.SelectEndpoints(profile, service, write, notify).GattNotifyCharacteristicUuid);
    }

    [Theory]
    [InlineData("a9da6040-0823-4995-94ec-9ce41ca28833")]
    [InlineData("75a9f022-af03-4e41-b4bc-9de90a47d50b")]
    public void BgxFlowControlAndModeCannotBeUsedAsDataNotifications(string notify)
    {
        BluetoothDeviceProfile profile = new(Device("address"), [], [], BluetoothTransportVerdict.Unknown);
        Assert.Throws<ArgumentException>(() => Program.SelectEndpoints(profile,
            BgxIdentityProbe.ServiceUuid, BgxIdentityProbe.ReceiveUuid, Guid.Parse(notify)));
    }

    [Theory]
    [InlineData("5C:C7:C1:02:E1:D8", true)]
    [InlineData("5cc7c102e1d8", true)]
    [InlineData("BlueDriver-E1D8", false)]
    [InlineData("GG:C7:C1:02:E1:D8", false)]
    public void BleAddressSelectorsBypassDiscovery(string selector, bool expected)
        => Assert.Equal(expected, Program.TryParseAddress(selector, out _));

    [Fact]
    public void IdentityIsFramedAcrossNotificationsNotByPacketBoundaries()
    {
        ElmIdentityProbe parser = new();
        Assert.Null(parser.Append("ATI\rEL"u8));
        Assert.Null(parser.Append("M327 v1.5\r"u8));
        Assert.Equal("ATI\rELM327 v1.5", parser.Append(">"u8));
    }

    [Theory]
    [InlineData("?\r>")]
    [InlineData("OK\r>")]
    [InlineData("ATI\r>")]
    [InlineData("NOT ELM327\r>")]
    public void PromptAloneDoesNotEstablishIdentity(string response)
        => Assert.Throws<FormatException>(() => new ElmIdentityProbe().Append(System.Text.Encoding.ASCII.GetBytes(response)));

    [Fact]
    public void BinaryAndOversizedResponsesAreRejected()
    {
        Assert.Throws<FormatException>(() => new ElmIdentityProbe().Append([0x80]));
        Assert.Throws<FormatException>(() => new ElmIdentityProbe().Append([0x19, 0x0d, 0x3e]));
        Assert.Throws<FormatException>(() => new ElmIdentityProbe().Append(new byte[16385].Select(_ => (byte)'A').ToArray()));
    }

    [Fact]
    public void ObservedBlueDriverReplyToAtiDecodesToUnknownCommandPrompt()
    {
        byte[] decoded = BlueDriverCodec.Transform([0x19, 0x0D, 0x3E]);
        Assert.Equal("?", new BlueDriverResponseFramer().Append(decoded));
    }

    [Fact]
    public void BlueDriverCommandsAreXorEncodedExceptCarriageReturnAndPrompt()
    {
        byte[] wire = BlueDriverCodec.EncodeCommand("LMI");
        Assert.Equal(new byte[] { (byte)'L' ^ 0x26, (byte)'M' ^ 0x26, (byte)'I' ^ 0x26, 0x0D }, wire);
        Assert.Equal("LMI\r"u8.ToArray(), BlueDriverCodec.Transform(wire));
    }

    [Fact]
    public void BlueDriverResponsesAreFramedAcrossFragments()
    {
        BlueDriverResponseFramer framer = new();
        Assert.Null(framer.Append(BlueDriverCodec.Transform(BlueDriverCodec.Transform("OSAPI v2"u8))));
        Assert.Equal("OSAPI v2.60\rOK", framer.Append(".60\rOK\r>"u8));
        Assert.Throws<FormatException>(() => framer.Append([0x80]));
    }

    [Theory]
    [InlineData("010D", "3C", "60 km/h")]
    [InlineData("010C", "0F 80", "992 rpm")]
    [InlineData("010C", "41 0C 0F 80", "992 rpm")]
    [InlineData("0105", "5A", "50 °C coolant")]
    [InlineData("010D", "NO DATA", "undecoded: 'NO DATA'")]
    public void BlueDriverPidResponsesDecodeStandardFormulas(string pid, string response, string expected)
        => Assert.Equal(expected, BlueDriverPid.Describe(pid, response));

    [Fact]
    public void BlueDriverCommandAcceptsOnlyOptionalLiveFlag()
    {
        Assert.True(Program.ValidArguments(["bluedriver", "5C:C7:C1:02:E1:D8"]));
        Assert.True(Program.ValidArguments(["bluedriver", "5C:C7:C1:02:E1:D8", "--live"]));
        Assert.False(Program.ValidArguments(["bluedriver", "5C:C7:C1:02:E1:D8", "--reset"]));
        Assert.False(Program.ValidArguments(["bluedriver", " "]));
    }

    private static BluetoothDeviceInfo Device(string address)
        => new(address, "BlueDriver", address, BluetoothDeviceKind.LowEnergy, true, null, []);
}
