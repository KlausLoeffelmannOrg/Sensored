using System.Reflection;
using System.Text.Json;
using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;
using WarpToolkit.Sensors.Presentation;
using WarpToolkit.WinForms.Containers;
using WarpToolkit.WinForms.DirectX.Controls;
using WarpToolkit.WinForms.Sensors.DirectX;
using WarpToolkit.WinForms.ToolStrips;
using WinFormsFusionGauges.WinFormsApp;

[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace FusionGauges.Tests;

/// <summary>Exercises the app with deterministic sources and actual Designer-created controls.</summary>
public sealed class FusionGaugesTests
{
    [Fact]
    public async Task Receiver_only_recording_finalizes_all_inputs_and_mirrors_recording_manifest()
    {
        string folder = CreateFolder();
        string destination = Path.Combine(folder, "mirror");
        PreviewGpsSource gps = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("gps-without-imu",
            _ => new SensorSourceSet(NoSensorImuSource.Instance, gps)));
        List<string> shutdown = [];
        hub.ShutdownProgress += shutdown.Add;

        try
        {
            FusionGaugesOptions options = new()
            {
                RecordingFolder = folder,
                MirrorEnabled = true,
                MirrorFolder = destination
            };
            await hub.StartAsync(options);
            await hub.StartRecordingAsync(options);
            for (int index = 0; index < 600; index++)
            {
                gps.Emit();
            }

            await WaitAsync(() => hub.Core.GpsHistory.Count == 600);
            await hub.StopRecordingAsync();
            Assert.Null(hub.Fault);
            Assert.Equal(0, hub.LastRecording!.RowsDropped);
            string recording = hub.LastRecording.FolderPath;
            using JsonDocument manifest = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(recording, "v2.manifest.json"),
                    TestContext.Current.CancellationToken));
            Assert.True(manifest.RootElement.GetProperty("Complete").GetBoolean());
            Assert.Equal(0, manifest.RootElement.GetProperty("FusionOutputs").GetInt32());
            Assert.True(manifest.RootElement.GetProperty("CsvContextTruncations").GetInt64() > 0);
            Assert.Equal(hub.LastRecording.RowsWritten,
                manifest.RootElement.GetProperty("ProcessedInputs").GetInt64());
            await WaitAsync(() => hub.CaptureMirrorStatus() is { Pending: 0, Published: >= 2 });
            string zip = Path.Combine(destination, Path.GetFileName(recording), "companions-and-manifests.zip");
            using (System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zip))
            {
                foreach (string name in new[]
                {
                    "recording.manifest.json", "companions.manifest.json", "v2.manifest.json",
                    "replay-inputs.compact.json", "replay-controls.jsonl", "replay-inputs.manifest.json"
                })
                {
                    Assert.NotNull(archive.GetEntry(name));
                }
            }

            await hub.DisposeAsync();
            Assert.Contains(shutdown, message => message.Contains("Fusion drained:", StringComparison.Ordinal));
            Assert.Contains(shutdown, message => message.Contains("ZIP journal:", StringComparison.Ordinal));
            Assert.Contains("All owned resources released; shutdown complete.", shutdown);
            Assert.DoesNotContain(shutdown, message => message.StartsWith("ERROR:", StringComparison.Ordinal));
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndGps, null)]
    [InlineData(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndGps, 200)]
    [InlineData(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.GpsOnly, null)]
    [InlineData(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.GpsOnly, 200)]
    public async Task WindowsGpsIntervalEditsPreserveSerialReceiverSettings(
        WarpToolkit.Windows.Sensors.WindowsSensorSourceKind sourceKind, int? serialInterval)
        => await RunStaAsync(() =>
        {
            using SourceOptionsView view = new();
            FusionGaugesOptions original = new()
            {
                SourceKind = sourceKind,
                GpsReportIntervalMilliseconds = 1000,
                TargetFixIntervalMilliseconds = serialInterval,
                GnssProfile = SerialGnssProfile.Passive
            };
            view.LoadOptions(original);
            var input = Field<NumericUpDown>(view, "_interval");
            Assert.True(input.Enabled);
            Assert.Equal(1000m, input.Value);
            input.Value = 5000;
            FusionGaugesOptions updated = view.ReadOptions(original);
            updated.Validate();
            Assert.Equal(sourceKind, updated.SourceKind);
            Assert.Equal(5000u, updated.GpsReportIntervalMilliseconds);
            Assert.Equal(original.GnssProfile, updated.GnssProfile);
            Assert.Equal(serialInterval, updated.TargetFixIntervalMilliseconds);
        });

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    public async Task SerialRateOptionsRequestExactLibraryRateWithoutChangingConstellations(int interval)
        => await RunStaAsync(() =>
        {
            using SourceOptionsView view = new();
            FusionGaugesOptions original = new()
            {
                SourceKind = WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps,
                GpsReportIntervalMilliseconds = 1000
            };
            view.LoadOptions(original);
            var input = Field<NumericUpDown>(view, "_interval");
            Assert.True(input.Enabled);
            input.Value = interval;
            Field<CheckBox>(view, "_authorize").Checked = true;
            FusionGaugesOptions updated = view.ReadOptions(original);
            Assert.Equal(SerialGnssProfile.Current, updated.GnssProfile);
            Assert.Equal(interval, updated.TargetFixIntervalMilliseconds);
            Assert.Equal(interval, updated.CreateSourceOptions().SerialGpsOptions.TargetFixIntervalMilliseconds);
            Assert.False(updated.AllowReceiverCommandsThisSession);
        });

    [Fact]
    public async Task SharedReceiverConsentDoesNotPromptForPassiveOrAlreadyAuthorizedOptions()
        => await RunStaAsync(() =>
        {
            using Form owner = new();
            Assert.True(GpsReceiverCommandConsent.Confirm(owner, new SerialNmeaGpsSourceOptions()));
            Assert.True(GpsReceiverCommandConsent.Confirm(owner, new SerialNmeaGpsSourceOptions
            {
                GnssProfile = SerialGnssProfile.Current,
                TargetFixIntervalMilliseconds = 100,
                AllowReceiverCommandsThisSession = true
            }));
        });

    [Fact]
    public void Display_options_keep_shared_defaults_and_validate_precision()
    {
        FusionGaugesOptions legacy = JsonSerializer.Deserialize<FusionGaugesOptions>("{}")!;
        Assert.Equal(SpeedometerSettings.Default, legacy.Speedometer);
        Assert.Equal(2, legacy.AccelerationDigits);
        FusionGaugesOptions changed = legacy with
        {
            AccelerationDigits = 5,
            Speedometer = legacy.Speedometer with
            {
                Inner = legacy.Speedometer.Inner with { Color = SpeedometerLedColor.Green, CaptionPadding = 9 }
            }
        };
        FusionGaugesOptions restored = JsonSerializer.Deserialize<FusionGaugesOptions>(
            JsonSerializer.Serialize(changed))!;
        restored.Validate();
        Assert.Equal(changed.Speedometer, restored.Speedometer);
        Assert.Equal(5, restored.AccelerationDigits);
        Assert.Throws<ArgumentOutOfRangeException>(() => (legacy with { AccelerationDigits = -1 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (legacy with { AccelerationDigits = 6 }).Validate());
    }

    [Theory]
    [InlineData(9f, 800, 600)]
    [InlineData(12f, 1600, 1000)]
    public async Task Cockpit_positions_and_telemetry_fonts_remain_correct_at_different_sizes(
        float points, int width, int height)
        => await RunStaAsync(() =>
        {
            using Font font = new(SystemFonts.MessageBoxFont!.FontFamily, points);
            using Form form = new() { ClientSize = new Size(width, height), Font = font };
            using InstrumentsView view = new() { Dock = DockStyle.Fill };
            form.Controls.Add(view);
            form.Show();
            var layout = Field<TableLayoutPanel>(view, "_layout");
            Assert.Equal(new TableLayoutPanelCellPosition(0, 0), layout.GetPositionFromControl(Field<Control>(view, "_speed")));
            Assert.Equal(new TableLayoutPanelCellPosition(0, 1), layout.GetPositionFromControl(Field<Control>(view, "_acceleration")));
            Assert.Equal(new TableLayoutPanelCellPosition(2, 0), layout.GetPositionFromControl(Field<Control>(view, "_horizon")));
            Assert.Equal(new TableLayoutPanelCellPosition(2, 1), layout.GetPositionFromControl(Field<Control>(view, "_compass")));
            var telemetry = Field<TelemetryValuesView>(view, "_telemetry");
            Assert.Equal(2, layout.GetRowSpan(telemetry));
            Assert.Equal(1.2f, layout.RowStyles[0].Height / layout.RowStyles[1].Height, precision: 3);
            if (layout.Height > view.ClientSize.Height)
            {
                Assert.True(view.VerticalScroll.Visible);
                view.AutoScrollPosition = new Point(0, layout.Height);
                Assert.True(layout.Top < 0);
                view.AutoScrollPosition = Point.Empty;
            }
            Assert.True(telemetry.AutoScroll);
            var groups = Field<TableLayoutPanel>(telemetry, "_groups");
            Assert.Equal(new[] { "SPEED", "GPS", "ACCELERATION (Sensor)", "INCLINOMETER (Sensor)" },
                Enumerable.Range(0, 4).Select(row => groups.GetControlFromPosition(0, row)!.Text));
            Assert.All(telemetry.Values, label =>
            {
                Assert.Equal(points, label.Font.SizeInPoints);
                Assert.True(label.Width >= label.PreferredSize.Width);
                Assert.True(label.Height >= label.PreferredSize.Height);
            });
            using Font larger = new(font.FontFamily, points + 2f);
            form.Font = larger;
            Assert.All(telemetry.Values, label => Assert.Equal(points + 2f, label.Font.SizeInPoints));
            var speed = Field<AnimatedValueLabel>(telemetry, "_fusionKmh");
            var trip = Field<AnimatedValueLabel>(telemetry, "_tripKm");
            Assert.Equal(300d, speed.MaximumValue);
            Assert.Equal(9999d, trip.MaximumValue);
            Assert.Equal("Yaw Accuracy: Unavailable", Field<Label>(telemetry, "_yawAccuracy").Text);
            using AppearanceOptionsView editor = new();
            FusionGaugesOptions original = new();
            editor.LoadOptions(original);
            var rings = Field<SpeedometerOptionsView>(editor, "_rings");
            rings.Settings = original.Speedometer with
            {
                Outer = original.Speedometer.Outer with { Caption = "custom" }
            };
            Assert.Equal(SpeedometerSettings.Default, original.Speedometer);
            Assert.Equal("custom", editor.ReadOptions(original).Speedometer.Outer.Caption);
        });

    [Fact]
    public async Task Telemetry_keeps_raw_and_accepted_receiver_channels_independent_from_fusion()
        => await RunStaAsync(() =>
        {
            using TelemetryValuesView view = new();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            SensorEnvelope imu = new()
            {
                Sequence = 1,
                SourceTimestamp = now,
                ArrivalTimestamp = now,
                Kind = SensorDataKind.Imu,
                Imu = new RawImuPayload(new Vec3(1, -2, 3), Vec3.Zero, false,
                    20, 1, 11, 22, 33, DeviceYawSource.None)
            };
            FusionEstimate estimate = new()
            {
                Sequence = 1,
                Timestamp = now,
                Speed = 100,
                Pitch = 99,
                Validity = FusionOutputValidity.Navigation
            };
            SensorObservationFrame frame = new(imu, null, null, new(imu, estimate, 0, "test"),
                1, 1, 0, 0, true, true, true);
            GpsDashboardSnapshot gps = new()
            {
                TripKilometers = 12345,
                Fix = new GeolocationPayload
                {
                    LatitudeDegrees = 51.1234567,
                    LongitudeDegrees = 7.1234567,
                    AltitudeMeters = 123,
                    HorizontalAccuracyMeters = 2,
                    PositionSource = GpsPositionSource.Satellite
                },
                Motion = new GpsMotionSnapshot
                {
                    ReceiverSpeedMetersPerSecond = 20,
                    ReceiverCourseDegrees = 42,
                    FourPoint = new GpsMotionWindow(4, TimeSpan.FromSeconds(3), 30, 10, 90)
                }
            };
            view.ApplyPrecision(5);
            view.Present(frame, gps, gpsFresh: true);
            Assert.Equal(360d, Field<AnimatedValueLabel>(view, "_fusionKmh").Value);
            Assert.Equal(72d, Field<AnimatedValueLabel>(view, "_receiverKmh").Value);
            Assert.Equal(36d, Field<AnimatedValueLabel>(view, "_calculatedKmh").Value);
            Assert.Equal(12345d, Field<AnimatedValueLabel>(view, "_tripKm").Value);
            Assert.Equal(12345d / 1.609344d, Field<AnimatedValueLabel>(view, "_tripMiles").Value);
            Assert.Equal(51.1234567d, Field<AnimatedValueLabel>(view, "_latitude").Value);
            Assert.Equal(42d, Field<AnimatedValueLabel>(view, "_heading").Value);
            Assert.Equal("Source: Satellite", Field<Label>(view, "_source").Text);
            Assert.Equal(1d, Field<AnimatedValueLabel>(view, "_x").Value);
            Assert.Equal(5, Field<AnimatedValueLabel>(view, "_x").DecimalPlaces);
            Assert.Equal(11d, Field<AnimatedValueLabel>(view, "_pitch").Value);
            view.Present(frame with { ImuFresh = false, FusionFresh = false }, gps, gpsFresh: false);
            Assert.True(double.IsNaN(Field<AnimatedValueLabel>(view, "_fusionKmh").Value));
            Assert.True(double.IsNaN(Field<AnimatedValueLabel>(view, "_receiverKmh").Value));
            Assert.True(double.IsNaN(Field<AnimatedValueLabel>(view, "_x").Value));
            Assert.Equal("Source: Unavailable", Field<Label>(view, "_source").Text);
            Assert.Equal(12345d, Field<AnimatedValueLabel>(view, "_tripKm").Value);
        });

    [Fact]
    public void Options_round_trip_source_and_storage_settings_but_not_receiver_authorization()
    {
        FusionGaugesOptions options = new()
        {
            SourceKind = WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps,
            SerialPortName = "COM42",
            GpsReportIntervalMilliseconds = 500,
            GnssProfile = SerialGnssProfile.Galileo,
            TargetFixIntervalMilliseconds = 200,
            AllowReceiverCommandsThisSession = true,
            IconSize = 48,
            FontSize = 12,
            NamePrefix = "test",
            AutomaticNaming = false,
            MirrorFolder = "C:\\sync",
            MirrorEnabled = true
        };
        FusionGaugesOptions restored = JsonSerializer.Deserialize<FusionGaugesOptions>(
            JsonSerializer.Serialize(options))!;
        Assert.Equal(options.SourceKind, restored.SourceKind);
        Assert.Equal("COM42", restored.SerialPortName);
        Assert.Equal(SerialGnssProfile.Galileo, restored.GnssProfile);
        Assert.Equal(500u, restored.GpsReportIntervalMilliseconds);
        Assert.Equal(200, restored.TargetFixIntervalMilliseconds);
        Assert.Equal(200, restored.CreateSourceOptions().SerialGpsOptions.TargetFixIntervalMilliseconds);
        Assert.Equal(48, restored.IconSize);
        Assert.False(restored.AllowReceiverCommandsThisSession);
        Assert.False(restored.AutomaticNaming);
        Assert.True(restored.MirrorEnabled);
    }

    [Fact]
    public async Task ToolbarTabButtonsReplaceHeadersAndFollowProgrammaticSelectionWithoutAcquisition()
        => await RunStaAsync(() =>
        {
            using MainForm form = new(CreateIdleHub());
            form.Show();
            FluentTabControl tabs = Field<FluentTabControl>(form, "_tabs");
            Assert.False(tabs.ShowTabHeaders);
            ToolStripButton instruments = Field<ToolStripButton>(form, "_instrumentsTabButton");
            ToolStripButton diagnostics = Field<ToolStripButton>(form, "_diagnosticsTabButton");
            ToolStripButton recording = Field<ToolStripButton>(form, "_recordingTabButton");
            Assert.True(instruments.Checked);
            diagnostics.PerformClick();
            Assert.Equal(1, tabs.CurrentTabIndex);
            Assert.True(diagnostics.Checked);
            Assert.False(instruments.Checked);
            recording.PerformClick();
            Assert.Equal(2, tabs.CurrentTabIndex);
            Assert.True(recording.Checked);
            tabs.CurrentTabIndex = 0;
            Assert.True(instruments.Checked);
            Assert.False(recording.Checked);
            Assert.False(Field<SensorHub>(form, "_hub").IsRunning);
        });

    [Fact]
    public async Task GpsShortcutsUseCarModeAndDeduplicateLastPortAndTargetWithoutStartingHardware()
        => await RunStaAsync(() =>
        {
            int sourceCreations = 0;
            SensorHub hub = new(_ =>
            {
                sourceCreations++;
                throw new InvalidOperationException("Editing configuration must not acquire sensors.");
            });
            using MainForm form = new(hub);
            form.Show();
            typeof(MainForm).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, new FusionGaugesOptions { SerialPortName = "COM8", TargetFixIntervalMilliseconds = 200 });
            typeof(MainForm).GetMethod("RefreshGpsConfiguration", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            var toolbar = Field<SymbolToolStrip>(form, "_toolbar");
            var root = Field<SymbolToolStripMenuItem>(form, "_gpsConfigurationButton");
            Assert.True(toolbar.CarMode);
            Assert.Equal("Speed source", root.Text);
            Assert.Equal(new[] { "OBD2", "GPS" }, root.DropDownItems.Cast<ToolStripItem>().Select(item => item.Text));
            var gpsRoot = root.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "GPS");
            var ports = gpsRoot.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "COM Port");
            var rates = gpsRoot.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "Target Res.");
            Assert.Equal("COM8 (last setting)", ports.DropDownItems[0].Text);
            Assert.Equal(9, ports.DropDownItems.Count);
            Assert.Single(ports.DropDownItems.Cast<ToolStripItem>(), item => item.Text!.StartsWith("COM8"));
            Assert.StartsWith("200", rates.DropDownItems[0].Text);
            Assert.Equal(5, rates.DropDownItems.Count);
            Assert.Contains(rates.DropDownItems.Cast<ToolStripItem>(), item => item.Text == "Dynamic");
            Assert.All(root.DropDownItems.OfType<SymbolToolStripMenuItem>(), item => Assert.NotNull(item.Symbol));
            ToolStripItem com3 = ports.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "COM3");
            ClickToolbarParent(toolbar, root);
            ClickToolbarParent(toolbar, gpsRoot);
            ClickToolbarParent(toolbar, ports);
            Application.DoEvents();
            Assert.Equal(3, toolbar.CarModeDepth);
            Assert.Equal("COM8", Field<FusionGaugesOptions>(form, "_options").SerialPortName);
            com3.PerformClick();
            Assert.Equal("COM3", Field<FusionGaugesOptions>(form, "_options").SerialPortName);
            Assert.Equal(0, toolbar.CarModeDepth);
            gpsRoot = root.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "GPS");
            var types = gpsRoot.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "GPS Type");
            ToolStripItem gps = types.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "GPS + QZSS");
            ClickToolbarParent(toolbar, root);
            ClickToolbarParent(toolbar, gpsRoot);
            ClickToolbarParent(toolbar, types);
            Application.DoEvents();
            Assert.Equal(3, toolbar.CarModeDepth);
            gps.PerformClick();
            Assert.Equal(SerialGnssProfile.Gps, Field<FusionGaugesOptions>(form, "_options").GnssProfile);
            Assert.Equal(0, toolbar.CarModeDepth);
            Assert.Equal(0, sourceCreations);
            Assert.False(hub.IsRunning);
            root.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "OBD2").PerformClick();
            Assert.True(hub.UseObdSpeed);
            Assert.True(Field<FusionGaugesOptions>(form, "_options").UseObdSpeed);
            Assert.Equal(WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndGps,
                Field<FusionGaugesOptions>(form, "_options").SourceKind);
            gpsRoot = root.DropDownItems.OfType<SymbolToolStripMenuItem>().Single(item => item.Text == "GPS");
            gpsRoot.DropDownItems.Cast<ToolStripItem>().Single(item => item.Text == "Use GPS").PerformClick();
            Assert.False(hub.UseObdSpeed);
        });

    [Fact]
    public async Task Acquisition_and_V2_pair_with_full_rate_journals_record_without_a_UI_timer()
    {
        string folder = CreateFolder();
        TestImuSource imu = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("deterministic",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));

        try
        {
            FusionGaugesOptions options = new() { RecordingFolder = folder };
            await hub.StartAsync(options);
            await hub.StartRecordingAsync(options);
            DateTimeOffset origin = DateTimeOffset.UtcNow.AddSeconds(-5);

            for (int index = 0; index < 200; index++)
            {
                imu.Emit(origin.AddMilliseconds(index * 10));
            }

            await WaitAsync(() => hub.Core.ImuHistory.Count == 200);
            hub.AddMarker("deterministic bump");
            await hub.StopRecordingAsync();
            Assert.True(hub.IsRunning);
            Assert.False(hub.IsRecording);
            SensorRecordingStatus recording = hub.LastRecording!;
            Assert.Null(recording.LastFaultMessage);
            Assert.Equal(0, recording.RowsDropped);
            Dictionary<SensorRecordingFormats, int> counts = [];

            foreach (SensorRecordingFormats format in new[] {
                SensorRecordingFormats.VerboseJsonLines, SensorRecordingFormats.CompactJson })
            {
                int count = 0;

                await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadFolderAsync(
                    recording.FolderPath, format, TestContext.Current.CancellationToken))
                {
                    if (row.Envelope.Kind == SensorDataKind.Imu)
                    {
                        count++;
                    }
                }

                counts.Add(format, count);
            }

            Assert.All(counts.Values, count => Assert.Equal(200, count));
            using JsonDocument manifest = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(recording.FolderPath, "companions.manifest.json"),
                    TestContext.Current.CancellationToken));
            Assert.True(manifest.RootElement.GetProperty("Complete").GetBoolean());
            Assert.Equal(200, File.ReadLines(Path.Combine(recording.FolderPath, "fusion-epoch-0000.jsonl")).Count());
            Assert.Single(File.ReadLines(Path.Combine(recording.FolderPath, "markers.jsonl")));
            string rawV2 = Assert.Single(Directory.GetFiles(recording.FolderPath, "*.RawV2.csv"));
            string fusedV2 = Assert.Single(Directory.GetFiles(recording.FolderPath, "*.FuseV2.csv"));
            Assert.Equal(SensorV2Schema.RawHeader, File.ReadLines(rawV2).First());
            Assert.Equal(201, File.ReadLines(fusedV2).Count());
            Assert.Single(Directory.GetFiles(recording.FolderPath, "*.MarkersV2.jsonl"));
            Assert.Empty(Directory.GetFiles(recording.FolderPath, "part-*.csv"));
            using JsonDocument v2 = JsonDocument.Parse(File.ReadAllText(Path.Combine(recording.FolderPath, "v2.manifest.json")));
            Assert.True(v2.RootElement.GetProperty("Complete").GetBoolean());
            Assert.Equal(10, v2.RootElement.GetProperty("RawSnapshotRate").GetInt32());
            await hub.StopAsync();
            Assert.False(hub.IsRunning);
            Assert.Equal(1, imu.StopCount);
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Recording_options_persist_rate_and_keep_mandatory_pair_and_journal(int rate)
        => await RunStaAsync(() =>
        {
            using RecordingOptionsView view = new();
            FusionGaugesOptions original = new();
            view.LoadOptions(original);
            Field<NumericUpDown>(view, "_rawRate").Value = rate;
            FusionGaugesOptions edited = view.ReadOptions(original);
            FusionGaugesOptions restored = JsonSerializer.Deserialize<FusionGaugesOptions>(JsonSerializer.Serialize(edited))!;
            Assert.Equal(rate, restored.RawSnapshotsPerSecond);
            Assert.True(Field<CheckBox>(view, "_compact").Checked);
            Assert.False(Field<CheckBox>(view, "_compact").Enabled);
            Assert.True(Field<CheckBox>(view, "_csv").Checked);
            Assert.False(Field<CheckBox>(view, "_csv").Enabled);
            Assert.True(restored.CreateRecordingRequest().Formats.HasFlag(SensorRecordingFormats.CompactJson));
            Assert.False(restored.CreateRecordingRequest().Formats.HasFlag(SensorRecordingFormats.SemicolonCsv));
            Assert.Throws<ArgumentOutOfRangeException>(() => (restored with { RawSnapshotsPerSecond = 0 }).Validate());
            Assert.Throws<ArgumentOutOfRangeException>(() => (restored with { RawSnapshotsPerSecond = 21 }).Validate());
        });

    [Fact]
    public async Task Mirror_failure_does_not_stop_local_recording_or_sensors()
    {
        string folder = CreateFolder();
        string blocked = Path.Combine(folder, "not-a-folder");
        await File.WriteAllTextAsync(blocked, "blocked", TestContext.Current.CancellationToken);
        TestImuSource imu = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("deterministic-mirror",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));

        try
        {
            FusionGaugesOptions options = new() { RecordingFolder = folder, MirrorFolder = blocked, MirrorEnabled = true };
            await hub.StartAsync(options);
            await hub.StartRecordingAsync(options);
            DateTimeOffset origin = DateTimeOffset.UtcNow.AddSeconds(-2);

            for (int index = 0; index < 100; index++)
            {
                imu.Emit(origin.AddMilliseconds(index * 10));
            }

            await WaitAsync(() => hub.Core.ImuHistory.Count == 100);
            await hub.StopRecordingAsync();
            await WaitAsync(() => hub.CaptureMirrorStatus().LastError is not null);
            Assert.True(hub.IsRunning);
            Assert.Equal(0, hub.LastRecording!.RowsDropped);
            Assert.True(hub.CaptureMirrorStatus().Pending > 0);
            await hub.StopAsync();
            await RunStaAsync(async () =>
            {
                using MainForm form = new(hub);
                form.Show();
                await Task.Yield();
                System.Windows.Forms.Timer timer = Field<System.Windows.Forms.Timer>(form, "_frameTimer");
                Assert.True(timer.Enabled);
                Assert.Equal(1000, timer.Interval);
                Assert.Contains("ZIP FAULT", Field<ToolStripStatusLabel>(form, "_statusLabel").Text);
                SensorObservationFrame stopped = hub.Capture();
                typeof(MainForm).GetMethod("RefreshPresentation", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(form, null);
                Assert.Same(stopped, hub.Capture());
                Assert.Equal(100, hub.Core.ImuHistory.Count);
            });
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Shutdown_during_recording_finalizes_and_releases_acquisition_without_a_UI_timer()
    {
        string folder = CreateFolder();
        TestImuSource imu = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("shutdown",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));

        try
        {
            await hub.StartAsync(new() { RecordingFolder = folder, AutoRecording = true });

            for (int index = 0; index < 40; index++)
            {
                imu.Emit(DateTimeOffset.UtcNow.AddMilliseconds(index));
            }

            await WaitAsync(() => hub.Core.ImuHistory.Count == 40);
            await hub.DisposeAsync();
            Assert.False(hub.IsRunning);
            Assert.False(hub.IsRecording);
            Assert.Equal(1, imu.StopCount);
            using JsonDocument manifest = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(hub.LastRecording!.FolderPath, "companions.manifest.json"),
                    TestContext.Current.CancellationToken));
            Assert.True(manifest.RootElement.GetProperty("Complete").GetBoolean());
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Source_stop_failure_is_terminal_but_does_not_skip_remaining_owned_cleanup()
    {
        string folder = CreateFolder();
        TestImuSource imu = new() { StopFailure = new IOException("deterministic source stop failure") };
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("failed-stop",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
        List<string> shutdown = [];
        hub.ShutdownProgress += shutdown.Add;

        try
        {
            await hub.StartAsync(new() { RecordingFolder = folder });
            imu.Emit(DateTimeOffset.UtcNow);
            await WaitAsync(() => hub.Core.ImuHistory.Count == 1);
            await Assert.ThrowsAsync<AggregateException>(hub.StopAsync);
            Assert.False(hub.IsRunning);
            Assert.NotNull(hub.Fault);
            Assert.Null(Field<object?>(hub, "_service"));
            Assert.Null(Field<object?>(hub, "_fusion"));
            Assert.Null(Field<object?>(hub, "_rawSubscription"));
            Assert.True(imu.StopCount > 0);
            Assert.Contains(shutdown, message => message.StartsWith(
                "ERROR: Release acquisition lease", StringComparison.Ordinal));
            Assert.Contains(shutdown, message => message.StartsWith(
                "OK: Dispose acquisition service", StringComparison.Ordinal));
            SensorObservationFrame stopped = hub.Capture();
            Assert.Same(stopped, hub.Capture());
            Assert.False(stopped.ImuFresh);
            Assert.Equal(0d, stopped.ImuHz);
        }
        finally
        {
            try
            {
                await hub.DisposeAsync();
            }
            catch (AggregateException exception)
            {
                Assert.Contains("shutdown", exception.ToString(), StringComparison.OrdinalIgnoreCase);
            }

            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task Engine_swap_during_recording_rotates_companions_without_restarting_or_losing_raw_history()
    {
        string folder = CreateFolder();
        TestImuSource imu = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("engine-swap",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));

        try
        {
            FusionGaugesOptions options = new() { RecordingFolder = folder };
            await hub.StartAsync(options);
            await hub.StartRecordingAsync(options);

            for (int index = 0; index < 20; index++)
            {
                imu.Emit(DateTimeOffset.UtcNow.AddMilliseconds(index));
            }

            await WaitAsync(() => hub.Core.ImuHistory.Count == 20
                && hub.Core.Capture(DateTimeOffset.UtcNow).Fusion is not null);
            Task replacement = hub.ReplaceEngineAsync(new TestFusionEngine());
            imu.Emit(DateTimeOffset.UtcNow.AddMilliseconds(100));
            await replacement;
            await WaitAsync(() => hub.Core.ImuHistory.Count == 21
                && hub.Core.Capture(DateTimeOffset.UtcNow).Fusion?.Epoch == 1);
            await hub.StopRecordingAsync();
            Assert.True(hub.IsRunning);
            Assert.Equal(0, imu.StopCount);
            Assert.Equal(21, hub.Core.ImuHistory.Count);
            string recording = hub.LastRecording!.FolderPath;
            Assert.Equal(2, Directory.GetFiles(recording, "fusion-epoch-*.csv").Length);
            using JsonDocument manifest = JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(recording, "companions.manifest.json"),
                    TestContext.Current.CancellationToken));
            Assert.True(manifest.RootElement.GetProperty("Complete").GetBoolean());
            Assert.Equal(2, manifest.RootElement.GetProperty("Epochs").GetArrayLength());
            string[] controls = await File.ReadAllLinesAsync(Path.Combine(recording, "replay-controls.jsonl"),
                TestContext.Current.CancellationToken);
            using JsonDocument finalControl = JsonDocument.Parse(controls[^1]);
            Assert.Equal(1, finalControl.RootElement.GetProperty("Control").GetProperty("Epoch").GetInt32());
            Assert.Equal(JsonValueKind.Null,
                finalControl.RootElement.GetProperty("Control").GetProperty("Phase").ValueKind);
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, recursive: true);
        }
    }

    [Theory]
    [InlineData(24, 9f)]
    [InlineData(36, 10f)]
    [InlineData(48, 12f)]
    public async Task Shell_has_four_gauges_and_explicit_strip_fonts_with_sized_icons(int icons, float points)
    {
        await RunStaAsync(() =>
        {
            using MainForm form = new(CreateIdleHub());
            typeof(MainForm).GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(form, new FusionGaugesOptions { IconSize = icons, FontSize = points });
            typeof(MainForm).GetMethod("ApplyAppearance", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, null);
            form.Show();
            FluentTabControl tabs = Field<FluentTabControl>(form, "_tabs");
            Assert.Equal(3, tabs.Tabs.Count());
            Assert.Equal(points, Field<MenuStrip>(form, "_menu").Font.SizeInPoints);
            Assert.Equal(points, Field<StatusStrip>(form, "_status").Font.SizeInPoints);
            ToolStrip toolbar = Field<ToolStrip>(form, "_toolbar");
            Assert.Equal(points, toolbar.Font.SizeInPoints);
            Assert.Equal(form.LogicalToDeviceUnits(icons), toolbar.ImageScalingSize.Width);
            Assert.All(toolbar.Items.Cast<ToolStripItem>().Where(item => item is not ToolStripSeparator), item =>
            {
                Assert.NotNull(item.Image);
                Assert.Equal(ToolStripItemDisplayStyle.ImageAndText, item.DisplayStyle);
            });
            InstrumentsView instruments = Field<InstrumentsView>(form, "_instruments");
            Assert.NotNull(Field<Control>(instruments, "_horizon"));
            Assert.NotNull(Field<Control>(instruments, "_acceleration"));
            Assert.NotNull(Field<Control>(instruments, "_compass"));
            Assert.NotNull(Field<Control>(instruments, "_speed"));
            KioskModeManager kiosk = Field<KioskModeManager>(form, "_kiosk");
            Assert.True(kiosk.AlwaysOn);
            Assert.Equal(Keys.F11, kiosk.ToggleFullScreenKeys);
            Assert.True(kiosk.EscapeExitsFullScreen);
            Assert.True(Field<ToolStrip>(form, "_toolbar").Visible);
            Assert.True(Field<StatusStrip>(form, "_status").Visible);
            Rectangle bounds = form.Bounds;
            kiosk.FullScreen = true;
            Assert.False(Field<MenuStrip>(form, "_menu").Visible);
            Assert.True(toolbar.Visible);
            Assert.True(Field<StatusStrip>(form, "_status").Visible);
            kiosk.FullScreen = false;
            Assert.Equal(bounds, form.Bounds);
            Assert.True(Field<MenuStrip>(form, "_menu").Visible);
            form.Close();
        });
    }

    [Fact]
    public async Task Uncalibrated_or_stale_fusion_is_not_substituted_with_raw_acceleration()
    {
        await RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            SensorEnvelope input = new()
            {
                Sequence = 1,
                Kind = SensorDataKind.Imu,
                SourceTimestamp = now,
                ArrivalTimestamp = now
            };
            FusionEstimate estimate = new()
            {
                Sequence = 1,
                Timestamp = now,
                Roll = 25,
                Pitch = 10,
                LongitudinalAccel = 100,
                Validity = FusionOutputValidity.None
            };
            view.Present(new(input, null, null, new(input, estimate, 0, "stub"),
                100, 0, 0, 0, true, false, true), TabletFrameStatus.Idle);
            WarpToolkit.WinForms.Sensors.DirectX.D2DArtificialHorizon horizon =
                Field<WarpToolkit.WinForms.Sensors.DirectX.D2DArtificialHorizon>(view, "_horizon");
            Assert.Equal(WarpToolkit.WinForms.Sensors.DirectX.HorizonTrustLevel.Unavailable, horizon.TrustLevel);
        });
    }

    [Fact]
    public async Task Stale_frame_resets_previously_valid_attitude_acceleration_heading_and_speed()
    {
        await RunStaAsync(() =>
        {
            using InstrumentsView view = new();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            SensorEnvelope input = new()
            {
                Sequence = 1,
                Kind = SensorDataKind.Imu,
                SourceTimestamp = now,
                ArrivalTimestamp = now
            };
            FusionEstimate estimate = new()
            {
                Sequence = 1,
                Timestamp = now,
                Roll = 25,
                Pitch = 10,
                Heading = 50,
                Speed = 20,
                LongitudinalAccel = 9.80665,
                LateralAccel = 0,
                VerticalAccel = 0,
                Validity = FusionOutputValidity.Attitude | FusionOutputValidity.Acceleration
                    | FusionOutputValidity.Heading | FusionOutputValidity.Navigation,
                CalibrationState = FusionCalibrationState.Calibrated,
                Confidence = 1
            };
            SensorEnvelope gps = new()
            {
                Sequence = 2,
                Kind = SensorDataKind.Geolocation,
                SourceTimestamp = now,
                ArrivalTimestamp = now,
                Geolocation = new GeolocationPayload
                {
                    LatitudeDegrees = 50,
                    LongitudeDegrees = 7,
                    AltitudeMeters = 10,
                    SpeedValid = true,
                    SpeedMetersPerSecond = 20,
                    SourceTimestamp = now,
                    CoordinateTimestamp = now
                }
            };
            SensorObservationFrame frame = new(input, gps, null, new(input, estimate, 0, "stub"),
                100, 5, 0, 0, true, true, true);
            view.Present(frame, TabletFrameStatus.Idle);
            var horizon = Field<WarpToolkit.WinForms.Sensors.DirectX.D2DArtificialHorizon>(view, "_horizon");
            var acceleration = Field<WarpToolkit.WinForms.Sensors.DirectX.D2DAccelerationGauge>(view, "_acceleration");
            var compass = Field<WarpToolkit.WinForms.Sensors.DirectX.D2DCompassGauge>(view, "_compass");
            var speed = Field<WarpToolkit.WinForms.Sensors.DirectX.D2DSpeedometer>(view, "_speed");
            Assert.Equal(WarpToolkit.WinForms.Sensors.DirectX.HorizonTrustLevel.Calibrated, horizon.TrustLevel);
            Assert.True(acceleration.IsAccelerationValid);
            Assert.Equal(1d, acceleration.ForwardG, 6);
            Assert.True(compass.IsHeadingValid);
            Assert.True(speed.IsSpeedValid);
            view.Present(frame with { ImuFresh = false, GpsFresh = false, FusionFresh = false }, TabletFrameStatus.Idle);
            Assert.Equal(WarpToolkit.WinForms.Sensors.DirectX.HorizonTrustLevel.Unavailable, horizon.TrustLevel);
            Assert.False(acceleration.IsAccelerationValid);
            Assert.False(compass.IsHeadingValid);
            Assert.False(speed.IsSpeedValid);
        });
    }

    private static T Field<T>(object owner, string name)
        => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    [Fact]
    public async Task Idle_shell_does_not_poll_or_restart_the_timer_for_one_shot_commands()
    {
        await RunStaAsync(async () =>
        {
            using MainForm form = new(CreateIdleHub());
            form.Show();
            await Task.Yield();
            System.Windows.Forms.Timer timer = Field<System.Windows.Forms.Timer>(form, "_frameTimer");
            Assert.False(timer.Enabled);
            DiagnosticsView diagnostics = Field<DiagnosticsView>(form, "_diagnostics");
            string before = Field<TextBox>(diagnostics, "_details").Text;
            Assert.Contains("SESSION STOPPED", before);
            await RunFormCommandAsync(form, () => Task.CompletedTask);
            typeof(MainForm).GetMethod("OnFrameTick", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(form, [null, EventArgs.Empty]);
            Assert.False(timer.Enabled);
            Assert.Equal(before, Field<TextBox>(diagnostics, "_details").Text);
        });
    }

    [Fact]
    public async Task Stop_freezes_final_sensor_history_and_restart_resumes_live_presentation()
    {
        await RunStaAsync(async () =>
        {
            TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
            TestImuSource imu = new() { StopTask = release.Task };
            await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("stopped-presentation",
                _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
            using MainForm form = new(hub);
            form.Show();
            System.Windows.Forms.Timer timer = Field<System.Windows.Forms.Timer>(form, "_frameTimer");
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

            try
            {
                await RunFormCommandAsync(form, () => hub.StartAsync(new()));
                Assert.True(timer.Enabled);
                Assert.Equal(16, timer.Interval);
                DateTimeOffset origin = DateTimeOffset.UtcNow.AddSeconds(-1);

                for (int index = 0; index < 100; index++)
                {
                    imu.Emit(origin.AddMilliseconds(index));
                }

                await WaitAsync(() => hub.Core.ImuHistory.Count == 100 && hub.Core.FusionHistory.Count == 100);
                Task stopping = RunFormCommandAsync(form, () =>
                    (Task)typeof(MainForm).GetMethod("StopSessionAsync", flags)!.Invoke(form, null)!);
                Assert.False(timer.Enabled);
                Assert.Contains("Stopping sensors", Field<ToolStripStatusLabel>(form, "_statusLabel").Text);
                typeof(MainForm).GetMethod("OnFrameTick", flags)!.Invoke(form, [null, EventArgs.Empty]);
                Assert.Contains("Stopping sensors", Field<ToolStripStatusLabel>(form, "_statusLabel").Text);
                release.SetResult();
                await stopping.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.False(timer.Enabled);
                Assert.False(hub.IsRunning);
                SensorObservationFrame stopped = hub.Capture();
                Assert.False(stopped.ImuFresh);
                Assert.False(stopped.GpsFresh);
                Assert.False(stopped.FusionFresh);
                Assert.Equal(0d, stopped.ImuHz);
                Assert.Equal(0d, stopped.GpsHz);
                DiagnosticsView diagnostics = Field<DiagnosticsView>(form, "_diagnostics");
                string final = Field<TextBox>(diagnostics, "_details").Text;
                Assert.Contains("SESSION STOPPED", final);
                Assert.Contains("Frozen final history: IMU 100, GPS 0, fusion 100", final);

                for (int tick = 0; tick < 10; tick++)
                {
                    typeof(MainForm).GetMethod("OnFrameTick", flags)!.Invoke(form, [null, EventArgs.Empty]);
                }

                await RunFormCommandAsync(form, () => Task.CompletedTask);
                Assert.Same(stopped, hub.Capture());
                Assert.Equal(final, Field<TextBox>(diagnostics, "_details").Text);
                Assert.False(timer.Enabled);
                await RunFormCommandAsync(form, () => hub.StartAsync(new()));
                Assert.True(timer.Enabled);
                Assert.Equal(0, hub.Core.ImuHistory.Count);
                Assert.NotSame(stopped, hub.Capture());
                imu.Emit(DateTimeOffset.UtcNow);
                await WaitAsync(() => hub.Core.ImuHistory.Count == 1);
                Assert.True(hub.Capture().ImuFresh);
                await RunFormCommandAsync(form, () =>
                    (Task)typeof(MainForm).GetMethod("StopSessionAsync", flags)!.Invoke(form, null)!);
                Assert.False(timer.Enabled);
            }
            finally
            {
                release.TrySetResult();
            }
        });
    }

    [Fact]
    public async Task Presentation_clock_gates_16ms_ticks_to_nominal_30Hz_without_catchup_bursts()
    {
        await RunStaAsync(() =>
        {
            using MainForm form = new();
            MethodInfo advance = typeof(MainForm).GetMethod("AdvancePresentationClock",
                BindingFlags.Instance | BindingFlags.NonPublic)!;
            int frames = 0;

            for (long milliseconds = 0; milliseconds < 2000; milliseconds += 16)
            {
                long ticks = milliseconds * System.Diagnostics.Stopwatch.Frequency / 1000;
                frames += (bool)advance.Invoke(form, [ticks])! ? 1 : 0;
            }

            Assert.Equal(60, frames);
            long delayed = 5 * System.Diagnostics.Stopwatch.Frequency;
            Assert.True((bool)advance.Invoke(form, [delayed])!);
            Assert.False((bool)advance.Invoke(form, [delayed])!);
        });
    }

    private static Task RunFormCommandAsync(MainForm form, Func<Task> command)
        => (Task)typeof(MainForm).GetMethod("RunCommandAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(form, [command])!;

    private static void ClickToolbarParent(SymbolToolStrip toolbar, ToolStripItem item)
    {
        Point point = new(item.Bounds.Left + item.Width / 2, item.Bounds.Top + item.Height / 2);
        Assert.Same(item, toolbar.GetItemAt(point));
        int depth = toolbar.CarModeDepth;
        typeof(ToolStrip).GetMethod("OnMouseDown", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(toolbar, [new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0)]);
        Assert.Equal(depth, toolbar.CarModeDepth);
        typeof(ToolStrip).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(toolbar, [new MouseEventArgs(MouseButtons.Left, 1, point.X, point.Y, 0)]);
        Assert.Equal(depth + 1, toolbar.CarModeDepth);
    }

    private static Task RunStaAsync(Action action)
        => RunStaAsync(() =>
        {
            action();

            return Task.CompletedTask;
        });

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

    private static string CreateFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"fusion-gauges-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);

        return folder;
    }

    [Fact]
    public async Task Mid_drive_recording_exports_full_causal_inputs_and_consumed_speed_policy()
    {
        string folder = CreateFolder();
        TestImuSource imu = new();
        await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("causal-recording",
            _ => new SensorSourceSet(imu, NoSensorGpsSource.Instance)));
        FusionGaugesOptions options = new() { RecordingFolder = folder };
        try
        {
            await hub.StartAsync(options);
            System.Collections.Concurrent.ConcurrentQueue<FusionEstimate> original = new();
            Field<SensorFusionSession>(hub, "_fusion").Processed += observation =>
            {
                if (observation.Estimate is { } estimate)
                {
                    original.Enqueue(estimate);
                }
            };
            DateTimeOffset origin = DateTimeOffset.UtcNow;
            for (int index = 0; index < 20; index++)
            {
                imu.Emit(origin.AddMilliseconds(index));
            }
            await WaitAsync(() => hub.Core.FusionHistory.Count == 20);
            hub.SelectObdSpeed(true);
            await hub.StartRecordingAsync(options);
            string recording = hub.RecordingStatus!.FolderPath;
            for (int index = 20; index < 25; index++)
            {
                imu.Emit(origin.AddMilliseconds(index));
            }
            await WaitAsync(() => hub.Core.FusionHistory.Count == 25);
            await hub.StopRecordingAsync();
            List<SensorEnvelope> causal = [];
            await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadPartAsync(
                Path.Combine(recording, "replay-inputs.compact.json"), TestContext.Current.CancellationToken))
            {
                causal.Add(row.Envelope);
            }
            Assert.Equal(25, causal.Count(static input => input.Kind == SensorDataKind.Imu));
            Assert.Contains(causal, input => input.Kind == SensorDataKind.Imu && input.SourceTimestamp == origin);
            string[] controls = await File.ReadAllLinesAsync(Path.Combine(recording, "replay-controls.jsonl"),
                TestContext.Current.CancellationToken);
            using JsonDocument initial = JsonDocument.Parse(controls[1]);
            Assert.Equal(JsonValueKind.Null,
                initial.RootElement.GetProperty("Control").GetProperty("Speed").ValueKind);
            using JsonDocument final = JsonDocument.Parse(controls[^1]);
            Assert.Equal(JsonValueKind.Object,
                final.RootElement.GetProperty("Control").GetProperty("Speed").ValueKind);
            IFusionEngine replay = FusionEngineFactory.CreateDefault();
            IVehicleSpeedConsumer speedControl = Assert.IsAssignableFrom<IVehicleSpeedConsumer>(replay);
            ITabletFrameController mounting = Assert.IsAssignableFrom<ITabletFrameController>(replay);
            List<FusionEstimate> estimates = [];
            JsonSerializerOptions json = new()
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
            };
            for (int index = 0; index < causal.Count; index++)
            {
                using JsonDocument row = JsonDocument.Parse(controls[index + 1]);
                JsonElement control = row.RootElement.GetProperty("Control");
                speedControl.SetVehicleSpeed(control.GetProperty("Speed").Deserialize<VehicleSpeedMeasurement>(json));
                TabletFramePhase? phase = control.GetProperty("Phase").ValueKind == JsonValueKind.Null
                    ? null : (TabletFramePhase)control.GetProperty("Phase").GetInt32();
                if (phase == TabletFramePhase.Idle)
                {
                    mounting.ClearSessionDownLock();
                }
                else if (phase == TabletFramePhase.Reading && mounting.FrameStatus.Phase != phase)
                {
                    mounting.BeginTabletFrameCapture();
                }
                if (control.GetProperty("Mount").ValueKind != JsonValueKind.Null)
                {
                    SessionDownLock mount = control.GetProperty("Mount").Deserialize<SessionDownLock>(json);
                    if (mounting.FrameStatus.Lock != mount)
                    {
                        mounting.ApplySessionDownLock(mount);
                    }
                }
                if (replay.Process(causal[index]) is { } estimate)
                {
                    estimates.Add(estimate);
                }
            }
            Assert.Equal(original.ToArray(), estimates);
            Assert.True(File.Exists(Path.Combine(recording, "replay-inputs.manifest.json")));
            Assert.True(hub.IsRunning);
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task Gps_preview_and_live_COM_switch_preserve_drive_engine_and_recording()
    {
        string folder = CreateFolder();
        GpsAcquisitionCoordinator owner = new();
        TestImuSource imu = new();
        PreviewGpsSource first = new();
        PreviewGpsSource second = new();
        SensorHub hub = new(_ => new DelegateSensorSourceFactory("shared-gps",
            _ => new SensorSourceSet(imu, owner.CreateSourceView())), gpsOwner: owner,
            gpsSelectionFactory: options => new(options.SerialGpsOptions.ConfigurationKey,
                () => options.SerialGpsOptions.PortName == "COM1" ? first : second, true));
        FusionGaugesOptions options = new()
        {
            SourceKind = WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps,
            SerialPortName = "COM1", RecordingFolder = folder
        };
        try
        {
            await hub.StartGpsPreviewAsync(options);
            await WaitAsync(() => owner.State == "connected");
            first.Emit();
            Assert.True(hub.Capture().GpsFresh);
            Assert.False(hub.IsRunning);
            Assert.Equal(0, hub.Core.ImuHistory.Count);
            Assert.Equal(0, hub.Core.FusionHistory.Count);
            await hub.StartAsync(options);
            await hub.StartRecordingAsync(options);
            string recording = hub.RecordingStatus!.FolderPath;
            object engine = Field<object>(hub, "_fusion");
            imu.Emit(DateTimeOffset.UtcNow);
            await WaitAsync(() => hub.Core.ImuHistory.Count == 1);
            await hub.ApplyGpsAsync(options with { SerialPortName = "COM2" });
            await WaitAsync(() => owner.State == "connected");
            second.Emit();
            imu.Emit(DateTimeOffset.UtcNow.AddMilliseconds(10));
            await WaitAsync(() => hub.Core.ImuHistory.Count == 2
                && hub.Capture().Gps?.SourceGeneration == owner.Generation);
            Assert.True(hub.Capture().GpsFresh);
            Assert.True(hub.IsRunning);
            Assert.True(hub.IsRecording);
            Assert.Equal(recording, hub.RecordingStatus!.FolderPath);
            Assert.Same(engine, Field<object>(hub, "_fusion"));
            Assert.Equal(0, imu.StopCount);
            Assert.Equal(1, first.Starts);
            Assert.Equal(1, first.Stops);
            Assert.Equal(1, second.Starts);
            await hub.StopRecordingAsync();
            List<SensorSourceTransition> transitions = [];
            await foreach (SensorEnvelopeRow row in SensorRecordingReader.ReadFolderAsync(
                recording, SensorRecordingFormats.CompactJson, TestContext.Current.CancellationToken))
            {
                if (row.Envelope.SourceTransition is { } transition)
                {
                    transitions.Add(transition);
                }
            }
            Assert.Contains(transitions, transition => transition.Generation == owner.Generation && transition.State == "probing");
            await hub.StopAsync();
            second.Emit();
            Assert.True(hub.Capture().GpsFresh);
            Assert.False(hub.Capture().ImuFresh);
            Assert.False(hub.Capture().FusionFresh);
            Assert.Equal(0, second.Stops);
            await hub.DisposeAsync();
            Assert.Equal(1, second.Stops);
        }
        finally
        {
            await hub.DisposeAsync();
            Directory.Delete(folder, true);
        }
    }

    [Fact]
    public async Task Stopped_drive_does_not_authorize_receiver_writes_without_fresh_stationary_speed()
    {
        GpsAcquisitionCoordinator owner = new();
        PreviewGpsSource first = new();
        PreviewGpsSource second = new();
        await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("gps-safety",
            _ => new SensorSourceSet(NoSensorImuSource.Instance, owner.CreateSourceView())), gpsOwner: owner,
            gpsSelectionFactory: options => new(options.SerialGpsOptions.ConfigurationKey,
                () => options.SerialGpsOptions.PortName == "COM1" ? first : second, true));
        FusionGaugesOptions initial = new()
        {
            SourceKind = WarpToolkit.Windows.Sensors.WindowsSensorSourceKind.ImuAndSerialNmeaGps,
            SerialPortName = "COM1"
        };
        await hub.StartGpsPreviewAsync(initial);
        await WaitAsync(() => owner.State == "connected");
        FusionGaugesOptions writing = initial with
        {
            SerialPortName = "COM2", GnssProfile = SerialGnssProfile.Current,
            AllowReceiverCommandsThisSession = true
        };
        Assert.False(hub.IsRunning);
        await Assert.ThrowsAsync<InvalidOperationException>(() => hub.ApplyGpsAsync(writing, allowReceiverWrites: true));
        first.Emit(speed: 12);
        await Assert.ThrowsAsync<InvalidOperationException>(() => hub.ApplyGpsAsync(writing, allowReceiverWrites: true));
        Assert.Equal(0, second.Starts);
        first.Emit(speed: 0);
        await hub.ApplyGpsAsync(writing, allowReceiverWrites: true);
        await WaitAsync(() => second.Starts == 1);
        Assert.False(hub.IsRunning);
    }

    [Fact]
    public async Task Preview_shell_refreshes_GPS_at_low_rate_without_starting_a_drive()
    {
        await RunStaAsync(async () =>
        {
            GpsAcquisitionCoordinator owner = new();
            PreviewGpsSource receiver = new();
            await using SensorHub hub = new(_ => new DelegateSensorSourceFactory("preview-shell",
                _ => new SensorSourceSet(NoSensorImuSource.Instance, owner.CreateSourceView())), gpsOwner: owner,
                gpsSelectionFactory: _ => new("test", () => receiver, true));
            using MainForm form = new(hub);
            form.Show();
            await WaitAsync(() => owner.State == "connected");
            receiver.Emit();
            await RunFormCommandAsync(form, () => Task.CompletedTask);
            System.Windows.Forms.Timer timer = Field<System.Windows.Forms.Timer>(form, "_frameTimer");
            Assert.True(timer.Enabled);
            Assert.Equal(200, timer.Interval);
            Assert.True(hub.Capture().GpsFresh);
            Assert.False(hub.IsRunning);
            Assert.Equal(0, hub.Core.FusionHistory.Count);
            DiagnosticsView diagnostics = Field<DiagnosticsView>(form, "_diagnostics");
            string details = Field<TextBox>(diagnostics, "_details").Text;
            Assert.Contains("GPS is live preview", details);
            Assert.DoesNotContain("Measurements below are historical, not live", details);
        });
    }

    private sealed class PreviewGpsSource : IGpsSource
    {
        private long _fixes;
        public int Starts;
        public int Stops;
        public event EventHandler<GpsSample>? FixReady;
        public event EventHandler? HealthChanged { add { } remove { } }
        public GpsAccessStatus AccessStatus => GpsAccessStatus.Allowed;
        public GpsAcquisitionStatus AcquisitionStatus => GpsAcquisitionStatus.Ready;
        public GpsPositionSource? LastDroppedFixSource => null;
        public DateTimeOffset? LastDroppedFixUtc => null;
        public GpsStreamStatistics Statistics => new(_fixes, _fixes, 0, 0, null);
        public Task StartAsync(CancellationToken cancellationToken = default) { Starts++; return Task.CompletedTask; }
        public Task StopAsync(CancellationToken cancellationToken = default) { Stops++; return Task.CompletedTask; }
        public void Emit(double speed = 12)
        {
            _fixes++;
            FixReady?.Invoke(this, new(speed, 90, true, 2, DateTimeOffset.UtcNow.AddTicks(_fixes),
                51, 7, GpsPositionSource.Satellite, fixQuality: 1));
        }
    }

    private static SensorHub CreateIdleHub()
        => new(_ => new DelegateSensorSourceFactory("idle-without-hardware",
            _ => new SensorSourceSet(NoSensorImuSource.Instance, NoSensorGpsSource.Instance)));

    private static async Task WaitAsync(Func<bool> predicate)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));

        while (!predicate())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    /// <summary>Provides sensor callbacks only when requested by a test.</summary>
    private sealed class TestImuSource : IImuSource
    {
        public event EventHandler<ImuSample>? SampleReady;
        public bool IsAccelerometerAvailable => true;
        public bool IsGyroscopeAvailable => true;
        public DateTimeOffset? LastCallbackReceivedUtc { get; private set; }
        public int StopCount { get; private set; }
        public Exception? StopFailure { get; init; }
        public Task? StopTask { get; init; }
        public Task StartAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;

            return StopFailure is null ? StopTask ?? Task.CompletedTask : Task.FromException(StopFailure);
        }

        public void Emit(DateTimeOffset timestamp)
        {
            LastCallbackReceivedUtc = DateTimeOffset.UtcNow;
            SampleReady?.Invoke(this, new(new(0, -1, 0), Vec3.Zero, timestamp));
        }
    }

    /// <summary>Emits deterministic estimates for the app's owner-thread replacement tests.</summary>
    private sealed class TestFusionEngine : IFusionEngine
    {
        public FusionEstimate? Process(SensorEnvelope envelope)
            => envelope.Kind == SensorDataKind.Imu
                ? new() { Sequence = envelope.Sequence, Timestamp = envelope.SourceTimestamp }
                : null;

        public void Reset() { }
    }
}
