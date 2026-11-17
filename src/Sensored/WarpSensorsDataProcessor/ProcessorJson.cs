using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WarpSensorsDataProcessor;

/// <summary>
///  Version-stable JSON options for analysis reports and fusion-reference manifests.
/// </summary>
internal static class ProcessorJson
{
    /// <summary>
    ///  Gets the serializer options used for machine-readable CLI JSON.
    /// </summary>
    internal static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        options.Converters.Add(new ExplicitDoubleConverter());
        options.Converters.Add(new ExplicitNullableDoubleConverter());

        return options;
    }

    /// <summary>
    ///  Writes finite numbers as JSON numbers and non-finite values as explicit strings.
    /// </summary>
    private sealed class ExplicitDoubleConverter : JsonConverter<double>
    {
        /// <inheritdoc/>
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => ReadDouble(ref reader);

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
            => WriteDouble(writer, value);
    }

    /// <summary>
    ///  Writes nullable doubles, preserving explicit non-finite values.
    /// </summary>
    private sealed class ExplicitNullableDoubleConverter : JsonConverter<double?>
    {
        /// <inheritdoc/>
        public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return ReadDouble(ref reader);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options)
        {
            if (value is not { } number)
            {
                writer.WriteNullValue();

                return;
            }

            WriteDouble(writer, number);
        }
    }

    private static double ReadDouble(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDouble();
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            string text = reader.GetString() ?? string.Empty;

            if (string.Equals(text, "NaN", StringComparison.Ordinal))
            {
                return double.NaN;
            }

            if (string.Equals(text, "Infinity", StringComparison.Ordinal))
            {
                return double.PositiveInfinity;
            }

            if (string.Equals(text, "-Infinity", StringComparison.Ordinal))
            {
                return double.NegativeInfinity;
            }

            return double.Parse(text, CultureInfo.InvariantCulture);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} for a double value.");
    }

    private static void WriteDouble(Utf8JsonWriter writer, double value)
    {
        if (double.IsNaN(value))
        {
            writer.WriteStringValue("NaN");

            return;
        }

        if (double.IsPositiveInfinity(value))
        {
            writer.WriteStringValue("Infinity");

            return;
        }

        if (double.IsNegativeInfinity(value))
        {
            writer.WriteStringValue("-Infinity");

            return;
        }

        writer.WriteNumberValue(value);
    }
}
