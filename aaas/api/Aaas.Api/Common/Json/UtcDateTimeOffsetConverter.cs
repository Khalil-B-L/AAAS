using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aaas.Api.Common.Json;

/// <summary>
/// <see cref="DateTimeOffset"/> en JSON (AD-11) : écrit toujours l'instant UTC avec le suffixe <c>Z</c> ;
/// refuse en lecture une date sans fuseau (ni <c>Z</c> ni décalage).
/// </summary>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // TryGetDateTime renvoie Kind = Unspecified quand la chaîne ISO 8601 ne porte aucun fuseau.
        if (reader.TokenType != JsonTokenType.String
            || !reader.TryGetDateTime(out var date)
            || date.Kind == DateTimeKind.Unspecified
            || !reader.TryGetDateTimeOffset(out var valeur))
        {
            throw new JsonException("Date attendue au format ISO 8601 avec fuseau (ex. 2026-01-15T10:00:00Z).");
        }
        return valeur.ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
