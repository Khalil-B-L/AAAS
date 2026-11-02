using System.Linq.Expressions;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Common.Fichiers;

namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Carte animal (<c>contrats-api.md</c> §6) : seul format de carte de l'API (AD-1), réutilisé tel quel par les autres slices.
/// <c>Statut</c> est le statut effectif (AD-5). <c>PhotoPrincipaleUrl</c> est l'URL relative de la photo d'<c>Ordre</c> minimal, ou <c>null</c>.
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
    /// La clé de la photo d'<c>Ordre</c> minimal est lue en SQL ; son URL est construite par <paramref name="stockage"/>
    /// (AD-12), côté client : la projection doit rester la dernière opération de la requête.
    /// </summary>
    public static Expression<Func<Animal, AnimalCarteDto>> Projection<TReservation>(
        IQueryable<TReservation> reservations, DateTimeOffset now, IFileStorage stockage)
        where TReservation : class, IReservationStatut =>
        AnimalRules.StatutEffectif<Animal, TReservation>(reservations, now)
            .Composer((a, statut) => new AnimalCarteDto(a.Id, a.Nom, a.Espece, a.Sexe, a.Age, a.Taille, statut,
                stockage.UrlPubliqueOuNull(a.Photos.OrderBy(p => p.Ordre).Select(p => p.Cle).FirstOrDefault())));
}
