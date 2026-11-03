using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>Suppression d'un animal (<c>DELETE /api/admin/animaux/:id</c>).</summary>
public sealed record SupprimerAnimalCommand(int Id);

/// <summary>
/// Refusée (409 <c>animal-a-des-reservations</c>) dès qu'une réservation existe, quel que soit son statut. Sinon,
/// l'animal et ses photos sont supprimés dans la transaction (cascade, AD-7), puis les fichiers après le commit
/// (AD-12) : un échec est journalisé, la suppression reste faite. Conflit <c>RowVersion</c> → 409 <c>modifie-ailleurs</c>.
/// Transaction <c>Serializable</c> (comme <see cref="AjouterPhotoHandler"/>) : aucune photo ni réservation ne peut être
/// ajoutée entre la lecture et la suppression, donc toute photo supprimée en cascade a sa clé dans la liste des
/// fichiers à supprimer ; un interblocage est rejoué par <see cref="TransactionRunner"/>.
/// </summary>
public sealed class SupprimerAnimalHandler(
    AppDbContext db,
    TransactionRunner transactions,
    IFileStorage stockage,
    ILogger<SupprimerAnimalHandler> journal)
    : ICommandHandler<SupprimerAnimalCommand>
{
    public async Task<Result> HandleAsync(SupprimerAnimalCommand command, CancellationToken cancellationToken)
    {
        List<string> cles = [];
        Result resultat;
        try
        {
            resultat = await transactions.RunAsync(IsolationLevel.Serializable, async ct =>
            {
                var animal = await db.Set<Animal>()
                    .Include(a => a.Photos)
                    .SingleOrDefaultAsync(a => a.Id == command.Id, ct);
                if (animal is null)
                    return Result.NotFound();

                if (await db.Set<Reservation>().AnyAsync(r => r.AnimalId == command.Id, ct))
                    return RefusAnimal.AReservations();

                cles = animal.Photos.Select(p => p.Cle).ToList();
                db.Set<Animal>().Remove(animal);
                return Result.Ok();
            }, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return RefusAnimal.ModifieAilleurs();
        }
        catch (DbUpdateException ex) when (SqlErreurs.EstViolationCleEtrangere(ex))
        {
            // Réservation créée entre la vérification et la suppression : la clé étrangère Restrict refuse.
            return RefusAnimal.AReservations();
        }

        if (!resultat.EstSucces)
            return resultat;

        // Après le commit : un fichier non supprimé reste orphelin et journalisé, l'animal reste supprimé.
        foreach (var cle in cles)
        {
            try
            {
                stockage.Supprimer(cle);
            }
            catch (Exception ex)
            {
                journal.LogError(ex, "Fichier {Cle} non supprimé après la suppression de l'animal {AnimalId}.", cle, command.Id);
            }
        }
        return resultat;
    }
}
