using System.Net;
using System.Text.Json;
using Aaas.Api.Common;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;

namespace Aaas.Api.Tests;

/// <summary>
/// « Ils ont trouvé une famille » (<c>GET /api/animaux/adoptes</c>) : une couverture par ligne API de la matrice de la
/// story, et non-régression de la fiche <c>GET /api/animaux/:id</c> déclarée dans le même groupe.
/// </summary>
[Collection(ApiCollection.Name)]
public class AdoptesTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    /// <summary>Propriétés de <c>AnimalCarteDto</c>, dans l'ordre de <c>contrats-api.md</c> §6.</summary>
    private static readonly string[] ProprietesCarte =
        ["id", "nom", "espece", "sexe", "age", "taille", "statut", "photoPrincipaleUrl"];

    [Fact]
    public async Task Seuls_les_Adoptes_sont_renvoyes_en_adopteLe_decroissant()
    {
        var membre = await factory.CreerMembreAsync();
        var reserve = Animaux.Nouveau("Reserve", Animaux.CreeLeParDefaut.AddHours(9)).Reserve();
        // Créés dans un ordre différent de l'ordre d'adoption : le tri suit adopteLe, pas creeLe ni l'Id.
        await factory.InsererAsync(
            Animaux.Nouveau("Ancien", Animaux.CreeLeParDefaut.AddHours(3)).Adopte(Animaux.CreeLeParDefaut.AddDays(1)),
            Animaux.Nouveau("Recent", Animaux.CreeLeParDefaut.AddHours(1)).Adopte(Animaux.CreeLeParDefaut.AddDays(3)),
            Animaux.Nouveau("Milieu", Animaux.CreeLeParDefaut.AddHours(2)).Adopte(Animaux.CreeLeParDefaut.AddDays(2)),
            Animaux.Nouveau("Dispo1", Animaux.CreeLeParDefaut.AddHours(8)),
            Animaux.Nouveau("Dispo2", Animaux.CreeLeParDefaut.AddHours(7)),
            reserve);
        await factory.InsererAsync(Reservations.Active(reserve.Id, membre, factory.Time.GetUtcNow()));

        using var doc = await GetAsync();

        Assert.Equal(["Recent", "Milieu", "Ancien"], Noms(doc));
        Assert.All(Items(doc), c => Assert.Equal("Adopte", c.GetProperty("statut").GetString()));
        Assert.All(Items(doc), c => Assert.Equal(ProprietesCarte, c.EnumerateObject().Select(p => p.Name)));
    }

    [Fact]
    public async Task Meme_adopteLe_departage_par_Id_decroissant()
    {
        var premier = Animaux.Nouveau("Premier").Adopte(Animaux.CreeLeParDefaut.AddDays(1));
        await factory.InsererAsync(premier);
        var second = Animaux.Nouveau("Second").Adopte(Animaux.CreeLeParDefaut.AddDays(1));
        await factory.InsererAsync(second);

        using var doc = await GetAsync();

        Assert.True(second.Id > premier.Id);
        Assert.Equal(["Second", "Premier"], Noms(doc));
    }

    [Fact]
    public async Task Reserve_avec_reservation_echue_n_est_pas_un_Adopte()
    {
        var membre = await factory.CreerMembreAsync();
        var echu = Animaux.Nouveau("Echu").Reserve();
        await factory.InsererAsync(echu);
        await factory.InsererAsync(Reservations.Echue(echu.Id, membre, factory.Time.GetUtcNow()));

        using var doc = await GetAsync();

        Assert.Empty(Items(doc));
    }

    [Fact]
    public async Task Aucun_adopte_renvoie_items_vide_et_serverNow()
    {
        await factory.InsererAsync(Animaux.Nouveau("Dispo"), Animaux.Nouveau("Reserve").Reserve());

        using var doc = await GetAsync();

        Assert.Equal(["items", "serverNow"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Empty(Items(doc));
        Assert.Equal(factory.Time.GetUtcNow(), doc.RootElement.GetProperty("serverNow").GetDateTimeOffset());
    }

    [Fact]
    public async Task En_Development_Simba_et_Kira_sont_les_adoptes_de_demonstration()
    {
        // Development charge les user-secrets du poste : un Smtp:Password vide force le mode Journal.
        using (factory.Redemarrer(new Dictionary<string, string?> { ["Smtp:Password"] = "" }, "Development"))
        {
        }

        using var doc = await GetAsync();

        Assert.Equal(["Simba", "Kira"], Noms(doc)); // Simba adopté plus récemment que Kira (DemoAnimaux)
        Assert.All(Items(doc), c => Assert.Equal("Adopte", c.GetProperty("statut").GetString()));
    }

    // --- Non-régression de la fiche ---------------------------------------------------------------------

    [Fact]
    public async Task Fiche_par_id_reste_servie_pour_un_Adopte()
    {
        var simba = Animaux.Nouveau("Simba").Adopte(Animaux.CreeLeParDefaut.AddDays(1));
        await factory.InsererAsync(simba);

        var response = await factory.CreateClient().GetAsync($"/api/animaux/{simba.Id}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var fiche = await RequetesApi.LireJsonAsync(response);
        Assert.Equal("Simba", fiche.RootElement.GetProperty("nom").GetString());
        Assert.Equal("Adopte", fiche.RootElement.GetProperty("statut").GetString());
        Assert.Equal("2026-01-16T10:00:00Z", fiche.RootElement.GetProperty("adopteLe").GetString());
    }

    [Theory]
    [InlineData("/api/animaux/adopte")]
    [InlineData("/api/animaux/adoptes/1")]
    public async Task Route_voisine_de_adoptes_renvoie_404(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageIntrouvable);
    }

    private async Task<JsonDocument> GetAsync()
    {
        var response = await factory.CreateClient().GetAsync("/api/animaux/adoptes", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await RequetesApi.LireJsonAsync(response);
    }

    private static JsonElement[] Items(JsonDocument doc) =>
        doc.RootElement.GetProperty("items").EnumerateArray().ToArray();

    private static string[] Noms(JsonDocument doc) =>
        Items(doc).Select(c => c.GetProperty("nom").GetString()!).ToArray();
}
