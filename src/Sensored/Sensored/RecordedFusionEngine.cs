using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Restores recorded configuration epochs on the sequential replay owner.</summary>
internal sealed class RecordedFusionEngine :
    IFusionEngine, IFusionConfigurationSource, IFusionTimingPolicy, IVehicleSpeedConsumer, ITabletFrameController,
    IFusionPlausibilityController
{
    private readonly ConfigurationEpoch[] _epochs;
    private int _index;
    private IFusionEngine _engine;

    private RecordedFusionEngine(ConfigurationEpoch[] epochs)
    {
        _epochs = epochs;
        _engine = FusionEngineFactory.CreateFromRecordedConfiguration(epochs[0].Configuration);
        Configuration = new { Engine = "recorded-configuration-epochs", Version = 1, Epochs = epochs };
    }

    /// <summary>Loads recorded provenance, leaving older recordings on the mount-free fallback.</summary>
    internal static RecordedFusionEngine? Open(string folder)
    {
        string path = Path.Combine(folder, "v2.manifest.json");
        if (!File.Exists(path))
        {
            return null;
        }

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(path));
        if (!manifest.RootElement.TryGetProperty("EngineConfigurations", out JsonElement configurations))
        {
            return null;
        }

        if (manifest.RootElement.GetProperty("Schema").GetString() != "warp-sensor-v2-pair"
            || manifest.RootElement.GetProperty("Version").GetInt32() != 2
            || configurations.ValueKind != JsonValueKind.Array
            || configurations.GetArrayLength() > 256)
        {
            throw new InvalidDataException("Unsupported recorded engine-configuration manifest.");
        }

        List<ConfigurationEpoch> epochs = [];
        foreach (JsonElement entry in configurations.EnumerateArray())
        {
            int epoch = entry.GetProperty("Epoch").GetInt32();
            long first = entry.GetProperty("FirstObservedInputSequence").GetInt64();
            string json = entry.GetProperty("ConfigurationJson").GetString()
                ?? throw new InvalidDataException("Missing recorded fusion configuration.");
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
            if (!string.Equals(hash, entry.GetProperty("ConfigurationHash").GetString(), StringComparison.OrdinalIgnoreCase)
                || (epochs.Count > 0 && (epoch <= epochs[^1].Epoch || first <= epochs[^1].FirstObservedInputSequence)))
            {
                throw new InvalidDataException("Recorded fusion configurations are corrupt or unordered.");
            }

            using JsonDocument configuration = JsonDocument.Parse(json);
            // Validate every epoch before starting replay, not halfway through a drive.
            _ = FusionEngineFactory.CreateFromRecordedConfiguration(configuration.RootElement);
            epochs.Add(new(epoch, first, configuration.RootElement.Clone(),
                configuration.RootElement.GetProperty(nameof(VehicleMount)).Deserialize<VehicleMountReference>()));
        }

        return epochs.Count == 0 ? null : new(epochs.ToArray());
    }

    /// <summary>Selects the epoch before restoring this input's recorded controls or warming up.</summary>
    internal void PrepareInput(SensorEnvelope input)
    {
        int index = 0;
        while (index + 1 < _epochs.Length && _epochs[index + 1].FirstObservedInputSequence <= input.Sequence)
        {
            index++;
        }

        if (index != _index)
        {
            _engine = FusionEngineFactory.CreateFromRecordedConfiguration(_epochs[index].Configuration);
            _index = index;
        }
    }

    /// <summary>Gets the physical installation in the active recorded epoch, never the live profile.</summary>
    internal VehicleMountReference? VehicleMount
        => _epochs[_index].VehicleMount;

    /// <inheritdoc/>
    public FusionEstimate? Process(SensorEnvelope envelope)
    {
        PrepareInput(envelope);

        return _engine.Process(envelope);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        _index = 0;
        _engine = FusionEngineFactory.CreateFromRecordedConfiguration(_epochs[0].Configuration);
    }

    /// <inheritdoc/>
    public object Configuration { get; }

    /// <inheritdoc/>
    public double MaximumGpsFutureOffsetSeconds
        => ((IFusionTimingPolicy)_engine).MaximumGpsFutureOffsetSeconds;

    /// <inheritdoc/>
    public void SetVehicleSpeed(VehicleSpeedMeasurement? measurement)
        => ((IVehicleSpeedConsumer)_engine).SetVehicleSpeed(measurement);

    /// <inheritdoc/>
    public TabletFrameStatus FrameStatus
        => ((ITabletFrameController)_engine).FrameStatus;

    /// <inheritdoc/>
    public bool HasSessionDownLock
        => ((ITabletFrameController)_engine).HasSessionDownLock;

    /// <inheritdoc/>
    public void BeginTabletFrameCapture()
        => ((ITabletFrameController)_engine).BeginTabletFrameCapture();

    /// <inheritdoc/>
    public bool TryAcceptTabletFrame(out string? reason)
        => ((ITabletFrameController)_engine).TryAcceptTabletFrame(out reason);

    /// <inheritdoc/>
    public void ApplySessionDownLock(SessionDownLock sample)
        => ((ITabletFrameController)_engine).ApplySessionDownLock(sample);

    /// <inheritdoc/>
    public void ScheduleSessionDownLock(SessionDownLock sample)
        => ((ITabletFrameController)_engine).ScheduleSessionDownLock(sample);

    /// <inheritdoc/>
    public void ClearSessionDownLock()
        => ((ITabletFrameController)_engine).ClearSessionDownLock();

    /// <inheritdoc/>
    public FusionPlausibilitySnapshot CapturePlausibility(
        SensorEnvelope input, FusionEstimate estimate, PlausibilitySensorReading? comparison)
        => ((IFusionPlausibilityController)_engine).CapturePlausibility(input, estimate, comparison);

    /// <inheritdoc/>
    public void PostPlausibility(FusionPlausibilityFeedback feedback)
        => ((IFusionPlausibilityController)_engine).PostPlausibility(feedback);

    /// <summary>Dates an immutable configuration by its original canonical input sequence.</summary>
    private sealed record ConfigurationEpoch(
        int Epoch, long FirstObservedInputSequence, JsonElement Configuration, VehicleMountReference? VehicleMount);
}
