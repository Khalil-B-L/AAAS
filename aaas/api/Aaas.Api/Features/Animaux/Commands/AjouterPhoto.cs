using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>
/// Ajout d'une photo à un animal (<c>POST /api/admin/animaux/:id/photos</c>, AD-12). <paramref name="Fichiers"/> : les
/// fichiers reçus dans le champ <c>fichier</c> (un seul attendu).
/// </summary>
public sealed record AjouterPhotoCommand(int AnimalId, IReadOnlyList<IFormFile> Fichiers);

/// <summary>
/// Dans la transaction (<see cref="TransactionRunner"/>, <c>Serializable</c> pour le nombre de photos et l'ordre) :
/// animal existant, moins de 10 photos, écriture du fichier, <c>Ordre = max + 1</c>. Si la transaction échoue
/// (refus ou exception), le fichier écrit est supprimé.
/// </summary>
public sealed class AjouterPhotoHandler(
    AppDbContext db,
    TransactionRunner transactions,
    IFileStorage stockage,
    ILogger<AjouterPhotoHandler> journal)
    : ICommandHandler<AjouterPhotoCommand, PhotoDto>
{
    public async Task<Result<PhotoDto>> HandleAsync(AjouterPhotoCommand command, CancellationToken cancellationToken)
    {
        // Taille, type et signature : sans base, avant toute écriture.
        var (fichier, refus) = await RegleFichier.ValiderAsync(command.Fichiers, cancellationToken);
        if (refus is not null)
            return refus;

        // Clé générée une fois : un nouvel essai sur interblocage réécrit le même fichier au lieu d'en laisser un orphelin.
        var cle = CleFichier.Nouvelle(fichier!.Type);
        PhotoAnimal? photo = null;

        try
        {
            var resultat = await transactions.RunAsync(IsolationLevel.Serializable, async ct =>
            {
                if (!await db.Set<Animal>().AnyAsync(a => a.Id == command.AnimalId, ct))
                    return Result<PhotoDto>.NotFound();

                var ordres = db.Set<PhotoAnimal>().Where(p => p.AnimalId == command.AnimalId).Select(p => p.Ordre);
                if (RegleFichier.ValiderNombre(await ordres.CountAsync(ct)) is { } tropDePhotos)
                    return tropDePhotos;
                var ordre = (await ordres.MaxAsync(o => (int?)o, ct) ?? 0) + 1;

                // Fichier écrit avant le commit (AD-12).
                await using (var contenu = new MemoryStream(fichier.Contenu, writable: false))
                    await stockage.EcrireAsync(cle, contenu, ct);

                photo = new PhotoAnimal(command.AnimalId, cle, ordre);
                db.Set<PhotoAnimal>().Add(photo);
                // Id renseigné après le SaveChanges du runner : le DTO est construit après le commit.
                return Result.Ok(new PhotoDto(0, stockage.UrlPublique(cle), ordre));
            }, cancellationToken);

            if (!resultat.EstSucces)
            {
                SupprimerSansEchec(cle);
                return resultat;
            }
            return Result.Ok(resultat.Valeur! with { Id = photo!.Id });
        }
        catch
        {
            // Commit en échec : le fichier écrit ne doit pas rester sans ligne en base ; l'erreur est propagée.
            SupprimerSansEchec(cle);
            throw;
        }
    }

    private void SupprimerSansEchec(string cle)
    {
        try
        {
            stockage.Supprimer(cle);
        }
        catch (Exception ex)
        {
            journal.LogError(ex, "Fichier {Cle} non supprimé après l'échec de l'ajout de la photo.", cle);
        }
    }
}
