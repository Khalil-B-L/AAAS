using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Site;
using Aaas.Api.Features.Site.Queries;
using Aaas.Api.Tests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Aaas.Api.Tests.Outils.AssertionsErreur;

namespace Aaas.Api.Tests;

/// <summary>Briques communes (story 2) : une couverture par ligne de la matrice d'entrées / sorties.</summary>
[Collection(ApiCollection.Name)]
public class CommonTests(AaasFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Format d'erreur unique (AD-10) -------------------------------------------------------

    [Fact]
    public async Task Route_api_inconnue_renvoie_404_au_format_unique()
    {
        var response = await factory.CreateClient().GetAsync("/api/inexistant", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageIntrouvable);
    }

    [Fact]
    public async Task Exception_non_geree_renvoie_500_sans_detail_et_est_journalisee()
    {
        var response = await factory.CreateClient().GetAsync("/tests/exception", Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var json = await AssertErreurAsync(response, ApiErrors.MessageInterne);
        Assert.DoesNotContain("secret", json);
        Assert.DoesNotContain("InvalidOperationException", json);
        Assert.Contains(factory.Journal.Entrees, e =>
            e.Niveau >= LogLevel.Error && e.Exception is InvalidOperationException { Message: "détail secret" });
    }

    [Fact]
    public async Task Methode_non_autorisee_renvoie_405_au_format_unique()
    {
        var response = await factory.CreateClient().GetAsync("/tests/enum", Ct);

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageMethodeNonAutorisee);
    }

    [Fact]
    public async Task Type_de_contenu_non_pris_en_charge_renvoie_415_au_format_unique()
    {
        var response = await factory.CreateClient().PostAsync("/tests/enum",
            new StringContent("EnAttente", Encoding.UTF8, "text/plain"), Ct);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageTypeNonPrisEnCharge);
    }

    // --- JSON : dates et énumérations (AD-11, AD-19) ------------------------------------------

    [Fact]
    public async Task Date_non_utc_et_enum_sont_ecrites_en_utc_Z_et_en_chaine_sans_null()
    {
        var response = await factory.CreateClient().GetAsync("/tests/date-enum", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("\"quand\":\"2026-01-15T10:00:00Z\"", json);
        Assert.Contains("\"statut\":\"EnAttente\"", json);
    }

    [Theory]
    [InlineData("2026-01-15T12:00:00+02:00", "2026-01-15T10:00:00Z")]
    [InlineData("2026-01-15T10:00:00.5Z", "2026-01-15T10:00:00.5Z")]
    public async Task Date_avec_fuseau_est_lue_et_renvoyee_en_utc(string entree, string attendu)
    {
        var response = await factory.PostJsonAsync("/tests/date", $$"""{"quand":"{{entree}}"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"\"quand\":\"{attendu}\"", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Date_sans_fuseau_en_entree_renvoie_400()
    {
        var response = await factory.PostJsonAsync("/tests/date", """{"quand":"2026-01-15T12:00:00"}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageRequeteInvalide);
    }

    [Fact]
    public async Task Enum_valide_en_entree_est_lue()
    {
        var response = await factory.PostJsonAsync("/tests/enum", """{"statut":"Confirmee"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"statut\":\"Confirmee\"", await response.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData("\"Inconnue\"")]
    [InlineData("1")]
    [InlineData("\"1\"")]
    [InlineData("\"enAttente\"")]
    public async Task Enum_invalide_en_entree_renvoie_400(string valeur)
    {
        var response = await factory.PostJsonAsync("/tests/enum", $$"""{"statut":{{valeur}}}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageRequeteInvalide);
    }

    [Fact]
    public async Task Corps_invalide_en_Development_renvoie_400_et_non_500()
    {
        // En Development, la liaison des paramètres lève BadHttpRequestException (ThrowOnBadRequest).
        using var developpement = factory.Configurer([], "Development");

        var response = await developpement.CreateClient().PostAsync("/tests/enum",
            new StringContent("""{"statut":"Inconnue"}""", Encoding.UTF8, "application/json"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageRequeteInvalide);
    }

    // --- Result<T> → HTTP (AD-2)---------------------------------------------------------------

    [Theory]
    [InlineData("introuvable", 404, ApiErrors.MessageIntrouvable, null)]
    [InlineData("introuvable-message", 404, "Cet animal est introuvable.", null)]
    [InlineData("conflit", 409, "Trop tard.", "animal-indisponible")]
    [InlineData("invalide-code", 400, "Email ou mot de passe incorrect.", "identifiants-invalides")]
    [InlineData("interdit", 403, ApiErrors.MessageInterdit, null)]
    [InlineData("indisponible", 503, "Envoi impossible.", "envoi-impossible")]
    [InlineData("sans-valeur-echec", 409, "Réservation échue.", "reservation-echue")]
    public async Task Echec_de_Result_renvoie_statut_et_corps_unique(string variante, int statut, string message, string? code)
    {
        var response = await factory.CreateClient().GetAsync($"/tests/result/{variante}", Ct);

        Assert.Equal(statut, (int)response.StatusCode);
        await AssertErreurAsync(response, message, code);
    }

    [Fact]
    public async Task Result_Invalid_renvoie_400_avec_errors_par_champ()
    {
        var response = await factory.CreateClient().GetAsync("/tests/result/invalide", Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var racine = doc.RootElement;
        Assert.Equal(["error", "errors"], racine.EnumerateObject().Select(p => p.Name));
        Assert.Equal(ApiErrors.MessageValidation, racine.GetProperty("error").GetString());
        Assert.Equal("Le nom est obligatoire.", racine.GetProperty("errors").GetProperty("nom").GetString());
    }

    [Theory]
    [InlineData("ok", HttpStatusCode.OK)]
    [InlineData("cree", HttpStatusCode.Created)]
    public async Task Succes_de_Result_renvoie_la_valeur_avec_le_statut_choisi_par_l_endpoint(string variante, HttpStatusCode statut)
    {
        var response = await factory.CreateClient().GetAsync($"/tests/result/{variante}", Ct);

        Assert.Equal(statut, response.StatusCode);
        Assert.Equal("\"valeur\"", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Succes_de_Result_sans_valeur_renvoie_204()
    {
        var response = await factory.CreateClient().GetAsync("/tests/result/sans-valeur", Ct);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync(Ct));
    }

    // --- Handlers (AD-2) -----------------------------------------------------------------------

    [Fact]
    public async Task Site_passe_par_un_IQueryHandler_enregistre_en_scoped()
    {
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();

        var handler = scope1.ServiceProvider.GetRequiredService<IQueryHandler<ObtenirSiteQuery, SiteDto>>();

        Assert.IsType<ObtenirSiteHandler>(handler);
        Assert.Same(handler, scope1.ServiceProvider.GetRequiredService<IQueryHandler<ObtenirSiteQuery, SiteDto>>());
        Assert.NotSame(handler, scope2.ServiceProvider.GetRequiredService<IQueryHandler<ObtenirSiteQuery, SiteDto>>());

        var result = await handler.HandleAsync(new ObtenirSiteQuery(), Ct);
        Assert.True(result.EstSucces);
        Assert.Equal(new SiteDto(AaasFactory.SiteTelephone, AaasFactory.SiteFacebook, AaasFactory.SiteInstagram), result.Valeur);
    }

    // --- TransactionRunner (AD-6) --------------------------------------------------------------

    [Theory]
    [InlineData("succes", HttpStatusCode.OK, 1)]
    [InlineData("echec", HttpStatusCode.Conflict, 0)]
    [InlineData("exception", HttpStatusCode.InternalServerError, 0)]
    public async Task TransactionRunner_ne_commite_que_le_succes(string issue, HttpStatusCode statut, int lignesAttendues)
    {
        await CreerTableTransactionAsync();
        var valeur = Guid.NewGuid().ToString("N");

        var response = await factory.CreateClient().PostAsync($"/tests/transaction/{issue}?valeur={valeur}", null, Ct);

        Assert.Equal(statut, response.StatusCode);
        Assert.Equal(lignesAttendues, await CompterLignesAsync(valeur));
    }

    // --- Conventions EF (AD-7, AD-11) ----------------------------------------------------------

    [Fact]
    public void Conventions_datetimeoffset_utc_et_enum_en_chaine_sur_un_contexte_derive()
    {
        using var db = new ContexteConventions(OptionsHorsLigne());
        var entite = db.Model.FindEntityType(typeof(EntiteConventions))!;

        foreach (var nom in new[] { nameof(EntiteConventions.Quand), nameof(EntiteConventions.QuandOptionnel) })
        {
            var propriete = entite.FindProperty(nom)!;
            Assert.Equal("datetimeoffset", propriete.GetColumnType());
            var converter = propriete.GetTypeMapping().Converter;
            Assert.NotNull(converter);
            var ecrit = Assert.IsType<DateTimeOffset>(converter.ConvertToProvider(EndpointsDeTest.DateNonUtc));
            Assert.Equal(TimeSpan.Zero, ecrit.Offset);
            Assert.Equal(EndpointsDeTest.DateNonUtc.UtcDateTime, ecrit.UtcDateTime);
        }

        foreach (var nom in new[] { nameof(EntiteConventions.Statut), nameof(EntiteConventions.StatutOptionnel) })
        {
            var converter = entite.FindProperty(nom)!.GetTypeMapping().Converter;
            Assert.NotNull(converter);
            Assert.Equal(typeof(string), converter.ProviderClrType);
            Assert.Equal("EnAttente", converter.ConvertToProvider(StatutTest.EnAttente));
        }
    }

    [Fact]
    public void Propriete_DateTime_leve_a_la_construction_du_modele()
    {
        using var db = new ContexteAvecDateTime(OptionsHorsLigne());

        var exception = Assert.ThrowsAny<InvalidOperationException>(() => db.Model);

        Assert.Contains($"{nameof(EntiteAvecDateTime)}.{nameof(EntiteAvecDateTime.Le)} ", exception.Message);
        Assert.Contains("DateTimeOffset", exception.Message);
    }

    // --- LiensFront (AD-21, contrats-api.md §7) ------------------------------------------------

    [Fact]
    public void LiensFront_est_aligne_sur_les_routes_angular()
    {
        var attendus = new Dictionary<string, string>
        {
            [nameof(LiensFront.Accueil)] = "/",
            [nameof(LiensFront.Adopter)] = "/adopter",
            [nameof(LiensFront.IlsOntTrouveUneFamille)] = "/ils-ont-trouve-une-famille",
            [nameof(LiensFront.Actualites)] = "/actualites",
            [nameof(LiensFront.NousAiderDons)] = "/nous-aider/dons",
            [nameof(LiensFront.NousAiderBenevolat)] = "/nous-aider/benevolat",
            [nameof(LiensFront.NousAiderFamilleAccueil)] = "/nous-aider/famille-accueil",
            [nameof(LiensFront.Contact)] = "/contact",
            [nameof(LiensFront.Connexion)] = "/connexion",
            [nameof(LiensFront.Inscription)] = "/inscription",
            [nameof(LiensFront.MesReservations)] = "/mes-reservations",
            [nameof(LiensFront.MesFavoris)] = "/mes-favoris",
            [nameof(LiensFront.AdminReservations)] = "/admin/reservations",
            [nameof(LiensFront.AdminAnimaux)] = "/admin/animaux",
            [nameof(LiensFront.AdminAnimalNouveau)] = "/admin/animaux/nouveau",
            [nameof(LiensFront.AdminActualites)] = "/admin/actualites",
            [nameof(LiensFront.AccesRefuse)] = "/acces-refuse",
            [nameof(LiensFront.ParametreRetour)] = "returnUrl",
            [nameof(LiensFront.ParametreStatut)] = "statut",
        };
        var constantes = typeof(LiensFront).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .ToDictionary(f => f.Name, f => (string)f.GetRawConstantValue()!);
        Assert.Equal(attendus.OrderBy(p => p.Key), constantes.OrderBy(p => p.Key));

        Assert.Equal("/animaux/7", LiensFront.FicheAnimal(7));
        Assert.Equal("/animaux/7/reserver", LiensFront.ReserverAnimal(7));
        Assert.Equal("/reservations/12/envoyee", LiensFront.ReservationEnvoyee(12));
        Assert.Equal("/admin/animaux/7", LiensFront.AdminAnimal(7));
        Assert.Equal("/admin/reservations?statut=EnAttente", LiensFront.AdminReservationsParStatut("EnAttente"));
        Assert.Equal("/connexion?returnUrl=%2Fanimaux%2F7%2Freserver", LiensFront.ConnexionAvecRetour("/animaux/7/reserver"));
        Assert.Equal("/inscription?returnUrl=%2Fmes-favoris", LiensFront.InscriptionAvecRetour("/mes-favoris"));

        var methodes = typeof(LiensFront).GetMethods(BindingFlags.Public | BindingFlags.Static).Select(m => m.Name).Order();
        Assert.Equal(
            [nameof(LiensFront.AdminAnimal), nameof(LiensFront.AdminReservationsParStatut), nameof(LiensFront.ConnexionAvecRetour),
             nameof(LiensFront.FicheAnimal), nameof(LiensFront.InscriptionAvecRetour), nameof(LiensFront.ReservationEnvoyee),
             nameof(LiensFront.ReserverAnimal)],
            methodes);
    }

    // --- Outils ----------------------------------------------------------------------------------

    private async Task CreerTableTransactionAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "IF OBJECT_ID(N'dbo.TestsTransaction') IS NULL CREATE TABLE dbo.TestsTransaction (Id int IDENTITY PRIMARY KEY, Valeur nvarchar(64) NOT NULL)",
            Ct);
    }

    private async Task<int> CompterLignesAsync(string valeur)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.TestsTransaction WHERE Valeur = {valeur}")
            .SingleAsync(Ct);
    }

    private static DbContextOptions<AppDbContext> OptionsHorsLigne() =>
        new DbContextOptionsBuilder<AppDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=NonUtilisee").Options;

    private sealed class EntiteConventions
    {
        public int Id { get; set; }
        public DateTimeOffset Quand { get; set; }
        public DateTimeOffset? QuandOptionnel { get; set; }
        public StatutTest Statut { get; set; }
        public StatutTest? StatutOptionnel { get; set; }
    }

    private sealed class EntiteAvecDateTime
    {
        public int Id { get; set; }
        public DateTime Le { get; set; }
    }

    private sealed class ContexteConventions(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EntiteConventions>();
        }
    }

    private sealed class ContexteAvecDateTime(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<EntiteAvecDateTime>();
        }
    }
}
