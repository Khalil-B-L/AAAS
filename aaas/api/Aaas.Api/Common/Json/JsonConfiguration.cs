namespace Aaas.Api.Common.Json;

public static class JsonConfiguration
{
    /// <summary>
    /// Options JSON globales de l'API : dates UTC avec <c>Z</c> (AD-11), énumérations en chaîne de même casse,
    /// sans valeur entière (AD-19). Jamais par attribut.
    /// </summary>
    public static IServiceCollection AddJsonConventions(this IServiceCollection services) =>
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new UtcDateTimeOffsetConverter());
            options.SerializerOptions.Converters.Add(new EnumEnChaineConverter());
        });
}
