using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Animaux.Commands;
using Aaas.Api.Features.Reservations;
using Aaas.Api.Tests.Outils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;

namespace Aaas.Api.Tests;

/// <summary>
/// Forçage du statut d'un animal (<c>POST /api/admin/animaux/:id/statut</c>) : une couverture par ligne de la matrice
/// de la story « Forcer le statut d'un animal (API) ».
/// </summary>
[Collection(ApiCollection.Name)]
public class ForcerStatutTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    private static string Route(int id) => $"/api/admin/animaux/{id}/statut";

    private Task<string> JetonAdminAsync() => factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);

    private static Dictionary<string, object?> Corps(string statut, string statutAttendu) => new()
    {
        ["statut"] = statut,
        ["statutAttendu"] = statutAttendu,
    };

    private static Task<HttpResponseMessage> PostAsync(WebApplicationFactory<Program> hote, string? jeton, string url, object corps)
    {
        var client = hote.CreateClient();
        if (jeton is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return client.PostAsync(url, RequetesApi.JsonContent(corps as string ?? JsonSerializer.Serialize(corps)), Ct);
    }

    private async Task<HttpResponseMessage> ForcerAsync(int id, object corps) =>
        await PostAsync(factory, await JetonAdminAsync(), Route(id), corps);

    private static async Task<JsonElement> LireAsync(HttpResponseMessage response, HttpStatusCode attendu)
    {
        Assert.Equal(attendu, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        return doc.RootElement.Clone();
    }

    private async Task<Animal> AnimalEnBaseAsync(int id)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>().AsNoTracking()
            .SingleAsync(a => a.Id == id, Ct);
    }

    private async Task<Reservation> ReservationEnBaseAsync(int id)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Reservation>().AsNoTracking()
            .SingleAsync(r => r.Id == id, Ct);
    }

    private async Task<int[]> IdsAsync(string url)
    {
        var corps = await LireAsync(await factory.CreateClient().GetAsync(url, Ct), HttpStatusCode.OK);
        return corps.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()).ToArray();
    }

    // --- Succès ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Adopter_un_Disponible_pose_adopteLe_et_le_sort_du_catalogue()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);

        var fiche = await LireAsync(await ForcerAsync(animal.Id, Corps("Adopte", "Disponible")), HttpStatusCode.OK);

        Assert.Equal(animal.Id, fiche.GetProperty("id").GetInt32());
        Assert.Equal("Filou", fiche.GetProperty("nom").GetString());
        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal(factory.Time.GetUtcNow(), fiche.GetProperty("adopteLe").GetDateTimeOffset());
        Assert.True(fiche.TryGetProperty("photos", out _));

        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Adopte, enBase.Statut);
        Assert.Equal(factory.Time.GetUtcNow(), enBase.AdopteLe);
        Assert.Contains(animal.Id, await IdsAsync("/api/animaux/adoptes"));
        Assert.DoesNotContain(animal.Id, await IdsAsync("/api/animaux"));
    }

    [Fact]
    public async Task Remettre_disponible_un_Adopte_efface_adopteLe()
    {
        var animal = Animaux.Nouveau("Filou").Adopte(Animaux.CreeLeParDefaut.AddDays(1));
        await factory.InsererAsync(animal);

        var fiche = await LireAsync(await ForcerAsync(animal.Id, Corps("Disponible", "Adopte")), HttpStatusCode.OK);

        Assert.Equal("Disponible", fiche.GetProperty("statut").GetString());
        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("adopteLe").ValueKind);
        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Disponible, enBase.Statut);
        Assert.Null(enBase.AdopteLe);
        Assert.Contains(animal.Id, await IdsAsync("/api/animaux"));
    }

    [Fact]
    public async Task Adopter_de_nouveau_un_Adopte_garde_adopteLe()
    {
        var adopteLe = Animaux.CreeLeParDefaut.AddDays(1);
        var animal = Animaux.Nouveau("Filou").Adopte(adopteLe);
        await factory.InsererAsync(animal);
        factory.Time.Advance(TimeSpan.FromMinutes(1));

        var fiche = await LireAsync(await ForcerAsync(animal.Id, Corps("Adopte", "Adopte")), HttpStatusCode.OK);

        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal(adopteLe, fiche.GetProperty("adopteLe").GetDateTimeOffset());
        Assert.Equal(adopteLe, (await AnimalEnBaseAsync(animal.Id)).AdopteLe);
    }

    [Fact]
    public async Task Reserve_echu_attendu_Disponible_est_adopte_et_sa_reservation_reste_telle_quelle()
    {
        var animal = Animaux.Nouveau("Filou").Reserve();
        await factory.InsererAsync(animal);
        var membre = await factory.CreerMembreAsync();
        var reservation = Reservations.Echue(animal.Id, membre, factory.Time.GetUtcNow());
        await factory.InsererAsync(reservation);

        var fiche = await LireAsync(await ForcerAsync(animal.Id, Corps("Adopte", "Disponible")), HttpStatusCode.OK);

        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal(factory.Time.GetUtcNow(), fiche.GetProperty("adopteLe").GetDateTimeOffset());
        Assert.Equal(StatutAnimal.Adopte, (await AnimalEnBaseAsync(animal.Id)).Statut);
        Assert.Equal(StatutReservation.EnAttente, (await ReservationEnBaseAsync(reservation.Id)).Statut);
    }

    // --- Validation -----------------------------------------------------------------------------------

    [Fact]
    public async Task Forcer_Reserve_renvoie_400_errors_statut_et_ne_modifie_rien()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);

        var errors = await AssertValidationAsync(await ForcerAsync(animal.Id, Corps("Reserve", "Disponible")));

        Assert.Equal(["statut"], errors.Keys);
        Assert.Equal(StatutAnimal.Disponible, (await AnimalEnBaseAsync(animal.Id)).Statut);
    }

    [Fact]
    public async Task Corps_vide_renvoie_400_errors_statut_et_statutAttendu()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);

        var errors = await AssertValidationAsync(await ForcerAsync(animal.Id, "{}"));

        Assert.Equal(["statut", "statutAttendu"], errors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(StatutAnimal.Disponible, (await AnimalEnBaseAsync(animal.Id)).Statut);
    }

    [Theory]
    [InlineData("statut", "adopte")]
    [InlineData("statut", "Inconnu")]
    [InlineData("statut", 2)]
    [InlineData("statutAttendu", "disponible")]
    public async Task Enumeration_invalide_renvoie_400_et_ne_modifie_rien(string champ, object valeur)
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var corps = Corps("Adopte", "Disponible");
        corps[champ] = valeur;

        var response = await ForcerAsync(animal.Id, corps);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(StatutAnimal.Disponible, (await AnimalEnBaseAsync(animal.Id)).Statut);
    }

    [Fact]
    public async Task Corps_invalide_sur_animal_inconnu_renvoie_400_avant_404()
    {
        var errors = await AssertValidationAsync(await ForcerAsync(999999, Corps("Reserve", "Disponible")));

        Assert.Equal(["statut"], errors.Keys);
    }

    // --- Refus ----------------------------------------------------------------------------------------

    [Fact]
    public async Task Statut_attendu_perime_renvoie_409_modifie_ailleurs_et_ne_modifie_rien()
    {
        var adopteLe = Animaux.CreeLeParDefaut.AddDays(1);
        var animal = Animaux.Nouveau("Filou").Adopte(adopteLe);
        await factory.InsererAsync(animal);

        var response = await ForcerAsync(animal.Id, Corps("Disponible", "Disponible"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageModifieAilleurs, "modifie-ailleurs");
        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Adopte, enBase.Statut);
        Assert.Equal(adopteLe, enBase.AdopteLe);
    }

    [Fact]
    public async Task Reservation_active_renvoie_409_reservation_active_et_ne_modifie_rien()
    {
        var animal = Animaux.Nouveau("Filou").Reserve();
        await factory.InsererAsync(animal);
        var membre = await factory.CreerMembreAsync();
        var reservation = Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow());
        await factory.InsererAsync(reservation);

        var response = await ForcerAsync(animal.Id, Corps("Adopte", "Reserve"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageReservationActive, "reservation-active");
        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Reserve, enBase.Statut);
        Assert.Null(enBase.AdopteLe);
        Assert.Equal(StatutReservation.EnAttente, (await ReservationEnBaseAsync(reservation.Id)).Statut);
    }

    [Fact]
    public async Task Attendu_perime_est_verifie_avant_la_reservation_active()
    {
        var animal = Animaux.Nouveau("Filou").Reserve();
        await factory.InsererAsync(animal);
        var membre = await factory.CreerMembreAsync();
        await factory.InsererAsync(Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow()));

        var response = await ForcerAsync(animal.Id, Corps("Adopte", "Disponible"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageModifieAilleurs, "modifie-ailleurs");
        Assert.Equal(StatutAnimal.Reserve, (await AnimalEnBaseAsync(animal.Id)).Statut);
    }

    [Fact]
    public async Task Animal_inconnu_renvoie_404()
    {
        var response = await ForcerAsync(999999, Corps("Adopte", "Disponible"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageIntrouvable);
    }

    // --- Concurrence ----------------------------------------------------------------------------------

    [Fact]
    public async Task RowVersion_modifie_pendant_l_ecriture_renvoie_409_modifie_ailleurs()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var jeton = await JetonAdminAsync();
        var intercepteur = new ModificationConcurrente(factory.Services);
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(intercepteur))));

        var response = await PostAsync(hote, jeton, Route(animal.Id), Corps("Adopte", "Disponible"));

        Assert.True(intercepteur.Declenche, "L'écriture concurrente doit avoir eu lieu.");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageModifieAilleurs, "modifie-ailleurs");
        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Disponible, enBase.Statut);
        Assert.Null(enBase.AdopteLe);
    }

    // --- Accès ----------------------------------------------------------------------------------------

    [Fact]
    public async Task Membre_recoit_403_et_anonyme_401()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var jetonMembre = await factory.InscrireAsync(NouvelEmail());

        var membre = await PostAsync(factory, jetonMembre, Route(animal.Id), Corps("Adopte", "Disponible"));
        var anonyme = await PostAsync(factory, null, Route(animal.Id), Corps("Adopte", "Disponible"));

        Assert.Equal(HttpStatusCode.Forbidden, membre.StatusCode);
        await AssertErreurAsync(membre, ApiErrors.MessageInterdit);
        Assert.Equal(HttpStatusCode.Unauthorized, anonyme.StatusCode);
        await AssertErreurAsync(anonyme, ApiErrors.MessageNonConnecte);
        Assert.Equal(StatutAnimal.Disponible, (await AnimalEnBaseAsync(animal.Id)).Statut);
    }
}
