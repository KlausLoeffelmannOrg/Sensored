using System.Reflection;
using System.Text.Json;
using Sensored;
using WarpToolkit.ComponentModel;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.AppServices;
using WarpToolkit.WinForms.Sensors.DirectX;
using WinFormsFusionGauges.WinFormsApp;
using WarpToolkit.Windows.Bluetooth;
using GaugeGeometry = WarpToolkit.Sensors.Presentation.GaugeGeometry;

namespace FusionGauges.Tests;

/// <summary>Checks app activation, source-time differentiation and persisted mount composition.</summary>
public sealed class FusionSetupAppTests
{
    [Fact]
    public async Task RefreshedBluetoothIdsAreAcceptedForBothSelectedRoles()
        => await RunStaAsync(() =>
        {
            BluetoothDeviceInfo device = new("old-id", "Adapter", "00:11:22:33:44:55",
                BluetoothDeviceKind.LowEnergy, false, -60, []);
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default with
            {
                ObdDevice = device,
                BluetoothSensorDevice = device
            }, bluetoothScanner: new OptionsBluetoothScanner());
            BluetoothDeviceInfo refreshed = device with { Id = "new-windows-id", IsPaired = true };
            Invoke(dialog, "ObserveBluetoothDevice", refreshed);
            Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
            Assert.Equal(refreshed, dialog.Options.ObdDevice);
            Assert.Equal(refreshed, dialog.Options.BluetoothSensorDevice);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task LearningQuizReplacesImprovementCommandsAndPreservesFusionCalibration()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            ToolStrip toolbar = Field<ToolStrip>(form, "_toolStrip");
            ToolStripButton quiz = Field<ToolStripButton>(form, "_learningQuizButton");
            ToolStripButton calibration = Field<ToolStripButton>(form, "_calibrateButton");
            Assert.Same(toolbar, quiz.Owner);
            Assert.True(quiz.Available);
            Assert.False(quiz.Enabled);
            Assert.False(quiz.Checked);
            Assert.True(calibration.Available);
            Assert.Equal(toolbar.Items.IndexOf(calibration) + 1, toolbar.Items.IndexOf(quiz));
            Assert.DoesNotContain(toolbar.Items.Cast<ToolStripItem>(),
                item => item.Text is "Fusion feedback" or "Driving tasks");
            Invoke(form, "ConfigureCommandImages");
            Assert.Single(toolbar.Items.Cast<ToolStripItem>(), item => item == quiz);
            Assert.Equal(SensorServiceState.Stopped, service.State);
        });

    [Fact]
    public async Task EvidenceProfileChangesAtTheCanonicalGpsBoundaryNotWhenOptionsAreEdited()
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-profile-boundary-{Guid.NewGuid():N}");
            string path = Path.Combine(folder, "evidence.json");
            try
            {
                await using SensorService service = CreateService();
                using Sensored.MainForm form = new(service);
                using FusionLearningStore learning = FusionLearningStore.Open(path);
                SetField(form, "_fusionLearning", learning);
                SetField(form, "_fusionEngine", DemoOptions.Default.CreateFusionEngine(isReplay: false));
                Invoke(form, "BeginFusionLearning");
                string originalKey = Field<string>(form, "_learningGpsSelectionKey");
                SetField(form, "_options", DemoOptions.Default with { SerialPortName = "COM99" });
                SensorEnvelope imu = ReplayImu(0);
                Invoke(form, "ObserveFusionLearning", new FusionObservation(imu, null, 0, "test"));
                Assert.Equal(originalKey, Field<string>(form, "_learningGpsSelectionKey"));
                foreach (string key in new[] { "serial:COM7:GPS", "serial:COM8:Galileo" })
                {
                    SensorEnvelope transition = imu with
                    {
                        Kind = SensorDataKind.SourceTransition,
                        SourceTransition = new("GPS", 2, "probing", key)
                    };
                    Invoke(form, "ObserveFusionLearning", new FusionObservation(transition, null, 0, "test"));
                    Assert.Equal(key, Field<string>(form, "_learningGpsSelectionKey"));
                    // Retrying the same physical selection must not start another evidence session.
                    Invoke(form, "ObserveFusionLearning", new FusionObservation(transition, null, 0, "test"));
                }

                await learning.SaveAsync();
                using JsonDocument saved = JsonDocument.Parse(await File.ReadAllTextAsync(path));
                JsonElement[] profiles = saved.RootElement.GetProperty("Profiles").EnumerateArray().ToArray();
                Assert.Equal(3, profiles.Length);
                Assert.All(profiles, profile => Assert.Equal(1, profile.GetProperty("Sessions").GetInt32()));
                Assert.Contains(profiles, profile => profile.GetProperty("SourceConfiguration").GetString()!
                    .Contains("serial:COM8:Galileo", StringComparison.Ordinal));
                SetField(form, "_fusionLearning", null!);
            }
            finally
            {
                File.Delete(path);
                File.Delete(path + ".lock");
                Directory.Delete(folder);
            }
        });

    [Fact]
    public async Task LiveCompanionIncludesEveryCanonicalInputAndReplayDoesNotProcessThemTwice()
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-canonical-journal-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try
            {
                await using SensorService service = CreateService();
                using Sensored.MainForm form = new(service);
                await using ObdRecordingCompanion journal = new();
                IFusionEngine live = DemoOptions.Default.CreateFusionEngine(isReplay: false);
                SetField(form, "_fusionEngine", live);
                SetField(form, "_obdRecording", journal);
                SensorEnvelope[] inputs =
                [
                    ReplayImu(0) with { Sequence = 0, Kind = SensorDataKind.SourceTransition,
                        SourceTransition = new("GPS", 1, "probing", "windows-gps:test") },
                    ReplayImu(0) with { Sequence = 1, Kind = SensorDataKind.OBD2,
                        VehicleTelemetry = new() { Connected = true, SelectedForSpeed = true,
                            SpeedKph = new(0, DateTimeOffset.UnixEpoch, SensorReadingState.Valid) } },
                    ReplayImu(0) with { Sequence = 2, Kind = SensorDataKind.Health },
                    ReplayImu(1) with { Sequence = 3 },
                    ReplayImu(2) with { Sequence = 4 }
                ];
                List<FusionEstimate?> expected = [];
                foreach (SensorEnvelope input in inputs)
                {
                    Invoke(form, "OnObdFusionInput", input);
                    expected.Add(live.Process(input));
                }

                await journal.ExportAsync(folder);
                SetField(form, "_obdRecording", null!);
                using ObdReplayCursor cursor = ObdReplayCursor.Open(folder)!;
                Assert.True(cursor.IncludesCanonicalInputs);
                IFusionEngine replay = DemoOptions.Default.CreateFusionEngine(isReplay: false);
                SetField(form, "_fusionEngine", replay);
                SetField(form, "_obdReplay", cursor);
                SetField(form, "_obdReplayActive", true);
                for (int index = 0; index < inputs.Length; index++)
                {
                    SensorEnvelope restored = (SensorEnvelope)typeof(Sensored.MainForm)
                        .GetMethod("PrepareObdInput", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(form, [inputs[index]])!;
                    Assert.Equal(inputs[index], restored);
                    Invoke(form, "OnObdFusionInput", restored);
                    Assert.Equal(expected[index], replay.Process(restored));
                }

                Assert.Null(cursor.ReadNext());
                SetField(form, "_obdReplay", null!);
            }
            finally
            {
                File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName));
                Directory.Delete(folder);
            }
        });

    [Fact]
    public async Task CompanionSeekRestoresAcceptedRenewalAndCanonicalIngestOrdinals()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-renewal-seek-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try
        {
            await using ObdRecordingCompanion journal = new();
            VehicleMountReference mount = new("renewal-fixture", 1, new(1, 0, 0), new(0, 0, 1));
            System.Text.Json.Nodes.JsonNode configuration = System.Text.Json.Nodes.JsonNode.Parse(
                JsonSerializer.Serialize(((IFusionConfigurationSource)FusionEngineFactory.CreateDefault(mount))
                    .Configuration))!;
            configuration["Version"] = 1;
            JsonElement profile = JsonSerializer.SerializeToElement(configuration);
            IFusionEngine live = FusionEngineFactory.CreateFromRecordedConfiguration(profile);
            IFusionPlausibilityController controller = (IFusionPlausibilityController)live;
            LongitudinalPlausibilityService plausibility = new();
            FusionPlausibilityFeedback? pending = null;
            List<ObdRecordedInput> recordedInputs = [];
            List<FusionEstimate?> expected = [];
            bool accepted = false;
            long sequence = 0;
            for (int index = 0; index <= 2000; index++)
            {
                double seconds = index / 100d;
                DateTimeOffset time = DateTimeOffset.UnixEpoch.AddMilliseconds(index * 10);
                double speed = 0.5 * Math.Clamp(seconds - 2, 0, 28);
                List<SensorEnvelope> tick = [];
                if (index == 0)
                {
                    tick.Add(ReplayImu(index) with { Kind = SensorDataKind.SourceTransition,
                        SourceTransition = new("GPS", 1, "probing", "windows-gps:test") });
                }

                if (index % 10 == 0)
                {
                    tick.Add(ReplayImu(index) with { Kind = SensorDataKind.OBD2,
                        SourceId = "odometer", VehicleTelemetry = new()
                        {
                            Connected = true, SelectedForSpeed = true,
                            SpeedKph = new(speed * 3.6, time, SensorReadingState.Valid)
                        } });
                }

                tick.Add(ReplayImu(index) with { Imu = ReplayImu(index).Imu!.Value with
                {
                    Acceleration = new(seconds is > 2 and < 30 ? -0.5 / 9.80665 : 0, 0, 1)
                } });
                foreach (SensorEnvelope original in tick)
                {
                    SensorEnvelope input = original with { Sequence = sequence++ };
                    ObdFusionState state = new(ObdTelemetrySnapshot.Disabled, null, null)
                    {
                        Plausibility = input.Kind == SensorDataKind.Imu ? pending : null
                    };
                    if (input.Kind == SensorDataKind.Imu)
                    {
                        pending = null;
                    }

                    state.Apply(live);
                    journal.Publish(input, state);
                    FusionEstimate? estimate = live.Process(input);
                    recordedInputs.Add(new(input, state));
                    expected.Add(estimate);
                    accepted |= state.Plausibility is not null
                        && estimate?.PatternDiagnostics?.PlausibilityReason?.Contains(
                            "feedback-applied", StringComparison.Ordinal) == true;
                    if (input.Kind == SensorDataKind.Imu && index > 0 && index % 400 == 0)
                    {
                        pending = plausibility.Evaluate(controller.CapturePlausibility(input, estimate!, null)).Feedback;
                    }
                }
            }

            Assert.True(accepted);
            await journal.ExportAsync(folder, TestContext.Current.CancellationToken);
            foreach (double seekSeconds in new[] { 10d, 17d })
            {
                int start = recordedInputs.FindIndex(recorded => recorded.Input.Kind == SensorDataKind.Imu
                    && recorded.Input.SourceTimestamp >= DateTimeOffset.UnixEpoch.AddSeconds(seekSeconds));
                IFusionEngine replay = FusionEngineFactory.CreateFromRecordedConfiguration(profile);
                using ObdReplayCursor cursor = ObdReplayCursor.Open(folder)!;
                int prepared = 0;
                int warmed = 0;
                for (int index = start; index < recordedInputs.Count; index++)
                {
                    ObdRecordedInput restored = cursor.Resolve(recordedInputs[index].Input,
                        recordedInputs[start].Input.Sequence, replay, _ => warmed++, _ => prepared++);
                    prepared++;
                    restored.State.Apply(replay);
                    Assert.Equal(expected[index], replay.Process(restored.Input));
                }

                Assert.Equal(recordedInputs.Count, prepared);
                Assert.Equal(expected.Take(start).Count(estimate => estimate is not null), warmed);
            }
        }
        finally
        {
            File.Delete(Path.Combine(folder, ObdRecordingCompanion.FileName));
            Directory.Delete(folder);
        }
    }

    [Fact]
    public async Task BluetoothOptionsScanAllDevicesAndPersistIndependentUnpairedRoles()
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-bluetooth-options-{Guid.NewGuid():N}");
            string path = Path.Combine(folder, "settings.json");
            try
            {
                OptionsBluetoothScanner scanner = new();
                List<string> statuses = [];
                DemoOptions selected;
                using (Sensored.OptionsDialog dialog = new(DemoOptions.Default,
                    reportBluetoothDiscoveryStatus: statuses.Add, bluetoothScanner: scanner))
                {
                    await InvokeTask(dialog, "StartBluetoothOptionsScanAsync");
                    Assert.True(scanner.Options!.IncludePaired);
                    Assert.True(scanner.Options.IncludeUnpaired);
                    Assert.True(scanner.Options.IncludeClassic);
                    Assert.True(scanner.Options.IncludeLowEnergy);
                    Assert.True(scanner.Options.IncludeAdvertisements);
                    Assert.Equal(2, Field<ListView>(dialog, "_bluetoothDevices").Items.Count);
                    ComboBox obd = Field<ComboBox>(dialog, "_obdDeviceComboBox");
                    ComboBox sensor = Field<ComboBox>(dialog, "_sensorDeviceComboBox");
                    obd.SelectedIndex = 1;
                    sensor.SelectedIndex = 2;
                    Assert.Equal(DemoOptions.Default, dialog.Options);
                    Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
                    selected = dialog.Options;
                    Assert.NotNull(selected.ObdDevice);
                    Assert.NotNull(selected.BluetoothSensorDevice);
                    Assert.NotEqual(selected.ObdDevice.Address, selected.BluetoothSensorDevice.Address);
                    Assert.False(selected.ObdDevice.IsPaired);
                    Assert.False(selected.BluetoothSensorDevice.IsPaired);
                    Assert.Contains(statuses, status => status.Contains("Scan complete", StringComparison.Ordinal));
                    Assert.Contains(statuses, status => status.Contains("2 paired/unpaired", StringComparison.Ordinal));
                }

                using (WinFormsUserSettingsService settings = Settings(path))
                {
                    DemoSettings.Save(settings, selected);
                    settings.Flush();
                }

                using WinFormsUserSettingsService restarted = Settings(path);
                DemoOptions restored = DemoSettings.Load(restarted, DemoOptions.Default);
                Assert.Equal(selected.ObdDevice!.Id, restored.ObdDevice!.Id);
                Assert.Equal(selected.BluetoothSensorDevice!.Address, restored.BluetoothSensorDevice!.Address);
                using Sensored.OptionsDialog remembered = new(restored, bluetoothScanner: scanner);
                Assert.Contains("saved; not seen", Field<ComboBox>(remembered, "_obdDeviceComboBox").Text);
                Assert.Contains("saved; not seen", Field<ComboBox>(remembered, "_sensorDeviceComboBox").Text);
                Field<ComboBox>(remembered, "_obdDeviceComboBox").SelectedIndex = 0;
                Invoke(remembered, "OnAcceptClick", null!, EventArgs.Empty);
                Assert.Null(remembered.Options.ObdDevice);
                Assert.Equal(restored.BluetoothSensorDevice, remembered.Options.BluetoothSensorDevice);
                DemoSettings.Save(restarted, remembered.Options);
                restarted.Flush();
                Assert.Null(DemoSettings.Load(restarted, DemoOptions.Default).ObdDevice);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder);
                }
            }
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BluetoothOptionsStopOrCloseAwaitsScanCleanup(bool close)
        => await RunStaAsync(async () =>
        {
            OptionsBluetoothScanner scanner = new() { WaitForCancellation = true };
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default, bluetoothScanner: scanner);
            dialog.Show();
            Task scan = InvokeTask(dialog, "StartBluetoothOptionsScanAsync");
            Assert.False(scan.IsCompleted);
            Assert.False(Field<Button>(dialog, "_scanBluetoothButton").Enabled);
            Assert.True(Field<Button>(dialog, "_stopBluetoothScanButton").Enabled);
            if (close)
            {
                dialog.Close();
                Assert.False(dialog.IsDisposed);
            }
            else
            {
                Invoke(dialog, "OnStopBluetoothScanClick", null!, EventArgs.Empty);
            }

            await scan;
            Assert.True(scanner.WatchEnded);
            if (close)
            {
                await Task.Delay(50);
                Assert.True(dialog.IsDisposed);
            }
            else
            {
                Assert.Contains("Scan stopped", Field<ToolStripStatusLabel>(dialog, "_bluetoothScanStatus").Text);
                Assert.True(Field<Button>(dialog, "_scanBluetoothButton").Enabled);
                Assert.False(Field<Button>(dialog, "_stopBluetoothScanButton").Enabled);
                Assert.Equal(DemoOptions.Default, dialog.Options);
            }
        });

    [Fact]
    public async Task BluetoothDiscoveryAccessFailureIsVisibleInStatusBar()
        => await RunStaAsync(async () =>
        {
            OptionsBluetoothScanner scanner = new() { Fail = true };
            List<string> statuses = [];
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default,
                reportBluetoothDiscoveryStatus: statuses.Add, bluetoothScanner: scanner);
            await InvokeTask(dialog, "StartBluetoothOptionsScanAsync");
            Assert.Contains("Scan failed: access denied", Field<ToolStripStatusLabel>(dialog, "_bluetoothScanStatus").Text);
            Assert.Contains(statuses, status => status.Contains("access denied", StringComparison.Ordinal));
            Assert.True(scanner.WatchEnded);
            Assert.True(Field<Button>(dialog, "_scanBluetoothButton").Enabled);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BluetoothScanCleanupPreservesModalAcceptOrCancel(bool accept)
        => await RunStaAsync(() =>
        {
            OptionsBluetoothScanner scanner = new() { WaitForCancellation = true };
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default, bluetoothScanner: scanner);
            dialog.Shown += (_, _) =>
            {
                _ = InvokeTask(dialog, "StartBluetoothOptionsScanAsync");
                Field<ComboBox>(dialog, "_obdDeviceComboBox").SelectedIndex = 1;
                if (accept)
                {
                    Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
                }
                else
                {
                    dialog.DialogResult = DialogResult.Cancel;
                }
            };
            Assert.Equal(accept ? DialogResult.OK : DialogResult.Cancel, dialog.ShowDialog());
            Assert.True(scanner.WatchEnded);
            Assert.Equal(accept, dialog.Options.ObdDevice is not null);

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(true, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, 0)]
    public async Task AutomaticAccelerationResultsAreSavedOnceAndNeverCreatedByPauseReplayOrNoFix(
        bool paused, bool replay, int fixQuality)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-acceleration-{Guid.NewGuid():N}");
        try
        {
            await RunStaAsync(async () =>
            {
                await using SensorService service = CreateService();
                using Sensored.MainForm form = new(service);
                SetField(form, "_options", DemoOptions.Default with { RecordingRootFolder = folder });
                SetField(form, "_paused", paused);
                SetField(form, "_obdReplayActive", replay);
                DateTimeOffset start = DateTimeOffset.UtcNow.AddSeconds(-8);
                for (int step = 0; step <= 70; step++)
                {
                    DateTimeOffset time = start.AddMilliseconds(step * 100);
                    GeolocationPayload fix = new(51d, 7d, double.NaN, 3d, double.NaN, step * 0.4d,
                        true, 0d, false, GpsPositionSource.Satellite, false, time, time,
                        double.NaN, double.NaN, double.NaN, fixQuality, null, 1d);
                    SensorEnvelope input = new()
                    {
                        Sequence = step, Kind = SensorDataKind.Geolocation,
                        SourceTimestamp = time, ArrivalTimestamp = time, Geolocation = fix
                    };
                    Invoke(form, "ObservePerformanceTiming", new FusionObservation(input, null, 0, "test"));
                }

                Invoke(form, "PresentPerformanceTiming");
                Invoke(form, "PresentPerformanceTiming");
                string path = Path.Combine(folder, "acceleration-runs.jsonl");
                if (paused || replay || fixQuality == 0)
                {
                    Assert.False(File.Exists(path));
                }
                else
                {
                    string line = Assert.Single(await File.ReadAllLinesAsync(path));
                    AccelerationRunResult result = JsonSerializer.Deserialize<AccelerationRunResult>(line)!;
                    Assert.Equal(AccelerationRunKind.ZeroToHundredKph, result.Kind);
                    Assert.Equal(AccelerationSpeedSource.Gps, result.Source);
                    Assert.Equal(51d, result.Latitude);
                    Assert.Equal(7d, result.Longitude);
                    Assert.InRange(result.Elapsed.TotalSeconds, 7.10d, 7.11d);
                }
            });
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SpeedometerModesCycleUnitsAndSourcesWithoutAffectingFusionAndSingleTapDoesNotCycle()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            D2DSpeedometer speed = Field<D2DSpeedometer>(form, "_speedometer");
            for (int mode = 0; mode < 6; mode++)
            {
                SetField(form, "_speedDisplayMode", mode);
                Invoke(form, "ApplySpeedModeOptions");
                Invoke(form, "UpdateSpeedModeCaption", mode % 3 != 2);
                Assert.StartsWith($"{(char)('A' + mode)}:", speed.DisplayModeLabel);
                Assert.Equal(mode >= 3 ? SpeedUnit.MilesPerHour : SpeedUnit.KilometersPerHour, speed.DisplayUnit);
                Assert.Equal(mode % 3 == 1, speed.ShowGpsSpeed);
                Assert.Equal(mode % 3 == 2, speed.DisplayModeLabel.Contains("(GPS)") && !speed.DisplayModeLabel.Contains("(OBD2)"));
                typeof(D2DSpeedometer).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(speed, [EventArgs.Empty]);
                Assert.Equal(mode, Field<int>(form, "_speedDisplayMode"));
                typeof(D2DSpeedometer).GetMethod("OnDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(speed, [EventArgs.Empty]);
                Assert.Equal((mode + 1) % 6, Field<int>(form, "_speedDisplayMode"));
            }
            Assert.Equal(SensorServiceState.Stopped, service.State);
        });

    [Fact]
    public async Task QuarterTurnButtonsWrapEachMountingAxisAndRestoreTheBluetoothNorm()
        => await RunStaAsync(() =>
        {
            foreach (SensorReferenceFrame initial in new[] { default(SensorReferenceFrame), new SensorReferenceFrame(0, 0, -90) })
            {
                using SensorReferenceFrameDialog editor = new(initial, "Mounting", resetFrame: initial);
                foreach (string axis in new[] { "Yaw", "Pitch", "Roll" })
                {
                    for (int turn = 0; turn < 4; turn++)
                    {
                        Invoke(editor, $"On{axis}QuarterTurnClick", null!, EventArgs.Empty);
                        if (turn == 0)
                        {
                            Assert.NotEqual(initial, editor.Frame);
                        }
                    }

                    Assert.Equal(initial, editor.Frame);
                }

                Invoke(editor, "OnRollQuarterTurnClick", null!, EventArgs.Empty);
                Invoke(editor, "OnResetClick", null!, EventArgs.Empty);
                Assert.Equal(initial, editor.Frame);
            }

            SensorAttitude normalized = DemoOptions.Default.BluetoothSensorFrame.Correct(0, 0, 0);
            Assert.Equal(0, normalized.RollDegrees, 8);
            Assert.Equal(0, normalized.PitchDegrees, 8);

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(12f)]
    [InlineData(24f)]
    public async Task LongStatusMessagesStaySingleRowAndRawObdRefreshRequiresTheFourthTab(float fontSize)
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, fontSize);
            await using ObdRawTrafficCollector collector = new();
            using Sensored.MainForm form = new(service);
            SetField(form, "_rawObd", collector);
            StatusStrip status = Field<StatusStrip>(form, "_statusStrip");
            status.Font = font;
            form.MinimumSize = Size.Empty;
            form.ClientSize = new Size(700, 500);
            Invoke(form, "FitStatusFields");
            int height = status.Height;
            SetField(form, "_statusWarning", string.Concat(Enumerable.Repeat("Long warning\r\n", 100)));
            Invoke(form, "RefreshStructuredStatus");
            status.PerformLayout();
            Assert.Equal(height, status.Height);
            Assert.All(status.Items.Cast<ToolStripItem>(), item => Assert.DoesNotContain('\n', item.Text ?? ""));
            ToolStripStatusLabel gps = Field<ToolStripStatusLabel>(form, "_gpsRateLabel");
            gps.Text = "GPS identity that needs more room";
            form.ClientSize = new Size(300, 500);
            Invoke(form, "FitStatusFields");
            int narrowGpsWidth = gps.Width;
            ToolStripStatusLabel obd = Field<ToolStripStatusLabel>(form, "_obdStatusLabel");
            ToolStripStatusLabel raw = Field<ToolStripStatusLabel>(form, "_rawStatusLabel");
            raw.Text = new string('W', 500);
            obd.Text = "OBD2: Selected adapter 10 HZ";
            Invoke(form, "FitStatusFields");
            Assert.True(gps.Width > 0);
            Assert.True(obd.Width > 0);
            Assert.True(raw.Width < DashboardStatusRenderer.MeasureTextWidth(raw));
            form.ClientSize = new Size(1600, 500);
            Invoke(form, "FitStatusFields");
            Assert.True(gps.Width > narrowGpsWidth);
            Assert.Equal(height, status.Height);

            UserControl rawView = Field<UserControl>(form, "_rawObdTabHost");
            TextBox messages = Field<TextBox>(rawView, "_messages");
            collector.Publish(new(DateTimeOffset.UtcNow, "RX", "7E8 03 41 0D 10"));
            await collector.StopRecordingAsync();
            Invoke(form, "PresentRawObd");
            Assert.Empty(messages.Text);
            var tabs = Field<WarpToolkit.WinForms.Containers.FluentTabControl>(form, "_bottomTabControl");
            tabs.CurrentTabIndex = 3;
            Assert.Same(rawView, tabs.CurrentTab);
            Invoke(form, "PresentRawObd");
            Assert.Contains("7E8 03 41 0D 10", messages.Text);
            tabs.CurrentTabIndex = 0;
            string retained = messages.Text;
            collector.Publish(new(DateTimeOffset.UtcNow, "RX", "Not on screen yet"));
            await collector.StopRecordingAsync();
            Invoke(form, "PresentRawObd");
            Assert.Equal(retained, messages.Text);
            await InvokeTask(form, "StopRawObdAsync");
        });

    [Fact]
    public async Task BluetoothAxisSwapUpdatesThePreviewAndCaptureStillZerosThePose()
        => await RunStaAsync(() =>
        {
            SensorReferenceSample sample = new(new(0, 0, -1), 12, -20, 70);
            using SensorReferenceFrameDialog editor = new(default, "Bluetooth reference", () => sample,
                allowAxisSwap: true);
            CheckBox swap = Field<CheckBox>(editor, "_swapPitchAndRollCheckBox");
            TabletOrientationView view = Field<TabletOrientationView>(editor, "_view");
            swap.Checked = true;
            Assert.True(editor.Frame.SwapPitchAndRoll);
            var (swappedRoll, swappedPitch, swappedHeading) = Field<(double, double, double)?>(view, "_attitude")!.Value;
            Assert.Equal(-20d, swappedRoll, 8);
            Assert.Equal(12d, swappedPitch, 8);
            Assert.Equal(70d, swappedHeading, 8);
            Invoke(editor, "OnCaptureClick", null!, EventArgs.Empty);
            Invoke(editor, "OnSampleTick", null!, EventArgs.Empty);
            Assert.True(editor.Frame.SwapPitchAndRoll);
            var (roll, pitch, heading) = Field<(double, double, double)?>(view, "_attitude")!.Value;
            Assert.Equal(0d, roll, 8);
            Assert.Equal(0d, pitch, 8);
            Assert.Equal(0d, GaugeGeometry.ShortestAngleDegrees(0d, heading), 8);
            Invoke(editor, "OnResetClick", null!, EventArgs.Empty);
            Assert.Equal(default, editor.Frame);
            Assert.False(swap.Checked);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task SensoredSphereLayoutAndAlwaysOnCanBeDisabledWithoutChangingFusion()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            KioskModeManager kiosk = Field<KioskModeManager>(form, "_kiosk");
            D2DArtificialHorizon horizon = Field<D2DArtificialHorizon>(form, "_artificialHorizon");
            TableLayoutPanel gauges = Field<TableLayoutPanel>(form, "_gaugeLayout");
            TableLayoutPanel map = Field<TableLayoutPanel>(form, "_mapLayout");
            Control obd = Field<Control>(form, "_obdView");
            ToolStripDropDownButton sources = Field<ToolStripDropDownButton>(form, "_sourcesButton");
            Assert.Collection(sources.DropDownItems.Cast<ToolStripItem>(),
                gps => Assert.StartsWith("GPS", gps.Text),
                obd2 => Assert.Equal("OBD2", obd2.Text),
                sensors => Assert.Equal("Sensors", sensors.Text));
            Assert.True(horizon.UseSphericalDisplay);
            Assert.True(kiosk.AlwaysOn);
            Assert.Equal(Keys.None, kiosk.ToggleFullScreenKeys);
            Assert.False(kiosk.EscapeExitsFullScreen);
            Assert.False(Field<ToolStripMenuItem>(form, "_bluetoothSensorsMenuItem").Enabled);
            IFusionEngine engine = DemoOptions.Default.CreateFusionEngine(isReplay: false);
            SetField(form, "_fusionEngine", engine);
            SetField(form, "_options", DemoOptions.Default with { ForceObdGaugesOn = true });
            Invoke(form, "ApplyDisplayOptions");
            Assert.Same(gauges, obd.Parent);
            Assert.Equal(new TableLayoutPanelCellPosition(0, 1), gauges.GetPositionFromControl(obd));
            Assert.Equal(0, map.ColumnStyles[0].Width);
            for (int mode = 1; mode <= 7; mode++)
            {
                Invoke(form, "CycleHorizonMode");
                Assert.Equal(mode % 7, (int)Field<HorizonSourceMode>(form, "_horizonMode"));
            }

            SetField(form, "_options", DemoOptions.Default with
            {
                UseSphericalHorizon = false, AlwaysOn = false, ForceObdGaugesOn = true
            });
            Invoke(form, "ApplyDisplayOptions");
            Assert.False(horizon.UseSphericalDisplay);
            Assert.False(kiosk.AlwaysOn);
            Assert.Same(gauges, obd.Parent);
            Assert.Equal(new TableLayoutPanelCellPosition(0, 1), gauges.GetPositionFromControl(obd));
            Assert.Equal(0, map.ColumnStyles[0].Width);
            Assert.Same(engine, Field<IFusionEngine>(form, "_fusionEngine"));
            Assert.Equal(SensorServiceState.Stopped, service.State);
            DemoOptions previous = Field<DemoOptions>(form, "_options");
            SetField(form, "_options", previous with { InternalSensorFrame = new(0, 0, 90) });
            await InvokeTask(form, "ApplySensorSourceOptionsAsync", previous);
            Assert.Equal(HorizonSourceMode.InternalGravityGps, Field<HorizonSourceMode>(form, "_horizonMode"));
            Assert.Same(engine, Field<IFusionEngine>(form, "_fusionEngine"));
            previous = previous with { SensorSource = SensorDisplaySource.Bluetooth };
            SetField(form, "_options", previous with { BluetoothSensorFrame = new(90, 15, -20) });
            SetField(form, "_horizonMode", HorizonSourceMode.FusionGps);
            await InvokeTask(form, "ApplySensorSourceOptionsAsync", previous);
            Assert.Equal(HorizonSourceMode.BluetoothInclinometerGps, Field<HorizonSourceMode>(form, "_horizonMode"));
            Assert.Same(engine, Field<IFusionEngine>(form, "_fusionEngine"));
            SetField(form, "_horizonMode", HorizonSourceMode.BluetoothInclinometerGps);
            await InvokeTask(form, "ApplySensorSourceOptionsAsync", previous);
            Assert.Equal(HorizonSourceMode.BluetoothInclinometerGps, Field<HorizonSourceMode>(form, "_horizonMode"));
        });

    [Theory]
    [InlineData("_yawUpDown")]
    [InlineData("_pitchUpDown")]
    [InlineData("_rollUpDown")]
    public async Task EveryReferenceAxisChangesTheRenderedPreview(string axis)
        => await RunStaAsync(() =>
        {
            SensorReferenceSample sample = new(new(0, 0, -1), 0, 0, 0);
            using SensorReferenceFrameDialog editor = new(default, "Bluetooth reference", () => sample);
            TabletOrientationView view = Field<TabletOrientationView>(editor, "_view");
            view.Size = new Size(420, 300);
            using Bitmap before = new(view.Width, view.Height);
            view.DrawToBitmap(before, view.ClientRectangle);
            Field<NumericUpDown>(editor, axis).Value = 30;
            using Bitmap after = new(view.Width, view.Height);
            view.DrawToBitmap(after, view.ClientRectangle);
            bool changed = false;
            for (int y = 0; y < before.Height && !changed; y++)
            {
                for (int x = 0; x < before.Width; x++)
                {
                    if (before.GetPixel(x, y) != after.GetPixel(x, y))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            Assert.True(changed, $"{axis} did not change the preview.");

            return Task.CompletedTask;
        });

    [Fact]
    public async Task PositivePreviewRollLowersTheRightSide()
        => await RunStaAsync(() =>
        {
            using TabletOrientationView view = new() { Size = new Size(420, 300) };
            view.SetAttitude(20, 0, 0);
            using Bitmap image = new(view.Width, view.Height);
            view.DrawToBitmap(image, view.ClientRectangle);
            List<int> left = [];
            List<int> right = [];
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (image.GetPixel(x, y).ToArgb() != Color.FromArgb(255, 196, 87).ToArgb())
                    {
                        continue;
                    }

                    if (x < image.Width * 0.4)
                    {
                        left.Add(y);
                    }
                    else if (x > image.Width * 0.6)
                    {
                        right.Add(y);
                    }
                }
            }

            Assert.NotEmpty(left);
            Assert.NotEmpty(right);
            Assert.True(right.Average() > left.Average());

            return Task.CompletedTask;
        });

    [Fact]
    public async Task UpDownEditorsRecoverTheirHeightOnShowAndFontChanges()
        => await RunStaAsync(() =>
        {
            using SensorReferenceFrameDialog reference = new(default, "Reference");
            using Sensored.OptionsDialog options = new(DemoOptions.Default);
            using Font large = new(SystemFonts.MessageBoxFont!.FontFamily, 24f);
            using Font small = new(SystemFonts.MessageBoxFont!.FontFamily, 12f);
            foreach (Form form in new Form[] { reference, options })
            {
                form.VisualStylesMode = VisualStylesMode.Net11;
                UpDownBase[] editors = Descendants(form).OfType<UpDownBase>().ToArray();
                Assert.NotEmpty(editors);
                foreach (UpDownBase editor in editors)
                {
                    editor.MinimumSize = Size.Empty;
                    editor.Height = 1;
                }

                Invoke(form, "OnShown", EventArgs.Empty);
                Assert.All(editors, editor => Assert.True(editor.Height >= editor.Font.Height));
                form.Font = large;
                Assert.All(editors, editor => Assert.True(editor.Height >= editor.Font.Height));
                int largeHeight = editors[0].Height;
                form.Font = small;
                Assert.All(editors, editor => Assert.True(editor.Height >= editor.Font.Height));
                Assert.True(editors[0].Height < largeHeight);
            }

            return Task.CompletedTask;

            static IEnumerable<Control> Descendants(Control root)
            {
                foreach (Control child in root.Controls)
                {
                    yield return child;
                    foreach (Control descendant in Descendants(child))
                    {
                        yield return descendant;
                    }
                }
            }
        });

    [Theory]
    [InlineData(12f)]
    [InlineData(24f)]
    public async Task ReferenceDialogStaysWithinTheScreenWithReadableEditors(float fontSize)
        => await RunStaAsync(() =>
        {
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, fontSize);
            SensorReferenceSample sample = new(new(0, 0, -1), 0, 0, 0);
            using SensorReferenceFrameDialog reference = new(default, "Bluetooth reference", () => sample, font);
            reference.VisualStylesMode = VisualStylesMode.Net11;
            reference.Show();
            reference.PerformLayout();
            Rectangle screen = Screen.FromControl(reference).WorkingArea;
            Assert.True(screen.Contains(reference.Bounds), $"{reference.Bounds} exceeds {screen}.");
            Control view = Field<Control>(reference, "_view");
            Assert.True(view.Height >= reference.LogicalToDeviceUnits(160));
            Assert.True(view.Width < reference.ClientSize.Width);
            NumericUpDown editor = Field<NumericUpDown>(reference, "_rollUpDown");
            reference.ScrollControlIntoView(editor);
            Assert.True(editor.Height >= editor.Font.Height);
            reference.ScrollControlIntoView(Field<Button>(reference, "_okButton"));
            Rectangle button = reference.RectangleToClient(
                Field<Button>(reference, "_okButton").RectangleToScreen(
                    Field<Button>(reference, "_okButton").ClientRectangle));
            Assert.True(reference.ClientRectangle.Contains(button));
            reference.Close();

            return Task.CompletedTask;
        });

    [Fact]
    public async Task SensorReferenceResetIsIndependentAndCancelDoesNotApplyEdits()
        => await RunStaAsync(() =>
        {
            DemoOptions options = DemoOptions.Default with
            {
                InternalSensorFrame = new(10, 20, 30), BluetoothSensorFrame = new(40, 50, 60)
            };
            using Sensored.OptionsDialog dialog = new(options);
            Invoke(dialog, "ResetSensorFrame", true);
            Assert.Equal(options, dialog.Options);
            Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
            Assert.Equal(default, dialog.Options.InternalSensorFrame);
            Assert.Equal(options.BluetoothSensorFrame, dialog.Options.BluetoothSensorFrame);
            using SensorReferenceFrameDialog editor = new(options.BluetoothSensorFrame, "Bluetooth reference");
            Invoke(editor, "OnResetClick", null!, EventArgs.Empty);
            Assert.Equal(default, editor.Frame);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task SensorReferenceCapturePreservesMissingYawAndUnavailableSamples()
        => await RunStaAsync(() =>
        {
            SensorReferenceSample? sample = new(new(0, 0, -1), 12, -20, double.NaN);
            using SensorReferenceFrameDialog editor = new(new(70, 5, 6), "Internal reference", () => sample);
            Invoke(editor, "OnCaptureClick", null!, EventArgs.Empty);
            Assert.Equal(new SensorReferenceFrame(70, -20, 12), editor.Frame);
            Assert.Contains("yaw offset was retained", Field<Label>(editor, "_liveLabel").Text);
            sample = null;
            Invoke(editor, "OnSampleTick", null!, EventArgs.Empty);
            Assert.False(Field<Button>(editor, "_captureButton").Enabled);
            Invoke(editor, "OnCaptureClick", null!, EventArgs.Empty);
            Assert.Equal(new SensorReferenceFrame(70, -20, 12), editor.Frame);
            sample = new(new(0, 0, -1), 370, -380, 450);
            Invoke(editor, "OnCaptureClick", null!, EventArgs.Empty);
            Assert.Equal(new SensorReferenceFrame(90, -20, 10), editor.Frame);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new SensorReferenceFrameDialog(new(double.NaN, 0, 0), "Invalid reference"));

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SensorOptionsAllowNewBluetoothSelectionOnlyWhenAvailable(bool available)
        => await RunStaAsync(() =>
        {
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default, bluetoothAvailable: available);
            Field<ComboBox>(dialog, "_sensorSourceComboBox").SelectedIndex = 1;
            Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
            Assert.Equal(available ? SensorDisplaySource.Bluetooth : SensorDisplaySource.Internal,
                dialog.Options.SensorSource);
            using Sensored.OptionsDialog remembered = new(DemoOptions.Default with
            {
                SensorSource = SensorDisplaySource.Bluetooth
            });
            Assert.Equal(1, Field<ComboBox>(remembered, "_sensorSourceComboBox").SelectedIndex);
            Invoke(remembered, "OnAcceptClick", null!, EventArgs.Empty);
            Assert.Equal(SensorDisplaySource.Bluetooth, remembered.Options.SensorSource);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task ResetPresentationClearsHistoricalReferencesAndInternalSampleHistory()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            DateTimeOffset historical = DateTimeOffset.UnixEpoch;
            SensorComparisonReference comparison = new(SensorDisplaySource.Bluetooth,
                new(true, false, "recorded", new(historical, 0, 0, 1, 0, 0, 0, 20, 10, 100)),
                default, default, null, null, null);
            SetField(form, "_presentedComparison", comparison);
            SetField(form, "_obdReplayTimeTicks", historical.UtcTicks);
            Invoke(form, "ObserveHorizonInput", new SensorEnvelope
            {
                Kind = SensorDataKind.Imu, Sequence = 1, SourceTimestamp = historical,
                ArrivalTimestamp = historical,
                Imu = new RawImuPayload(new(0, 0, -1), Vec3.Zero, true, 0, 1, 0, 0, 0, default)
            });
            Invoke(form, "ResetSessionPresentation");
            Assert.Null(Field<SensorComparisonReference?>(form, "_presentedComparison"));
            Assert.Equal(0, Field<long>(form, "_obdReplayTimeTicks"));
            Assert.Null(Field<RawImuPayload?>(form, "_horizonInternalSample"));
            Assert.Equal(default, Field<DateTimeOffset>(form, "_horizonInternalReceivedAt"));
        });

    [Fact]
    public async Task ReplayComparisonUsesHistoricalSensorTimeAndFramesNotCurrentSettings()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            DateTimeOffset historical = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
            Wt901Reading bluetooth = new(historical, 0, 0, 1, 0, 0, 0, 20, 10, 100);
            SensorComparisonReference reference = new(SensorDisplaySource.Bluetooth,
                new(true, false, "recorded", bluetooth), default, new(100, 10, 20), 10, historical, 2);
            SetField(form, "_options", DemoOptions.Default with { BluetoothSensorFrame = new(30, 40, 50) });
            SetField(form, "_obdReplayActive", true);
            SetField(form, "_obdReplayTimeTicks", historical.UtcTicks);
            SetField(form, "_presentedComparison", reference);
            SetField(form, "_horizonMode", HorizonSourceMode.BluetoothCompass);
            Invoke(form, "ApplySelectedHorizon");
            HorizonSourceReading reading = Field<HorizonSourceReading>(form, "_presentedHorizon");
            Assert.True(reading.HasAttitude);
            Assert.Equal(0, reading.RollDegrees, 8);
            Assert.Equal(0, reading.PitchDegrees, 8);
            Assert.True(Math.Abs(GaugeGeometry.ShortestAngleDegrees(0, reading.HeadingDegrees)) < 1e-8);
        });

    [Theory]
    [InlineData(SpeedometerAccelerationDamping.None)]
    [InlineData(SpeedometerAccelerationDamping.Light)]
    [InlineData(SpeedometerAccelerationDamping.Medium)]
    [InlineData(SpeedometerAccelerationDamping.Strong)]
    public async Task SensoredAccelerationDampingRoundTripsOnFirstOptionsPageAndAppliesToInstrument(
        SpeedometerAccelerationDamping level)
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-acceleration-settings-{Guid.NewGuid():N}");
            string path = Path.Combine(folder, "settings.json");
            try
            {
                DemoOptions options;
                using (Sensored.OptionsDialog dialog = new(DemoOptions.Default))
                {
                    ComboBox selector = Field<ComboBox>(dialog, "_accelerationDampingComboBox");
                    TabControl tabs = Field<TabControl>(dialog, "_tabControl");
                    Assert.True(tabs.TabPages[0].Contains(selector));
                    Assert.Equal(SpeedometerAccelerationDamping.None, selector.SelectedItem);
                    selector.SelectedItem = level;
                    Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
                    Assert.Equal(DialogResult.OK, dialog.DialogResult);
                    options = dialog.Options;
                }

                using (WinFormsUserSettingsService settings = Settings(path))
                {
                    DemoSettings.Save(settings, options);
                    settings.Flush();
                }

                using WinFormsUserSettingsService restarted = Settings(path);
                DemoOptions restored = DemoSettings.Load(restarted, DemoOptions.Default);
                Assert.Equal(level, restored.AccelerationDamping);
                using (Sensored.OptionsDialog dialog = new(restored))
                {
                    Assert.Equal(level, Field<ComboBox>(dialog, "_accelerationDampingComboBox").SelectedItem);
                }

                await using SensorService service = CreateService();
                using Sensored.MainForm form = new(service, restarted);
                D2DSpeedometer speed = Field<D2DSpeedometer>(form, "_speedometer");
                Assert.Equal(level, speed.AccelerationDamping);
                SpeedometerAccelerationDamping changed = level == SpeedometerAccelerationDamping.Strong
                    ? SpeedometerAccelerationDamping.Light
                    : SpeedometerAccelerationDamping.Strong;
                SetField(form, "_options", restored with { AccelerationDamping = changed });
                Invoke(form, "ApplyDisplayOptions");
                Assert.Equal(changed, speed.AccelerationDamping);
            }
            finally
            {
                File.Delete(path);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder);
                }
            }
        });

    [Fact]
    public async Task SensoredV2ViewerGroupsTriplesFormatsPerColumnAndRebuildsResolutionFromOriginalRows()
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"sensored-viewer-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "drive.RawV2.csv");
            string fuse = Path.Combine(folder, "drive.FuseV2.csv");
            try
            {
                await File.WriteAllTextAsync(path,
                    "recording_id;capture_utc;accel_x_g;accel_y_g;accel_z_g\n"
                    + "r;2026-10-01T12:00:00.010Z;1.123456;2.123456;3.123456\n"
                    + "r;2026-10-01T12:00:00.110Z;3.123456;4.123456;5.123456\n");
                await File.WriteAllTextAsync(fuse,
                    "recording_id;out_t;epoch;acc_long_mps2;acc_lat_mps2;acc_vert_mps2;acceleration_state\n"
                    + "r;2026-10-01T12:00:00.010Z;1;2;3;4;Supported\n");
                using RawLogViewerDialog viewer = new(new ReadingFormats(2, 7, 1, 2), DashboardToolbarSize.Small);
                await InvokeTask(viewer, "LoadPathAsync", folder);
                DataGridView grid = Field<DataGridView>(viewer, "_grid");
                Assert.Equal(2, grid.RowCount);
                Assert.Equal(3, grid.Columns.Count);
                Assert.Equal("Acceleration (g)", grid.Columns[2].HeaderText);
                Assert.Equal("X: 1.123\r\nY: 2.123\r\nZ: 3.123", grid.Rows[0].Cells[2].Value);
                Invoke(viewer, "OnPrecisionMenu", grid,
                    new DataGridViewCellMouseEventArgs(2, -1, 0, 0,
                        new MouseEventArgs(MouseButtons.Right, 1, 0, 0, 0)));
                ContextMenuStrip precision = Assert.Single(
                    Field<System.ComponentModel.IContainer>(viewer, "components").Components.OfType<ContextMenuStrip>());
                precision.Items[1].PerformClick();
                precision.Close();
                Assert.Equal("X: 1.1\r\nY: 2.1\r\nZ: 3.1", grid.Rows[0].Cells[2].Value);
                ToolStripComboBox resolution = Field<ToolStripComboBox>(viewer, "_resolutionSelector");
                resolution.SelectedItem = 1000;
                DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while (!resolution.Enabled && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(10, TestContext.Current.CancellationToken);
                }

                Assert.True(resolution.Enabled);
                Assert.Equal(1, grid.RowCount);
                Assert.Equal("X: 2.1\r\nY: 3.1\r\nZ: 4.1", grid.Rows[0].Cells[2].Value);
                Assert.False(Field<Panel>(viewer, "_editorPanel").Visible);
                grid.Rows[0].Selected = true;
                await InvokeTask(viewer, "ShowV2SourceAsync");
                DataGridView original = Field<DataGridView>(viewer, "_rawGrid");
                Assert.Equal("1.123456", original.Rows[2].Cells[1].Value);
                ToolStripComboBox fileSelector = Field<ToolStripComboBox>(viewer, "_v2FileSelector");
                Assert.Equal(2, fileSelector.Items.Count);
                fileSelector.SelectedIndex = 0;
                deadline = DateTime.UtcNow.AddSeconds(10);
                while (!fileSelector.Enabled && DateTime.UtcNow < deadline)
                {
                    await Task.Delay(10, TestContext.Current.CancellationToken);
                }

                Assert.True(fileSelector.Enabled);
                Assert.Equal("Fused acceleration (m/s2)", grid.Columns["acc_long_mps2"]!.HeaderText);
                Assert.Equal("Forward: 2.000\r\nRight: 3.000\r\nDown: 4.000",
                    grid.Rows[0].Cells["acc_long_mps2"].Value);
            }
            finally
            {
                File.Delete(path);
                File.Delete(fuse);
                Directory.Delete(folder);
            }
        });

    [Theory]
    [InlineData(ObdAdapterKind.ObdLinkCx)]
    [InlineData(ObdAdapterKind.Ad20)]
    public async Task AdapterOptionsRoundTripThroughBothEditorsAndPersistAcrossRestart(ObdAdapterKind adapter)
        => await RunStaAsync(() =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-adapter-settings-{Guid.NewGuid():N}");
            string path = Path.Combine(folder, "settings.json");
            try
            {
                DemoOptions sensored = DemoOptions.Default;
                FusionGaugesOptions gauges = new();
                using (Sensored.OptionsDialog dialog = new(sensored))
                {
                    ComboBox selector = Field<ComboBox>(dialog, "_obdAdapterComboBox");
                    Assert.Equal(ObdAdapterKind.ObdLinkCx, selector.SelectedItem);
                    Assert.Equal(2, selector.Items.Count);
                    selector.SelectedItem = adapter;
                    Assert.Equal(ObdAdapters.GetName(adapter), selector.GetItemText(selector.SelectedItem));
                    Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
                    Assert.Equal(DialogResult.OK, dialog.DialogResult);
                    sensored = dialog.Options;
                }

                using (WinFormsFusionGauges.WinFormsApp.OptionsDialog dialog = new(gauges, SystemFonts.MessageBoxFont!))
                {
                    SourceOptionsView page = Field<SourceOptionsView>(dialog, "_sources");
                    ComboBox selector = Field<ComboBox>(page, "_obdAdapter");
                    Assert.Equal(ObdAdapterKind.ObdLinkCx, selector.SelectedItem);
                    selector.SelectedItem = adapter;
                    Assert.Equal(ObdAdapters.GetName(adapter), selector.GetItemText(selector.SelectedItem));
                    Invoke(dialog, "OnAccept", null!, EventArgs.Empty);
                    Assert.Equal(DialogResult.OK, dialog.DialogResult);
                    gauges = dialog.Options;
                }

                using (WinFormsUserSettingsService settings = Settings(path))
                {
                    DemoSettings.Save(settings, sensored);
                    settings.Set("FusionGauges.Options", gauges);
                    settings.Flush();
                }

                using WinFormsUserSettingsService restarted = Settings(path);
                Assert.Equal(adapter, DemoSettings.Load(restarted, DemoOptions.Default).ObdAdapter);
                Assert.Equal(adapter, restarted.Get("FusionGauges.Options", new FusionGaugesOptions()).ObdAdapter);
                Assert.Equal(ObdAdapterKind.ObdLinkCx,
                    JsonSerializer.Deserialize<FusionGaugesOptions>("{}")!.ObdAdapter);
            }
            finally
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }

            return Task.CompletedTask;
        });

    [Fact]
    public async Task SensoredEnablesAutoAccelerationAndRetainsReadinessWithoutAPermanentBanner()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            D2DSpeedometer speed = Field<D2DSpeedometer>(form, "_speedometer");
            Assert.Equal(SpeedometerAccelerationMode.Auto, speed.AccelerationMode);
            Assert.Equal(4, speed.AccelerationSampleCount);
            Assert.Contains("measured physical mount", Field<string>(form, "_lastStatusNotification"));
            TableLayoutPanel gauges = Field<TableLayoutPanel>(form, "_gaugeLayout");
            Assert.Equal(2, gauges.RowCount);
            Assert.DoesNotContain(gauges.Controls.Cast<Control>(), control => control is Label);
            Assert.Equal("Fusion setup", Field<ToolStripButton>(form, "_calibrateButton").Text);
            Assert.Equal(SensorServiceState.Stopped, service.State);
        });

    [Fact]
    public async Task OnlineMapPermissionUsesExplicitControlsWithoutAnExtraDialogAndSavesImmediately()
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-map-permission-{Guid.NewGuid():N}");
            string path = Path.Combine(folder, "settings.json");
            try
            {
                using WinFormsUserSettingsService settings = Settings(path);
                await using SensorService service = CreateService();
                using Sensored.MainForm form = new(service, settings);
                SensorRouteMapControl map = Field<SensorRouteMapControl>(form, "_routeMap");
                map.UseOpenStreetMap = false;
                ToolStripMenuItem online = Field<ToolStripMenuItem>(form, "_onlineMapTilesMenuItem");
                Assert.False(online.Checked);

                online.PerformClick();

                Assert.True(map.AllowTileNetwork);
                using (WinFormsUserSettingsService restarted = Settings(path))
                {
                    Assert.True(DemoSettings.Load(restarted, DemoOptions.Default).AllowOnlineMapTiles);
                }

                online.PerformClick();

                Assert.False(map.AllowTileNetwork);
                using (WinFormsUserSettingsService restarted = Settings(path))
                {
                    Assert.False(DemoSettings.Load(restarted, DemoOptions.Default).AllowOnlineMapTiles);
                }

                using Sensored.OptionsDialog options = new(DemoOptions.Default);
                Field<CheckBox>(options, "_onlineTilesCheckBox").Checked = true;
                Invoke(options, "OnAcceptClick", null!, EventArgs.Empty);
                Assert.True(options.Options.AllowOnlineMapTiles);
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder);
                }
            }
        });

    [Fact]
    public async Task SensoredMapRecallsDismissedNotificationsWithoutRepeatedThroughputResettingThem()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            SetField(form, "_learningOpenFailed", true);
            form.Show();
            Invoke(form, "SetStatus", "Receiver ready", true);
            D2DPopup popup = Field<D2DPopup>(form, "_statusNotification");
            Assert.True(popup.Visible);
            Assert.Same(form, popup.Owner);
            Assert.Equal("Receiver ready", popup.Text);
            popup.UpdateAnimation(TimeSpan.FromMilliseconds(10800));
            Assert.Equal(0.5f, Field<float>(popup, "_fadeOpacity"), 3);
            Invoke(form, "SetStatus", "IMU 100; GPS 10; published 1000", false);
            Invoke(form, "SetStatus", "Receiver ready", true);
            Assert.Equal("Receiver ready", popup.Text);
            Assert.Equal(0.5f, Field<float>(popup, "_fadeOpacity"), 3);
            typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(popup, [new MouseEventArgs(MouseButtons.Left, 1, 20, 20, 0)]);
            Assert.False(popup.Visible);
            SensorRouteMapControl map = Field<SensorRouteMapControl>(form, "_routeMap");
            typeof(Control).GetMethod("OnMouseDoubleClick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(map.MapControl, [new MouseEventArgs(MouseButtons.Left, 2, 20, 20, 0)]);
            Assert.True(popup.Visible);
            Assert.Equal("Receiver ready", popup.Text);
            Assert.Equal(1, Field<float>(popup, "_fadeOpacity"));
            Invoke(form, "SetStatus", "Receiver disconnected", true);
            Assert.Equal("Receiver disconnected", popup.Text);
            Assert.Equal("Receiver disconnected", Field<string>(form, "_lastStatusNotification"));
            map.Visible = false;
            Assert.False(popup.Visible);
            Assert.False(Field<System.Windows.Forms.Timer>(popup, "_animationTimer").Enabled);
            form.Dispose();
            Assert.True(popup.IsDisposed);
        });

    [Theory]
    [InlineData(800, 600, 12)]
    [InlineData(360, 240, 24)]
    public async Task D2DNotificationSlidesHoldsForTenSecondsFadesAndStaysInsideItsAnchor(
        int width, int height, int fontSize)
        => await RunStaAsync(async () =>
        {
            using Font font = new("Segoe UI", fontSize);
            using Form owner = new() { ClientSize = new Size(width, height), Font = font };
            using Panel map = new() { Dock = DockStyle.Fill, BackColor = Color.SteelBlue };
            owner.Controls.Add(map);
            owner.Show();
            using D2DPopup popup = new();
            string message = string.Join(Environment.NewLine, Enumerable.Repeat(
                "Fusion status changed; open Fusion setup for the current support details.", 5));
            popup.ShowNotification(owner, map, message);
            Assert.True(popup.TopLevel);
            Assert.True(popup.UseDirectCompositionExclusiveRendering);
            Assert.Equal(0, popup.BackColor.A);
            Assert.Empty(popup.Controls);
            Assert.NotSame(popup, Form.ActiveForm);
            popup.UpdateAnimation(TimeSpan.FromMilliseconds(150));
            Assert.InRange(Field<float>(popup, "_slideProgress"), 0.01f, 0.99f);
            popup.UpdateAnimation(TimeSpan.FromMilliseconds(300));
            Assert.Equal(1, Field<float>(popup, "_slideProgress"));
            popup.UpdateAnimation(TimeSpan.FromMilliseconds(10299));
            Assert.Equal(1, Field<float>(popup, "_fadeOpacity"));
            popup.UpdateAnimation(TimeSpan.FromMilliseconds(10800));
            Assert.Equal(0.5f, Field<float>(popup, "_fadeOpacity"), 3);
            await Task.Delay(100, TestContext.Current.CancellationToken);
            Assert.NotNull(popup.FrameDiagnostics);
            Assert.True(popup.FrameDiagnostics.PresentCount > 0);
            Assert.Null(popup.FrameDiagnostics.LastException);
            owner.Location += new Size(10, 10);
            popup.Reposition();
            Assert.True(map.RectangleToScreen(map.ClientRectangle).Contains(popup.Bounds));
            if (fontSize == 24)
            {
                typeof(Control).GetMethod("OnMouseWheel", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(popup, [new MouseEventArgs(MouseButtons.None, 0, 20, 20, -120)]);
                Assert.True(Field<float>(popup, "_scrollOffset") > 0);
            }

            popup.UpdateAnimation(TimeSpan.FromMilliseconds(11300));
            Assert.False(popup.Visible);
            Assert.Equal(0, Field<float>(popup, "_fadeOpacity"));
            Assert.False(Field<System.Windows.Forms.Timer>(popup, "_animationTimer").Enabled);
            popup.ShowNotification(owner, map, "New notification");
            Assert.True(popup.Visible);
            Assert.Equal(1, Field<float>(popup, "_fadeOpacity"));
        });

    [Fact]
    public async Task D2DQuestionsUseConfiguredAnswersAndNeverSelectOnTimeoutOrDismissal()
        => await RunStaAsync(() =>
        {
            using Form owner = new() { ClientSize = new Size(800, 600) };
            using Panel map = new() { Dock = DockStyle.Fill };
            owner.Controls.Add(map);
            owner.Show();
            using D2DPopup popup = new();
            string[] answers = ["Keep", "Discard", "Later", "Pause questions"];
            int? selected = null;
            popup.ShowQuestion(owner, map, "Review this observation?", answers, index => selected = index);
            popup.UpdateAnimation(TimeSpan.FromMinutes(2));
            Assert.True(popup.IsQuestion);
            Assert.Null(selected);
            Assert.Equal(1, Field<float>(popup, "_fadeOpacity"));
            Assert.False(Field<System.Windows.Forms.Timer>(popup, "_animationTimer").Enabled);
            typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(popup, [new MouseEventArgs(MouseButtons.Left, 1, 2, 2, 0)]);
            Assert.True(popup.IsQuestion);
            RectangleF lastAnswer = Field<RectangleF[]>(popup, "_answerBounds")[3];
            typeof(Control).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(popup, [new MouseEventArgs(MouseButtons.Left, 1,
                    (int)(lastAnswer.Left + lastAnswer.Width / 2),
                    (int)(lastAnswer.Top + lastAnswer.Height / 2), 0)]);
            Assert.Equal(3, selected);
            Assert.False(popup.Visible);
            selected = null;
            popup.ShowQuestion(owner, map, "Review this observation?", answers, index => selected = index);
            Assert.True(popup.ProcessQuestionKey(Keys.Escape));
            Assert.Null(selected);
            popup.ShowQuestion(owner, map, "Review this observation?", answers, index => selected = index);
            Assert.True(popup.ProcessQuestionKey(Keys.Tab));
            Assert.True(popup.ProcessQuestionKey(Keys.Right));
            Assert.True(popup.ProcessQuestionKey(Keys.Enter));
            Assert.Equal(1, selected);
            Assert.False(popup.Visible);

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(double.NaN, false, true)]
    [InlineData(12d, false, true)]
    [InlineData(0d, true, true)]
    [InlineData(0d, false, false)]
    public async Task SensoredAllowsAnExplicitMotionOverrideForMissingMovingOrStaleGps(
        double speed, bool stale, bool warningExpected)
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            await using DemoSensorSourceFactory factory = new();
            ManualGpsSource gps = new();
            GpsPreviewSession preview = new(gps);
            SetField(factory, "_preview", preview);
            using Sensored.MainForm form = new(service, null, factory);
            if (double.IsFinite(speed))
            {
                gps.Emit(speed, DateTimeOffset.UtcNow - (stale ? TimeSpan.FromMinutes(1) : TimeSpan.Zero));
                if (stale)
                {
                    SetField(preview, "_latest", preview.Capture().Gps! with
                    {
                        ArrivalTimestamp = DateTimeOffset.UtcNow.AddMinutes(-1)
                    });
                }
            }

            DemoOptions options = DemoOptions.Default with
            {
                SourceKind = WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps,
                SerialGnssProfile = SerialGnssProfile.Current
            };
            MethodInfo confirm = typeof(Sensored.MainForm).GetMethod("ConfirmGpsApply",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            int prompts = 0;
            Func<string, bool> decline = warning =>
            {
                prompts++;
                Assert.Contains("Proceed anyway?", warning);

                return false;
            };
            Assert.Equal(!warningExpected, (bool)confirm.Invoke(form, [options, decline])!);
            Assert.Equal(warningExpected ? 1 : 0, prompts);
            Func<string, bool> accept = _ => true;
            Assert.True((bool)confirm.Invoke(form, [options, accept])!);
            // An override is scoped to one operation, never a permanent motion bypass.
            Assert.Equal(!warningExpected, (bool)confirm.Invoke(form, [options, decline])!);
            Assert.True((bool)confirm.Invoke(form,
                [options with { SerialGnssProfile = SerialGnssProfile.Passive }, decline])!);
        });

    [Fact]
    public async Task SensoredCalibrationPrefillsDuringAcquisitionAndSavesWithoutResettingActiveFusion()
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            await using ISensorServiceSessionLease lease = await service.AcquireSessionAsync(
                DemoOptions.CreateSensorServiceOptions());
            using Sensored.MainForm form = new(service);
            IFusionEngine engine = DemoOptions.Default.CreateFusionEngine(isReplay: false);
            FusionEstimate estimate = new() { Sequence = 42, Timestamp = DateTimeOffset.UtcNow };
            SetField(form, "_lease", lease);
            SetField(form, "_fusionEngine", engine);
            SetField(form, "_lastFusionEstimate", estimate);
            SetField(form, "_lastRawImu", new RawImuPayload(new Vec3(0, -0.8, -0.6),
                Vec3.Zero, true, double.NaN, 0, double.NaN, double.NaN, double.NaN, default));
            SetField(form, "_lastRawImuReceivedAt", DateTimeOffset.UtcNow);
            SetField(form, "_paused", true);
            bool inspected = false;
            bool editable = false;
            string down = string.Empty;
            using System.Windows.Forms.Timer closer = new() { Interval = 100 };
            closer.Tick += (_, _) =>
            {
                VehicleFusionSetupDialog? dialog = Application.OpenForms.OfType<VehicleFusionSetupDialog>().FirstOrDefault();
                if (dialog is null)
                {
                    return;
                }

                editable = Field<Button>(dialog, "_saveButton").Enabled;
                down = Field<TextBox>(dialog, "_down").Text;
                inspected = true;
                closer.Stop();
                dialog.DialogResult = DialogResult.OK;
            };
            closer.Start();
            Field<ToolStripButton>(form, "_calibrateButton").PerformClick();
            Assert.True(inspected);
            Assert.True(editable);
            Assert.Equal("0 -0.8 -0.6", down);
            VehicleMountReference mount = Assert.IsType<VehicleMountReference>(Field<DemoOptions>(form, "_options").VehicleMount);
            Assert.Contains("User assumes vehicle level and parked", mount.Provenance);
            Assert.Same(engine, Field<IFusionEngine>(form, "_fusionEngine"));
            Assert.Same(estimate, Field<FusionEstimate>(form, "_lastFusionEstimate"));
            Assert.Equal(1, service.ActiveSessionCount);
            SetField(form, "_lease", null!);
        });

    [Fact]
    public async Task SensoredSourceEditorAcceptsGpsTypeComPortAndObdAdapterTogether()
        => await RunStaAsync(() =>
        {
            using Sensored.OptionsDialog dialog = new(DemoOptions.Default);
            Field<ComboBox>(dialog, "_sourceComboBox").SelectedIndex = 2;
            Field<ComboBox>(dialog, "_serialPortComboBox").Text = "COM42";
            Field<ComboBox>(dialog, "_gnssProfileComboBox").SelectedIndex =
                Array.IndexOf(Enum.GetValues<SerialGnssProfile>(), SerialGnssProfile.Current);
            Field<ComboBox>(dialog, "_obdAdapterComboBox").SelectedItem = ObdAdapterKind.Ad20;
            Field<CheckBox>(dialog, "_allowReceiverCommandsCheckBox").Checked = false;
            Invoke(dialog, "OnAcceptClick", null!, EventArgs.Empty);
            Assert.Equal(DialogResult.OK, dialog.DialogResult);
            Assert.Equal(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps, dialog.Options.SourceKind);
            Assert.Equal("COM42", dialog.Options.SerialPortName);
            Assert.Equal(SerialGnssProfile.Current, dialog.Options.SerialGnssProfile);
            Assert.Equal(ObdAdapterKind.Ad20, dialog.Options.ObdAdapter);

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(9f)]
    [InlineData(12f)]
    [InlineData(24f)]
    public async Task LearningQuizHasSixLargeAnswersPauseBoundsAndDismissesViewedEvidence(float points)
        => await RunStaAsync(() =>
        {
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, points);
            RoadLearningSession session = new("test-profile");
            using RoadLearningJournal journal = new(Path.GetTempPath(), session.Id);
            using RoadLearningQuizDialog dialog = new(session, journal, () => true, () => null,
                (_, exception) => throw exception, font);
            dialog.StartPosition = FormStartPosition.Manual;
            dialog.Location = new Point(-20000, -20000);
            dialog.Show();
            FlowLayoutPanel answers = Field<FlowLayoutPanel>(dialog, "_answers");
            Assert.False(answers.Enabled);
            Assert.Equal(6, answers.Controls.Count);
            Assert.All(answers.Controls.OfType<RadioButton>(), answer =>
            {
                Assert.True(answer.Height >= 58);
                Assert.True(answer.Font.SizeInPoints >= 20);
            });
            for (int index = 0; index < 30; index++) { Field<Button>(dialog, "_less").PerformClick(); }
            Assert.Equal("Pause 1 min.", Field<Button>(dialog, "_pause").Text);
            for (int index = 0; index < 30; index++) { Field<Button>(dialog, "_more").PerformClick(); }
            Assert.Equal("Pause 20 min.", Field<Button>(dialog, "_pause").Text);
            Assert.False(Field<Button>(dialog, "_more").Enabled);
            DateTimeOffset start = DateTimeOffset.UtcNow.AddSeconds(-32);
            for (int tick = 0; tick <= 128; tick++)
            {
                DateTimeOffset time = start.AddSeconds(tick / 4d);
                session.Observe(new(time, time, tick, 0, "test-vehicle", 1, LearningSource.Vehicle, Speed: 5 + tick / 4d));
                if (tick == 120) { dialog.Poll(time); }
            }

            dialog.Poll(start.AddSeconds(32));
            Assert.True(answers.Enabled);
            Assert.All(answers.Controls.OfType<RadioButton>(), answer => Assert.False(answer.Checked));
            dialog.PerformLayout();
            Assert.Equal(points, dialog.Font.SizeInPoints);
            foreach (Control control in new Control[] { Field<CheckBox>(dialog, "_undo"),
                Field<Button>(dialog, "_pause"), Field<Button>(dialog, "_end") })
            {
                Assert.True(control.Parent!.ClientRectangle.Contains(control.Bounds),
                    $"{control.Text} extends outside its footer: {control.Bounds}, {control.Parent.ClientRectangle}");
            }

            bool inspected = false;
            using System.Windows.Forms.Timer closer = new() { Interval = 30 };
            closer.Tick += (_, _) =>
            {
                Form? details = Application.OpenForms.Cast<Form>().FirstOrDefault(form =>
                    form.Text == "Frozen guess evidence - question dismissed");
                if (details is null) { return; }
                inspected = true;
                closer.Stop();
                Assert.False(answers.Enabled);
                Assert.Equal(4, details.Controls.OfType<DataGridView>().Single().Rows.Count);
                details.Close();
            };
            closer.Start();
            Field<StatusStrip>(dialog, "_sources").Items[0].PerformClick();
            Assert.True(inspected);
            Assert.Null(Field<RoadLearningQuestion>(dialog, "_question"));
            Assert.False(journal.CanUndo);
            dialog.Close();
            Assert.False(session.Active);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task LearningQuizPersistsTapUndoPauseAndEndWithoutChangingAcquisition()
        => await RunStaAsync(async () =>
        {
            string root = Path.Combine(Path.GetTempPath(), $"sensored-quiz-ui-{Guid.NewGuid():N}");
            RoadLearningSession session = new("test-profile");
            try
            {
                using RoadLearningJournal journal = new(root, session.Id);
                List<Exception> errors = [];
                using RoadLearningQuizDialog dialog = new(session, journal, () => true, () => null,
                    (_, exception) => errors.Add(exception), SystemFonts.MessageBoxFont!);
                dialog.StartPosition = FormStartPosition.Manual;
                dialog.Location = new Point(-20000, -20000);
                dialog.Show();
                DateTimeOffset start = DateTimeOffset.UtcNow.AddSeconds(-32);
                for (int tick = 0; tick <= 128; tick++)
                {
                    DateTimeOffset time = start.AddSeconds(tick / 4d);
                    session.Observe(new(time, time, tick, 0, "test-vehicle", 1, LearningSource.Vehicle, Speed: 5 + tick / 4d));
                    if (tick == 120) { dialog.Poll(time); }
                }

                dialog.Poll(start.AddSeconds(32));
                RadioButton answer = Field<FlowLayoutPanel>(dialog, "_answers").Controls.OfType<RadioButton>()
                    .Single(item => Equals(item.Tag, 3));
                answer.PerformClick();
                DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(5);
                while (!journal.CanUndo && errors.Count == 0 && DateTimeOffset.UtcNow < deadline)
                {
                    await Task.Delay(20, TestContext.Current.CancellationToken);
                }

                Assert.Empty(errors);
                Assert.True(journal.CanUndo);
                CheckBox undo = Field<CheckBox>(dialog, "_undo");
                Assert.True(undo.Enabled);
                undo.Checked = true;
                while (journal.CanUndo && errors.Count == 0 && DateTimeOffset.UtcNow < deadline)
                {
                    await Task.Delay(20, TestContext.Current.CancellationToken);
                }

                Assert.Empty(errors);
                Assert.False(journal.CanUndo);
                string path = Directory.GetFiles(Path.Combine(root, session.Id.ToString("N")), "*.json").Single();
                using JsonDocument saved = JsonDocument.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken));
                Assert.Equal(3, saved.RootElement.GetProperty("SelectedLevel").GetInt32());
                Assert.True(saved.RootElement.GetProperty("Retracted").GetBoolean());
                Field<Button>(dialog, "_pause").PerformClick();
                Assert.True(session.PausedUntil > DateTimeOffset.UtcNow.AddMinutes(4));
                Assert.Null(Field<RoadLearningQuestion>(dialog, "_question"));
                Field<Button>(dialog, "_end").PerformClick();
                Assert.False(session.Active);
            }
            finally
            {
                if (Directory.Exists(root)) { Directory.Delete(root, recursive: true); }
            }
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SensoredDrivingTasksRecordIntentBoundariesWithoutChangingAcquisitionOrFusion(bool optIn)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-driving-tasks-{Guid.NewGuid():N}");
        try
        {
            await RunStaAsync(async () =>
            {
                ManualImuSource imu = new();
                await using SensorService service = new(new DelegateSensorSourceFactory("task-guide",
                    _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
                using Sensored.MainForm form = new(service);
                SetField(form, "_options", DemoOptions.Default with { RecordingRootFolder = folder });
                DrivingTaskSelection first = new(DrivingCaptureTask.All[2], "northbound; slope \"unknown\"\nno independent reference");
                DrivingTaskSelection second = new(DrivingCaptureTask.All[4], "left/right as encountered");
                if (optIn)
                {
                    Invoke(form, "ApplyDrivingTaskSelection", first);
                }

                try
                {
                    await InvokeTask(form, "StartSensorSessionAsync", null!, Type.Missing);
                    await InvokeTask(form, "StartRecordingAsync");
                    ISensorRecordingSession journal = Field<ISensorRecordingSession>(form, "_recording");
                    SensorV2Recording pair = Field<SensorV2Recording>(form, "_v2Recording");
                    SensorFusionSession fusion = Field<SensorFusionSession>(form, "_fusionSession");
                    IFusionEngine engine = fusion.Engine!;
                    imu.Emit();
                    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
                    while (fusion.Latest is null || pair.LastInputSequence < fusion.Latest.Sequence)
                    {
                        await Task.Delay(10, timeout.Token);
                    }

                    if (optIn)
                    {
                        SetField(form, "_paused", true);
                        Invoke(form, "ApplyDrivingTaskSelection", second);
                        Invoke(form, "ApplyDrivingTaskSelection", second);
                        Invoke(form, "ApplyDrivingTaskSelection", [null!]);
                        Invoke(form, "ApplyDrivingTaskSelection", first);
                    }

                    Invoke(form, "HandleRecordingMarkerShortcut", Keys.Control | Keys.M);
                    Assert.Same(engine, fusion.Engine);
                    Assert.Equal(1, service.ActiveSessionCount);
                    await InvokeTask(form, "StopRecordingAsync");
                    Assert.True(pair.IsComplete);
                    string markerPath = Assert.Single(Directory.GetFiles(journal.FolderPath, "*.MarkersV2.jsonl"));
                    SensorRecordingMarker[] markers = (await File.ReadAllLinesAsync(markerPath,
                            TestContext.Current.CancellationToken))
                        .Select(line => JsonSerializer.Deserialize<SensorRecordingMarker>(line)!)
                        .ToArray();
                    Assert.Single(markers, marker => marker.Label.StartsWith("Marker ", StringComparison.Ordinal));
                    SensorRecordingMarker[] tasks = markers
                        .Where(marker => marker.Label.StartsWith("driving-task-v1", StringComparison.Ordinal))
                        .ToArray();
                    Assert.Equal(optIn ? 6 : 0, tasks.Length);
                    if (optIn)
                    {
                        Assert.Contains("intent-start (recording started)", tasks[0].Label);
                        Assert.Contains(first.Notes, tasks[0].Label);
                        Assert.Contains("intent-end (selection changed)", tasks[1].Label);
                        Assert.Contains(second.Task.Id, tasks[2].Label);
                        Assert.Contains("intent-end (selection changed)", tasks[3].Label);
                        Assert.Contains("not task completion", tasks[^1].Label);
                        Assert.All(tasks, marker =>
                        {
                            Assert.Contains("not sensor-verified", marker.Label);
                            Assert.Equal(fusion.Epoch, marker.Epoch);
                            Assert.True(marker.InputSequence <= journal.Coverage!.LastSequence);
                        });
                        Assert.Same(first, Field<DrivingTaskSelection>(form, "_selectedDrivingTask"));
                    }

                    Assert.Equal(1, service.ActiveSessionCount);
                    Assert.Same(engine, fusion.Engine);
                }
                finally
                {
                    await InvokeTask(form, "StopSensorSessionAsync");
                }
            });
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(SourceSpeedKind.Gps)]
    [InlineData(SourceSpeedKind.Obd)]
    public async Task SensoredDifferentiatesOriginalSpeedTimesNotRepeatedDashboardFrames(SourceSpeedKind source)
        => await RunStaAsync(async () =>
        {
            await using SensorService service = CreateService();
            using Sensored.MainForm form = new(service);
            SetField(form, "_obdReplayActive", source == SourceSpeedKind.Obd);
            DateTimeOffset baseline = DateTimeOffset.UtcNow.AddSeconds(-1);
            DateTimeOffset imuTime = DateTimeOffset.UtcNow;
            long sequence = 0;
            for (int measurement = 0; measurement < 4; measurement++)
            {
                QualifiedSourceSpeed reading = new(source, 10 + measurement * 0.1,
                    baseline.AddMilliseconds(measurement * 50), "test-source", 1, measurement, true, "test");
                for (int presentation = 0; presentation < 5; presentation++)
                {
                    FusionEstimate estimate = new()
                    {
                        Sequence = ++sequence,
                        Timestamp = imuTime,
                        Speed = reading.MetersPerSecond,
                        HasAuthoritativeSpeed = source == SourceSpeedKind.Obd,
                        Support = new() { Speed = new(FusionQuantityState.Supported, "original-source") },
                        SpeedSelection = new("test", source, source, reading, false, "original-source")
                    };
                    SetField(form, "_lastFusionEstimate", estimate);
                    Invoke(form, "ApplyLatestFusionEstimate", false);
                }
            }

            D2DSpeedometer speed = Field<D2DSpeedometer>(form, "_speedometer");
            Assert.Equal(2 / 9.80665, speed.ForwardAccelerationG, 8);
            Assert.False(Field<D2DAccelerationGauge>(form, "_accelerationGauge").IsAccelerationValid);
        });

    [Fact]
    public async Task PatternFusionShowsTheConditionalTiltBoundInsteadOfAnUnquantifiedZeroConfidence()
        => await RunStaAsync(() =>
        {
            using Sensored.TabViews.TelemetryTabView view = new();
            IFusionEngine engine = FusionEngineFactory.CreateDefault(Mount());
            FusionEstimate? estimate = null;
            for (int index = 0; index <= 110; index++)
            {
                if (index % 10 == 0)
                {
                    engine.Process(ReplayGps(index));
                }

                estimate = engine.Process(ReplayImu(index));
            }

            Assert.True(estimate!.IsAttitudeSupported);
            view.ApplyFusion(estimate, VehicleGaugeReadings.FromEstimate(estimate));
            string diagnostic = Field<Label>(view, "_fusionTrustDiagnostic").Text;
            Assert.Contains("conditional tilt bound", diagnostic);
            Assert.DoesNotContain("confidence 0", diagnostic);
            FusionReadinessSnapshot readiness = FusionReadinessSnapshot.Create(estimate, Mount(), true);
            Assert.Contains("independent of speed", readiness.Details);
            Assert.Contains("not a learning score", readiness.Details);

            return Task.CompletedTask;
        });

    [Fact]
    public async Task RawImuFallbackPreservesTheCurrentAttitudeSupportReason()
        => await RunStaAsync(() =>
        {
            using Sensored.TabViews.TelemetryTabView view = new();
            IFusionEngine engine = FusionEngineFactory.CreateDefault();
            SensorEnvelope input = ReplayImu(0);
            FusionEstimate estimate = engine.Process(input)!;
            Assert.False(estimate.IsAttitudeSupported);
            view.ApplyFusion(estimate, VehicleGaugeReadings.FromEstimate(estimate));
            Label diagnostic = Field<Label>(view, "_fusionTrustDiagnostic");
            string expected = diagnostic.Text;
            Assert.Contains(FusionReadinessSnapshot.ExplainReason(estimate.Support!.BodyAttitude.Reason), expected);
            view.ApplyRawImuFallback(input.Imu!.Value, estimate);
            Assert.Equal(expected, diagnostic.Text);

            return Task.CompletedTask;
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RecordedConfigurationsRestoreExactOutputsAcrossEpochsAndCompanionWarmup(bool seekWithCompanion)
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-recorded-epochs-{Guid.NewGuid():N}");
        try
        {
            IFusionEngine first = FusionEngineFactory.CreateDefault(Mount());
            IFusionEngine second = FusionEngineFactory.CreateDefault();
            WriteRecordedConfigurations(folder, (3, 0, first), (4, 400, second));
            RecordedFusionEngine replay = Assert.IsType<RecordedFusionEngine>(RecordedFusionEngine.Open(folder));
            Assert.Equal(Mount(), replay.VehicleMount);
            List<FusionEstimate> expected = [];
            await using ObdRecordingCompanion journal = new();
            for (int index = 0; index <= 350; index++)
            {
                foreach (SensorEnvelope input in index % 10 == 0
                    ? new[] { ReplayGps(index), ReplayImu(index) }
                    : new[] { ReplayImu(index) })
                {
                    ObdFusionState state = new(ObdTelemetrySnapshot.Disabled, null);
                    IFusionEngine original = index < 200 ? first : second;
                    state.Apply(original);
                    FusionEstimate? estimate = original.Process(input);
                    if (estimate is not null)
                    {
                        expected.Add(estimate);
                    }

                    if (seekWithCompanion)
                    {
                        journal.Publish(input, state);
                    }
                    else
                    {
                        replay.PrepareInput(input);
                        state.Apply(replay);
                        Assert.Equal(estimate, replay.Process(input));
                    }
                }
            }

            Assert.True(expected[190].IsAttitudeSupported);
            Assert.True(expected[190].IsAccelerationSupported);
            Assert.False(expected[350].IsAttitudeSupported);
            if (seekWithCompanion)
            {
                await journal.ExportAsync(folder, TestContext.Current.CancellationToken);
                using ObdReplayCursor cursor = ObdReplayCursor.Open(folder)!;
                List<FusionEstimate> warmup = [];
                ObdRecordedInput target = cursor.Resolve(ReplayImu(350), ReplayImu(350).Sequence, replay,
                    warmup.Add, replay.PrepareInput);
                target.State.Apply(replay);
                Assert.Equal(expected.Take(350), warmup);
                Assert.Equal(expected[350], replay.Process(target.Input));
            }

            Assert.Null(replay.VehicleMount);
            replay.Reset();
            Assert.Equal(Mount(), replay.VehicleMount);
            replay.Process(ReplayGps(0));
            FusionEstimate restarted = replay.Process(ReplayImu(0))!;
            Assert.Equal(expected[0], restarted);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SensoredReplayLoadsRecordedMountOrUsesMountFreeFallbackAndCanReseek(bool hasProvenance)
        => await RunStaAsync(async () =>
        {
            string folder = Path.Combine(Path.GetTempPath(), $"warp-replay-mount-{Guid.NewGuid():N}");
            Directory.CreateDirectory(folder);
            try
            {
                if (hasProvenance)
                {
                    WriteRecordedConfigurations(folder, (0, 0, FusionEngineFactory.CreateDefault(Mount())));
                }

                await using SensorService service = CreateService();
                await using ISensorServiceSessionLease lease = await service.AcquireSessionAsync(
                    new(), TestContext.Current.CancellationToken);
                using Sensored.MainForm form = new(service);
                SetField(form, "_options", DemoOptions.Default with { VehicleMount = Mount() });
                SetField(form, "_fusionEngine", FusionEngineFactory.CreateDefault(Mount()));
                Type continuity = typeof(Sensored.MainForm).GetNestedType("FusionContinuity", BindingFlags.NonPublic)!;
                IFusionEngine? previous = null;
                for (int attempt = 0; attempt < 2; attempt++)
                {
                    await InvokeTask(form, "StartFusionInputAsync", Path.Combine(folder, "part.csv"),
                        Enum.Parse(continuity, "Reseek"));
                    SensorFusionSession session = Field<SensorFusionSession>(form, "_fusionSession");
                    IFusionEngine engine = session.Engine!;
                    try
                    {
                        if (hasProvenance)
                        {
                            Assert.IsType<RecordedFusionEngine>(engine);
                            Assert.NotSame(previous, engine);
                        }
                        else
                        {
                            Assert.Equal(JsonValueKind.Null, Configuration(engine).GetProperty("VehicleMount").ValueKind);
                        }

                        previous = engine;
                    }
                    finally
                    {
                        await Field<ISensorSubscription>(form, "_fusionSubscription").DisposeAsync();
                        await session.FinishInputAsync();
                        await InvokeTask(form, "CompleteFusionAsync", false);
                        Directory.Delete(Path.GetDirectoryName(session.TracePath!)!, recursive: true);
                    }
                }
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        });

    [Fact]
    public void RecordedConfigurationRejectsCorruptProvenanceInsteadOfUsingLiveSettings()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-corrupt-configuration-{Guid.NewGuid():N}");
        try
        {
            WriteRecordedConfigurations(folder, (0, 0, FusionEngineFactory.CreateDefault(Mount())));
            string path = Path.Combine(folder, "v2.manifest.json");
            File.WriteAllText(path, File.ReadAllText(path).Replace("measured-fixture", "tampered-fixture", StringComparison.Ordinal));
            Assert.Throws<InvalidDataException>(() => RecordedFusionEngine.Open(folder));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    private static void WriteRecordedConfigurations(string folder, params (int Epoch, long First, IFusionEngine Engine)[] epochs)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "v2.manifest.json"), JsonSerializer.Serialize(new
        {
            Schema = "warp-sensor-v2-pair",
            Version = 2,
            EngineConfigurations = epochs.Select(epoch =>
            {
                string json = Configuration(epoch.Engine).GetRawText();

                return new
                {
                    epoch.Epoch,
                    Engine = epoch.Engine.GetType().FullName,
                    FirstObservedInputSequence = epoch.First,
                    ConfigurationJson = json,
                    ConfigurationHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(json)))
                };
            }).ToArray()
        }));
    }

    private static SensorEnvelope ReplayImu(int index)
    {
        DateTimeOffset timestamp = DateTimeOffset.UnixEpoch.AddMilliseconds(index * 10);

        return new()
        {
            Sequence = index * 2 + 1,
            Kind = SensorDataKind.Imu,
            SourceId = "recorded-imu",
            SourceGeneration = 1,
            SourceTimestamp = timestamp,
            ArrivalTimestamp = timestamp,
            Imu = new(new Vec3(0, 0, 1), Vec3.Zero, true, double.NaN, 0, 12, 34, double.NaN, default)
        };
    }

    private static SensorEnvelope ReplayGps(int index)
    {
        DateTimeOffset timestamp = ReplayImu(index).SourceTimestamp;

        return new()
        {
            Sequence = index * 2,
            Kind = SensorDataKind.Geolocation,
            SourceId = "recorded-gps",
            SourceGeneration = 1,
            SourceTimestamp = timestamp,
            ArrivalTimestamp = timestamp,
            Geolocation = new(47, 8, 400, 2.5, 5, 0, true, 0, true,
                GpsPositionSource.Satellite, false, timestamp, timestamp,
                double.NaN, 1, double.NaN, 1, 10, 1)
        };
    }

    [Fact]
    public void SensoredMountPersistsAcrossRestartAndNeverLeaksIntoUnrelatedReplay()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-mount-settings-{Guid.NewGuid():N}");
        string path = Path.Combine(folder, "settings.json");
        try
        {
            DemoOptions options = DemoOptions.Default with { VehicleMount = Mount() };
            using (WinFormsUserSettingsService settings = Settings(path))
            {
                DemoSettings.Save(settings, options);
                settings.Flush();
            }

            using (WinFormsUserSettingsService settings = Settings(path))
            {
                DemoOptions restored = DemoSettings.Load(settings, DemoOptions.Default);
                Assert.Equal(options.VehicleMount, restored.VehicleMount);
                JsonElement live = Configuration(restored.CreateFusionEngine(isReplay: false));
                JsonElement replay = Configuration(restored.CreateFusionEngine(isReplay: true));
                Assert.Equal("measured-fixture", live.GetProperty("VehicleMount").GetProperty("Id").GetString());
                Assert.True(live.GetProperty("AllowGpsFallbackForSelectedObd").GetBoolean());
                Assert.Equal(JsonValueKind.Null, replay.GetProperty("VehicleMount").ValueKind);
                DemoSettings.Save(settings, restored with { VehicleMount = null });
                settings.Flush();
            }

            using WinFormsUserSettingsService cleared = Settings(path);
            Assert.Null(DemoSettings.Load(cleared, DemoOptions.Default).VehicleMount);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FusionGaugesComposesAndRecordsTheSelectedMountWithoutChangingSourcePolicy()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"warp-mount-recording-{Guid.NewGuid():N}");
        try
        {
            FusionGaugesOptions options = new() { VehicleMount = Mount(), RecordingFolder = folder };
            FusionGaugesOptions restored = JsonSerializer.Deserialize<FusionGaugesOptions>(
                JsonSerializer.Serialize(options))!;
            Assert.Equal(options.VehicleMount, restored.VehicleMount);
            ManualImuSource imu = new();
            await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("manual-imu",
                _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
            await hub.StartAsync(restored);
            SensorFusionSession session = Field<SensorFusionSession>(hub, "_fusion");
            JsonElement configuration = Configuration(session.Engine!);
            Assert.Equal("measured-fixture", configuration.GetProperty("VehicleMount").GetProperty("Id").GetString());
            Assert.False(configuration.GetProperty("AllowGpsFallbackForSelectedObd").GetBoolean());
            await hub.StartRecordingAsync(restored);
            imu.Emit();
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
            while (session.Latest is null)
            {
                await Task.Delay(10, timeout.Token);
            }

            Assert.False(session.Latest.IsAttitudeSupported);
            await hub.StopRecordingAsync();
            string manifestPath = Path.Combine(hub.LastRecording!.FolderPath, "v2.manifest.json");
            using JsonDocument manifest = JsonDocument.Parse(await File.ReadAllTextAsync(
                manifestPath, TestContext.Current.CancellationToken));
            Assert.True(manifest.RootElement.GetProperty("Complete").GetBoolean());
            JsonElement epoch = Assert.Single(manifest.RootElement.GetProperty("EngineConfigurations").EnumerateArray());
            using JsonDocument recorded = JsonDocument.Parse(epoch.GetProperty("ConfigurationJson").GetString()!);
            JsonElement mount = recorded.RootElement.GetProperty("VehicleMount");
            Assert.Equal("measured-fixture", mount.GetProperty("Id").GetString());
            Assert.Equal(1, mount.GetProperty("Version").GetInt32());
            Assert.Contains("independent fixture", mount.GetProperty("Provenance").GetString());
            Assert.True(JsonElement.DeepEquals(recorded.RootElement,
                Configuration(FusionEngineFactory.CreateFromRecordedConfiguration(recorded.RootElement))));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FusionSetupDoesNotStartOrRestartAcquisitionAndIsReadOnlyDuringADrive(bool running)
        => await RunStaAsync(async () =>
        {
            await using SensorHub hub = CreateHub();
            if (running)
            {
                await hub.StartAsync(new());
            }

            using WinFormsFusionGauges.WinFormsApp.MainForm form = new(hub);
            bool inspected = false;
            bool saveEnabled = false;
            string readiness = string.Empty;
            using System.Windows.Forms.Timer closer = new() { Interval = 100 };
            closer.Tick += (_, _) =>
            {
                VehicleFusionSetupDialog? dialog = Application.OpenForms.OfType<VehicleFusionSetupDialog>().FirstOrDefault();
                if (dialog is null)
                {
                    return;
                }

                saveEnabled = Field<Button>(dialog, "_saveButton").Enabled;
                readiness = Field<TextBox>(dialog, "_readiness").Text;
                inspected = true;
                closer.Stop();
                dialog.DialogResult = DialogResult.Cancel;
            };
            closer.Start();
            Field<ToolStripButton>(form, "_calibrateButton").PerformClick();
            Assert.True(inspected);
            Assert.Equal(!running, saveEnabled);
            Assert.Contains("QUANTITY SUPPORT", readiness);
            Assert.Equal(running, hub.IsRunning);
        });

    private static WinFormsUserSettingsService Settings(string path)
        => new(new()
        {
            SettingsFileProvider = () => new FileInfo(path),
            SaveMode = UserSettingsSaveMode.ExplicitFlush
        });

    private static SensorService CreateService()
        => new(new DelegateSensorSourceFactory("no-hardware", _ =>
            new SensorSourceSet(NoSensorImuSource.Instance, NoSensorGpsSource.Instance)));

    private static SensorHub CreateHub()
        => new(_ => new DelegateSensorSourceFactory("no-hardware", _ =>
            new SensorSourceSet(NoSensorImuSource.Instance, NoSensorGpsSource.Instance)));

    private static JsonElement Configuration(IFusionEngine engine)
        => JsonSerializer.SerializeToElement(((IFusionConfigurationSource)engine).Configuration);

    private static VehicleMountReference Mount()
        => new("measured-fixture", 1, new Vec3(1, 0, 0), new Vec3(0, 0, 1),
            "independent fixture; measured physical axes; uncertainty documented by fixture");

    private static T Field<T>(object target, string name)
        => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static void SetField(object target, string name, object value)
        => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static void Invoke(object target, string name, params object[] args)
        => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args);

    private static Task InvokeTask(object target, string name, params object[] args)
        => (Task)target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, args)!;

    private static Task RunStaAsync(Func<Task> action)
    {
        TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Thread thread = new(() =>
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException, threadScope: true);
            using Form dispatcher = new();
            dispatcher.Shown += async (_, _) =>
            {
                try
                {
                    await action();
                    completion.SetResult();
                }
                catch (Exception exception)
                {
                    completion.SetException(exception);
                }
                finally
                {
                    dispatcher.Close();
                }
            };
            Application.Run(dispatcher);
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        return completion.Task;
    }

    /// <summary>Supplies discovery observations and awaited cleanup without a Bluetooth radio.</summary>
    private sealed class OptionsBluetoothScanner : IBluetoothDeviceScanner
    {
        internal bool WaitForCancellation { get; init; }
        internal bool Fail { get; init; }
        internal bool WatchEnded { get; private set; }
        internal BluetoothScanOptions? Options { get; private set; }

        public Task<IReadOnlyList<BluetoothDeviceInfo>> ScanAsync(BluetoothScanOptions options,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public async IAsyncEnumerable<BluetoothDeviceInfo> WatchAsync(BluetoothScanOptions options,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Options = options;
            try
            {
                if (Fail)
                {
                    throw new UnauthorizedAccessException("access denied");
                }

                BluetoothDeviceInfo device = new("00:11:22:33:44:55", "Same advertised name", "00:11:22:33:44:55",
                    BluetoothDeviceKind.LowEnergy, false, -60, []);
                yield return device;
                yield return device with { Id = "richer-windows-id", Rssi = -55 };
                yield return device with { Id = "other", Address = "00:11:22:33:44:66" };
                if (WaitForCancellation)
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
            }
            finally
            {
                await Task.Delay(20);
                WatchEnded = true;
            }
        }
    }

    /// <summary>Supplies deterministic speed and freshness evidence without a physical receiver.</summary>
    private sealed class ManualGpsSource : IGpsSource
    {
        public event EventHandler<GpsSample>? FixReady;
        public event EventHandler? HealthChanged { add { } remove { } }
        public GpsAccessStatus AccessStatus => GpsAccessStatus.Allowed;
        public GpsAcquisitionStatus AcquisitionStatus => GpsAcquisitionStatus.Ready;
        public GpsPositionSource? LastDroppedFixSource => null;
        public DateTimeOffset? LastDroppedFixUtc => null;
        public GpsStreamStatistics Statistics => new(1, 1, 0, 0, null);
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        internal void Emit(double speed, DateTimeOffset timestamp)
            => FixReady?.Invoke(this, new(speed, 90, true, 2, timestamp,
                51, 7, GpsPositionSource.Satellite, fixQuality: 1));
    }

    /// <summary>Publishes only the explicit IMU input needed to verify recorded configuration provenance.</summary>
    private sealed class ManualImuSource : IImuSource
    {
        public event EventHandler<ImuSample>? SampleReady;
        public bool IsAccelerometerAvailable => true;
        public bool IsGyroscopeAvailable => true;
        public DateTimeOffset? LastCallbackReceivedUtc { get; private set; }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Emit()
        {
            DateTimeOffset time = DateTimeOffset.UtcNow;
            LastCallbackReceivedUtc = time;
            SampleReady?.Invoke(this, new(new Vec3(0, 0, 1), Vec3.Zero, time));
        }
    }
}
