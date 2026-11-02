using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Commands;
using Aaas.Api.Features.Animaux.Contrats;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux;

/// <summary>
/// Photos de démonstration (entrée 7) : au démarrage, après <see cref="DemoAnimaux"/>, en <c>Development</c> seulement,
/// chaque animal de démonstration (reconnu par son nom et son espèce) <b>sans aucune photo</b> reçoit son illustration
/// <c>Demo/Photos/&lt;nom&gt;.png</c> (générée par <c>Demo/generer-illustrations.mjs</c>, copiée avec la sortie de build).
/// L'ajout passe par <see cref="AjouterPhotoHandler"/>, comme un envoi admin. Jamais en <c>Testing</c>.
/// </summary>
public static class DemoPhotos
{
    /// <summary>Dossier des illustrations dans la sortie de build.</summary>
    public static string DossierParDefaut => Path.Combine(AppContext.BaseDirectory, "Demo", "Photos");

    /// <summary>Fichier de l'illustration d'un animal de démonstration.</summary>
    public static string Fichier(string dossier, string nom) => Path.Combine(dossier, $"{nom.ToLowerInvariant()}.png");

    public static async Task ExecuterAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using (var scope = services.CreateScope())
        {
            if (!scope.ServiceProvider.GetRequiredService<IHostEnvironment>().IsDevelopment())
                return;
        }

        await AttacherAsync(services, DossierParDefaut, cancellationToken);
    }

    /// <summary>
    /// Rattache les illustrations de <paramref name="dossier"/> aux animaux de démonstration sans photo, sans regarder
    /// l'environnement. Une illustration absente ou refusée est sautée avec un avertissement : le démarrage continue.
    /// </summary>
    internal static async Task AttacherAsync(IServiceProvider services, string dossier, CancellationToken cancellationToken)
    {
        var journal = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoPhotos));

        List<(int Id, string Nom)> sansPhoto;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var noms = DemoAnimaux.Noms.ToArray();
            sansPhoto = (await db.Set<Animal>()
                    .Where(a => noms.Contains(a.Nom) && !db.Set<PhotoAnimal>().Any(p => p.AnimalId == a.Id))
                    .OrderBy(a => a.Id)
                    .Select(a => new { a.Id, a.Nom, a.Espece })
                    .ToListAsync(cancellationToken))
                // Nom ET espèce du modèle : un chat nommé « Rex » par l'admin n'est pas l'animal de démonstration.
                .Where(a => DemoAnimaux.Identites.Contains((a.Nom, a.Espece)))
                .Select(a => (a.Id, a.Nom))
                .ToList();
        }

        var ajoutees = 0;
        foreach (var (id, nom) in sansPhoto)
        {
            var chemin = Fichier(dossier, nom);
            if (!File.Exists(chemin))
            {
                journal.LogWarning("Illustration de démonstration absente pour {Nom} : {Chemin}.", nom, chemin);
                continue;
            }

            var contenu = await File.ReadAllBytesAsync(chemin, cancellationToken);
            await using var flux = new MemoryStream(contenu, writable: false);
            var fichier = new FormFile(flux, 0, contenu.Length, "fichier", Path.GetFileName(chemin))
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png",
            };

            // Un scope par animal : un refus ou un échec ne laisse rien dans le contexte des suivants.
            using var scope = services.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<AjouterPhotoCommand, PhotoDto>>();
            var resultat = await handler.HandleAsync(new AjouterPhotoCommand(id, [fichier]), cancellationToken);
            if (resultat.EstSucces)
            {
                ajoutees++;
                continue;
            }

            var echec = resultat.Echec!;
            journal.LogWarning("Illustration de démonstration refusée pour {Nom} : {Motif}.", nom,
                echec.Errors?.GetValueOrDefault(Common.Fichiers.RegleFichier.Champ) ?? echec.Message);
        }

        if (ajoutees > 0)
            journal.LogInformation("Photos de démonstration ajoutées : {Nombre} (Development).", ajoutees);
    }
}
