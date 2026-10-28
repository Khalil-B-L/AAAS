using Aaas.Api.Common.Disponibilite;

namespace Aaas.Api.Features.Reservations;

/// <summary>
/// Réservation d'un animal par un membre (AD-1 : seule la slice Reservations l'écrit). Créée
/// <see cref="StatutReservation.EnAttente"/> avec <see cref="ExpireLe"/> = création + <see cref="Delai"/> (AD-5) ;
/// le statut effectif se lit par <see cref="ReservationRules"/>, jamais par <see cref="Statut"/> seul.
/// </summary>
public class Reservation : IReservationStatut
{
    public const int ExperienceMax = 1000;

    /// <summary>Délai de confirmation par l'admin (AD-5).</summary>
    public static readonly TimeSpan Delai = TimeSpan.FromMinutes(15);

    /// <summary>Pour EF Core.</summary>
    private Reservation()
    {
    }

    /// <summary>Nouvelle réservation <see cref="StatutReservation.EnAttente"/>, créée maintenant (AD-11).</summary>
    public Reservation(int animalId, int userId, Logement logement, bool jardin, string? experience, TimeProvider temps)
    {
        AnimalId = animalId;
        UserId = userId;
        Logement = logement;
        Jardin = jardin;
        // Texte facultatif (AD-22) : trimé, vide → null.
        Experience = string.IsNullOrWhiteSpace(experience) ? null : experience.Trim();
        CreeLe = temps.GetUtcNow();
        ExpireLe = CreeLe + Delai;
    }

    public int Id { get; private set; }

    public int AnimalId { get; private set; }

    public int UserId { get; private set; }

    public StatutReservation Statut { get; private set; } = StatutReservation.EnAttente;

    public DateTimeOffset CreeLe { get; private set; }

    public DateTimeOffset ExpireLe { get; private set; }

    public Logement Logement { get; private set; }

    public bool Jardin { get; private set; }

    /// <summary>Texte facultatif du questionnaire (<c>null</c> si vide).</summary>
    public string? Experience { get; private set; }

    /// <summary>Jeton de concurrence des transitions (AD-18).</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// EnAttente → Confirmée. La commande vérifie avant l'appel que la réservation n'est pas échue (AD-5, AD-18).
    /// </summary>
    public void Confirmer()
    {
        ExigerEnAttente(nameof(Confirmer));
        Statut = StatutReservation.Confirmee;
    }

    /// <summary>
    /// EnAttente → Annulée. La commande vérifie avant l'appel que la réservation n'est pas échue (AD-5, AD-18).
    /// </summary>
    public void Annuler()
    {
        ExigerEnAttente(nameof(Annuler));
        Statut = StatutReservation.Annulee;
    }

    private void ExigerEnAttente(string transition)
    {
        if (Statut != StatutReservation.EnAttente)
            throw new TransitionReservationInterditeException(Statut, transition);
    }
}

/// <summary>
/// Transition de réservation appelée hors de <see cref="StatutReservation.EnAttente"/>. Erreur de programmation, jamais un refus
/// métier : les commandes de l'epic réservations vérifient l'état avant l'appel et répondent 409 (AD-18).
/// </summary>
public sealed class TransitionReservationInterditeException(StatutReservation depuis, string transition)
    : InvalidOperationException($"Transition « {transition} » interdite depuis le statut {depuis}.")
{
    public StatutReservation Depuis { get; } = depuis;

    public string Transition { get; } = transition;
}
