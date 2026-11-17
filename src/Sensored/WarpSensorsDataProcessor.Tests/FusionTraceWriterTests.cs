using System.Globalization;
using System.Security.Cryptography;

using WarpToolkit.Sensors;
using WarpToolkit.Sensors.Fusion;
using WarpToolkit.Sensors.Payloads;

namespace WarpSensorsDataProcessor.Tests;

/// <summary>
///  Verifies the locked golden-trace writer contract.
/// </summary>
public class FusionTraceWriterTests
{
    [Fact]
    public async Task Header_matches_the_locked_schema()
    {
        string path = NewTracePath();
        await using (FusionTraceWriter writer = new(path))
        {
        }

        string header = (await File.ReadAllLinesAsync(
            path,
            TestContext.Current.CancellationToken))[0];
        Assert.Equal(FusionTraceSchema.Header, header);
        Assert.Equal(2, FusionTraceSchema.Version);
        Assert.Equal(72, FusionTraceSchema.ColumnCount);
        Assert.Equal("aux", FusionTraceSchema.Columns[61]);
        Assert.Equal("diag_down_spread_rad", FusionTraceSchema.Columns[62]);
        Assert.Equal("diag_calib_transition", FusionTraceSchema.Columns[^1]);
        Assert.Equal(FusionTraceSchema.ColumnCount, header.Split(';').Length);
    }

    [Fact]
    public void Existing_file_is_rejected()
    {
        string path = NewTracePath();
        File.WriteAllText(path, "existing");

        IOException exception = Assert.Throws<IOException>(() => new FusionTraceWriter(path));
        Assert.Contains(path, exception.Message);
    }

    [Fact]
    public async Task Null_estimate_does_not_write_a_row()
    {
        string path = NewTracePath();
        SensorEnvelope imu = TestRecordings.CreateImuRow(0, TestRecordings.Origin, 1d).Envelope;

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                imu,
                estimate: null,
                recordingRow: 0,
                TestContext.Current.CancellationToken);
        }

        string[] lines = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
        Assert.Single(lines);
    }

    [Fact]
    public async Task Nonfinite_raw_values_are_written_explicitly()
    {
        string path = NewTracePath();
        SensorEnvelopeRow row = TestRecordings.CreateImuRow(
            0,
            TestRecordings.Origin,
            1d,
            magneticHeading: double.NaN,
            magneticConfidence: 0d);
        FusionEstimate estimate = CreateEstimate(0, TestRecordings.Origin);

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                row.Envelope,
                estimate,
                row.Row,
                TestContext.Current.CancellationToken);
        }

        string data = (await File.ReadAllLinesAsync(
            path,
            TestContext.Current.CancellationToken))[1];
        Assert.Contains(";NaN;", data);
        Assert.DoesNotContain("NaN.NaN", data);
    }

    [Fact]
    public async Task Rejection_reason_with_separator_is_quoted()
    {
        string path = NewTracePath();
        SensorEnvelopeRow row = TestRecordings.CreateImuRow(0, TestRecordings.Origin, 1d);
        FusionEstimate estimate = CreateEstimate(0, TestRecordings.Origin) with
        {
            RejectedSample = true,
            Diagnostics = new FusionDiagnostics { RejectionReason = "spike; \"z\"" }
        };

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                row.Envelope,
                estimate,
                row.Row,
                TestContext.Current.CancellationToken);
        }

        string data = (await File.ReadAllLinesAsync(
            path,
            TestContext.Current.CancellationToken))[1];
        Assert.Contains("\"spike; \"\"z\"\"\"", data);
    }

    [Fact]
    public async Task Multiple_gps_observations_are_audited_without_future_leakage()
    {
        string path = NewTracePath();
        DateTimeOffset t0 = TestRecordings.Origin;
        SensorEnvelopeRow imu0 = TestRecordings.CreateImuRow(0, t0, 1d);
        SensorEnvelopeRow gpsFuture = TestRecordings.CreateGpsRow(
            1,
            t0.AddMilliseconds(1),
            47d,
            8d,
            speed: 3d,
            coordinateTimestamp: t0.AddSeconds(2),
            fixTimestamp: t0.AddSeconds(2));
        SensorEnvelopeRow gpsNow = TestRecordings.CreateGpsRow(2, t0.AddMilliseconds(2), 48d, 9d, speed: 4d);
        SensorEnvelopeRow imu1 = TestRecordings.CreateImuRow(3, t0.AddMilliseconds(3), 2d);
        SensorEnvelopeRow imuCatchUp = TestRecordings.CreateImuRow(4, t0.AddSeconds(2), 3d);

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                imu0.Envelope,
                CreateEstimate(0, imu0.Envelope.SourceTimestamp),
                imu0.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                gpsFuture.Envelope,
                null,
                gpsFuture.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                gpsNow.Envelope,
                null,
                gpsNow.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                imu1.Envelope,
                CreateEstimate(1, imu1.Envelope.SourceTimestamp),
                imu1.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                imuCatchUp.Envelope,
                CreateEstimate(2, imuCatchUp.Envelope.SourceTimestamp),
                imuCatchUp.Row,
                TestContext.Current.CancellationToken);
        }

        string[] lines = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
        Assert.Equal(4, lines.Length);
        string[] first = lines[1].Split(';');
        string[] second = lines[2].Split(';');
        string[] third = lines[3].Split(';');
        int gpsLat = Index("gps_lat");
        int aux = Index("aux");

        Assert.Equal(string.Empty, first[gpsLat]);
        Assert.Equal("48", second[gpsLat]);
        Assert.Contains("G,1,", second[aux]);
        Assert.Contains("G,2,", second[aux]);
        Assert.Equal("47", third[gpsLat]);
        Assert.Equal(string.Empty, third[aux]);
    }

    [Fact]
    public async Task Gps_measurement_time_prefers_coordinate_over_receiver()
    {
        string path = NewTracePath();
        DateTimeOffset t0 = TestRecordings.Origin;
        SensorEnvelopeRow imu0 = TestRecordings.CreateImuRow(0, t0, 1d);
        SensorEnvelopeRow gps = TestRecordings.CreateGpsRow(
            1,
            t0.AddMilliseconds(1),
            47d,
            8d,
            speed: 3d,
            coordinateTimestamp: t0.AddSeconds(2),
            fixTimestamp: t0);
        SensorEnvelopeRow imu1 = TestRecordings.CreateImuRow(2, t0.AddMilliseconds(3), 2d);
        SensorEnvelopeRow imuCatchUp = TestRecordings.CreateImuRow(3, t0.AddSeconds(2), 3d);

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                imu0.Envelope,
                CreateEstimate(0, t0),
                imu0.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                gps.Envelope,
                null,
                gps.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                imu1.Envelope,
                CreateEstimate(1, imu1.Envelope.SourceTimestamp),
                imu1.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                imuCatchUp.Envelope,
                CreateEstimate(2, imuCatchUp.Envelope.SourceTimestamp),
                imuCatchUp.Row,
                TestContext.Current.CancellationToken);
        }

        string[] lines = await File.ReadAllLinesAsync(path, TestContext.Current.CancellationToken);
        string[] early = lines[2].Split(';');
        string[] late = lines[3].Split(';');
        int gpsLat = Index("gps_lat");
        int gpsMeas = Index("gps_meas_t");

        Assert.Equal(string.Empty, early[gpsLat]);
        Assert.Equal("47", late[gpsLat]);
        Assert.Contains("T12:00:02", late[gpsMeas]);
    }

    [Fact]
    public async Task Held_regressing_imu_ages_gps_from_estimate_timestamp()
    {
        string path = NewTracePath();
        DateTimeOffset t0 = TestRecordings.Origin;
        SensorEnvelopeRow imu = TestRecordings.CreateImuRow(0, t0, 1d);
        SensorEnvelopeRow gps = TestRecordings.CreateGpsRow(1, t0.AddMilliseconds(-1), 48d, 9d);
        SensorEnvelopeRow regressing = TestRecordings.CreateImuRow(2, t0.AddMilliseconds(-5), 2d);
        FusionEstimate held = CreateEstimate(1, t0) with { RejectedSample = true };

        await using (FusionTraceWriter writer = new(path))
        {
            await writer.WriteAsync(
                imu.Envelope,
                CreateEstimate(0, t0),
                imu.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                gps.Envelope,
                null,
                gps.Row,
                TestContext.Current.CancellationToken);
            await writer.WriteAsync(
                regressing.Envelope,
                held,
                regressing.Row,
                TestContext.Current.CancellationToken);
        }

        string[] heldRow = (await File.ReadAllLinesAsync(
            path,
            TestContext.Current.CancellationToken))[2].Split(';');
        double age = double.Parse(
            heldRow[Index("gps_age_s")],
            CultureInfo.InvariantCulture);
        Assert.True(age >= 0d, $"gps_age_s was {age}");
        Assert.Equal(0.001d, age, 6);
    }

    [Fact]
    public async Task Auxiliary_overflow_fails_instead_of_dropping_provenance()
    {
        string path = NewTracePath();
        await using FusionTraceWriter writer = new(path);
        SensorEnvelope gps = TestRecordings.CreateGpsRow(0, TestRecordings.Origin, 47d, 8d).Envelope;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            for (int index = 0; index <= FusionTraceFormatting.MaxAuxiliaryEvents; index++)
            {
                await writer.WriteAsync(
                    gps,
                    estimate: null,
                    recordingRow: index,
                    TestContext.Current.CancellationToken);
            }
        });
    }

    [Fact]
    public async Task Default_gps_timestamps_stay_aux_only_and_do_not_fill_pending()
    {
        string path = NewTracePath();
        DateTimeOffset t0 = TestRecordings.Origin;
        SensorEnvelopeRow imu = TestRecordings.CreateImuRow(0, t0, 1d);
        SensorEnvelope defaultGps = new()
        {
            Sequence = 1,
            Kind = SensorDataKind.Geolocation,
            SourceTimestamp = default,
            ArrivalTimestamp = t0,
            Geolocation = new GeolocationPayload(
                47d,
                8d,
                AltitudeMeters: 100d,
                HorizontalAccuracyMeters: 3d,
                VerticalAccuracyMeters: 5d,
                SpeedMetersPerSecond: 1d,
                SpeedValid: true,
                CourseDegrees: 0d,
                CourseValid: false,
                PositionSource: GpsPositionSource.Satellite,
                IsRemoteSource: false,
                CoordinateTimestamp: default,
                SourceTimestamp: default,
                PositionDilutionOfPrecision: 1d,
                HorizontalDilutionOfPrecision: 1d,
                VerticalDilutionOfPrecision: 1d,
                FixQuality: 1,
                SatelliteCount: 8,
                Confidence: 1d)
        };

        await using (FusionTraceWriter writer = new(path))
        {
            for (int index = 0; index < FusionTraceFormatting.MaxPendingFutureGps + 1; index++)
            {
                await writer.WriteAsync(
                    defaultGps,
                    estimate: null,
                    recordingRow: index,
                    TestContext.Current.CancellationToken);
            }

            await writer.WriteAsync(
                imu.Envelope,
                CreateEstimate(0, t0),
                imu.Row,
                TestContext.Current.CancellationToken);
        }

        string[] row = (await File.ReadAllLinesAsync(
            path,
            TestContext.Current.CancellationToken))[1].Split(';');
        Assert.Equal(string.Empty, row[Index("gps_lat")]);
        Assert.Contains("G,", row[Index("aux")]);
    }

    [Fact]
    public async Task Pending_future_gps_overflow_fails()
    {
        string path = NewTracePath();
        DateTimeOffset t0 = TestRecordings.Origin;
        await using FusionTraceWriter writer = new(path);
        await writer.WriteAsync(
            TestRecordings.CreateImuRow(0, t0, 1d).Envelope,
            CreateEstimate(0, t0),
            0,
            TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            for (int index = 1; index <= FusionTraceFormatting.MaxPendingFutureGps + 1; index++)
            {
                SensorEnvelopeRow gps = TestRecordings.CreateGpsRow(
                    index,
                    t0.AddMilliseconds(index),
                    47d,
                    8d,
                    coordinateTimestamp: t0.AddYears(1),
                    fixTimestamp: t0.AddYears(1));
                await writer.WriteAsync(
                    gps.Envelope,
                    estimate: null,
                    gps.Row,
                    TestContext.Current.CancellationToken);
            }
        });
    }

    [Fact]
    public async Task Repeat_runs_under_invariant_and_german_culture_hash_identically()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        string first = await WriteSampleTraceAsync();

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            string second = await WriteSampleTraceAsync();
            Assert.Equal(Hash(first), Hash(second));
            int pitch = Index("pitch_deg");
            string pitchText = (await File.ReadAllLinesAsync(
                second,
                TestContext.Current.CancellationToken))[1].Split(';')[pitch];
            Assert.Equal("1.5", pitchText);
            Assert.DoesNotContain(',', pitchText);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task Disposed_writer_rejects_further_writes()
    {
        string path = NewTracePath();
        FusionTraceWriter writer = new(path);
        await writer.DisposeAsync();
        SensorEnvelope imu = TestRecordings.CreateImuRow(0, TestRecordings.Origin, 1d).Envelope;

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => writer.WriteAsync(
                imu,
                CreateEstimate(0, TestRecordings.Origin),
                0,
                TestContext.Current.CancellationToken).AsTask());
    }

    private static async Task<string> WriteSampleTraceAsync()
    {
        string path = NewTracePath();
        SensorEnvelopeRow imu = TestRecordings.CreateImuRow(0, TestRecordings.Origin, 1.25d);
        SensorEnvelopeRow gps = TestRecordings.CreateGpsRow(1, TestRecordings.Origin.AddMilliseconds(1), 47.1d, 8.2d);
        SensorEnvelopeRow imu2 = TestRecordings.CreateImuRow(2, TestRecordings.Origin.AddMilliseconds(3), 1.5d);

        await using FusionTraceWriter writer = new(path);
        await writer.WriteAsync(
            imu.Envelope,
            CreateEstimate(0, imu.Envelope.SourceTimestamp),
            imu.Row,
            TestContext.Current.CancellationToken);
        await writer.WriteAsync(
            gps.Envelope,
            null,
            gps.Row,
            TestContext.Current.CancellationToken);
        await writer.WriteAsync(
            imu2.Envelope,
            CreateEstimate(1, imu2.Envelope.SourceTimestamp),
            imu2.Row,
            TestContext.Current.CancellationToken);

        return path;
    }

    private static FusionEstimate CreateEstimate(long sequence, DateTimeOffset timestamp)
        => new()
        {
            Sequence = sequence,
            Timestamp = timestamp,
            Pitch = 1.5d,
            Roll = -0.25d,
            Heading = 12.5d,
            LongitudinalAccel = 0.1d,
            LateralAccel = 0.2d,
            VerticalAccel = 0.3d,
            Speed = 4.5d,
            Position = new FusionPosition(47.1d, 8.2d),
            CalibrationState = FusionCalibrationState.Converging,
            Confidence = 0.4d,
            Validity = FusionOutputValidity.Attitude | FusionOutputValidity.Acceleration,
            Diagnostics = new FusionDiagnostics { CalibrationProgress = 0.5d }
        };

    private static int Index(string name)
    {
        int index = FusionTraceSchema.Columns.ToList().IndexOf(name);
        Assert.True(index >= 0, name);

        return index;
    }

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);

        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string NewTracePath()
    {
        string folder = TestRecordings.CreateFolder();

        return Path.Combine(folder, "golden.trace.csv");
    }
}
