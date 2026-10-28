using System.Linq.Expressions;

namespace Aaas.Api.Common.Disponibilite;

/// <summary>
/// Prédicat unique du statut effectif d'un animal (AD-5), en expression traduite en SQL par EF Core
/// (sous-requête sur les réservations de l'animal, jamais évaluée en mémoire).
/// </summary>
public static class AnimalRules
{
    /// <summary>
    /// Statut effectif : un animal <see cref="StatutAnimal.Reserve"/> sans réservation active
    /// (<see cref="ReservationRules.EstActive{TReservation}"/>) est <see cref="StatutAnimal.Disponible"/> ; sinon le statut stocké.
    /// </summary>
    /// <param name="reservations">Source des réservations, typiquement <c>db.Set&lt;Reservation&gt;()</c>.</param>
    public static Expression<Func<TAnimal, StatutAnimal>> StatutEffectif<TAnimal, TReservation>(
        IQueryable<TReservation> reservations, DateTimeOffset now)
        where TAnimal : class, IAnimalStatut
        where TReservation : class, IReservationStatut
    {
        // Ne dépend pas de l'animal : EF Core l'intègre telle quelle à la sous-requête.
        var actives = reservations.Where(ReservationRules.EstActive<TReservation>(now));

        return a => a.Statut == StatutAnimal.Reserve && !actives.Any(r => r.AnimalId == a.Id)
            ? StatutAnimal.Disponible
            : a.Statut;
    }
}
