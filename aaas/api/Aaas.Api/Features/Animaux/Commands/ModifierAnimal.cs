using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Animaux.Queries;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>
/// Modification des champs d'édition d'un animal (<c>PUT /api/admin/animaux/:id</c>) : jamais le statut ni
/// <c>AdopteLe</c> (AD-4).
/// </summary>
public sealed record ModifierAnimalCommand(int Id, AnimalEcritureDto Champs);

/// <summary>
/// Valide les champs, les applique dans la transaction (<see cref="TransactionRunner"/>), puis relit la fiche par la
/// projection publiée après le commit. Le <c>PUT</c> ne porte pas de <c>RowVersion</c> (§6) : seul un conflit survenu
/// pendant la transaction (forçage de statut ou suppression concurrents) donne 409 <c>modifie-ailleurs</c>.
/// </summary>
public sealed class ModifierAnimalHandler(
    AppDbContext db,
    TransactionRunner transactions,
    IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto> fiche)
    : ICommandHandler<ModifierAnimalCommand, AnimalDetailDto>
{
    public async Task<Result<AnimalDetailDto>> HandleAsync(ModifierAnimalCommand command, CancellationToken cancellationToken)
    {
        var errors = command.Champs.Valider();
        if (errors.Count > 0)
            return Result.Invalid(errors);

        Result resultat;
        try
        {
            resultat = await transactions.RunAsync(IsolationLevel.ReadCommitted, async ct =>
            {
                var animal = await db.Set<Animal>().SingleOrDefaultAsync(a => a.Id == command.Id, ct);
                if (animal is null)
                    return Result.NotFound();

                command.Champs.Appliquer(animal);
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

/// <summary>Refus métier des commandes d'écriture d'un animal (<c>contrats-api.md</c> §5).</summary>
public static class RefusAnimal
{
    public const string CodeModifieAilleurs = "modifie-ailleurs";
    public const string MessageModifieAilleurs = "L'animal a été modifié entre-temps. Rechargez la fiche puis recommencez.";
    public const string CodeAReservations = "animal-a-des-reservations";
    public const string MessageAReservations = "Cet animal a des réservations : il ne peut pas être supprimé.";
    public const string CodeReservationActive = "reservation-active";
    public const string MessageReservationActive =
        "Cet animal a une réservation en cours : traitez-la avant de changer son statut.";

    /// <summary>409 <c>modifie-ailleurs</c> : conflit <c>RowVersion</c> sur l'animal.</summary>
    public static Echec ModifieAilleurs() => Result.Conflict(CodeModifieAilleurs, MessageModifieAilleurs);

    /// <summary>409 <c>animal-a-des-reservations</c> : au moins une réservation, quel que soit son statut.</summary>
    public static Echec AReservations() => Result.Conflict(CodeAReservations, MessageAReservations);

    /// <summary>409 <c>reservation-active</c> : forcer le statut d'un animal qui a une réservation active.</summary>
    public static Echec ReservationActive() => Result.Conflict(CodeReservationActive, MessageReservationActive);
}
