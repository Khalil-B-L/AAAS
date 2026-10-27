using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Aaas.Api.Tests.Outils;

/// <summary>Hôtes dérivés de la factory (même base), avec une configuration en mémoire surchargée.</summary>
public static class HoteDeTest
{
    /// <summary>Nouvel hôte, non démarré, dans l'environnement donné (sinon celui de la factory : <c>Testing</c>).</summary>
    public static WebApplicationFactory<Program> Configurer(this AaasFactory factory, Dictionary<string, string?> configuration,
        string? environnement = null) =>
        factory.WithWebHostBuilder(builder =>
        {
            if (environnement is not null)
                builder.UseEnvironment(environnement);
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration));
        });

    /// <summary>Nouvel hôte (seed rejoué) avec la configuration surchargée ; démarré avant le retour.</summary>
    public static WebApplicationFactory<Program> Redemarrer(this AaasFactory factory, Dictionary<string, string?> configuration,
        string? environnement = null)
    {
        var hote = factory.Configurer(configuration, environnement);
        _ = hote.Services;
        return hote;
    }

    /// <summary>Rejoue le seed avec la configuration de la factory : admin@aaas.test redevient le seul Admin, téléphone compris.</summary>
    public static void RestaurerAdminParDefaut(this AaasFactory factory)
    {
        using var redemarree = factory.Redemarrer([]);
    }
}
