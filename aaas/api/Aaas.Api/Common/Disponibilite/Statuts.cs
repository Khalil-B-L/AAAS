namespace Aaas.Api.Common.Disponibilite;

// Énumérations de statut de contrats-api.md §1 (AD-19), partagées par les prédicats du statut effectif (AD-5).
// Elles vivent ici pour que Common/ ne dépende d'aucune slice ; stockées et sérialisées en chaîne.

/// <summary>Statut stocké d'un animal ; ne change que par les transitions de l'entité Animal (AD-4).</summary>
public enum StatutAnimal
{
    Disponible,
    Reserve,
    Adopte,
}

/// <summary>Statut stocké d'une réservation ; le statut effectif vient de <see cref="ReservationRules.StatutEffectif{TReservation}"/>.</summary>
public enum StatutReservation
{
    EnAttente,
    Confirmee,
    Annulee,
    Expiree,
}

/// <summary>Ce que les prédicats du statut effectif lisent d'un animal.</summary>
public interface IAnimalStatut
{
    int Id { get; }

    StatutAnimal Statut { get; }
}

/// <summary>Ce que les prédicats du statut effectif lisent d'une réservation.</summary>
public interface IReservationStatut
{
    int AnimalId { get; }

    StatutReservation Statut { get; }

    DateTimeOffset ExpireLe { get; }
}
