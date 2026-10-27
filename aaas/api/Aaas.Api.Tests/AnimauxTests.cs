using System.Net;
using System.Text.Json;
using Aaas.Api.Features.Animaux.Queries;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;

namespace Aaas.Api.Tests;

/// <summary><c>GET /api/animaux?limite=</c> : une couverture par ligne de la matrice de la story.</summary>
[Collection(ApiCollection.Name)]
public class AnimauxTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Base partagée par la collection : chaque test part d'une table vide.
    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Derniers_pensionnaires_renvoie_les_4_plus_recents_en_creeLe_decroissant()
    {
        await factory.InsererAsync(Animaux.Serie(6));

        using var doc = await GetAsync("/api/animaux?limite=4");

        Assert.Equal(["items", "serverNow"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["Animal6", "Animal5", "Animal4", "Animal3"], Noms(doc));
        Assert.Equal(
            factory.Time.GetUtcNow(),
            DateTimeOffset.Parse(doc.RootElement.GetProperty("serverNow").GetString()!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.EndsWith("Z", doc.RootElement.GetProperty("serverNow").GetString());
    }

    [Fact]
    public async Task Carte_porte_exactement_les_proprietes_du_glossaire_en_chaine()
    {
        var animal = Animaux.Nouveau("Rex", espece: Features.Animaux.Espece.Chien, sexe: Features.Animaux.Sexe.Male,
            age: Features.Animaux.TrancheAge.Senior, taille: Features.Animaux.Taille.Grande).Reserve();
        await factory.InsererAsync(animal);

        using var doc = await GetAsync("/api/animaux");

        var carte = Assert.Single(doc.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(
            ["id", "nom", "espece", "sexe", "age", "taille", "statut", "photoPrincipaleUrl"],
            carte.EnumerateObject().Select(p => p.Name));
        Assert.Equal(animal.Id, carte.GetProperty("id").GetInt32());
        Assert.Equal("Rex", carte.GetProperty("nom").GetString());
        Assert.Equal("Chien", carte.GetProperty("espece").GetString());
        Assert.Equal("Male", carte.GetProperty("sexe").GetString());
        Assert.Equal("Senior", carte.GetProperty("age").GetString());
        Assert.Equal("Grande", carte.GetProperty("taille").GetString());
        Assert.Equal("Reserve", carte.GetProperty("statut").GetString());
        Assert.Equal(JsonValueKind.Null, carte.GetProperty("photoPrincipaleUrl").ValueKind);
    }

    [Fact]
    public async Task Adoptes_sont_exclus()
    {
        await factory.InsererAsync(
            Animaux.Nouveau("Adopte", Animaux.CreeLeParDefaut.AddHours(2)).Adopte(),
            Animaux.Nouveau("Disponible", Animaux.CreeLeParDefaut.AddHours(1)),
            Animaux.Nouveau("Reserve").Reserve());

        using var doc = await GetAsync("/api/animaux");

        Assert.Equal(["Disponible", "Reserve"], Noms(doc));
    }

    [Fact]
    public async Task Sans_limite_renvoie_tous_les_non_adoptes()
    {
        await factory.InsererAsync(Animaux.Serie(7));
        await factory.InsererAsync(Animaux.Nouveau("Parti").Adopte());

        using var doc = await GetAsync("/api/animaux");

        Assert.Equal(["Animal7", "Animal6", "Animal5", "Animal4", "Animal3", "Animal2", "Animal1"], Noms(doc));
    }

    [Fact]
    public async Task Catalogue_vide_renvoie_items_vide_et_serverNow()
    {
        using var doc = await GetAsync("/api/animaux?limite=4");

        Assert.Empty(doc.RootElement.GetProperty("items").EnumerateArray());
        Assert.True(doc.RootElement.TryGetProperty("serverNow", out _));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("50", 3)]
    public async Task Limite_aux_bornes_est_acceptee(string limite, int attendus)
    {
        await factory.InsererAsync(Animaux.Serie(3));

        using var doc = await GetAsync($"/api/animaux?limite={limite}");

        Assert.Equal(attendus, doc.RootElement.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("0")]
    [InlineData("51")]
    [InlineData("abc")]
    [InlineData("-1")]
    [InlineData("2.5")]
    [InlineData("")]
    public async Task Limite_invalide_renvoie_400_errors_limite(string limite)
    {
        var response = await factory.CreateClient().GetAsync($"/api/animaux?limite={limite}", Ct);

        var errors = await AssertValidationAsync(response);
        Assert.Equal(new Dictionary<string, string> { ["limite"] = ListerAnimauxHandler.MessageLimite }, errors);
    }

    private async Task<JsonDocument> GetAsync(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await RequetesApi.LireJsonAsync(response);
    }

    private static string[] Noms(JsonDocument doc) =>
        doc.RootElement.GetProperty("items").EnumerateArray().Select(c => c.GetProperty("nom").GetString()!).ToArray();
}
