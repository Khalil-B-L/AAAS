using System.Linq.Expressions;

namespace Aaas.Api.Common.Disponibilite;

/// <summary>
/// Prédicats uniques du statut effectif d'une réservation (AD-5), en expressions traduites en SQL par EF Core.
/// <c>now</c> vient toujours de <see cref="TimeProvider"/> (AD-11).
/// </summary>
public static class ReservationRules
{
    /// <summary>Active : <see cref="StatutReservation.EnAttente"/> et <c>ExpireLe &gt; now</c>.</summary>
    public static Expression<Func<TReservation, bool>> EstActive<TReservation>(DateTimeOffset now)
        where TReservation : class, IReservationStatut =>
        r => r.Statut == StatutReservation.EnAttente && r.ExpireLe > now;

    /// <summary>Statut effectif : une réservation <see cref="StatutReservation.EnAttente"/> échue est <see cref="StatutReservation.Expiree"/>.</summary>
    public static Expression<Func<TReservation, StatutReservation>> StatutEffectif<TReservation>(DateTimeOffset now)
        where TReservation : class, IReservationStatut =>
        r => r.Statut == StatutReservation.EnAttente && r.ExpireLe <= now ? StatutReservation.Expiree : r.Statut;
}
