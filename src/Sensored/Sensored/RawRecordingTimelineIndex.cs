using System.Globalization;
using System.Text.Json;
using WarpToolkit.Sensors.Fusion;

namespace Sensored;

/// <summary>
///  A compact, disposable disk-backed index of time-aligned recording snapshots.
/// </summary>
/// <remarks>
///  The index contains only presentation values and source provenance, not recording payloads.
///  Pages are read on demand so even multi-million-row recordings have bounded UI memory use.
/// </remarks>
internal sealed class RawRecordingTimelineIndex : IDisposable
{
    private const int PageSize = 128;
    private const int RecordSize = (sizeof(long) * 4) + (sizeof(int) * 2) + (sizeof(double) * 9);
    private const double StandardGravity = 9.80665d;

    private readonly string _fileName;
    private readonly FileStream _stream;
    private readonly BinaryReader _reader;
    private readonly string[] _parts;
    private readonly string[] _partPaths;
    private TimelineSnapshot[] _page = [];
    private int _pageStart = -1;

    private RawRecordingTimelineIndex(string fileName, string[] parts, int count)
    {
        _fileName = fileName;
        _partPaths = parts;
        _parts = [.. parts.Select(static part => Path.GetFileName(part))];
        Count = count;
        _stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 64 * 1024, FileOptions.RandomAccess);
        _reader = new BinaryReader(_stream);
    }

    internal int Count { get; }

    internal string PartName(int index) => _parts[index];

    internal string PartPath(int index) => _partPaths[index];

    internal TimelineSnapshot Get(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        if (index < _pageStart || index >= _pageStart + _page.Length)
        {
            _pageStart = index / PageSize * PageSize;
            int length = Math.Min(PageSize, Count - _pageStart);
            _page = new TimelineSnapshot[length];
            _stream.Position = (long)_pageStart * RecordSize;

            for (int i = 0; i < length; i++)
            {
                _page[i] = Read(_reader);
            }
        }

        return _page[index - _pageStart];
    }

    internal static async Task<RawRecordingTimelineIndex> BuildAsync(
        string path,
        CancellationToken cancellationToken,
        int resolutionMilliseconds = 250)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(resolutionMilliseconds, 50);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(resolutionMilliseconds, 1000);
        long bucketTicks = TimeSpan.TicksPerMillisecond * resolutionMilliseconds;
        bool isFolder = Directory.Exists(path);
        string[] files = isFolder
            ? FindParts(path)
            : [path];

        if (files.Length == 0)
        {
            throw new FileNotFoundException("No finalized recording parts were found.", path);
        }

        Dictionary<string, (long FirstRow, long LastRow)>? expectedParts = isFolder
            ? ValidateFolder(path, files)
            : null;

        string cacheFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Sensored", "TimelineIndex");

        Directory.CreateDirectory(cacheFolder);
        string fileName = Path.Combine(cacheFolder, $"{Guid.NewGuid():N}.bin");
        int count = 0;
        IFusionEngine fusion = FusionEngineFactory.CreateDefault();
        double[] means = new double[9];
        double[] sine = new double[9];
        double[] cosine = new double[9];
        long[] samples = new long[9];
        int speedSource = 0;
        long bucket = long.MinValue;
        long firstRow = 0;
        long lastRow = 0;
        long firstTicks = 0;
        long lastTicks = 0;
        int bucketPart = 0;

        try
        {
            await using (FileStream stream = new(fileName, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, bufferSize: 64 * 1024, FileOptions.SequentialScan))
            {
                using BinaryWriter writer = new(stream);

                for (int part = 0; part < files.Length; part++)
                {
                    long partRows = 0;
                    long expectedFirstRow = 0;
                    long expectedLastRow = 0;
                    bool expectRows = expectedParts is not null
                        || TryReadRowBounds(files[part], out expectedFirstRow, out expectedLastRow);

                    if (expectedParts is not null)
                    {
                        (expectedFirstRow, expectedLastRow) = expectedParts[Path.GetFileName(files[part])];
                    }

                    await foreach (SensorEnvelopeRow row in SensorRecordingReader
                        .ReadPartAsync(files[part], cancellationToken).ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (expectRows && row.Row != expectedFirstRow + partRows)
                        {
                            throw new InvalidDataException(
                                $"Recording part '{Path.GetFileName(files[part])}' has missing or unexpected source rows.");
                        }

                        partRows++;
                        DateTimeOffset arrival = row.Envelope.ArrivalTimestamp;
                        long ticks = arrival.UtcTicks;
                        long nextBucket = ticks / bucketTicks;

                        if (bucket != long.MinValue && (nextBucket != bucket || bucketPart != part))
                        {
                            WriteSnapshot();
                        }

                        if (nextBucket != bucket || bucketPart != part)
                        {
                            bucket = nextBucket;
                            bucketPart = part;
                            firstRow = row.Row;
                            firstTicks = ticks;
                            Array.Clear(means);
                            Array.Clear(samples);
                            Array.Clear(sine);
                            Array.Clear(cosine);
                            speedSource = 0;
                        }

                        lastRow = row.Row;
                        lastTicks = ticks;

                        FusionEstimate? output = fusion.Process(row.Envelope);

                        if (row.Envelope.IsValid && row.Envelope.Geolocation is { } fix)
                        {
                            if (fix.CourseValid)
                            {
                                Add(1, fix.CourseDegrees);
                            }

                            if (fix.SpeedValid && output?.IsSpeedSupported != true)
                            {
                                Add(0, fix.SpeedMetersPerSecond);
                                speedSource = 1;
                            }
                        }

                        if (output is not null)
                        {
                            if (output.IsSpeedSupported)
                            {
                                Add(0, output.Speed);
                                speedSource = 2;
                            }

                            if (output.IsAttitudeSupported)
                            {
                                Add(2, output.Pitch);
                                Add(3, output.Roll);
                            }

                            if (output.IsHeadingSupported)
                            {
                                Add(4, output.Heading);
                            }

                            if (output.IsAccelerationSupported)
                            {
                                Add(5, output.LongitudinalAccel / StandardGravity);
                                Add(6, Math.Max(-output.VerticalAccel / StandardGravity, 0));
                                Add(7, Math.Max(output.VerticalAccel / StandardGravity, 0));
                                Add(8, output.LateralAccel / StandardGravity);
                            }
                        }
                    }

                    if (partRows == 0
                        || expectRows && partRows != expectedLastRow - expectedFirstRow + 1)
                    {
                        throw new InvalidDataException(
                            $"Recording part '{Path.GetFileName(files[part])}' is empty or incomplete.");
                    }
                }

                if (bucket != long.MinValue)
                {
                    WriteSnapshot();
                }

                void WriteSnapshot()
                {
                    TimelineSnapshot snapshot = new(
                        firstTicks, lastTicks, firstRow, lastRow, bucketPart,
                        speedSource,
                        Mean(0), Mean(1), Mean(2), Mean(3), Mean(4),
                        Mean(5), Mean(6), Mean(7), Mean(8));

                    Write(writer, snapshot);

                    if (count == int.MaxValue)
                    {
                        throw new InvalidDataException("The recording exceeds the viewer's maximum timeline row count.");
                    }

                    count++;
                }

                double Mean(int channel)
                {
                    if (samples[channel] == 0)
                    {
                        return double.NaN;
                    }

                    if (channel is 1 or 4)
                    {
                        return Math.Sqrt(sine[channel] * sine[channel] + cosine[channel] * cosine[channel])
                            <= samples[channel] * 1e-12
                            ? double.NaN
                            : (Math.Atan2(sine[channel], cosine[channel]) * 180d / Math.PI + 360d) % 360d;
                    }

                    return means[channel];
                }

                void Add(int channel, double value)
                {
                    if (double.IsFinite(value))
                    {
                        samples[channel]++;
                        means[channel] += (value - means[channel]) / samples[channel];
                        if (channel is 1 or 4)
                        {
                            double radians = value * Math.PI / 180d;
                            sine[channel] += Math.Sin(radians);
                            cosine[channel] += Math.Cos(radians);
                        }
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            return new RawRecordingTimelineIndex(fileName, files, count);
        }
        catch
        {
            File.Delete(fileName);
            throw;
        }
    }

    private static string[] FindParts(string folder)
    {
        string[] extensions = ["csv", "jsonl", "compact.json"];

        foreach (string extension in extensions)
        {
            string[] files = [.. Directory.EnumerateFiles(folder, $"part-*.{extension}")
                .Where(static file => !Path.GetFileName(file).Contains("--writing", StringComparison.Ordinal))
                .OrderBy(static file => PartNumber(file))
                .ThenBy(static file => file, StringComparer.Ordinal)];

            if (files.Length > 0)
            {
                return files;
            }
        }

        return [];
    }

    private static Dictionary<string, (long FirstRow, long LastRow)> ValidateFolder(
        string folder,
        string[] files)
    {
        if (Directory.EnumerateFiles(folder, "part-*")
            .Any(static file => Path.GetFileName(file).Contains("--writing", StringComparison.Ordinal))
            || File.Exists(Path.Combine(folder, "recording.manifest.json.writing")))
        {
            throw new InvalidDataException("This recording still has unfinished part or manifest files.");
        }

        string manifestPath = Path.Combine(folder, "recording.manifest.json");

        if (!File.Exists(manifestPath))
        {
            throw new InvalidDataException("A finalized recording manifest is required to open a folder.");
        }

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement root = document.RootElement;

        Dictionary<string, (long FirstRow, long LastRow)> expected =
            [with(StringComparer.OrdinalIgnoreCase)];

        long totalRows = 0;

        foreach (JsonElement part in root.GetProperty("parts").EnumerateArray())
        {
            long first = part.GetProperty("firstRow").GetInt64();
            long last = part.GetProperty("lastRow").GetInt64();

            if (last < first)
            {
                throw new InvalidDataException("The recording manifest contains an invalid part row range.");
            }

            totalRows = checked(totalRows + last - first + 1);

            foreach (JsonElement name in part.GetProperty("files").EnumerateArray())
            {
                string fileName = name.GetString()
                    ?? throw new InvalidDataException("The recording manifest contains an empty file name.");

                if (Path.GetFileName(fileName) != fileName
                    || !File.Exists(Path.Combine(folder, fileName))
                    || !expected.TryAdd(fileName, (first, last)))
                {
                    throw new InvalidDataException(
                        $"Recording part '{fileName}' is missing, duplicated, or incomplete.");
                }
            }
        }

        if (totalRows == 0 || totalRows != root.GetProperty("rowsWritten").GetInt64()
            || files.Any(file => !expected.ContainsKey(Path.GetFileName(file)))
            || files.Length != root.GetProperty("parts").GetArrayLength())
        {
            throw new InvalidDataException("The recording folder does not match its finalized manifest.");
        }

        return expected;
    }

    private static long PartNumber(string file)
    {
        string name = Path.GetFileName(file);
        int end = name.IndexOf('-', 5);

        return end >= 0 && long.TryParse(name.AsSpan(5, end - 5), out long index)
            ? index : long.MaxValue;
    }

    private static bool TryReadRowBounds(string file, out long firstRow, out long lastRow)
    {
        ReadOnlySpan<char> name = Path.GetFileName(file);
        int marker = name.IndexOf("--rows-", StringComparison.Ordinal);

        if (marker < 0)
        {
            firstRow = 0;
            lastRow = 0;
            return false;
        }

        ReadOnlySpan<char> bounds = name[(marker + "--rows-".Length)..];
        int divider = bounds.IndexOf('-');
        int suffix = bounds.IndexOf("--", StringComparison.Ordinal);

        if (divider < 0 || suffix <= divider
            || !long.TryParse(bounds[..divider], NumberStyles.None,
                CultureInfo.InvariantCulture, out firstRow)
            || !long.TryParse(bounds[(divider + 1)..suffix], NumberStyles.None,
                CultureInfo.InvariantCulture, out lastRow)
            || lastRow < firstRow)
        {
            throw new InvalidDataException($"Recording part '{Path.GetFileName(file)}' has invalid row bounds.");
        }

        return true;
    }

    private static void Write(BinaryWriter writer, TimelineSnapshot row)
    {
        writer.Write(row.StartTicks);
        writer.Write(row.EndTicks);
        writer.Write(row.FirstSourceRow);
        writer.Write(row.LastSourceRow);
        writer.Write(row.PartIndex);
        writer.Write(row.SpeedSource);
        writer.Write(row.Speed);
        writer.Write(row.Course);
        writer.Write(row.Pitch);
        writer.Write(row.Roll);
        writer.Write(row.Yaw);
        writer.Write(row.Forward);
        writer.Write(row.Up);
        writer.Write(row.Down);
        writer.Write(row.Side);
    }

    private static TimelineSnapshot Read(BinaryReader reader)
        => new(
            reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64(), reader.ReadInt64(),
            reader.ReadInt32(), reader.ReadInt32(), reader.ReadDouble(), reader.ReadDouble(),
            reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble(),
            reader.ReadDouble(), reader.ReadDouble(), reader.ReadDouble());

    public void Dispose()
    {
        _reader.Dispose();
        File.Delete(_fileName);
    }
}

/// <summary>
///  Holds one time-bucket's source row bounds and validated vehicle readings.
/// </summary>
internal readonly record struct TimelineSnapshot(
    long StartTicks,
    long EndTicks,
    long FirstSourceRow,
    long LastSourceRow,
    int PartIndex,
    int SpeedSource,
    double Speed,
    double Course,
    double Pitch,
    double Roll,
    double Yaw,
    double Forward,
    double Up,
    double Down,
    double Side)
{
    internal DateTimeOffset Start => new(StartTicks, TimeSpan.Zero);
    internal DateTimeOffset End => new(EndTicks, TimeSpan.Zero);
}
