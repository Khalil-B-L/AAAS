using System.Net.Http.Json;
using System.Text.Json;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Tests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aaas.Api.Tests;

/// <summary>Animaux de démonstration (story 2.3) : Development seulement, et seulement sur une table vide.</summary>
[Collection(ApiCollection.Name)]
public class DemoAnimauxTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Development charge les user-secrets du poste : un Smtp:Password vide force le mode Journal, quel que soit le poste.
    private static readonly Dictionary<string, string?> ConfigurationDevelopment = new() { ["Smtp:Password"] = "" };

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    [Fact]
    public async Task En_Development_sur_une_table_vide_cree_4_chats_4_chiens_dont_2_adoptes()
    {
        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        var animaux = await LireAnimauxAsync();
        Assert.Equal(DemoAnimaux.Nombre, animaux.Count);
        Assert.Equal(4, animaux.Count(a => a.Espece == Espece.Chat));
        Assert.Equal(4, animaux.Count(a => a.Espece == Espece.Chien));
        Assert.Equal(2, animaux.Count(a => a.Statut == StatutAnimal.Adopte));
        Assert.All(animaux.Where(a => a.Statut == StatutAnimal.Adopte), a => Assert.NotNull(a.AdopteLe));
        Assert.DoesNotContain(animaux, a => a.Statut == StatutAnimal.Reserve);
        Assert.Equal(animaux.Count, animaux.Select(a => a.CreeLe).Distinct().Count());

        // L'accueil (4 plus récents non adoptés) montre deux chats et deux chiens.
        var accueil = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/api/animaux?limite=4", Ct);
        Assert.Equal(["Caramel", "Rex", "Mimi", "Filou"],
            accueil.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("nom").GetString()));
    }

    [Fact]
    public async Task En_Development_un_animal_existant_empeche_le_jeu_de_demonstration()
    {
        await factory.InsererAsync(Animaux.Nouveau("Existant"));

        using (factory.Redemarrer(ConfigurationDevelopment, "Development"))
        {
        }

        var animaux = await LireAnimauxAsync();
        Assert.Equal(["Existant"], animaux.Select(a => a.Nom));
    }

    [Fact]
    public async Task En_Testing_aucun_animal_de_demonstration()
    {
        using (factory.Redemarrer([]))
        {
        }

        Assert.Empty(await LireAnimauxAsync());
    }

    private async Task<List<Animal>> LireAnimauxAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>().AsNoTracking().ToListAsync(Ct);
    }
}
