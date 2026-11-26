using System.Text.Json;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.WinForms.Extensions.UI;
using WarpToolkit.WinForms.Symbols;
using WarpToolkit.WinForms.ToolStrips;

namespace Sensored;

public partial class MainForm
{
    private readonly SymbolToolStripButton _learningQuizButton = new()
    {
        Name = "_learningQuizButton",
        Text = "Start learning Quizz"
    };
    private RoadLearningSession? _roadLearning;
    private RoadLearningQuizDialog? _roadQuizDialog;

    private bool CanRunRoadLearning => _lease is not null && _replayPlayer is null && !_obdReplayActive
        && !_transitioning && !_shutdownStarted && _fusionSession is { Fault: null };

    private void ConfigureRoadLearningCommand(int symbolSize)
        => _learningQuizButton.ConfigureItem(_toolStrip, FluentSymbols.AllSymbols.Ruler,
            (OnRoadLearningCommand, true),
            "Start an opt-in road-learning Quizz. Passenger use only; never operate while driving.", symbolSize);

    private void UpdateRoadLearningCommand()
    {
        _learningQuizButton.Enabled = CanRunRoadLearning;
        _learningQuizButton.Text = _roadLearning?.Active == true ? "Learning Quizz ON" : "Start learning Quizz";
        _learningQuizButton.Checked = _roadLearning?.Active == true;
    }

    private void OnRoadLearningCommand(object? sender, EventArgs e)
        => StartRoadLearning(confirmPassenger: true);

    private void StartRoadLearning(bool confirmPassenger)
    {
        if (_roadQuizDialog is { IsDisposed: false } existing)
        {
            existing.Activate();
            return;
        }

        if (!CanRunRoadLearning)
        {
            return;
        }

        if (confirmPassenger && MessageBox.Show(this,
            "Only a passenger may answer while the vehicle is moving. Do not interact with this quiz as the driver.\r\n\r\n"
            + "The quiz saves the last 30 seconds of sensor evidence with each answer locally. "
            + "Experimental confidence is not calibrated accuracy. Fusion and calibration stay unchanged.\r\n\r\n"
            + "Start a learning session?", "Road learning - passenger operation",
            MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
        {
            return;
        }

        RoadLearningSession session = new(JsonSerializer.Serialize(new
        {
            Mount = _activeVehicleMount,
            InternalFrame = _options.InternalSensorFrame,
            BluetoothFrame = _options.BluetoothSensorFrame,
            Gps = _learningGpsSelectionKey,
            ObdDevice = _options.ObdDevice?.Id,
            BluetoothDevice = _options.BluetoothSensorDevice?.Id,
            Engine = (_fusionEngine as IFusionConfigurationSource)?.Configuration
        }));
        RoadLearningJournal journal = new(_automation?.LabelsPath ?? RoadLearningRoot, session.Id);
        RoadLearningQuizDialog dialog = new(session, journal, () => CanRunRoadLearning,
            () => _recording?.FolderPath, (message, exception) => ShowError(message, exception), Font);
        _roadLearning = session;
        _roadQuizDialog = dialog;
        dialog.FormClosed += (_, _) =>
        {
            session.End();
            if (ReferenceEquals(_roadQuizDialog, dialog))
            {
                _roadQuizDialog = null;
                _roadLearning = null;
            }

            dialog.Dispose();
            journal.Dispose();
            UpdateRoadLearningCommand();
            SetStatusAndLog("Road learning ended. Saved labels are retained; physical fusion and calibration are unchanged.");
            if (_automation is null) { _ = ShowRoadLearningReadinessAsync(session.Id); }
        };
        dialog.Show(this);
        UpdateRoadLearningCommand();
    }

    private static string RoadLearningRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WARP", "Sensored", "RoadLearning", "quiz-v1");

    private async Task ShowRoadLearningReadinessAsync(Guid sessionId)
    {
        try
        {
            string root = RoadLearningRoot;
            (RoadLearningReadinessReport report, RoadLearningStage lastStage) = await Task.Run(
                () => (RoadLearningReadiness.Assess(root, sessionId), RoadLearningReadiness.ReadLastStage(root)));
            if (_shutdownStarted || IsDisposed || !RoadLearningReadiness.ShouldRemind(report, lastStage))
            {
                return;
            }

            RoadLearningReadiness.WriteLastStage(root, (RoadLearningStage)Math.Max((int)report.Stage, (int)lastStage));
            MessageBox.Show(this, RoadLearningReadiness.Format(report), "Road learning - data collected so far",
                MessageBoxButtons.OK, report.Stage == RoadLearningStage.Ready ? MessageBoxIcon.Information : MessageBoxIcon.None);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            SetStatusAndLog($"Road learning summary unavailable: {exception.Message}");
        }
    }

    private void ObserveRoadLearning(FusionObservation observation)
    {
        if (_obdReplayActive || Volatile.Read(ref _roadLearning) is not { Active: true } session)
        {
            return;
        }

        SensorEnvelope input = observation.Input;
        if (input.Kind is SensorDataKind.Gap or SensorDataKind.SourceTransition)
        {
            string reason = $"{input.Kind}: {(object?)input.SourceTransition ?? input.SourceId}";
            string? sourceId = input.SourceTransition?.SourceId ?? input.SourceId;
            if (sourceId is null)
            {
                session.BreakContinuity(reason);
            }
            else
            {
                // Only the affected producer restarts; a missing receiver's retries cost nothing.
                session.BreakSourceContinuity(sourceId, reason);
            }

            return;
        }

        if (input.Kind == SensorDataKind.Geolocation && input.Geolocation is { SpeedValid: true } gps && gps.FixQuality is not <= 0
            && double.IsFinite(gps.SpeedMetersPerSecond) && gps.SpeedMetersPerSecond >= 0)
        {
            session.Observe(new(input.ArrivalTimestamp, gps.CoordinateTimestamp, input.Sequence, observation.Epoch,
                input.SourceId ?? "GPS", input.SourceGeneration, LearningSource.Gps, Speed: gps.SpeedMetersPerSecond,
                GpsCourseDegrees: double.IsFinite(gps.CourseDegrees) ? gps.CourseDegrees : null,
                GpsAltitudeMeters: double.IsFinite(gps.AltitudeMeters)
                    && !(gps.VerticalAccuracyMeters > 30) ? gps.AltitudeMeters : null));
        }

        if (input.Kind == SensorDataKind.OBD2 && input.VehicleTelemetry is { Connected: true } vehicle)
        {
            bool speedValid = vehicle.SpeedKph is { State: SensorReadingState.Valid, Value: >= 0, Timestamp: { } stamp }
                && input.ArrivalTimestamp >= stamp && input.ArrivalTimestamp - stamp <= TimeSpan.FromSeconds(2);
            bool rpmValid = vehicle.Rpm is { State: SensorReadingState.Valid, Value: >= 0, Timestamp: { } rpmStamp }
                && input.ArrivalTimestamp >= rpmStamp && input.ArrivalTimestamp - rpmStamp <= TimeSpan.FromSeconds(2);
            session.Observe(new(input.ArrivalTimestamp, speedValid ? vehicle.SpeedKph.Timestamp!.Value : input.SourceTimestamp,
                input.Sequence, observation.Epoch, input.SourceId ?? "OBD2", input.SourceGeneration, LearningSource.Vehicle,
                Speed: speedValid ? vehicle.SpeedKph.Value / 3.6 : null, Rpm: rpmValid ? vehicle.Rpm.Value : null));
        }

        if (input.Kind == SensorDataKind.Imu && input.Imu is { } imu)
        {
            FusionEstimate? estimate = observation.Estimate;
            session.Observe(new(input.ArrivalTimestamp, input.SourceTimestamp, input.Sequence, observation.Epoch,
                input.SourceId ?? "Internal accelerometer/gyroscope", input.SourceGeneration, LearningSource.Internal,
                AccelerationMagnitude: imu.Acceleration.Magnitude,
                AngularRateMagnitude: imu.HasAngularRate ? imu.AngularRate.Magnitude : null,
                SupportedPitch: estimate is { RejectedSample: false, IsAttitudeSupported: true } ? estimate.Pitch : null,
                SupportedLateralAcceleration: estimate is { RejectedSample: false, IsAccelerationSupported: true } ? estimate.LateralAccel : null,
                AccelerationVectorG: imu.Acceleration,
                AngularRateVectorDegreesPerSecond: imu.HasAngularRate ? imu.AngularRate : null,
                SupportedLongitudinalAcceleration: estimate is { RejectedSample: false, IsAccelerationSupported: true } ? estimate.LongitudinalAccel : null));
            if (_bluetoothSensorSource?.Latest is { } bluetooth && bluetooth.IsFreshAt(input.ArrivalTimestamp)
                && bluetooth.Reading is { } external)
            {
                session.Observe(new(input.ArrivalTimestamp, external.Timestamp, input.Sequence, observation.Epoch,
                    $"Bluetooth accelerometer/gyroscope ({_options.BluetoothSensorDevice?.Id ?? "auto; identity unavailable"})",
                    0, LearningSource.Bluetooth,
                    AccelerationMagnitude: Math.Sqrt(external.AccelerationX * external.AccelerationX
                        + external.AccelerationY * external.AccelerationY + external.AccelerationZ * external.AccelerationZ),
                    AngularRateMagnitude: Math.Sqrt(external.AngularRateX * external.AngularRateX
                        + external.AngularRateY * external.AngularRateY + external.AngularRateZ * external.AngularRateZ),
                    AccelerationVectorG: new(external.AccelerationX, external.AccelerationY, external.AccelerationZ),
                    AngularRateVectorDegreesPerSecond: new(external.AngularRateX, external.AngularRateY, external.AngularRateZ)));
            }
        }
    }
}
