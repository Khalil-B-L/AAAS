using Aaas.Api.Data;
using Aaas.Api.Features.Auth;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests.Outils;

/// <summary>
/// Jeu de données de test « réservation » partagé : membre de test, réservations active, échue, confirmée ou annulée
/// datées par rapport à un instant choisi (sans toucher à l'horloge de la factory, qui n'avance que vers l'avant),
/// insertion et nettoyage de la table.
/// </summary>
public static class Reservations
{
    /// <summary>Âge d'une réservation active à l'instant donné : il lui reste 5 minutes.</summary>
    public static readonly TimeSpan AgeActive = TimeSpan.FromMinutes(10);

    /// <summary>Âge d'une réservation échue à l'instant donné : expirée depuis 5 minutes.</summary>
    public static readonly TimeSpan AgeEchue = TimeSpan.FromMinutes(20);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Crée un membre en base (sans passer par l'API) et renvoie son identifiant.</summary>
    public static async Task<int> CreerMembreAsync(this AaasFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membre = new User
        {
            Prenom = "Camille",
            Nom = "Martin",
            Email = Comptes.NouvelEmail(),
            Telephone = "+21672123456",
            MotDePasseHash = "hachage-de-test",
            Role = Role.Membre,
            CreeLe = factory.Time.GetUtcNow(),
        };
        db.Set<User>().Add(membre);
        await db.SaveChangesAsync(Ct);
        return membre.Id;
    }

    /// <summary>Réservation EnAttente créée à <paramref name="creeLe"/> (expire 15 minutes plus tard).</summary>
    public static Reservation Nouvelle(int animalId, int userId, DateTimeOffset creeLe) =>
        new(animalId, userId, Logement.Maison, jardin: true, experience: "Deux chats déjà adoptés.", new FakeTimeProvider(creeLe));

    /// <summary>Réservation EnAttente encore active à <paramref name="now"/> (expire à <c>now + 5 min</c>).</summary>
    public static Reservation Active(int animalId, int userId, DateTimeOffset now) => Nouvelle(animalId, userId, now - AgeActive);

    /// <summary>Réservation EnAttente échue à <paramref name="now"/> (<c>ExpireLe = now - 5 min</c>).</summary>
    public static Reservation Echue(int animalId, int userId, DateTimeOffset now) => Nouvelle(animalId, userId, now - AgeEchue);

    /// <summary>Passe la réservation Confirmée par <see cref="Reservation.Confirmer"/>.</summary>
    public static Reservation Confirmee(this Reservation reservation)
    {
        reservation.Confirmer();
        return reservation;
    }

    /// <summary>Passe la réservation Annulée par <see cref="Reservation.Annuler"/>.</summary>
    public static Reservation Annulee(this Reservation reservation)
    {
        reservation.Annuler();
        return reservation;
    }

    /// <summary>Insère les réservations en base ; leurs <c>Id</c> sont renseignés au retour.</summary>
    public static async Task InsererAsync(this AaasFactory factory, params Reservation[] reservations)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<Reservation>().AddRange(reservations);
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>Vide la table des réservations ; à faire avant de vider les animaux (suppression <c>Restrict</c>, AD-7).</summary>
    public static async Task ViderReservationsAsync(this AaasFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Reservation>().ExecuteDeleteAsync(Ct);
    }
}
