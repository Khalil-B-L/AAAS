using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Features.Reservations;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests;

/// <summary>Entité Réservation (story 2.4) : texte facultatif et transitions gardées.</summary>
public class ReservationTests
{
    private static Reservation Nouvelle(string? experience = null) =>
        new(animalId: 1, userId: 1, Logement.Maison, jardin: true, experience,
            new FakeTimeProvider(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero)));

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("   ", null)]
    [InlineData("  Deux chats déjà adoptés.  ", "Deux chats déjà adoptés.")]
    public void Experience_est_trimee_et_vide_devient_null(string? saisie, string? attendu) =>
        Assert.Equal(attendu, Nouvelle(saisie).Experience);

    [Fact]
    public void Confirmer_ou_annuler_hors_EnAttente_leve_TransitionReservationInterditeException()
    {
        var reservation = Nouvelle();
        reservation.Confirmer();

        var exception = Assert.Throws<TransitionReservationInterditeException>(reservation.Annuler);

        Assert.Equal(StatutReservation.Confirmee, exception.Depuis);
        Assert.Equal(nameof(Reservation.Annuler), exception.Transition);
    }
}
