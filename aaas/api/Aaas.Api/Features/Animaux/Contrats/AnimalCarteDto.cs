using System.Linq.Expressions;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;

namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Carte animal (<c>contrats-api.md</c> §6) : seul format de carte de l'API (AD-1), réutilisé tel quel par les autres slices.
/// <c>Statut</c> est le statut effectif (AD-5). <c>PhotoPrincipaleUrl</c> reste <c>null</c> tant que le stockage des photos n'existe pas.
/// </summary>
public sealed record AnimalCarteDto(
    int Id,
    string Nom,
    Espece Espece,
    Sexe Sexe,
    TrancheAge Age,
    Taille Taille,
    StatutAnimal Statut,
    string? PhotoPrincipaleUrl)
{
    /// <summary>
    /// Projection unique <see cref="Animal"/> → carte à l'instant <paramref name="now"/>, traduite en SQL : le statut est
    /// celui de <see cref="AnimalRules.StatutEffectif{TAnimal, TReservation}"/> sur <paramref name="reservations"/>.
    /// </summary>
    public static Expression<Func<Animal, AnimalCarteDto>> Projection<TReservation>(
        IQueryable<TReservation> reservations, DateTimeOffset now)
        where TReservation : class, IReservationStatut =>
        AnimalRules.StatutEffectif<Animal, TReservation>(reservations, now)
            .Composer((a, statut) => new AnimalCarteDto(a.Id, a.Nom, a.Espece, a.Sexe, a.Age, a.Taille, statut, null));
}
