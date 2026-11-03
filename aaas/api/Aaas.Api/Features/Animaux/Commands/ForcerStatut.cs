using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Animaux.Queries;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>
/// Forçage admin du statut d'un animal (<c>POST /api/admin/animaux/:id/statut</c>) vers Disponible ou Adopté, hors du
/// parcours de réservation (CAP-7, AD-4, AD-18).
/// </summary>
public sealed record ForcerStatutCommand(int Id, StatutAnimalDto Corps);

/// <summary>
/// Refus dans l'ordre : 400 (corps), 404 (animal inconnu), 409 <c>modifie-ailleurs</c> si le statut effectif (AD-5)
/// diffère de <c>statutAttendu</c>, 409 <c>reservation-active</c> si une réservation de l'animal est active. Statut
/// effectif et réservation active sont évalués en SQL dans la transaction, au même <c>now</c>. La transition passe par
/// <see cref="Animal.ForcerStatut"/> ; un conflit <c>RowVersion</c> à l'enregistrement (réservation ou forçage
/// concurrents) donne 409 <c>modifie-ailleurs</c>. Succès : fiche relue par la projection publiée après le commit.
/// Aucune réservation n'est modifiée : forcer un Réservé échu laisse sa réservation <c>EnAttente</c> échue telle quelle.
/// </summary>
public sealed class ForcerStatutHandler(
    AppDbContext db,
    TransactionRunner transactions,
    TimeProvider temps,
    IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto> fiche)
    : ICommandHandler<ForcerStatutCommand, AnimalDetailDto>
{
    public async Task<Result<AnimalDetailDto>> HandleAsync(ForcerStatutCommand command, CancellationToken cancellationToken)
    {
        var errors = command.Corps.Valider();
        if (errors.Count > 0)
            return Result.Invalid(errors);

        var statut = command.Corps.Statut!.Value;
        var statutAttendu = command.Corps.StatutAttendu!.Value;

        Result resultat;
        try
        {
            resultat = await transactions.RunAsync(IsolationLevel.ReadCommitted, async ct =>
            {
                var animal = await db.Set<Animal>().SingleOrDefaultAsync(a => a.Id == command.Id, ct);
                if (animal is null)
                    return Result.NotFound();

                var now = temps.GetUtcNow();
                var reservations = db.Set<Reservation>().AsNoTracking();

                var statutEffectif = await db.Set<Animal>().AsNoTracking()
                    .Where(a => a.Id == command.Id)
                    .Select(AnimalRules.StatutEffectif<Animal, Reservation>(reservations, now)
                        .Composer((_, s) => (StatutAnimal?)s))
                    .SingleOrDefaultAsync(ct);
                // Supprimé par une autre transaction depuis le chargement (ReadCommitted).
                if (statutEffectif is null)
                    return Result.NotFound();
                if (statutEffectif != statutAttendu)
                    return RefusAnimal.ModifieAilleurs();

                if (await reservations.Where(ReservationRules.EstActive<Reservation>(now))
                        .AnyAsync(r => r.AnimalId == command.Id, ct))
                    return RefusAnimal.ReservationActive();

                animal.ForcerStatut(statut, temps);
                return Result.Ok();
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return RefusAnimal.ModifieAilleurs();
        }

        if (resultat.Echec is { } echec)
            return echec;
        return await fiche.HandleAsync(new ObtenirAnimalQuery(command.Id), cancellationToken);
    }
}
