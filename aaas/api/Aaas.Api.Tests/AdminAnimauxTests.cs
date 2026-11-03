using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Animaux.Commands;
using Aaas.Api.Tests.Outils;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;

namespace Aaas.Api.Tests;

/// <summary>
/// Gestion des animaux côté API (<c>/api/admin/animaux</c>) : une couverture par ligne de la matrice de la story
/// « Gestion des animaux côté API ».
/// </summary>
[Collection(ApiCollection.Name)]
public class AdminAnimauxTests(AaasFactory factory) : IAsyncLifetime
{
    private const string Route = "/api/admin/animaux";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    private Task<string> JetonAdminAsync() => factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);

    private static HttpClient Client(WebApplicationFactory<Program> hote, string? jeton)
    {
        var client = hote.CreateClient();
        if (jeton is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return client;
    }

    private static Task<HttpResponseMessage> EnvoyerAsync(WebApplicationFactory<Program> hote, string? jeton,
        HttpMethod methode, string url, object? corps = null)
    {
        var requete = new HttpRequestMessage(methode, url);
        if (corps is not null)
            requete.Content = RequetesApi.JsonContent(corps as string ?? JsonSerializer.Serialize(corps));
        return Client(hote, jeton).SendAsync(requete, Ct);
    }

    private Task<HttpResponseMessage> EnvoyerAsync(string? jeton, HttpMethod methode, string url, object? corps = null) =>
        EnvoyerAsync(factory, jeton, methode, url, corps);

    /// <summary>Corps valide de <c>AnimalEcritureDto</c> (les 12 champs du §6).</summary>
    private static Dictionary<string, object?> Corps(string nom = "Caramel") => new()
    {
        ["nom"] = nom,
        ["espece"] = "Chat",
        ["sexe"] = "Femelle",
        ["age"] = "Jeune",
        ["taille"] = "Moyenne",
        ["description"] = "Douce et joueuse.",
        ["vaccine"] = true,
        ["sterilise"] = false,
        ["puce"] = true,
        ["ententeEnfants"] = "Oui",
        ["ententeChiens"] = "AVoir",
        ["ententeChats"] = "Non",
    };

    private static async Task<JsonElement> LireAsync(HttpResponseMessage response, HttpStatusCode attendu)
    {
        Assert.Equal(attendu, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        return doc.RootElement.Clone();
    }

    private async Task<int> NombreAnimauxAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>().CountAsync(Ct);
    }

    private async Task<Animal?> AnimalEnBaseAsync(int id)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>().AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id, Ct);
    }

    // --- Liste ----------------------------------------------------------------------------------------

    [Fact]
    public async Task Liste_renvoie_tous_les_statuts_creeLe_decroissant_au_statut_effectif()
    {
        var debut = factory.Time.GetUtcNow();
        var disponible = Animaux.Nouveau("Disponible", debut.AddMinutes(-3));
        var reserve = Animaux.Nouveau("Reserve", debut.AddMinutes(-2)).Reserve();
        var adopte = Animaux.Nouveau("Adopte", debut.AddMinutes(-1)).Adopte();
        var reserveEchu = Animaux.Nouveau("ReserveEchu", debut.AddMinutes(-4)).Reserve();
        await factory.InsererAsync(disponible, reserve, adopte, reserveEchu);
        var membre = await factory.CreerMembreAsync();
        await factory.InsererAsync(
            Reservations.Active(reserve.Id, membre, debut),
            Reservations.Echue(reserveEchu.Id, membre, debut));

        var corps = await LireAsync(await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Get, Route), HttpStatusCode.OK);

        Assert.Equal(["items", "serverNow"], corps.EnumerateObject().Select(p => p.Name));
        var items = corps.GetProperty("items").EnumerateArray().ToArray();
        Assert.Equal(["Adopte", "Reserve", "Disponible", "ReserveEchu"], items.Select(i => i.GetProperty("nom").GetString()));
        Assert.Equal(["Adopte", "Reserve", "Disponible", "Disponible"], items.Select(i => i.GetProperty("statut").GetString()));
        Assert.All(items, i => Assert.Equal(
            ["id", "nom", "espece", "sexe", "age", "taille", "statut", "photoPrincipaleUrl"],
            i.EnumerateObject().Select(p => p.Name)));
    }

    [Fact]
    public async Task Liste_a_creeLe_egal_trie_par_Id_decroissant_et_vide_renvoie_200()
    {
        var jeton = await JetonAdminAsync();
        var vide = await LireAsync(await EnvoyerAsync(jeton, HttpMethod.Get, Route), HttpStatusCode.OK);
        Assert.Empty(vide.GetProperty("items").EnumerateArray());

        var premier = Animaux.Nouveau("Premier");
        var second = Animaux.Nouveau("Second");
        await factory.InsererAsync(premier, second);

        var corps = await LireAsync(await EnvoyerAsync(jeton, HttpMethod.Get, Route), HttpStatusCode.OK);
        Assert.Equal([second.Id, premier.Id], corps.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetInt32()));
    }

    // --- Fiche ----------------------------------------------------------------------------------------

    [Fact]
    public async Task Fiche_admin_renvoie_la_fiche_d_un_animal_adopte()
    {
        var adopte = Animaux.Nouveau("Filou").Adopte(Animaux.CreeLeParDefaut.AddDays(1));
        await factory.InsererAsync(adopte);

        var fiche = await LireAsync(
            await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Get, $"{Route}/{adopte.Id}"), HttpStatusCode.OK);

        Assert.Equal(adopte.Id, fiche.GetProperty("id").GetInt32());
        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal("2026-01-16T10:00:00Z", fiche.GetProperty("adopteLe").GetString());
    }

    // --- Création -------------------------------------------------------------------------------------

    [Fact]
    public async Task Creation_renvoie_201_Disponible_ignore_le_statut_et_apparait_au_catalogue()
    {
        var corps = Corps("  Caramel  ");
        corps["description"] = "  Douce et joueuse.  ";
        corps["statut"] = "Adopte";

        var fiche = await LireAsync(await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Post, Route, corps), HttpStatusCode.Created);

        var id = fiche.GetProperty("id").GetInt32();
        Assert.True(id > 0);
        Assert.Equal("Caramel", fiche.GetProperty("nom").GetString());
        Assert.Equal("Douce et joueuse.", fiche.GetProperty("description").GetString());
        Assert.Equal("Disponible", fiche.GetProperty("statut").GetString());
        Assert.Equal("Chat", fiche.GetProperty("espece").GetString());
        Assert.Equal("Femelle", fiche.GetProperty("sexe").GetString());
        Assert.Equal("Jeune", fiche.GetProperty("age").GetString());
        Assert.Equal("Moyenne", fiche.GetProperty("taille").GetString());
        Assert.True(fiche.GetProperty("vaccine").GetBoolean());
        Assert.False(fiche.GetProperty("sterilise").GetBoolean());
        Assert.True(fiche.GetProperty("puce").GetBoolean());
        Assert.Equal("Oui", fiche.GetProperty("ententeEnfants").GetString());
        Assert.Equal("AVoir", fiche.GetProperty("ententeChiens").GetString());
        Assert.Equal("Non", fiche.GetProperty("ententeChats").GetString());
        Assert.Equal(factory.Time.GetUtcNow(), fiche.GetProperty("creeLe").GetDateTimeOffset());
        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("adopteLe").ValueKind);
        Assert.Equal(JsonValueKind.Null, fiche.GetProperty("photoPrincipaleUrl").ValueKind);
        Assert.Empty(fiche.GetProperty("photos").EnumerateArray());

        var enBase = await AnimalEnBaseAsync(id);
        Assert.Equal(StatutAnimal.Disponible, enBase!.Statut);

        var catalogue = await LireAsync(await factory.CreateClient().GetAsync("/api/animaux", Ct), HttpStatusCode.OK);
        var carte = catalogue.GetProperty("items").EnumerateArray().Single(c => c.GetProperty("id").GetInt32() == id);
        Assert.Equal("Disponible", carte.GetProperty("statut").GetString());
    }

    [Fact]
    public async Task Creation_invalide_renvoie_400_par_champ_et_n_ecrit_rien()
    {
        var corps = Corps("   ");
        corps["description"] = new string('a', Animal.DescriptionMax + 1);
        corps.Remove("sexe");

        var errors = await AssertValidationAsync(await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Post, Route, corps));

        Assert.Equal(["description", "nom", "sexe"], errors.Keys.Order());
        Assert.Equal(0, await NombreAnimauxAsync());
    }

    [Fact]
    public async Task Creation_sans_aucun_champ_renvoie_une_erreur_par_champ()
    {
        var errors = await AssertValidationAsync(await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Post, Route, "{}"));

        Assert.Equal(
            ["age", "description", "ententeChats", "ententeChiens", "ententeEnfants", "espece", "nom", "puce", "sexe",
                "sterilise", "taille", "vaccine"],
            errors.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(0, await NombreAnimauxAsync());
    }

    [Fact]
    public async Task Nom_de_51_caracteres_est_refuse_et_50_accepte()
    {
        var jeton = await JetonAdminAsync();

        var errors = await AssertValidationAsync(
            await EnvoyerAsync(jeton, HttpMethod.Post, Route, Corps(new string('n', Animal.NomMax + 1))));
        Assert.Equal(["nom"], errors.Keys);

        var response = await EnvoyerAsync(jeton, HttpMethod.Post, Route, Corps(new string('n', Animal.NomMax)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("espece", "chat")]
    [InlineData("espece", "Lapin")]
    [InlineData("espece", 0)]
    [InlineData("ententeChats", "avoir")]
    public async Task Enumeration_invalide_renvoie_400_et_n_ecrit_rien(string champ, object valeur)
    {
        var corps = Corps();
        corps[champ] = valeur;

        var response = await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Post, Route, corps);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await NombreAnimauxAsync());
    }

    // --- Modification ---------------------------------------------------------------------------------

    [Fact]
    public async Task Modification_d_un_adopte_met_a_jour_les_champs_sans_toucher_au_statut()
    {
        var adopteLe = Animaux.CreeLeParDefaut.AddDays(2);
        var animal = Animaux.Nouveau("Filou").Adopte(adopteLe);
        await factory.InsererAsync(animal);
        var corps = Corps("Filou le chien");
        corps["espece"] = "Chien";
        corps["sexe"] = "Male";
        corps["age"] = "Senior";
        corps["taille"] = "Grande";
        corps["description"] = "Un vieux sage.";
        corps["vaccine"] = false;
        corps["sterilise"] = true;
        corps["puce"] = false;
        corps["ententeEnfants"] = "Non";
        corps["ententeChiens"] = "Oui";
        corps["ententeChats"] = "AVoir";
        corps["statut"] = "Disponible";

        var fiche = await LireAsync(
            await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Put, $"{Route}/{animal.Id}", corps), HttpStatusCode.OK);

        Assert.Equal(animal.Id, fiche.GetProperty("id").GetInt32());
        Assert.Equal("Filou le chien", fiche.GetProperty("nom").GetString());
        Assert.Equal("Chien", fiche.GetProperty("espece").GetString());
        Assert.Equal("Male", fiche.GetProperty("sexe").GetString());
        Assert.Equal("Senior", fiche.GetProperty("age").GetString());
        Assert.Equal("Grande", fiche.GetProperty("taille").GetString());
        Assert.Equal("Un vieux sage.", fiche.GetProperty("description").GetString());
        Assert.False(fiche.GetProperty("vaccine").GetBoolean());
        Assert.True(fiche.GetProperty("sterilise").GetBoolean());
        Assert.False(fiche.GetProperty("puce").GetBoolean());
        Assert.Equal("Non", fiche.GetProperty("ententeEnfants").GetString());
        Assert.Equal("Oui", fiche.GetProperty("ententeChiens").GetString());
        Assert.Equal("AVoir", fiche.GetProperty("ententeChats").GetString());
        Assert.Equal("Adopte", fiche.GetProperty("statut").GetString());
        Assert.Equal(adopteLe, fiche.GetProperty("adopteLe").GetDateTimeOffset());
        Assert.Equal(Animaux.CreeLeParDefaut, fiche.GetProperty("creeLe").GetDateTimeOffset());

        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.Equal(StatutAnimal.Adopte, enBase!.Statut);
        Assert.Equal(adopteLe, enBase.AdopteLe);
    }

    [Fact]
    public async Task Modification_invalide_renvoie_400_et_ne_modifie_rien()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var corps = Corps("");
        corps.Remove("vaccine");

        var errors = await AssertValidationAsync(
            await EnvoyerAsync(await JetonAdminAsync(), HttpMethod.Put, $"{Route}/{animal.Id}", corps));

        Assert.Equal(["nom", "vaccine"], errors.Keys.Order());
        Assert.Equal("Filou", (await AnimalEnBaseAsync(animal.Id))!.Nom);
    }

    // --- Inconnu --------------------------------------------------------------------------------------

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Animal_inconnu_renvoie_404(string methode)
    {
        var response = await EnvoyerAsync(await JetonAdminAsync(), new HttpMethod(methode), $"{Route}/999999",
            methode == "PUT" ? Corps() : null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageIntrouvable);
    }

    // --- Suppression ----------------------------------------------------------------------------------

    private async Task<(int AnimalId, string[] Cles)> AnimalAvecDeuxPhotosAsync(string jeton)
    {
        var animal = Animaux.Nouveau("Caramel");
        await factory.InsererAsync(animal);
        var cles = new List<string>();
        for (var i = 0; i < 2; i++)
        {
            var photo = await LireAsync(await factory.EnvoyerPhotoAsync(animal.Id, jeton, Fichiers.Jpeg(1024)), HttpStatusCode.Created);
            cles.Add(photo.GetProperty("url").GetString()!["/fichiers/".Length..]);
        }
        Assert.All(cles, c => Assert.Contains(c, factory.FichiersStockes()));
        return (animal.Id, cles.ToArray());
    }

    private async Task<int> NombrePhotosAsync(int animalId)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<PhotoAnimal>()
            .CountAsync(p => p.AnimalId == animalId, Ct);
    }

    [Fact]
    public async Task Suppression_sans_reservation_supprime_l_animal_ses_photos_et_leurs_fichiers()
    {
        var jeton = await JetonAdminAsync();
        var (animalId, cles) = await AnimalAvecDeuxPhotosAsync(jeton);

        var response = await EnvoyerAsync(jeton, HttpMethod.Delete, $"{Route}/{animalId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await AnimalEnBaseAsync(animalId));
        Assert.Equal(0, await NombrePhotosAsync(animalId));
        foreach (var cle in cles)
        {
            Assert.DoesNotContain(cle, factory.FichiersStockes());
            var fichier = await factory.CreateClient().GetAsync($"/fichiers/{cle}", Ct);
            Assert.Equal(HttpStatusCode.NotFound, fichier.StatusCode);
        }
    }

    [Fact]
    public async Task Echec_de_suppression_du_fichier_est_journalise_et_la_suppression_reste_faite()
    {
        var jeton = await JetonAdminAsync();
        var (animalId, cles) = await AnimalAvecDeuxPhotosAsync(jeton);
        var stockage = factory.Services.GetRequiredService<IFileStorage>();
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddSingleton<IFileStorage>(new StockageSansSuppression(stockage))));

        var response = await EnvoyerAsync(hote, jeton, HttpMethod.Delete, $"{Route}/{animalId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await AnimalEnBaseAsync(animalId));
        Assert.Equal(0, await NombrePhotosAsync(animalId));
        foreach (var cle in cles)
        {
            Assert.Contains(factory.Journal.Entrees, e =>
                e.Niveau == LogLevel.Error
                && e.Categorie == typeof(SupprimerAnimalHandler).FullName
                && e.Message.Contains(cle, StringComparison.Ordinal));
            stockage.Supprimer(cle);
        }
    }

    /// <summary>Stockage dont la suppression échoue toujours ; le reste est délégué au stockage réel.</summary>
    private sealed class StockageSansSuppression(IFileStorage reel) : IFileStorage
    {
        public Task EcrireAsync(string cle, Stream contenu, CancellationToken cancellationToken) =>
            reel.EcrireAsync(cle, contenu, cancellationToken);

        public Stream? Ouvrir(string cle) => reel.Ouvrir(cle);

        public void Supprimer(string cle) => throw new IOException("Suppression simulée en échec.");

        public string UrlPublique(string cle) => reel.UrlPublique(cle);
    }

    [Fact]
    public async Task Reservation_inseree_entre_la_verification_et_la_suppression_renvoie_409()
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var membre = await factory.CreerMembreAsync();
        var jeton = await JetonAdminAsync();
        var intercepteur = new ReservationConcurrente(membre, factory.Time.GetUtcNow());
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(intercepteur))));

        var response = await EnvoyerAsync(hote, jeton, HttpMethod.Delete, $"{Route}/{animal.Id}");

        Assert.True(intercepteur.Declenche, "La réservation concurrente doit avoir été insérée.");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageAReservations, "animal-a-des-reservations");
        Assert.NotNull(await AnimalEnBaseAsync(animal.Id));
    }

    /// <summary>
    /// Avant le <c>SaveChanges</c> qui supprime un animal, insère une réservation pour cet animal après la vérification
    /// du handler, pour atteindre la clé étrangère <c>Restrict</c> (SQL 547). L'insertion passe par la connexion et la
    /// transaction du handler : en <c>Serializable</c>, le verrou de plage posé par la vérification bloquerait une
    /// autre connexion jusqu'au commit, qui attend lui-même l'intercepteur. Le rollback annule aussi l'insertion.
    /// </summary>
    private sealed class ReservationConcurrente(int membreId, DateTimeOffset now) : SaveChangesInterceptor
    {
        public bool Declenche { get; private set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var entree = eventData.Context!.ChangeTracker.Entries<Animal>().FirstOrDefault(e => e.State == EntityState.Deleted);
            if (entree is not null && !Declenche)
            {
                Declenche = true;
                var animalId = entree.Entity.Id;
                var expireLe = now.AddMinutes(15);
                await eventData.Context.Database.ExecuteSqlAsync(
                    $"INSERT INTO Reservations (AnimalId, UserId, Statut, CreeLe, ExpireLe, Logement, Jardin) VALUES ({animalId}, {membreId}, 'Annulee', {now}, {expireLe}, 'Maison', 1)",
                    cancellationToken);
            }
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    [Theory]
    [InlineData("Annulee")]
    [InlineData("Active")]
    [InlineData("Echue")]
    [InlineData("Confirmee")]
    public async Task Suppression_avec_une_reservation_quel_que_soit_son_statut_renvoie_409(string sorte)
    {
        var jeton = await JetonAdminAsync();
        var (animalId, cles) = await AnimalAvecDeuxPhotosAsync(jeton);
        var membre = await factory.CreerMembreAsync();
        var now = factory.Time.GetUtcNow();
        await factory.InsererAsync(sorte switch
        {
            "Annulee" => Reservations.Active(animalId, membre, now).Annulee(),
            "Active" => Reservations.Active(animalId, membre, now),
            "Echue" => Reservations.Echue(animalId, membre, now),
            _ => Reservations.Active(animalId, membre, now).Confirmee(),
        });

        var response = await EnvoyerAsync(jeton, HttpMethod.Delete, $"{Route}/{animalId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageAReservations, "animal-a-des-reservations");
        Assert.NotNull(await AnimalEnBaseAsync(animalId));
        Assert.Equal(2, await NombrePhotosAsync(animalId));
        Assert.All(cles, c => Assert.Contains(c, factory.FichiersStockes()));
    }

    // --- Concurrence ----------------------------------------------------------------------------------

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task RowVersion_modifie_pendant_l_ecriture_renvoie_409_modifie_ailleurs(string methode)
    {
        var animal = Animaux.Nouveau("Filou");
        await factory.InsererAsync(animal);
        var jeton = await JetonAdminAsync();
        var intercepteur = new ModificationConcurrente(factory.Services);
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureDbContext<AppDbContext>((_, options) => options.AddInterceptors(intercepteur))));

        var response = await EnvoyerAsync(hote, jeton, new HttpMethod(methode), $"{Route}/{animal.Id}",
            methode == "PUT" ? Corps("Nouveau nom") : null);

        Assert.True(intercepteur.Declenche, "L'écriture concurrente doit avoir eu lieu.");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await AssertErreurAsync(response, RefusAnimal.MessageModifieAilleurs, "modifie-ailleurs");
        var enBase = await AnimalEnBaseAsync(animal.Id);
        Assert.NotNull(enBase);
        Assert.Equal("Filou", enBase.Nom);
    }

    // --- Accès ----------------------------------------------------------------------------------------

    public static TheoryData<string, string> Routes => new()
    {
        { "GET", Route },
        { "POST", Route },
        { "GET", $"{Route}/1" },
        { "PUT", $"{Route}/1" },
        { "DELETE", $"{Route}/1" },
    };

    [Theory]
    [MemberData(nameof(Routes))]
    public async Task Membre_recoit_403_et_anonyme_401(string methode, string url)
    {
        var corps = methode is "POST" or "PUT" ? Corps() : null;
        var jetonMembre = await factory.InscrireAsync(NouvelEmail());

        var membre = await EnvoyerAsync(jetonMembre, new HttpMethod(methode), url, corps);
        var anonyme = await EnvoyerAsync(null, new HttpMethod(methode), url, corps);

        Assert.Equal(HttpStatusCode.Forbidden, membre.StatusCode);
        await AssertErreurAsync(membre, ApiErrors.MessageInterdit);
        Assert.Equal(HttpStatusCode.Unauthorized, anonyme.StatusCode);
        await AssertErreurAsync(anonyme, ApiErrors.MessageNonConnecte);
        Assert.Equal(0, await NombreAnimauxAsync());
    }
}
