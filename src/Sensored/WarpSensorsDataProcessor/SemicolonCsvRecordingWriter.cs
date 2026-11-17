using System.Text;
using WarpToolkit.Sensors;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Writes schema-compatible WARP semicolon-CSV recording files.
/// </summary>
internal sealed class SemicolonCsvRecordingWriter : IAsyncDisposable
{
    private const char Separator = ';';
    private readonly StreamWriter _writer;

    /// <summary>
    ///  Initializes a new recording writer.
    /// </summary>
    /// <param name="path">The new output file.</param>
    internal SemicolonCsvRecordingWriter(string path)
        : this(new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            65536,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
    {
    }

    /// <summary>
    ///  Initializes a recording writer that owns the supplied output stream.
    /// </summary>
    /// <param name="stream">The writable output stream.</param>
    internal SemicolonCsvRecordingWriter(Stream stream)
    {
        _writer = new StreamWriter(
            stream,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
        {
            NewLine = "\n"
        };
    }

    /// <summary>
    ///  Writes the stable schema header.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel writing.</param>
    internal async Task WriteHeaderAsync(CancellationToken cancellationToken)
    {
        string header = string.Join(
            Separator,
            SensorRecordingSchema.Fields.Select(static field => Quote(field.Name)));
        await _writer.WriteLineAsync(header.AsMemory(), cancellationToken);
    }

    /// <summary>
    ///  Writes one recording row.
    /// </summary>
    /// <param name="row">The row to write.</param>
    /// <param name="cancellationToken">A token used to cancel writing.</param>
    internal async Task WriteRowAsync(
        SensorEnvelopeRow row,
        CancellationToken cancellationToken)
    {
        StringBuilder builder = new(1024);

        for (int index = 0; index < SensorRecordingSchema.FieldCount; index++)
        {
            if (index > 0)
            {
                builder.Append(Separator);
            }

            SensorRecordingField field = SensorRecordingSchema.Fields[index];
            string value = SensorRecordingSchema.FormatValue(
                SensorRecordingSchema.GetValue(row, index),
                field.Type);
            builder.Append(Quote(value));
        }

        await _writer.WriteLineAsync(builder.ToString().AsMemory(), cancellationToken);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
        => _writer.DisposeAsync();

    private static string Quote(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        bool needsQuoting = value.IndexOfAny([Separator, '"', '\r', '\n']) >= 0;

        return needsQuoting
            ? string.Concat("\"", value.Replace("\"", "\"\""), "\"")
            : value;
    }
}
