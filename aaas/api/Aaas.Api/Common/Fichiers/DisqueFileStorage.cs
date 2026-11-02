using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Fichiers;

/// <summary>
/// <see cref="IFileStorage"/> sur le disque local, dans le dossier <c>Storage:PhotosPath</c> (relatif au dossier de l'API
/// s'il n'est pas absolu). Seule classe de l'API qui touche au système de fichiers.
/// </summary>
public sealed class DisqueFileStorage(IOptions<StorageOptions> options, IHostEnvironment environnement) : IFileStorage
{
    public const string PrefixeUrl = "/fichiers/";

    /// <summary>Dossier absolu des fichiers.</summary>
    public string Racine { get; } = Path.GetFullPath(Path.Combine(environnement.ContentRootPath, options.Value.PhotosPath));

    public async Task EcrireAsync(string cle, Stream contenu, CancellationToken cancellationToken)
    {
        var chemin = Chemin(cle);
        Directory.CreateDirectory(Racine);
        await using var fichier = new FileStream(chemin, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await contenu.CopyToAsync(fichier, cancellationToken);
    }

    public Stream? Ouvrir(string cle)
    {
        if (!CleFichier.EstValide(cle))
            return null;
        try
        {
            return new FileStream(Chemin(cle), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 81920, useAsync: true);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public void Supprimer(string cle)
    {
        var chemin = Chemin(cle);
        // Dossier pas encore créé (aucun fichier écrit) : rien à supprimer ; File.Delete lèverait DirectoryNotFoundException.
        if (Directory.Exists(Racine))
            File.Delete(chemin);
    }

    public string UrlPublique(string cle) => PrefixeUrl + cle;

    /// <summary>Chemin du fichier ; la clé est validée avant tout accès (aucun chemin construit depuis une saisie libre).</summary>
    private string Chemin(string cle) => CleFichier.EstValide(cle)
        ? Path.Combine(Racine, cle)
        : throw new ArgumentException($"Clé de fichier mal formée : « {cle} ».", nameof(cle));
}
