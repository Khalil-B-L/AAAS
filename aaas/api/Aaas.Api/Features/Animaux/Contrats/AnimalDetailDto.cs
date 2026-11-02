using System.Linq.Expressions;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Common.Fichiers;

namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Fiche animal (<c>contrats-api.md</c> §6) : champs de la carte (<see cref="AnimalCarteDto"/>) puis santé, ententes,
/// dates et photos. DTO plat : seule imbrication, <see cref="PhotoDto"/> (triées par <c>Ordre</c>, URLs relatives).
/// <c>Statut</c> est le statut effectif (AD-5) ; <c>AdopteLe</c> vaut <c>null</c> sauf pour un animal Adopté.
/// Format unique de la fiche, réutilisé tel quel par l'admin.
/// </summary>
public sealed record AnimalDetailDto(
    int Id,
    string Nom,
    Espece Espece,
    Sexe Sexe,
    TrancheAge Age,
    Taille Taille,
    StatutAnimal Statut,
    string? PhotoPrincipaleUrl,
    string Description,
    bool Vaccine,
    bool Sterilise,
    bool Puce,
    Entente EntenteEnfants,
    Entente EntenteChiens,
    Entente EntenteChats,
    DateTimeOffset CreeLe,
    DateTimeOffset? AdopteLe,
    IReadOnlyList<PhotoDto> Photos)
{
    /// <summary>
    /// Projection unique <see cref="Animal"/> → fiche à l'instant <paramref name="now"/>, traduite en SQL : le statut est
    /// celui de <see cref="AnimalRules.StatutEffectif{TAnimal, TReservation}"/> sur <paramref name="reservations"/>.
    /// Les clés des photos sont lues en SQL ; leurs URLs sont construites par <paramref name="stockage"/> (AD-12), côté
    /// client : la projection doit rester la dernière opération de la requête.
    /// </summary>
    public static Expression<Func<Animal, AnimalDetailDto>> Projection<TReservation>(
        IQueryable<TReservation> reservations, DateTimeOffset now, IFileStorage stockage)
        where TReservation : class, IReservationStatut =>
        AnimalRules.StatutEffectif<Animal, TReservation>(reservations, now)
            .Composer((a, statut) => new AnimalDetailDto(
                a.Id,
                a.Nom,
                a.Espece,
                a.Sexe,
                a.Age,
                a.Taille,
                statut,
                stockage.UrlPubliqueOuNull(a.Photos.OrderBy(p => p.Ordre).Select(p => p.Cle).FirstOrDefault()),
                a.Description,
                a.Vaccine,
                a.Sterilise,
                a.Puce,
                a.EntenteEnfants,
                a.EntenteChiens,
                a.EntenteChats,
                a.CreeLe,
                statut == StatutAnimal.Adopte ? a.AdopteLe : null,
                a.Photos
                    .OrderBy(p => p.Ordre)
                    .Select(p => new PhotoDto(p.Id, stockage.UrlPublique(p.Cle), p.Ordre))
                    .ToList()));
}
