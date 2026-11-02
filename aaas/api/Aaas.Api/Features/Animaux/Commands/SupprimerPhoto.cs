using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>Suppression d'une photo d'un animal (<c>DELETE /api/admin/animaux/:id/photos/:photoId</c>, AD-12).</summary>
public sealed record SupprimerPhotoCommand(int AnimalId, int PhotoId);

/// <summary>
/// Supprime la ligne dans la transaction, puis le fichier après le commit (AD-7, AD-12). Les ordres restants ne sont
/// pas renumérotés. Une photo d'un autre animal → 404.
/// </summary>
public sealed class SupprimerPhotoHandler(
    AppDbContext db,
    TransactionRunner transactions,
    IFileStorage stockage,
    ILogger<SupprimerPhotoHandler> journal)
    : ICommandHandler<SupprimerPhotoCommand>
{
    public async Task<Result> HandleAsync(SupprimerPhotoCommand command, CancellationToken cancellationToken)
    {
        string? cle = null;
        var resultat = await transactions.RunAsync(IsolationLevel.ReadCommitted, async ct =>
        {
            var photo = await db.Set<PhotoAnimal>()
                .SingleOrDefaultAsync(p => p.Id == command.PhotoId && p.AnimalId == command.AnimalId, ct);
            if (photo is null)
                return Result.NotFound();

            cle = photo.Cle;
            db.Set<PhotoAnimal>().Remove(photo);
            return Result.Ok();
        }, cancellationToken);

        if (resultat.EstSucces && cle is not null)
        {
            // Après le commit : un échec de suppression du fichier laisse un orphelin journalisé, la photo reste supprimée.
            try
            {
                stockage.Supprimer(cle);
            }
            catch (Exception ex)
            {
                journal.LogError(ex, "Fichier {Cle} non supprimé après la suppression de la photo {PhotoId}.", cle, command.PhotoId);
            }
        }
        return resultat;
    }
}
