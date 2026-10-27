namespace Aaas.Api.Features.Animaux;

/// <summary>
/// Animal proposé à l'adoption (AD-1 : seule la slice Animaux l'écrit, sauf les transitions appelées par Reservations).
/// <see cref="Statut"/> ne change que par <see cref="Reserver"/>, <see cref="Liberer"/>, <see cref="MarquerAdopte"/>
/// et <see cref="ForcerStatut"/> (AD-4) ; l'heure vient toujours de <see cref="TimeProvider"/> (AD-11).
/// </summary>
public class Animal
{
    public const int NomMax = 50;
    public const int DescriptionMax = 2000;

    /// <summary>Pour EF Core.</summary>
    private Animal()
    {
    }

    /// <summary>Nouvel animal <see cref="StatutAnimal.Disponible"/>, créé maintenant.</summary>
    public Animal(TimeProvider temps)
    {
        CreeLe = temps.GetUtcNow();
    }

    public int Id { get; private set; }

    public string Nom { get; set; } = string.Empty;

    public Espece Espece { get; set; }

    public Sexe Sexe { get; set; }

    public TrancheAge Age { get; set; }

    public Taille Taille { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Vaccine { get; set; }

    public bool Sterilise { get; set; }

    public bool Puce { get; set; }

    public Entente EntenteEnfants { get; set; }

    public Entente EntenteChiens { get; set; }

    public Entente EntenteChats { get; set; }

    public StatutAnimal Statut { get; private set; } = StatutAnimal.Disponible;

    public DateTimeOffset CreeLe { get; private set; }

    /// <summary>Posée en passant <see cref="StatutAnimal.Adopte"/> ; <c>null</c> sinon.</summary>
    public DateTimeOffset? AdopteLe { get; private set; }

    /// <summary>Jeton de concurrence des transitions (AD-18).</summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>Disponible → Réservé.</summary>
    public void Reserver()
    {
        Exiger(StatutAnimal.Disponible, nameof(Reserver));
        Statut = StatutAnimal.Reserve;
    }

    /// <summary>Réservé → Disponible.</summary>
    public void Liberer()
    {
        Exiger(StatutAnimal.Reserve, nameof(Liberer));
        Statut = StatutAnimal.Disponible;
    }

    /// <summary>Réservé → Adopté ; pose <see cref="AdopteLe"/>.</summary>
    public void MarquerAdopte(TimeProvider temps)
    {
        Exiger(StatutAnimal.Reserve, nameof(MarquerAdopte));
        Statut = StatutAnimal.Adopte;
        AdopteLe = temps.GetUtcNow();
    }

    /// <summary>
    /// Forçage admin vers <see cref="StatutAnimal.Disponible"/> (efface <see cref="AdopteLe"/>) ou
    /// <see cref="StatutAnimal.Adopte"/> (pose <see cref="AdopteLe"/>), jamais vers <see cref="StatutAnimal.Reserve"/>.
    /// Aucun état source n'est exigé : un Réservé stocké dont la réservation est échue est effectivement Disponible (AD-5).
    /// Le refus « réservation active » (409 <c>reservation-active</c>) est vérifié par la commande avant l'appel.
    /// </summary>
    public void ForcerStatut(StatutAnimal statut, TimeProvider temps)
    {
        switch (statut)
        {
            case StatutAnimal.Disponible:
                Statut = StatutAnimal.Disponible;
                AdopteLe = null;
                break;
            case StatutAnimal.Adopte:
                // AdopteLe n'est posée qu'à l'entrée en Adopté : un forçage répété garde la date d'adoption.
                if (Statut != StatutAnimal.Adopte)
                    AdopteLe = temps.GetUtcNow();
                Statut = StatutAnimal.Adopte;
                break;
            default:
                throw new TransitionInterditeException(Statut, $"{nameof(ForcerStatut)}({statut})");
        }
    }

    private void Exiger(StatutAnimal source, string transition)
    {
        if (Statut != source)
            throw new TransitionInterditeException(Statut, transition);
    }
}
