using System.Globalization;
using System.Text.Json;
using Microsoft.VisualBasic.FileIO;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>Indexes recorded V2 values on disk, averaging independently valid channels per time bucket.</summary>
internal sealed class V2RecordingTimelineIndex : IDisposable
{
    private readonly string _cachePath;
    private readonly BinaryReader _reader;
    private readonly long[] _offsets;
    private int _cachedIndex = -1;
    private V2TimelineRow? _cachedRow;

    private V2RecordingTimelineIndex(string path, string cachePath, string[] fields, long[] offsets)
    {
        SourcePath = path;
        _cachePath = cachePath;
        Fields = fields;
        _offsets = offsets;
        _reader = new BinaryReader(File.OpenRead(cachePath));
    }

    internal string SourcePath { get; }
    internal string[] Fields { get; }
    internal int Count => _offsets.Length;
    internal bool IsRaw => Array.IndexOf(Fields, "capture_utc") >= 0;

    internal bool IsMeasurementColumn(int index)
        => IsMeasurement(Fields[index], IsRaw);

    internal V2TimelineRow Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
        if (_cachedIndex != index)
        {
            _reader.BaseStream.Position = _offsets[index];
            _cachedRow = JsonSerializer.Deserialize<V2TimelineRow>(_reader.ReadString())
                ?? throw new InvalidDataException("Invalid V2 timeline cache.");
            _cachedIndex = index;
        }

        return _cachedRow!;
    }

    internal static string[] FindFiles(string path)
        => Directory.Exists(path)
            ? [.. Directory.EnumerateFiles(path, "*.csv")
                .Where(IsV2File).Order(StringComparer.OrdinalIgnoreCase)]
            : IsV2File(path) ? [path] : [];

    private static bool IsV2File(string path)
        => path.EndsWith(".RawV2.csv", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".FuseV2.csv", StringComparison.OrdinalIgnoreCase);

    internal static async Task<V2RecordingTimelineIndex> BuildAsync(
        string path, int resolutionMilliseconds, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(resolutionMilliseconds, 50);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(resolutionMilliseconds, 1000);
        using TextFieldParser parser = OpenParser(path);
        string[] fields = parser.ReadFields() ?? throw new InvalidDataException("The V2 CSV has no header.");
        if (fields.Distinct(StringComparer.Ordinal).Count() != fields.Length)
        {
            throw new InvalidDataException("The V2 CSV contains duplicate column names.");
        }

        Dictionary<string, int> columns = fields.Select((name, index) => (name, index))
            .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
        bool raw = columns.TryGetValue("capture_utc", out int timeColumn);
        if ((!raw && !columns.TryGetValue("out_t", out timeColumn))
            || !columns.TryGetValue("recording_id", out int identityColumn))
        {
            throw new InvalidDataException("Expected a RawV2 or FuseV2 CSV header.");
        }

        bool[] numeric = fields.Select(name => IsMeasurement(name, raw)).ToArray();
        bool[] circular = fields.Select(IsDirection).ToArray();
        int[] states = fields.Select(name => StateColumn(name, raw, columns)).ToArray();
        int[] contextFields = fields.Select((name, index) => (name, index))
            .Where(item => item.name is "epoch" or "engine" or "actual_speed_source" or "selected_speed_source"
                || item.name.EndsWith("_generation", StringComparison.Ordinal)
                || raw && item.name.EndsWith("_source", StringComparison.Ordinal))
            .Select(item => item.index).ToArray();
        string folder = Path.Combine(Path.GetTempPath(), "SensoredTimeline");
        Directory.CreateDirectory(folder);
        string cachePath = Path.Combine(folder, $"{Guid.NewGuid():N}.bin");
        List<long> offsets = [];
        long bucket = long.MinValue;
        long rowNumber = 0;
        long first = 0;
        long start = 0;
        long end = 0;
        long previousTime = long.MinValue;
        string? identity = null;
        string? context = null;
        string[] latest = [];
        double[] means = new double[fields.Length];
        double[] sine = new double[fields.Length];
        double[] cosine = new double[fields.Length];
        long[] counts = new long[fields.Length];

        try
        {
            await using (FileStream stream = new(cachePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using BinaryWriter writer = new(stream);
                while (!parser.EndOfData)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string[] values = parser.ReadFields()
                        ?? throw new InvalidDataException("Missing V2 CSV row.");
                    if (values.Length != fields.Length
                        || !DateTimeOffset.TryParse(values[timeColumn], CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTimeOffset time))
                    {
                        throw new InvalidDataException($"Malformed V2 data at row {rowNumber + 1}.");
                    }

                    if (time.UtcTicks < previousTime)
                    {
                        throw new InvalidDataException($"V2 time moves backwards at row {rowNumber + 1}.");
                    }

                    previousTime = time.UtcTicks;
                    long next = time.UtcTicks / (resolutionMilliseconds * TimeSpan.TicksPerMillisecond);
                    string nextIdentity = values[identityColumn];
                    string nextContext = string.Join('\0', contextFields.Select(field => values[field]));
                    if (bucket != long.MinValue
                        && (next != bucket || identity != nextIdentity || context != nextContext))
                    {
                        Flush();
                    }

                    rowNumber++;
                    if (bucket != next || identity != nextIdentity || context != nextContext)
                    {
                        bucket = next;
                        identity = nextIdentity;
                        context = nextContext;
                        first = rowNumber;
                        start = time.UtcTicks;
                        Array.Clear(means);
                        Array.Clear(counts);
                        Array.Clear(sine);
                        Array.Clear(cosine);
                    }

                    end = time.UtcTicks;
                    latest = values;
                    for (int field = 0; field < fields.Length; field++)
                    {
                        if (!numeric[field]
                            || states[field] >= 0 && values[states[field]] is not ("Valid" or "Supported" or "true")
                            || !double.TryParse(values[field], NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                            || !double.IsFinite(value))
                        {
                            continue;
                        }

                        counts[field]++;
                        means[field] += (value - means[field]) / counts[field];
                        if (circular[field])
                        {
                            double radians = value * Math.PI / 180d;
                            sine[field] += Math.Sin(radians);
                            cosine[field] += Math.Cos(radians);
                        }
                    }
                }

                if (bucket != long.MinValue)
                {
                    Flush();
                }

                void Flush()
                {
                    string[] result = (string[])latest.Clone();
                    for (int field = 0; field < fields.Length; field++)
                    {
                        if (numeric[field])
                        {
                            double mean = means[field];
                            if (circular[field])
                            {
                                mean = Math.Sqrt(sine[field] * sine[field] + cosine[field] * cosine[field])
                                    <= counts[field] * 1e-12
                                    ? double.NaN
                                    : (Math.Atan2(sine[field], cosine[field]) * 180d / Math.PI + 360d) % 360d;
                            }

                            result[field] = counts[field] > 0
                                && double.IsFinite(mean) ? mean.ToString("R", CultureInfo.InvariantCulture) : "";
                        }
                    }

                    offsets.Add(stream.Position);
                    writer.Write(JsonSerializer.Serialize(new V2TimelineRow(
                        start, end, first, rowNumber, result, (long[])counts.Clone())));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new(path, cachePath, fields, [.. offsets]);
        }
        catch
        {
            File.Delete(cachePath);
            throw;
        }
    }

    internal static string[] ReadSourceRow(string path, long row, CancellationToken cancellationToken)
    {
        using TextFieldParser parser = OpenParser(path);
        parser.ReadFields();
        for (long index = 1; !parser.EndOfData; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string[] values = parser.ReadFields() ?? throw new InvalidDataException("Missing V2 source row.");
            if (index == row)
            {
                return values;
            }
        }

        throw new InvalidDataException("The selected V2 source row is no longer present.");
    }

    private static TextFieldParser OpenParser(string path)
    {
        TextFieldParser parser = new(path) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
        parser.SetDelimiters(";");

        return parser;
    }

    private static bool IsMeasurement(string name, bool raw)
        => raw ? SensorV2Schema.Channels.Contains(name)
            : name is "dt_s" or "elapsed_s" or "confidence"
                or "imu_ax" or "imu_ay" or "imu_az" or "imu_gx" or "imu_gy" or "imu_gz"
                or "imu_mag_deg" or "imu_mag_conf" or "imu_pitch_deg" or "imu_roll_deg" or "imu_yaw_deg"
                or "gps_lat" or "gps_lon" or "gps_alt_m" or "gps_hacc_m" or "gps_vacc_m"
                or "gps_speed_mps" or "gps_course_deg" or "gps_conf" or "gps_age_s"
                or "pitch_deg" or "roll_deg" or "heading_deg"
                or "acc_long_mps2" or "acc_lat_mps2" or "acc_vert_mps2"
                or "speed_mps" or "pos_lat" or "pos_lon" or "obd_speed_mps" or "source_gps_speed_mps"
                or "ground_track_course_deg" or "road_grade_deg" or "road_bank_deg"
                || !raw && name.StartsWith("diag_", StringComparison.Ordinal)
                    && name is not ("diag_reject_reason" or "diag_has_forward_axis"
                        or "diag_mount_gate" or "diag_calib_transition");

    private static bool IsDirection(string name)
        => name is "compass_deg" or "incl_yaw_deg" or "imu_yaw_deg" or "imu_mag_deg"
            or "gps_course_deg" or "heading_deg" or "ground_track_course_deg";

    private static int StateColumn(string name, bool raw, Dictionary<string, int> columns)
    {
        string state = raw ? name + "_state" : name switch
        {
            "pitch_deg" or "roll_deg" => "body_attitude_state",
            "heading_deg" => "body_heading_state",
            "acc_long_mps2" or "acc_lat_mps2" or "acc_vert_mps2" => "acceleration_state",
            "speed_mps" => "speed_state",
            "pos_lat" or "pos_lon" => "position_state",
            "ground_track_course_deg" => "ground_track_course_state",
            "road_grade_deg" => "road_grade_state",
            "road_bank_deg" => "road_bank_state",
            "obd_speed_mps" => "obd_speed_state",
            "source_gps_speed_mps" => "source_gps_speed_state",
            "gps_speed_mps" => "gps_speed_valid",
            "gps_course_deg" => "gps_course_valid",
            "imu_gx" or "imu_gy" or "imu_gz" => "imu_has_rate",
            _ => ""
        };

        return columns.GetValueOrDefault(state, -1);
    }

    public void Dispose()
    {
        _reader.Dispose();
        File.Delete(_cachePath);
    }
}

/// <summary>Holds averaged channels and exact inclusive source-row bounds for one V2 time bucket.</summary>
internal sealed record V2TimelineRow(
    long StartTicks, long EndTicks, long FirstSourceRow, long LastSourceRow,
    string[] Values, long[] SampleCounts);
