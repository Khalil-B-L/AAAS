using System.Net;
using System.Text.Json;
using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Tests.Outils;
using Microsoft.Extensions.DependencyInjection;
using static Aaas.Api.Tests.Outils.AssertionsErreur;

namespace Aaas.Api.Tests;

/// <summary>Fiche animal (<c>GET /api/animaux/:id</c>) : une couverture par ligne API de la matrice de la story « Fiche animal ».</summary>
[Collection(ApiCollection.Name)]
public class FicheAnimalTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    /// <summary>Propriétés de <c>AnimalDetailDto</c>, dans l'ordre de <c>contrats-api.md</c> §6.</summary>
    private static readonly string[] Proprietes =
    [
        "id", "nom", "espece", "sexe", "age", "taille", "statut", "photoPrincipaleUrl",
        "description", "vaccine", "sterilise", "puce", "ententeEnfants", "ententeChiens", "ententeChats",
        "creeLe", "adopteLe", "photos",
    ];

    /// <summary>Insère des photos aux ordres donnés (dans cet ordre d'insertion) ; clé = <c>photo{ordre}.jpg</c>.</summary>
    private async Task InsererPhotosAsync(int animalId, params int[] ordres)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<PhotoAnimal>().AddRange(ordres.Select(o => new PhotoAnimal(animalId, $"photo{o}.jpg", o)));
        await db.SaveChangesAsync(Ct);
    }

    private async Task<JsonElement> FicheAsync(int id)
    {
        var response = await factory.CreateClient().GetAsync($"/api/animaux/{id}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Fiche_Disponible_renvoie_tous_les_champs_et_les_photos_triees_par_ordre()
    {
        var caramel = Animaux.Nouveau("Caramel");
        await factory.InsererAsync(caramel);
        await InsererPhotosAsync(caramel.Id, 3, 1, 2);

        var fiche = await FicheAsync(caramel.Id);

        Assert.Equal(Proprietes, fiche.EnumerateObject().Select(p => p.Name));
        Assert.Equal(caramel.Id, fiche.GetProperty("id").GetInt32());
        Assert.Equal("Caramel", fiche.GetProperty("nom").GetString());
        Assert.Equal("Chat", fiche.GetProperty("espece").GetString());
        Assert.Equal("Femelle", fiche.GetProperty("sexe").GetString());
        Assert.Equal("Jeune", fiche.GetProperty("age").GetString());
        Assert.Equal("Moyenne", fiche.GetProperty("taille").GetString());
        Assert.Equal("Disponible", fiche.GetProperty("statut").GetString());
        Assert.Equal("/fichiers/photo1.jpg", fiche.GetProperty("photoPrincipaleUrl").GetString());
        Assert.Equal("Caramel attend sa famille.", fiche.GetProperty("description").GetString());
        Assert.True(fiche.GetProperty("vaccine").GetBoolean());
        Assert.False(fiche.GetProperty("sterilise").GetBoolean());
        Assert.True(fiche.GetProperty("puce").GetBoolean());
        Assert.Equal("Oui", fiche.GetProperty("ententeEnfants").GetString());
        Assert.Equal("AVoir", fiche.GetProperty("ententeChiens").GetString());
        Assert.Equal("Non", fiche.GetProperty("ententeChats").GetString());
        Assert.Equal("2026-01-15T10:00:00Z", fiche.GetProperty("creeLe").GetString());
        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("adopteLe").ValueKind);

        var photos = fiche.GetProperty("photos").EnumerateArray().ToArray();
        Assert.All(photos, p => Assert.Equal(["id", "url", "ordre"], p.EnumerateObject().Select(x => x.Name)));
        Assert.Equal([1, 2, 3], photos.Select(p => p.GetProperty("ordre").GetInt32()));
        Assert.Equal(["/fichiers/photo1.jpg", "/fichiers/photo2.jpg", "/fichiers/photo3.jpg"],
            photos.Select(p => p.GetProperty("url").GetString()));
        Assert.All(photos, p => Assert.True(p.GetProperty("id").GetInt32() > 0));
    }

    [Fact]
    public async Task Reserve_avec_reservation_active_reste_Reserve()
    {
        var membre = await factory.CreerMembreAsync();
        var rex = Animaux.Nouveau("Rex", espece: Espece.Chien).Reserve();
        await factory.InsererAsync(rex);
        await factory.InsererAsync(Reservations.Active(rex.Id, membre, factory.Time.GetUtcNow()));

        var fiche = await FicheAsync(rex.Id);

        Assert.Equal("Reserve", fiche.GetProperty("statut").GetString());
        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("adopteLe").ValueKind);
    }

    [Fact]
    public async Task Reserve_sans_reservation_active_est_renvoye_Disponible()
    {
        var membre = await factory.CreerMembreAsync();
        var echu = Animaux.Nouveau("Echu").Reserve();
        var orphelin = Animaux.Nouveau("Orphelin").Reserve();
        await factory.InsererAsync(echu, orphelin);
        await factory.InsererAsync(Reservations.Echue(echu.Id, membre, factory.Time.GetUtcNow()));

        Assert.Equal("Disponible", (await FicheAsync(echu.Id)).GetProperty("statut").GetString());
        Assert.Equal("Disponible", (await FicheAsync(orphelin.Id)).GetProperty("statut").GetString());
    }

    [Fact]
    public async Task Adopte_est_renvoye_avec_adopteLe()
    {
        var adopteLe = new DateTimeOffset(2026, 1, 10, 9, 30, 0, TimeSpan.Zero);
        var pacha = Animaux.Nouveau("Pacha").Adopte(adopteLe);
        await factory.InsererAsync(pacha);

        var fiche = await FicheAsync(pacha.Id);

        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal("2026-01-10T09:30:00Z", fiche.GetProperty("adopteLe").GetString());
    }

    [Fact]
    public async Task Sans_photo_renvoie_photos_vide_et_photoPrincipaleUrl_null()
    {
        var mimi = Animaux.Nouveau("Mimi");
        await factory.InsererAsync(mimi);

        var fiche = await FicheAsync(mimi.Id);

        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("photoPrincipaleUrl").ValueKind);
        Assert.Empty(fiche.GetProperty("photos").EnumerateArray());
    }

    [Theory]
    [InlineData("/api/animaux/999999")]
    [InlineData("/api/animaux/abc")]
    [InlineData("/api/animaux/0")]
    [InlineData("/api/animaux/99999999999")]
    public async Task Animal_inconnu_renvoie_404_error(string url)
    {
        var response = await factory.CreateClient().GetAsync(url, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageIntrouvable);
    }

    [Fact]
    public async Task Fiche_est_publique_et_n_expose_pas_un_autre_animal()
    {
        var caramel = Animaux.Nouveau("Caramel");
        var rex = Animaux.Nouveau("Rex", espece: Espece.Chien);
        await factory.InsererAsync(caramel, rex);
        await InsererPhotosAsync(rex.Id, 1);

        var fiche = await FicheAsync(caramel.Id);

        Assert.Equal("Caramel", fiche.GetProperty("nom").GetString());
        Assert.Empty(fiche.GetProperty("photos").EnumerateArray());
    }
}
