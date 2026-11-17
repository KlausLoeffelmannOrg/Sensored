using System.IO.Compression;
using System.Text.Json.Serialization;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace WinFormsFusionGauges.WinFormsApp;

/// <summary>Defines persistent, tablet-oriented source, appearance and recording choices.</summary>
internal sealed record FusionGaugesOptions
{
    public WindowsSensorSourceKind SourceKind { get; init; } = WindowsSensorSourceKind.ImuAndGps;
    public bool UseObdSpeed { get; init; }
    public ObdAdapterKind ObdAdapter { get; init; } = ObdAdapterKind.ObdLinkCx;
    public VehicleMountReference? VehicleMount { get; init; }
    public uint GpsReportIntervalMilliseconds { get; init; } = 200;
    public string? SerialPortName { get; init; }
    public SerialGnssProfile GnssProfile { get; init; } = SerialGnssProfile.Passive;
    public int? TargetFixIntervalMilliseconds { get; init; }
    [JsonIgnore]
    public bool AllowReceiverCommandsThisSession { get; init; }
    public int IconSize { get; init; } = 36;
    public float FontSize { get; init; } = 10;
    public int AccelerationDigits { get; init; } = 2;
    public WarpToolkit.WinForms.Sensors.DirectX.SpeedometerSettings Speedometer { get; init; }
        = WarpToolkit.WinForms.Sensors.DirectX.SpeedometerSettings.Default;
    public string RecordingFolder { get; init; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WARP", "FusionGauges", "Recordings");
    public SensorRecordingFormats Formats { get; init; } = SensorRecordingFormats.All;
    public int RawSnapshotsPerSecond { get; init; } = 10;
    public string NamePrefix { get; init; } = "drive";
    public bool AutomaticNaming { get; init; } = true;
    public bool AutoRecording { get; init; }
    public string MarkerLabel { get; init; } = "Marker";
    public bool MirrorEnabled { get; init; }
    public string MirrorFolder { get; init; } = "";
    public CompressionLevel CompressionLevel { get; init; } = CompressionLevel.Optimal;

    internal WindowsSensorSourceOptions CreateSourceOptions()
        => new()
        {
            Sources = SourceKind,
            GpsReportIntervalMilliseconds = GpsReportIntervalMilliseconds,
            SerialGpsOptions = new()
            {
                PortName = string.IsNullOrWhiteSpace(SerialPortName) ? null : SerialPortName.Trim(),
                GnssProfile = GnssProfile,
                TargetFixIntervalMilliseconds = TargetFixIntervalMilliseconds,
                AllowReceiverCommandsThisSession = AllowReceiverCommandsThisSession
            }
        };

    internal SensorRecordingRequest CreateRecordingRequest(string? name = null)
        => new()
        {
            RootFolderPath = RecordingFolder,
            Formats = SensorRecordingFormats.CompactJson | (Formats & SensorRecordingFormats.VerboseJsonLines),
            NamePrefix = name ?? NamePrefix
        };

    internal void Validate()
    {
        CreateSourceOptions().Validate();
        _ = ObdAdapters.GetName(ObdAdapter);
        CreateRecordingRequest().Validate();
        WarpToolkit.Sensors.Fusion.SensorV2Recording.ValidateRate(RawSnapshotsPerSecond);
        if (TargetFixIntervalMilliseconds is < 100 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(TargetFixIntervalMilliseconds),
                "Target fix interval must be between 100 and 1000 ms, or Dynamic.");
        }

        if (!Enum.IsDefined(GnssProfile))
        {
            throw new ArgumentOutOfRangeException(nameof(GnssProfile));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(AccelerationDigits);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(AccelerationDigits, 5);
        ArgumentNullException.ThrowIfNull(Speedometer);
        Speedometer.Validate();

        if (IconSize is not (24 or 36 or 48) || FontSize is not (9 or 10 or 12))
        {
            throw new ArgumentException("Select a supported font and icon size.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(MarkerLabel);

        if (MirrorEnabled)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(MirrorFolder);
            _ = Path.GetFullPath(MirrorFolder);
        }
    }
}
