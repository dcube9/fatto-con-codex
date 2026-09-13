using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace ViteKlub.Core.Data;

public sealed partial class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString() ?? throw new JsonException("Il timestamp non può essere nullo.");
        if (!OffsetSuffix().IsMatch(value))
        {
            throw new JsonException($"Il timestamp '{value}' deve includere Z o un offset esplicito.");
        }

        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset parsed))
        {
            throw new JsonException($"Il timestamp '{value}' non è un valore ISO 8601 valido.");
        }

        return parsed.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        string utc = value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        writer.WriteStringValue(string.Concat(utc.AsSpan(0, utc.Length - 6), "Z"));
    }

    [GeneratedRegex(@"(?:Z|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex OffsetSuffix();
}
