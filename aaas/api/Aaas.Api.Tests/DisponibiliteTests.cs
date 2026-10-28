using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Auth;
using Aaas.Api.Features.Reservations;
using Aaas.Api.Tests.Outils;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aaas.Api.Tests;

/// <summary>
/// Statut effectif (AD-5) et entité Réservation : une couverture par ligne de la matrice de la story. Les prédicats
/// sont toujours exécutés par une requête EF sur LocalDB, jamais appelés en mémoire.
/// </summary>
[Collection(ApiCollection.Name)]
public class DisponibiliteTests(AaasFactory factory) : IAsyncLifetime
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await factory.ViderAnimauxAsync();

    public async ValueTask DisposeAsync() => await factory.ViderAnimauxAsync();

    // --- Réservation active ---------------------------------------------------------------------------

    [Fact]
    public async Task Reservation_active_garde_l_animal_Reserve_et_est_active()
    {
        var (animal, reservation) = await AnimalReserveAvecReservationActiveAsync();

        Assert.Equal("Reserve", await StatutCarteAsync(animal.Id));
        var now = factory.Time.GetUtcNow();
        Assert.True(await EstActiveEnSqlAsync(reservation.Id, now));
        Assert.Equal(StatutReservation.EnAttente, await StatutReservationEnSqlAsync(reservation.Id, now));
        Assert.Equal(StatutAnimal.Reserve, await StatutAnimalEnSqlAsync(animal.Id, now));
    }

    // --- Réservation échue ----------------------------------------------------------------------------

    [Fact]
    public async Task Reservation_echue_rend_l_animal_Disponible_et_la_reservation_Expiree()
    {
        var (animal, reservation) = await AnimalReserveAvecReservationActiveAsync();

        factory.Time.Advance(Reservation.Delai);

        var now = factory.Time.GetUtcNow();
        Assert.True(reservation.ExpireLe <= now);
        Assert.Equal("Disponible", await StatutCarteAsync(animal.Id));
        Assert.False(await EstActiveEnSqlAsync(reservation.Id, now));
        Assert.Equal(StatutReservation.Expiree, await StatutReservationEnSqlAsync(reservation.Id, now));
        Assert.Equal(StatutAnimal.Disponible, await StatutAnimalEnSqlAsync(animal.Id, now));
    }

    [Fact]
    public async Task Reservation_echue_a_la_seconde_pres()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Seconde").Reserve();
        await factory.InsererAsync(animal);
        var reservation = Reservations.Nouvelle(animal.Id, membre, factory.Time.GetUtcNow());
        await factory.InsererAsync(reservation);

        // ExpireLe > now : encore active une seconde avant ; ExpireLe = now : échue.
        Assert.True(await EstActiveEnSqlAsync(reservation.Id, reservation.ExpireLe.AddSeconds(-1)));
        Assert.False(await EstActiveEnSqlAsync(reservation.Id, reservation.ExpireLe));
        Assert.Equal(StatutAnimal.Reserve, await StatutAnimalEnSqlAsync(animal.Id, reservation.ExpireLe.AddSeconds(-1)));
        Assert.Equal(StatutAnimal.Disponible, await StatutAnimalEnSqlAsync(animal.Id, reservation.ExpireLe));
    }

    // --- Réservé sans réservation ---------------------------------------------------------------------

    [Fact]
    public async Task Reserve_sans_reservation_est_Disponible()
    {
        var animal = Animaux.Nouveau("Orphelin").Reserve();
        await factory.InsererAsync(animal);

        Assert.Equal("Disponible", await StatutCarteAsync(animal.Id));
        Assert.Equal(StatutAnimal.Disponible, await StatutAnimalEnSqlAsync(animal.Id, factory.Time.GetUtcNow()));
    }

    [Fact]
    public async Task Reserve_avec_seulement_une_reservation_echue_est_Disponible()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Oublie").Reserve();
        await factory.InsererAsync(animal);
        await factory.InsererAsync(Reservations.Echue(animal.Id, membre, factory.Time.GetUtcNow()));

        Assert.Equal("Disponible", await StatutCarteAsync(animal.Id));
    }

    // --- Confirmée / annulée --------------------------------------------------------------------------

    [Theory]
    [InlineData(StatutReservation.Confirmee)]
    [InlineData(StatutReservation.Annulee)]
    public async Task Reservation_confirmee_ou_annulee_n_est_jamais_active_et_garde_son_statut(StatutReservation statut)
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Traite");
        await factory.InsererAsync(animal);
        var reservation = Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow());
        _ = statut == StatutReservation.Confirmee ? reservation.Confirmee() : reservation.Annulee();
        await factory.InsererAsync(reservation);

        // Avant et après l'échéance : jamais active, statut effectif = statut stocké.
        foreach (var now in new[] { factory.Time.GetUtcNow(), reservation.ExpireLe.AddHours(1) })
        {
            Assert.False(await EstActiveEnSqlAsync(reservation.Id, now));
            Assert.Equal(statut, await StatutReservationEnSqlAsync(reservation.Id, now));
        }
    }

    [Fact]
    public async Task Animal_dont_la_reservation_est_traitee_garde_son_statut_stocke()
    {
        var membre = await factory.CreerMembreAsync();
        var now = factory.Time.GetUtcNow();
        var adopte = Animaux.Nouveau("Adopte", Animaux.CreeLeParDefaut.AddHours(2)).Adopte();
        var libere = Animaux.Nouveau("Libere", Animaux.CreeLeParDefaut.AddHours(1));
        await factory.InsererAsync(adopte, libere);
        await factory.InsererAsync(
            Reservations.Active(adopte.Id, membre, now).Confirmee(),
            Reservations.Active(libere.Id, membre, now).Annulee());

        Assert.Equal(StatutAnimal.Adopte, await StatutAnimalEnSqlAsync(adopte.Id, now));
        Assert.Equal(StatutAnimal.Disponible, await StatutAnimalEnSqlAsync(libere.Id, now));
        using var doc = await CatalogueAsync();
        var carte = Assert.Single(doc.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("Libere", carte.GetProperty("nom").GetString());
        Assert.Equal("Disponible", carte.GetProperty("statut").GetString());
    }

    // --- Adopté ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Adopte_est_absent_du_catalogue_et_Reserve_echu_y_reste_Disponible()
    {
        var membre = await factory.CreerMembreAsync();
        var now = factory.Time.GetUtcNow();
        var adopte = Animaux.Nouveau("Adopte", Animaux.CreeLeParDefaut.AddHours(3)).Adopte();
        var reserve = Animaux.Nouveau("Reserve", Animaux.CreeLeParDefaut.AddHours(2)).Reserve();
        var echu = Animaux.Nouveau("Echu", Animaux.CreeLeParDefaut.AddHours(1)).Reserve();
        await factory.InsererAsync(adopte, reserve, echu);
        await factory.InsererAsync(Reservations.Active(reserve.Id, membre, now), Reservations.Echue(echu.Id, membre, now));

        using var doc = await CatalogueAsync("?limite=2");

        // Tri creeLe décroissant et limite inchangés : l'adopté exclu ne consomme pas la limite.
        Assert.Equal(
            [("Reserve", "Reserve"), ("Echu", "Disponible")],
            doc.RootElement.GetProperty("items").EnumerateArray()
                .Select(c => (c.GetProperty("nom").GetString(), c.GetProperty("statut").GetString())));
    }

    // --- Deux réservations EnAttente sur un animal ----------------------------------------------------

    [Fact]
    public async Task Deux_reservations_EnAttente_sur_un_animal_sont_refusees_par_l_index_unique()
    {
        var membre = await factory.CreerMembreAsync();
        var autre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Convoite").Reserve();
        await factory.InsererAsync(animal);
        await factory.InsererAsync(Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow()));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => factory.InsererAsync(Reservations.Nouvelle(animal.Id, autre, factory.Time.GetUtcNow())));
        Assert.True(SqlErreurs.EstViolationUnicite(exception));
    }

    [Fact]
    public async Task Index_unique_filtre_ignore_les_reservations_traitees()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Rereserve").Reserve();
        await factory.InsererAsync(animal);
        var now = factory.Time.GetUtcNow();
        await factory.InsererAsync(Reservations.Active(animal.Id, membre, now).Annulee());

        await factory.InsererAsync(Reservations.Active(animal.Id, membre, now));

        Assert.Equal(2, await LireAsync(db => db.Set<Reservation>().CountAsync(r => r.AnimalId == animal.Id, Ct)));
    }

    // --- Suppression bloquée --------------------------------------------------------------------------

    [Fact]
    public async Task Supprimer_un_animal_qui_a_une_reservation_est_refuse_par_la_base()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Garde");
        await factory.InsererAsync(animal);
        await factory.InsererAsync(Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow()).Annulee());

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => SupprimerAsync<Animal>(animal.Id));
        AssertViolationCleEtrangere(exception);
    }

    [Fact]
    public async Task Supprimer_un_compte_qui_a_une_reservation_est_refuse_par_la_base()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Fidele");
        await factory.InsererAsync(animal);
        await factory.InsererAsync(Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow()).Confirmee());

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => SupprimerAsync<User>(membre));
        AssertViolationCleEtrangere(exception);
    }

    // --- Traduction SQL -------------------------------------------------------------------------------

    [Fact]
    public async Task Catalogue_calcule_le_statut_effectif_dans_la_requete_SQL()
    {
        await AnimalReserveAvecReservationActiveAsync();
        var journal = new JournalSql();
        using var hote = factory.WithWebHostBuilder(b => b.ConfigureLogging(l => l.AddProvider(journal)));

        var response = await hote.CreateClient().GetAsync("/api/animaux", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sql = Assert.Single(journal.Entrees, e => e.Categorie == DbLoggerCategory.Database.Command.Name
            && e.Message.Contains("FROM [Animaux]")).Message;
        Assert.Contains("[Reservations]", sql);
        Assert.Contains("EXISTS", sql);
        Assert.Contains("CASE", sql);
        Assert.DoesNotContain(journal.Entrees, e => e.Categorie.StartsWith("Microsoft.EntityFrameworkCore") && e.Niveau >= LogLevel.Warning);
    }

    [Fact]
    public async Task Predicats_sont_traduits_en_SQL()
    {
        var now = factory.Time.GetUtcNow();

        var sqlAnimal = await LireAsync(db => Task.FromResult(db.Set<Animal>()
            .Select(AnimalRules.StatutEffectif<Animal, Reservation>(db.Set<Reservation>(), now)).ToQueryString()));
        var sqlActive = await LireAsync(db => Task.FromResult(db.Set<Reservation>()
            .Where(ReservationRules.EstActive<Reservation>(now)).ToQueryString()));
        var sqlReservation = await LireAsync(db => Task.FromResult(db.Set<Reservation>()
            .Select(ReservationRules.StatutEffectif<Reservation>(now)).ToQueryString()));

        Assert.Contains("EXISTS", sqlAnimal);
        Assert.Contains("[ExpireLe] >", sqlAnimal);
        Assert.Contains("[ExpireLe] >", sqlActive);
        Assert.Contains("N'EnAttente'", sqlActive);
        Assert.Contains("CASE", sqlReservation);
        Assert.Contains("N'Expiree'", sqlReservation);
    }

    // --- Outils ---------------------------------------------------------------------------------------

    private async Task<(Animal Animal, Reservation Reservation)> AnimalReserveAvecReservationActiveAsync()
    {
        var membre = await factory.CreerMembreAsync();
        var animal = Animaux.Nouveau("Reserve").Reserve();
        await factory.InsererAsync(animal);
        var reservation = Reservations.Active(animal.Id, membre, factory.Time.GetUtcNow());
        await factory.InsererAsync(reservation);
        Assert.Equal(factory.Time.GetUtcNow().AddMinutes(5), reservation.ExpireLe);
        return (animal, reservation);
    }

    private async Task<JsonDocument> CatalogueAsync(string requete = "")
    {
        var response = await factory.CreateClient().GetAsync($"/api/animaux{requete}", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await RequetesApi.LireJsonAsync(response);
    }

    private async Task<string?> StatutCarteAsync(int animalId)
    {
        using var doc = await CatalogueAsync();
        var carte = Assert.Single(doc.RootElement.GetProperty("items").EnumerateArray(),
            c => c.GetProperty("id").GetInt32() == animalId);
        return carte.GetProperty("statut").GetString();
    }

    private Task<bool> EstActiveEnSqlAsync(int reservationId, DateTimeOffset now) =>
        LireAsync(db => db.Set<Reservation>().Where(r => r.Id == reservationId)
            .AnyAsync(ReservationRules.EstActive<Reservation>(now), Ct));

    private Task<StatutReservation> StatutReservationEnSqlAsync(int reservationId, DateTimeOffset now) =>
        LireAsync(db => db.Set<Reservation>().Where(r => r.Id == reservationId)
            .Select(ReservationRules.StatutEffectif<Reservation>(now)).SingleAsync(Ct));

    private Task<StatutAnimal> StatutAnimalEnSqlAsync(int animalId, DateTimeOffset now) =>
        LireAsync(db => db.Set<Animal>().Where(a => a.Id == animalId)
            .Select(AnimalRules.StatutEffectif<Animal, Reservation>(db.Set<Reservation>(), now)).SingleAsync(Ct));

    private async Task<T> LireAsync<T>(Func<AppDbContext, Task<T>> lecture)
    {
        using var scope = factory.Services.CreateScope();
        return await lecture(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private async Task SupprimerAsync<T>(int id) where T : class
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<T>().Remove((await db.Set<T>().FindAsync([id], Ct))!);
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>Violation de clé étrangère : erreur SQL 547.</summary>
    private static void AssertViolationCleEtrangere(DbUpdateException exception) =>
        Assert.Equal(547, Assert.IsType<SqlException>(exception.InnerException).Number);

    /// <summary>Capture les requêtes SQL d'EF Core et ses avertissements.</summary>
    private sealed class JournalSql : ILoggerProvider
    {
        public sealed record Entree(string Categorie, LogLevel Niveau, string Message);

        private readonly ConcurrentQueue<Entree> _entrees = new();

        public IReadOnlyCollection<Entree> Entrees => _entrees.ToArray();

        public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _entrees);

        public void Dispose()
        {
        }

        private sealed class Logger(string categorie, ConcurrentQueue<Entree> entrees) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) =>
                categorie.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) && logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel))
                    entrees.Enqueue(new Entree(categorie, logLevel, formatter(state, exception)));
            }
        }
    }
}
