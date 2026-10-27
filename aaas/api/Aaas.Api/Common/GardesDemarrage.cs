namespace Aaas.Api.Common;

/// <summary>
/// Gardes de démarrage : chaque slice déclare la sienne à l'enregistrement de ses options (<see cref="AddGardeDemarrage"/>) ;
/// <c>Program</c> les exécute toutes, dans l'ordre d'enregistrement, par <see cref="VerifierConfiguration"/> avant <c>Migrate()</c>.
/// Une configuration invalide lève <see cref="Microsoft.Extensions.Options.OptionsValidationException"/> avant toute base.
/// </summary>
public static class GardesDemarrage
{
    public static IServiceCollection AddGardeDemarrage(this IServiceCollection services, Action<IServiceProvider> garde)
    {
        services.AddSingleton(new GardeDemarrage(garde));
        return services;
    }

    public static void VerifierConfiguration(this IServiceProvider services)
    {
        foreach (var garde in services.GetServices<GardeDemarrage>())
            garde.Verifier(services);
    }
}

/// <summary>Une garde de démarrage déclarée par une slice.</summary>
public sealed record GardeDemarrage(Action<IServiceProvider> Verifier);
