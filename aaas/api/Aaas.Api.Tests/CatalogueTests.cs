using System.Net;
using System.Text.Json;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Animaux.Queries;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;

namespace Aaas.Api.Tests;

/// <summary>
/// Filtres du catalogue (<c>GET /api/animaux</c>, <c>contrats-api.md</c> §2) : une couverture par ligne API de la matrice
/// de la story « Catalogue filtré ».
/// </summary>
[Collection(ApiCollection.Name)]
public class CatalogueTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    /// <summary>
    /// Quatre animaux qui diffèrent sur chaque critère, du plus récent (Rex) au plus ancien (Pacha) :
    /// <list type="bullet">
    /// <item>Rex : Chien, Male, Adulte, Grande ; enfants AVoir, chiens Oui, chats Non.</item>
    /// <item>Mimi : Chat, Femelle, Bebe, Petite ; enfants Oui, chiens Non, chats Oui.</item>
    /// <item>Bella : Chien, Femelle, Bebe, Grande ; enfants Non, chiens AVoir, chats AVoir.</item>
    /// <item>Pacha : Chat, Male, Senior, Moyenne ; enfants Non, chiens Oui, chats Non.</item>
    /// </list>
    /// </summary>
    private async Task InsererPensionnairesAsync() =>
        await factory.InsererAsync(
            Animaux.Nouveau("Rex", Animaux.CreeLeParDefaut.AddHours(4), Espece.Chien, Sexe.Male, TrancheAge.Adulte,
                Taille.Grande, enfants: Entente.AVoir, chiens: Entente.Oui, chats: Entente.Non),
            Animaux.Nouveau("Mimi", Animaux.CreeLeParDefaut.AddHours(3), Espece.Chat, Sexe.Femelle, TrancheAge.Bebe,
                Taille.Petite, enfants: Entente.Oui, chiens: Entente.Non, chats: Entente.Oui),
            Animaux.Nouveau("Bella", Animaux.CreeLeParDefaut.AddHours(2), Espece.Chien, Sexe.Femelle, TrancheAge.Bebe,
                Taille.Grande, enfants: Entente.Non, chiens: Entente.AVoir, chats: Entente.AVoir),
            Animaux.Nouveau("Pacha", Animaux.CreeLeParDefaut.AddHours(1), Espece.Chat, Sexe.Male, TrancheAge.Senior,
                Taille.Moyenne, enfants: Entente.Non, chiens: Entente.Oui, chats: Entente.Non));

    // --- Un filtre ------------------------------------------------------------------------------------

    [Theory]
    [InlineData("espece=Chat", new[] { "Mimi", "Pacha" })]
    [InlineData("espece=Chien", new[] { "Rex", "Bella" })]
    [InlineData("sexe=Male", new[] { "Rex", "Pacha" })]
    [InlineData("sexe=Femelle", new[] { "Mimi", "Bella" })]
    [InlineData("age=Bebe", new[] { "Mimi", "Bella" })]
    [InlineData("age=Senior", new[] { "Pacha" })]
    [InlineData("age=Jeune", new string[0])]
    [InlineData("taille=Grande", new[] { "Rex", "Bella" })]
    [InlineData("taille=Petite", new[] { "Mimi" })]
    public async Task Un_filtre_garde_l_egalite_en_creeLe_decroissant(string filtre, string[] attendus)
    {
        await InsererPensionnairesAsync();

        using var doc = await GetAsync($"/api/animaux?{filtre}");

        Assert.Equal(attendus, Noms(doc));
    }

    // --- Filtres combinés -----------------------------------------------------------------------------

    [Theory]
    [InlineData("espece=Chien&taille=Grande&sexe=Male", new[] { "Rex" })]
    [InlineData("espece=Chien&taille=Grande", new[] { "Rex", "Bella" })]
    [InlineData("espece=Chat&age=Bebe", new[] { "Mimi" })]
    [InlineData("age=Bebe&chats=Oui", new[] { "Mimi", "Bella" })]
    [InlineData("espece=Chat&enfants=Oui", new[] { "Mimi" })]
    [InlineData("enfants=Oui&chiens=Oui&chats=Oui", new string[0])]
    [InlineData("espece=Chat&sexe=Femelle&age=Bebe&taille=Petite&enfants=Oui&chats=Oui", new[] { "Mimi" })]
    public async Task Filtres_combines_donnent_l_intersection(string filtres, string[] attendus)
    {
        await InsererPensionnairesAsync();

        using var doc = await GetAsync($"/api/animaux?{filtres}");

        Assert.Equal(attendus, Noms(doc));
    }

    [Fact]
    public async Task Filtres_et_limite_gardent_les_n_plus_recents_parmi_les_correspondants()
    {
        await InsererPensionnairesAsync();

        using var doc = await GetAsync("/api/animaux?taille=Grande&limite=1");

        Assert.Equal(["Rex"], Noms(doc));
    }

    // --- Ententes -------------------------------------------------------------------------------------

    [Theory]
    [InlineData("enfants", new[] { "Rex", "Mimi" })]
    [InlineData("chiens", new[] { "Rex", "Bella", "Pacha" })]
    [InlineData("chats", new[] { "Mimi", "Bella" })]
    public async Task Entente_Oui_garde_Oui_et_AVoir_jamais_Non(string parametre, string[] attendus)
    {
        await InsererPensionnairesAsync();

        using var doc = await GetAsync($"/api/animaux?{parametre}=Oui");

        Assert.Equal(attendus, Noms(doc));
    }

    [Fact]
    public async Task Entente_enfants_Oui_sur_les_trois_valeurs()
    {
        await factory.InsererAsync(
            Animaux.Nouveau("Oui", Animaux.CreeLeParDefaut.AddHours(3), enfants: Entente.Oui),
            Animaux.Nouveau("AVoir", Animaux.CreeLeParDefaut.AddHours(2), enfants: Entente.AVoir),
            Animaux.Nouveau("Non", Animaux.CreeLeParDefaut.AddHours(1), enfants: Entente.Non));

        using var doc = await GetAsync("/api/animaux?enfants=Oui");

        Assert.Equal(["Oui", "AVoir"], Noms(doc));
    }

    // --- Valeur inconnue → 400 errors.<param> ---------------------------------------------------------

    public static TheoryData<string, string, string> ValeursInconnues => new()
    {
        { "espece=Lapin", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "espece=chat", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "espece=0", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "espece=", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "espece=Chat&espece=Chien", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "espece=Chat,Chien", "espece", ListerAnimauxHandler.MessageEnumeration<Espece>() },
        { "sexe=male", "sexe", ListerAnimauxHandler.MessageEnumeration<Sexe>() },
        { "sexe=Male&sexe=Male", "sexe", ListerAnimauxHandler.MessageEnumeration<Sexe>() },
        { "age=jeune", "age", ListerAnimauxHandler.MessageEnumeration<TrancheAge>() },
        { "age=1", "age", ListerAnimauxHandler.MessageEnumeration<TrancheAge>() },
        { "taille=Enorme", "taille", ListerAnimauxHandler.MessageEnumeration<Taille>() },
        { "enfants=AVoir", "enfants", ListerAnimauxHandler.MessageEntente },
        { "enfants=Non", "enfants", ListerAnimauxHandler.MessageEntente },
        { "enfants=oui", "enfants", ListerAnimauxHandler.MessageEntente },
        { "enfants=", "enfants", ListerAnimauxHandler.MessageEntente },
        { "chiens=AVoir", "chiens", ListerAnimauxHandler.MessageEntente },
        { "chats=Non", "chats", ListerAnimauxHandler.MessageEntente },
        { "chats=Oui&chats=Oui", "chats", ListerAnimauxHandler.MessageEntente },
    };

    [Theory]
    [MemberData(nameof(ValeursInconnues))]
    public async Task Valeur_inconnue_renvoie_400_errors_du_parametre(string filtre, string parametre, string message)
    {
        var response = await factory.CreateClient().GetAsync($"/api/animaux?{filtre}", Ct);

        var errors = await AssertValidationAsync(response);
        Assert.Equal(new Dictionary<string, string> { [parametre] = message }, errors);
    }

    [Fact]
    public async Task Plusieurs_valeurs_inconnues_renvoient_une_erreur_par_parametre()
    {
        var response = await factory.CreateClient().GetAsync("/api/animaux?espece=Lapin&age=jeune&enfants=AVoir&limite=0", Ct);

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["age", "enfants", "espece", "limite"], errors.Keys.Order());
    }

    // --- Statut effectif ------------------------------------------------------------------------------

    [Fact]
    public async Task Reserve_expire_correspondant_est_renvoye_Disponible()
    {
        var membre = await factory.CreerMembreAsync();
        var echu = Animaux.Nouveau("Echu", Animaux.CreeLeParDefaut.AddHours(3), Espece.Chat).Reserve();
        var orphelin = Animaux.Nouveau("Orphelin", Animaux.CreeLeParDefaut.AddHours(2), Espece.Chat).Reserve();
        var reserve = Animaux.Nouveau("Reserve", Animaux.CreeLeParDefaut.AddHours(1), Espece.Chat).Reserve();
        var chien = Animaux.Nouveau("Chien", espece: Espece.Chien).Reserve();
        await factory.InsererAsync(echu, orphelin, reserve, chien);
        var now = factory.Time.GetUtcNow();
        await factory.InsererAsync(
            Reservations.Echue(echu.Id, membre, now),
            Reservations.Active(reserve.Id, membre, now),
            Reservations.Echue(chien.Id, membre, now));

        using var doc = await GetAsync("/api/animaux?espece=Chat");

        Assert.Equal(["Echu", "Orphelin", "Reserve"], Noms(doc));
        Assert.Equal(["Disponible", "Disponible", "Reserve"], Statuts(doc));
    }

    [Fact]
    public async Task Adopte_correspondant_a_tous_les_filtres_n_est_jamais_renvoye()
    {
        await factory.InsererAsync(
            Animaux.Nouveau("Parti", Animaux.CreeLeParDefaut.AddHours(2), Espece.Chat, Sexe.Femelle, TrancheAge.Bebe,
                Taille.Petite, enfants: Entente.Oui, chiens: Entente.Oui, chats: Entente.Oui).Adopte(),
            Animaux.Nouveau("Reste", Animaux.CreeLeParDefaut.AddHours(1), Espece.Chat, Sexe.Femelle, TrancheAge.Bebe,
                Taille.Petite, enfants: Entente.Oui, chiens: Entente.Oui, chats: Entente.Oui));

        using var tous = await GetAsync("/api/animaux");
        using var filtres = await GetAsync(
            "/api/animaux?espece=Chat&sexe=Femelle&age=Bebe&taille=Petite&enfants=Oui&chiens=Oui&chats=Oui");

        Assert.Equal(["Reste"], Noms(tous));
        Assert.Equal(["Reste"], Noms(filtres));
    }

    [Fact]
    public async Task Aucun_resultat_renvoie_items_vide_et_serverNow()
    {
        await InsererPensionnairesAsync();

        using var doc = await GetAsync("/api/animaux?espece=Chat&taille=Grande");

        Assert.Equal(["items", "serverNow"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Empty(doc.RootElement.GetProperty("items").EnumerateArray());
    }

    private async Task<JsonDocument> GetAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await RequetesApi.LireJsonAsync(response);
    }

    private static string[] Noms(JsonDocument doc) =>
        doc.RootElement.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("nom").GetString()!).ToArray();

    private static string[] Statuts(JsonDocument doc) =>
        doc.RootElement.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("statut").GetString()!).ToArray();
}
