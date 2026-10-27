using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aaas.Api.Common.Json;

/// <summary>
/// Énumérations en JSON (AD-19) : écrites par leur nom C# et lues seulement par ce nom exact (même casse).
/// Entier, nombre en chaîne, liste à virgules ou casse différente → <see cref="JsonException"/> (400).
/// Remplace <see cref="JsonStringEnumConverter"/>, dont la lecture ignore la casse.
/// </summary>
public sealed class EnumEnChaineConverter : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        (JsonConverter)Activator.CreateInstance(typeof(Convertisseur<>).MakeGenericType(typeToConvert))!;

    private sealed class Convertisseur<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly FrozenDictionary<string, T> ParNom =
            Enum.GetValues<T>().ToFrozenDictionary(v => v.ToString(), StringComparer.Ordinal);

        private static readonly FrozenDictionary<T, string> ParValeur =
            Enum.GetValues<T>().ToFrozenDictionary(v => v, v => v.ToString());

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.String ? Lire(reader.GetString()) : throw Invalide();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(Nom(value));

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            Lire(reader.GetString());

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WritePropertyName(Nom(value));

        private static T Lire(string? nom) =>
            nom is not null && ParNom.TryGetValue(nom, out var valeur) ? valeur : throw Invalide();

        private static string Nom(T valeur) => ParValeur.TryGetValue(valeur, out var nom)
            ? nom
            : throw new JsonException($"La valeur {valeur} n'est pas définie dans {typeof(T).Name}.");

        private static JsonException Invalide() =>
            new($"Valeur attendue parmi : {string.Join(", ", ParNom.Keys.Order())}.");
    }
}
