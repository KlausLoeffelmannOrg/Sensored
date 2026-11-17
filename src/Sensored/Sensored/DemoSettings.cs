using WarpToolkit.ComponentModel;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.Windows.Sensors;
using WarpToolkit.Windows.Bluetooth;

namespace Sensored;

/// <summary>
///  Loads and saves the demo choices that are not already stored by the map and replay keys.
/// </summary>
internal static class DemoSettings
{
    internal const string DampingMillisecondsKey = "Display.DampingMilliseconds";
    internal const string AccelerationDampingKey = "Display.SpeedometerAccelerationDamping";
    internal const string GScaleKey = "Display.GScale";
    internal const string BaseFontPointSizeKey = "Display.BaseFontPointSize";
    internal const string SourceKindKey = "Source.Kind";
    internal const string SerialPortNameKey = "Source.SerialPortName";
    internal const string SerialGnssProfileKey = "Source.SerialGnssProfile";
    internal const string AccelerationDigitsKey = "Display.AccelerationDigits";
    internal const string GpsDigitsKey = "Display.GpsDigits";
    internal const string SpeedDigitsKey = "Display.SpeedDigits";
    internal const string RestDigitsKey = "Display.RestDigits";
    internal const string SpeedometerKey = "Display.Speedometer";
    internal const string RouteFilePathKey = "Map.RouteFilePath";
    internal const string FollowCurrentPositionKey = "Map.FollowCurrentPosition";
    internal const string AllowOnlineMapTilesKey = "Map.AllowOnlineTiles";
    internal const string RecordingRootFolderKey = "Recording.RootFolder";
    internal const string RecordingFormatsKey = "Recording.Formats";
    internal const string RawSnapshotRateKey = "Recording.RawSnapshotsPerSecond";
    internal const string RecordingMarkerLabelKey = "Recording.MarkerLabel";
    internal const string ReplayDirectoryKey = "Dialogs.ReplayDirectory";
    internal const string RouteDirectoryKey = "Dialogs.RouteDirectory";
    internal const string RecordingFolderDirectoryKey = "Dialogs.RecordingFolderDirectory";
    internal const string RawLogDirectoryKey = "Dialogs.RawLogDirectory";
    internal const string FrameModeKey = "Calibration.FrameMode";
    internal const string AssumeStaticMountedDeviceKey = "Calibration.AssumeStaticMountedDevice";
    internal const string VehicleMountKey = "Calibration.VehicleMount";
    internal const string UseObdKey = "Source.UseObd";
    internal const string SensorSourceKey = "Source.SensorDisplaySource";
    internal const string BluetoothSensorSearchMinutesKey = "Source.BluetoothSensorSearchMinutes";
    internal const string InternalSensorFrameKey = "Source.InternalSensorFrame";
    internal const string BluetoothSensorFrameKey = "Source.BluetoothSensorFrame";
    internal const string SphericalHorizonKey = "Display.SphericalHorizon";
    internal const string AlwaysOnKey = "Display.AlwaysOn";
    internal const string ObdAdapterKey = "Source.ObdAdapter";
    internal const string ObdDeviceKey = "Bluetooth.ObdDevice";
    internal const string BluetoothSensorDeviceKey = "Bluetooth.SensorDevice";
    internal const string ForceObdGaugesOnKey = "Display.ForceObdGaugesOn";
    internal const string MaximumRpmKey = "Display.MaximumRpm";
    internal const string ObdSearchMinutesKey = "Source.ObdSearchMinutes";
    internal const string ExceptionPopupSecondsKey = "Display.ExceptionPopupSeconds";
    internal const string GpsTargetIntervalKey = "Source.TargetGpsFixIntervalMilliseconds";
    internal const string WindowsGpsIntervalKey = "Source.WindowsGpsReportIntervalMilliseconds";
    internal const string ConfirmGpsReceiverChangesKey = "Source.ConfirmGpsReceiverChanges";
    internal const string ConfirmSensorStartKey = "Source.ConfirmSensorStart";
    internal const string ToolbarCommandTimeoutKey = "Display.ToolbarCommandTimeoutSeconds";
    internal const string ToolbarIdleTimeoutKey = "Display.ToolbarIdleTimeoutSeconds";

    /// <summary>
    ///  Applies saved display, source, route, and recording choices onto <paramref name="options"/>.
    /// </summary>
    internal static DemoOptions Load(IUserSettingsService? settings, DemoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (settings is null)
        {
            return options;
        }

        int damping = settings.Get(DampingMillisecondsKey, options.DampingMilliseconds);

        if (damping is < 0 or > 2000)
        {
            damping = options.DampingMilliseconds;
        }

        double gScale = settings.Get(GScaleKey, options.GScale);
        SpeedometerAccelerationDamping accelerationDamping = settings.Get(
            AccelerationDampingKey, options.AccelerationDamping);

        if (!Enum.IsDefined(accelerationDamping))
        {
            accelerationDamping = options.AccelerationDamping;
        }

        if (!double.IsFinite(gScale) || gScale is <= 0d or > 10d)
        {
            gScale = options.GScale;
        }

        float fontSize = settings.Get(BaseFontPointSizeKey, options.BaseFontPointSize);

        if (!float.IsFinite(fontSize) || fontSize is < 8f or > 36f)
        {
            fontSize = options.BaseFontPointSize;
        }

        WindowsSensorSourceKind sourceKind = settings.Get(SourceKindKey, options.SourceKind);

        if (!Enum.IsDefined(sourceKind))
        {
            sourceKind = options.SourceKind;
        }

        string? serialPort = NormalizeOptional(settings.Get(
            SerialPortNameKey,
            options.SerialPortName));
        SerialGnssProfile profile = settings.Get(SerialGnssProfileKey, options.SerialGnssProfile);
        if (!Enum.IsDefined(profile))
        {
            profile = SerialGnssProfile.Passive;
        }

        string? routePath = NormalizeOptional(settings.Get(
            RouteFilePathKey,
            options.RouteFilePath));
        string recordingFolder = settings.Get(
            RecordingRootFolderKey,
            options.RecordingRootFolder);

        if (string.IsNullOrWhiteSpace(recordingFolder))
        {
            recordingFolder = options.RecordingRootFolder;
        }

        SensorRecordingFormats recordingFormats = settings.Get(
            RecordingFormatsKey,
            options.RecordingFormats);

        if (recordingFormats == SensorRecordingFormats.None)
        {
            recordingFormats = options.RecordingFormats;
        }

        ReadingFormats formats = new ReadingFormats(
            settings.Get(AccelerationDigitsKey, options.ReadingFormats.AccelerationDigits),
            settings.Get(GpsDigitsKey, options.ReadingFormats.GpsDigits),
            settings.Get(SpeedDigitsKey, options.ReadingFormats.SpeedDigits),
            settings.Get(RestDigitsKey, options.ReadingFormats.RestDigits)).Normalized();
        var speedometer = settings.Get(SpeedometerKey, options.Speedometer);
        ArgumentNullException.ThrowIfNull(speedometer);
        speedometer.Validate();
        ObdAdapterKind adapter = settings.Get(ObdAdapterKey, options.ObdAdapter);
        _ = ObdAdapters.GetName(adapter);

        return options with
        {
            DampingMilliseconds = damping,
            AccelerationDamping = accelerationDamping,
            GScale = gScale,
            BaseFontPointSize = fontSize,
            SourceKind = sourceKind,
            UseObd = settings.Get(UseObdKey, false),
            SensorSource = ReadSensorSource(settings, options.SensorSource),
            BluetoothSensorSearchMinutes = Math.Clamp(settings.Get(
                BluetoothSensorSearchMinutesKey, options.BluetoothSensorSearchMinutes), 1, 15),
            InternalSensorFrame = ReadSensorFrame(settings, InternalSensorFrameKey, options.InternalSensorFrame),
            BluetoothSensorFrame = ReadSensorFrame(settings, BluetoothSensorFrameKey, options.BluetoothSensorFrame),
            UseSphericalHorizon = settings.Get(SphericalHorizonKey, options.UseSphericalHorizon),
            AlwaysOn = settings.Get(AlwaysOnKey, options.AlwaysOn),
            ObdAdapter = adapter,
            ObdDevice = settings.Get(ObdDeviceKey, options.ObdDevice),
            BluetoothSensorDevice = settings.Get(BluetoothSensorDeviceKey, options.BluetoothSensorDevice),
            ForceObdGaugesOn = settings.Get(ForceObdGaugesOnKey, options.ForceObdGaugesOn),
            ObdSearchMinutes = Math.Clamp(settings.Get(ObdSearchMinutesKey, options.ObdSearchMinutes), 1, 15),
            ExceptionPopupSeconds = Math.Clamp(settings.Get(ExceptionPopupSecondsKey, options.ExceptionPopupSeconds), 1, 300),
            ConfirmGpsReceiverChanges = settings.Get(ConfirmGpsReceiverChangesKey, options.ConfirmGpsReceiverChanges),
            ConfirmSensorStart = settings.Get(ConfirmSensorStartKey, options.ConfirmSensorStart),
            ToolbarCommandTimeoutSeconds = Math.Clamp(settings.Get(
                ToolbarCommandTimeoutKey, options.ToolbarCommandTimeoutSeconds), 1, 300),
            ToolbarIdleTimeoutSeconds = Math.Clamp(settings.Get(
                ToolbarIdleTimeoutKey, options.ToolbarIdleTimeoutSeconds), 1, 300),
            TargetGpsFixIntervalMilliseconds = settings.Get(GpsTargetIntervalKey, 0) is int interval
                && interval is 100 or 200 or 500 or 1000 ? interval : null,
            WindowsGpsReportIntervalMilliseconds = Math.Clamp(
                settings.Get(WindowsGpsIntervalKey, options.WindowsGpsReportIntervalMilliseconds), 100u, 60000u),
            MaximumRpm = Math.Clamp(settings.Get(MaximumRpmKey, options.MaximumRpm), 1000, 20000),
            SerialPortName = serialPort,
            SerialGnssProfile = profile,
            AllowReceiverCommandsThisSession = false,
            ReadingFormats = formats,
            Speedometer = speedometer,
            RouteFilePath = routePath,
            FollowCurrentPosition = settings.Get(
                FollowCurrentPositionKey,
                options.FollowCurrentPosition),
            AllowOnlineMapTiles = settings.Get(AllowOnlineMapTilesKey, false),
            RecordingRootFolder = recordingFolder,
            RecordingFormats = recordingFormats,
            RawSnapshotsPerSecond = Math.Clamp(settings.Get(RawSnapshotRateKey, 10), 1, 20),
            RecordingMarkerLabel = settings.Get(RecordingMarkerLabelKey, "Marker"),
            FrameMode = ReadFrameMode(settings, options.FrameMode),
            AssumeStaticMountedDevice = settings.Get(
                AssumeStaticMountedDeviceKey,
                options.AssumeStaticMountedDevice),
            VehicleMount = settings.Get(VehicleMountKey, options.VehicleMount)
        };
    }

    /// <summary>
    ///  Writes the user-owned choices in <paramref name="options"/>.
    /// </summary>
    internal static void Save(IUserSettingsService? settings, DemoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (settings is null)
        {
            return;
        }

        settings.Set(DampingMillisecondsKey, options.DampingMilliseconds);
        settings.Set(AccelerationDampingKey, options.AccelerationDamping);
        settings.Set(GScaleKey, options.GScale);
        settings.Set(BaseFontPointSizeKey, options.BaseFontPointSize);
        settings.Set(SourceKindKey, options.SourceKind);
        settings.Set(UseObdKey, options.UseObd);
        settings.Set(SensorSourceKey, options.SensorSource);
        settings.Set(BluetoothSensorSearchMinutesKey, options.BluetoothSensorSearchMinutes);
        settings.Set(InternalSensorFrameKey, options.InternalSensorFrame);
        settings.Set(BluetoothSensorFrameKey, options.BluetoothSensorFrame);
        settings.Set(SphericalHorizonKey, options.UseSphericalHorizon);
        settings.Set(AlwaysOnKey, options.AlwaysOn);
        settings.Set(ObdAdapterKey, options.ObdAdapter);
        settings.Set(ObdDeviceKey, options.ObdDevice);
        settings.Set(BluetoothSensorDeviceKey, options.BluetoothSensorDevice);
        settings.Set(ForceObdGaugesOnKey, options.ForceObdGaugesOn);
        settings.Set(ObdSearchMinutesKey, options.ObdSearchMinutes);
        settings.Set(ExceptionPopupSecondsKey, options.ExceptionPopupSeconds);
        settings.Set(ConfirmGpsReceiverChangesKey, options.ConfirmGpsReceiverChanges);
        settings.Set(ConfirmSensorStartKey, options.ConfirmSensorStart);
        settings.Set(ToolbarCommandTimeoutKey, options.ToolbarCommandTimeoutSeconds);
        settings.Set(ToolbarIdleTimeoutKey, options.ToolbarIdleTimeoutSeconds);
        settings.Set(GpsTargetIntervalKey, options.TargetGpsFixIntervalMilliseconds ?? 0);
        settings.Set(WindowsGpsIntervalKey, options.WindowsGpsReportIntervalMilliseconds);
        settings.Set(MaximumRpmKey, options.MaximumRpm);
        settings.Set(SerialPortNameKey, options.SerialPortName ?? string.Empty);
        settings.Set(SerialGnssProfileKey, options.SerialGnssProfile);
        settings.Set(AccelerationDigitsKey, options.ReadingFormats.AccelerationDigits);
        settings.Set(GpsDigitsKey, options.ReadingFormats.GpsDigits);
        settings.Set(SpeedDigitsKey, options.ReadingFormats.SpeedDigits);
        settings.Set(RestDigitsKey, options.ReadingFormats.RestDigits);
        settings.Set(SpeedometerKey, options.Speedometer);
        settings.Set(RouteFilePathKey, options.RouteFilePath ?? string.Empty);
        settings.Set(FollowCurrentPositionKey, options.FollowCurrentPosition);
        settings.Set(AllowOnlineMapTilesKey, options.AllowOnlineMapTiles);
        settings.Set(RecordingRootFolderKey, options.RecordingRootFolder);
        settings.Set(RecordingFormatsKey, options.RecordingFormats);
        settings.Set(RawSnapshotRateKey, options.RawSnapshotsPerSecond);
        settings.Set(RecordingMarkerLabelKey, options.RecordingMarkerLabel);
        settings.Set(FrameModeKey, options.FrameMode.ToString());
        settings.Set(AssumeStaticMountedDeviceKey, options.AssumeStaticMountedDevice);
        settings.Set(VehicleMountKey, options.VehicleMount);
    }

    private static SensorReferenceFrame ReadSensorFrame(
        IUserSettingsService settings, string key, SensorReferenceFrame fallback)
    {
        SensorReferenceFrame frame = settings.Get(key, fallback);
        if (!frame.IsFinite)
        {
            throw new InvalidDataException($"Saved sensor reference '{key}' must contain finite angles.");
        }

        return frame;
    }

    private static SensorDisplaySource ReadSensorSource(IUserSettingsService settings, SensorDisplaySource fallback)
    {
        SensorDisplaySource saved = settings.Get(SensorSourceKey, fallback);

        return Enum.IsDefined(saved) ? saved : fallback;
    }

    private static TabletFrameMode ReadFrameMode(IUserSettingsService settings, TabletFrameMode fallback)
    {
        string saved = settings.Get(FrameModeKey, fallback.ToString());

        return Enum.TryParse(saved, out TabletFrameMode mode) && Enum.IsDefined(mode)
            ? mode
            : fallback;
    }

    /// <summary>
    ///  Returns a directory that still exists, preferring the saved picker directory.
    /// </summary>
    internal static string ResolveInitialDirectory(
        IUserSettingsService? settings,
        string key,
        string fallback)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        string? saved = settings?.Get(key, string.Empty);

        if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
        {
            return saved;
        }

        if (!string.IsNullOrWhiteSpace(fallback) && Directory.Exists(fallback))
        {
            return fallback;
        }

        return Environment.CurrentDirectory;
    }

    /// <summary>
    ///  Remembers the directory of a chosen file, or the chosen folder itself.
    /// </summary>
    internal static void RememberPath(IUserSettingsService? settings, string key, string? path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (settings is null || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        string directory = Directory.Exists(path)
            ? path
            : Path.GetDirectoryName(path) ?? string.Empty;

        if (Directory.Exists(directory))
        {
            settings.Set(key, directory);
        }
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
