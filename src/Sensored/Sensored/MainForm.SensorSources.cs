using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.WinForms.Sensors.DirectX;

namespace Sensored;

/// <summary>Owns display/reference selection without replacing the internal fusion IMU.</summary>
public partial class MainForm
{
    private BluetoothSensorSource? _bluetoothSensorSource;
    private Exception? _observedBluetoothSensorFault;
    private Exception? _terminalBluetoothSensorFailure;
    private bool _sensorSelectionPending;
    private HorizonSourceMode _horizonMode;
    private readonly HorizonSourceModel _horizonSources = new();
    private readonly object _horizonGate = new();
    private RawImuPayload? _horizonInternalSample;
    private DateTimeOffset _horizonInternalReceivedAt;
    private HorizonSourceReading? _presentedHorizon;
    private string? _lastBluetoothSensorStatus;
    private string? _lastHorizonDiagnostic;

    private HorizonSourceMode PreferredLiveHorizonMode
        => HorizonSourceModel.PreferredMode(_options.SensorSource, _options.InternalSensorFrame);

    private bool BluetoothSensorsAvailable
        => !_obdReplayActive && _bluetoothSensorSource?.Latest.IsFreshAt(DateTimeOffset.UtcNow) == true;

    private string BluetoothSensorsStatus
        => _obdReplayActive ? "Live Bluetooth reference disabled during replay"
            : _bluetoothSensorSource?.Latest.Status ?? "Bluetooth sensors: not started";

    private void StartBluetoothSensorDiscovery()
    {
        if (_bluetoothSensorSource is null && _sensorSourceFactory is not null
            && !_shutdownStarted && !_obdReplayActive)
        {
            if (_terminalBluetoothSensorFailure is { } failure)
            {
                throw new InvalidOperationException(
                    "Bluetooth sensor cleanup failed terminally; restart before reconnecting.", failure);
            }

            _bluetoothSensorSource = new(TimeSpan.FromMinutes(_options.BluetoothSensorSearchMinutes),
                selectedDevice: _options.BluetoothSensorDevice);
            _log.Information($"Bluetooth reference search started; {_options.BluetoothSensorSearchMinutes} minute window. "
                + "Primary fusion IMU remains internal.");
        }
    }

    private async Task StopBluetoothSensorsAsync()
    {
        if (_bluetoothSensorSource is not { } source)
        {
            return;
        }

        try
        {
            await source.DisposeAsync().ConfigureAwait(true);
            _bluetoothSensorSource = null;
        }
        catch (Exception exception)
        {
            _terminalBluetoothSensorFailure ??= exception;
            throw;
        }
    }

    private void InitializeSensorSourcePresentation()
    {
        _horizonMode = PreferredLiveHorizonMode;
        _artificialHorizon.DoubleClick += OnHorizonDoubleClick;
    }

    private void OnHorizonDoubleClick(object? sender, EventArgs e)
        => CycleHorizonMode();

    private void CycleHorizonMode()
    {
        _horizonMode = (HorizonSourceMode)(((int)_horizonMode + 1) % 7);
        ResetHorizonSourcePresentation();
        _log.Information($"Horizon display mode {(int)_horizonMode + 1}: {_horizonMode}; fusion IMU remains internal.");
        ApplySelectedHorizon();
        _artificialHorizon.ShowModeCaption(_artificialHorizon.SourceLabel);
    }

    private void ResetHorizonSourcePresentation()
    {
        _presentedHorizon = null;
        _artificialHorizon.ResetPresentation();
    }

    private async void OnInternalSensorsCommand(object? sender, EventArgs e)
        => await SelectSensorSourceAsync(SensorDisplaySource.Internal).ConfigureAwait(true);

    private async void OnBluetoothSensorsCommand(object? sender, EventArgs e)
        => await SelectSensorSourceAsync(SensorDisplaySource.Bluetooth).ConfigureAwait(true);

    private async Task SelectSensorSourceAsync(SensorDisplaySource source)
    {
        if (_sensorSelectionPending || _shutdownStarted || _obdReplayActive)
        {
            return;
        }

        if (source == SensorDisplaySource.Bluetooth && !BluetoothSensorsAvailable)
        {
            SetStatusAndLog(BluetoothSensorsStatus);

            return;
        }

        _sensorSelectionPending = true;
        DemoOptions previous = _options;
        try
        {
            _options = _options with { SensorSource = source };
            await ApplySensorSourceOptionsAsync(previous).ConfigureAwait(true);
            DemoSettings.Save(_userSettings, _options);
            _userSettings?.Flush();
        }
        catch (Exception exception)
        {
            ShowError("Sensor reference selection failed", exception);
        }
        finally
        {
            _sensorSelectionPending = false;
        }
    }

    private async Task ApplySensorSourceOptionsAsync(DemoOptions previous)
    {
        if (previous.BluetoothSensorSearchMinutes != _options.BluetoothSensorSearchMinutes
            || previous.BluetoothSensorDevice != _options.BluetoothSensorDevice
            || _bluetoothSensorSource?.IsCompleted == true)
        {
            await StopBluetoothSensorsAsync().ConfigureAwait(true);
        }

        StartBluetoothSensorDiscovery();
        if (previous.SensorSource != _options.SensorSource
            || (_options.SensorSource == SensorDisplaySource.Internal
                && previous.InternalSensorFrame != _options.InternalSensorFrame)
            || (_options.SensorSource == SensorDisplaySource.Bluetooth
                && previous.BluetoothSensorFrame != _options.BluetoothSensorFrame))
        {
            bool matchingMode = _options.SensorSource == SensorDisplaySource.Bluetooth
                ? _horizonMode is HorizonSourceMode.BluetoothGravityGps
                    or HorizonSourceMode.BluetoothInclinometerGps or HorizonSourceMode.BluetoothCompass
                : _horizonMode is HorizonSourceMode.InternalGravityGps or HorizonSourceMode.InternalInclinometerGps;
            if (previous.SensorSource != _options.SensorSource || !matchingMode)
            {
                _horizonMode = PreferredLiveHorizonMode;
            }

            _log.Information($"Display/reference collection: {_options.SensorSource}; "
                + "primary fusion IMU remains internal. Independent Bluetooth data is comparison evidence, not ground truth.");
        }

        ResetHorizonSourcePresentation();
        ApplySelectedHorizon();
    }

    private void ObserveHorizonInput(SensorEnvelope input)
    {
        if (input.Imu is not { } sample)
        {
            return;
        }

        lock (_horizonGate)
        {
            _horizonSources.ObserveInternal(sample, input.SourceTimestamp, _options.DampingMilliseconds);
            _horizonInternalSample = sample;
            _horizonInternalReceivedAt = DateTimeOffset.UtcNow;
        }
    }

    private SensorReferenceSample? ReadSensorReferenceSample(SensorDisplaySource source)
    {
        if (_obdReplayActive || _shutdownStarted)
        {
            return null;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (source == SensorDisplaySource.Bluetooth)
        {
            return _bluetoothSensorSource?.Latest is { } snapshot && snapshot.IsFreshAt(now)
                && snapshot.Reading is { } external
                ? SensorReferenceSample.FromBluetooth(external,
                    _horizonMode is HorizonSourceMode.BluetoothInclinometerGps or HorizonSourceMode.BluetoothCompass)
                : null;
        }

        RawImuPayload? sample = _lastRawImu is { } local
            && now >= _lastRawImuReceivedAt && now - _lastRawImuReceivedAt <= TimeSpan.FromSeconds(2)
                ? local : null;
        Vec3? acceleration = sample?.Acceleration
            ?? (_sensorSourceFactory is not null ? ReadCalibrationAcceleration() : null);
        if (acceleration is not { } raw || !double.IsFinite(raw.Magnitude) || raw.Magnitude < 1e-6d)
        {
            return null;
        }

        Windows.Devices.Sensors.InclinometerReading? inclination = sample is null && _sensorSourceFactory is not null
            ? Windows.Devices.Sensors.Inclinometer.GetDefault()?.GetCurrentReading() : null;
        Windows.Devices.Sensors.CompassReading? compass = sample is null && _sensorSourceFactory is not null
            ? Windows.Devices.Sensors.Compass.GetDefault()?.GetCurrentReading() : null;
        bool inclinationFresh = inclination is not null && now >= inclination.Timestamp
            && now - inclination.Timestamp <= TimeSpan.FromSeconds(2);
        double roll = sample?.AbsoluteRollDegrees ?? (inclinationFresh ? inclination!.RollDegrees : double.NaN);
        double pitch = sample?.AbsolutePitchDegrees ?? (inclinationFresh ? inclination!.PitchDegrees : double.NaN);
        if (!double.IsFinite(roll) || !double.IsFinite(pitch))
        {
            const double ToDegrees = 180d / Math.PI;
            roll = Math.Atan2(raw.X, -raw.Z) * ToDegrees;
            pitch = Math.Asin(Math.Clamp(-raw.Y / raw.Magnitude, -1d, 1d)) * ToDegrees;
        }

        double heading = sample is { MagneticHeadingConfidence: > 0d } measured
            ? measured.MagneticHeadingDegrees
            : compass is not null && now >= compass.Timestamp && now - compass.Timestamp <= TimeSpan.FromSeconds(2)
                ? compass.HeadingMagneticNorth : double.NaN;

        return new(raw, roll, pitch, heading);
    }

    private void ApplySelectedHorizon()
    {
        UpdateSensorSourcesUi(BluetoothSensorsAvailable, BluetoothSensorsStatus);
        if (_bluetoothSensorSource?.Fault is { } fault
            && !ReferenceEquals(fault, _observedBluetoothSensorFault))
        {
            _observedBluetoothSensorFault = fault;
            ShowError("Bluetooth reference acquisition or cleanup failed", fault);
        }

        if (_lastBluetoothSensorStatus != BluetoothSensorsStatus)
        {
            _lastBluetoothSensorStatus = BluetoothSensorsStatus;
            _log.Information(_lastBluetoothSensorStatus);
            NotifyStatusChange(_lastBluetoothSensorStatus);
        }

        if (_paused)
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        SensorComparisonReference? recordedReference = _obdReplayActive ? Volatile.Read(ref _presentedComparison) : null;
        DateTimeOffset sourceNow = _obdReplayActive
            ? new DateTimeOffset(Interlocked.Read(ref _obdReplayTimeTicks), TimeSpan.Zero) : now;
        BluetoothSensorSnapshot? bluetooth = _obdReplayActive
            ? recordedReference?.Bluetooth : _bluetoothSensorSource?.Latest;
        _gpsMotionTabHost.ApplyBluetoothReadings(
            bluetooth?.IsFreshAt(sourceNow) == true ? bluetooth.Reading : null,
            bluetooth?.Connected == true);
        FusionEstimate? estimate = _fusionSession?.Latest ?? _lastFusionEstimate;
        double gpsCourse = _displayedGpsFresh && _displayedGpsMotion is { } gps
            ? double.IsFinite(gps.ReceiverCourseDegrees) ? gps.ReceiverCourseDegrees : gps.FourPoint.CourseDegrees
            : double.NaN;
        HorizonSourceReading reading;
        lock (_horizonGate)
        {
            if (bluetooth?.IsFreshAt(sourceNow) == true && bluetooth.Reading is { } external)
            {
                _horizonSources.ObserveBluetooth(external, _options.DampingMilliseconds);
            }
            else
            {
                _horizonSources.MarkBluetoothUnavailable();
            }

            RawImuPayload? sample = _horizonInternalSample ?? _lastRawImu;
            DateTimeOffset received = _horizonInternalSample is null
                ? _lastRawImuReceivedAt : _horizonInternalReceivedAt;
            bool fresh = (_obdReplayActive && (_replayPlayer is not null || _hasCompletedReplayPresentation))
                || (sample is not null && now >= received && now - received <= TimeSpan.FromSeconds(2));
            reading = _horizonSources.Resolve(_horizonMode, estimate, sample, fresh, bluetooth, sourceNow, gpsCourse,
                _obdReplayActive ? recordedReference?.InternalFrame ?? default : _options.InternalSensorFrame,
                _obdReplayActive ? recordedReference?.BluetoothFrame ?? default : _options.BluetoothSensorFrame);
        }

        if (reading != _presentedHorizon)
        {
            _presentedHorizon = reading;
            _artificialHorizon.SetSourceLabel($"{(char)('A' + (int)_horizonMode)}: {reading.AttitudeSource} / {reading.HeadingSource}");
            _readingsToolTip.SetToolTip(_artificialHorizon,
                _artificialHorizon.SourceLabel);
        }

        string diagnostic = $"Horizon {(int)_horizonMode + 1}/7: {reading.AttitudeSource}"
            + $" ({(reading.HasAttitude ? "available" : "unavailable")}); direction: {reading.HeadingSource}.";
        if (diagnostic != _lastHorizonDiagnostic)
        {
            _lastHorizonDiagnostic = diagnostic;
            _log.Information(diagnostic);
        }

        double damping = _horizonMode is HorizonSourceMode.InternalGravityGps or HorizonSourceMode.BluetoothGravityGps
            ? 0d : _options.DampingMilliseconds;
        if (_artificialHorizon.DampingMilliseconds != damping)
        {
            _artificialHorizon.DampingMilliseconds = damping;
        }

        _artificialHorizon.SetAttitude(reading.RollDegrees, reading.PitchDegrees, now);
        _artificialHorizon.SetHeading(reading.HeadingDegrees, reading.HeadingSource, now);
        bool fusionMode = _horizonMode is HorizonSourceMode.FusionGps or HorizonSourceMode.FusionCompass;
        _artificialHorizon.SetTrust(
            !reading.HasAttitude ? HorizonTrustLevel.Unavailable
                : fusionMode
                    ? estimate?.IsAccelerationSupported == true
                        ? estimate.CalibrationState == FusionCalibrationState.Calibrated
                            ? HorizonTrustLevel.Calibrated : HorizonTrustLevel.Converging
                        : HorizonTrustLevel.GravityOnly
                    : HorizonTrustLevel.GravityOnly,
            reading.HasAttitude && fusionMode && estimate?.PatternDiagnostics is null
                ? estimate?.Confidence ?? double.NaN : double.NaN);
    }
}
