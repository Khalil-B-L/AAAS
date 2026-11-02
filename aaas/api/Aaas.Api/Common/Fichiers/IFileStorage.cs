using System.Text.RegularExpressions;

namespace Aaas.Api.Common.Fichiers;

/// <summary>
/// Seul accès aux fichiers de l'API (AD-12) : lecture, écriture, suppression et URL publique. Les fichiers sont désignés
/// par une clé générée par le serveur (<see cref="CleFichier"/>) ; la base ne stocke que cette clé, jamais un chemin.
/// </summary>
public interface IFileStorage
{
    /// <summary>Écrit (ou remplace) le fichier de clé <paramref name="cle"/>. Clé mal formée → <see cref="ArgumentException"/>.</summary>
    Task EcrireAsync(string cle, Stream contenu, CancellationToken cancellationToken);

    /// <summary>Flux en lecture du fichier, ou <c>null</c> si la clé est mal formée ou si le fichier n'existe pas.</summary>
    Stream? Ouvrir(string cle);

    /// <summary>Supprime le fichier ; sans effet s'il n'existe pas. Clé mal formée → <see cref="ArgumentException"/>.</summary>
    void Supprimer(string cle);

    /// <summary>URL relative servie par <c>GET /fichiers/:cle</c> : <c>/fichiers/&lt;clé&gt;</c>.</summary>
    string UrlPublique(string cle);
}

public static class FileStorageExtensions
{
    /// <summary>
    /// <see cref="IFileStorage.UrlPublique"/>, ou <c>null</c> sans clé. Utilisable dans une projection EF :
    /// la clé est lue en SQL, l'URL construite côté client.
    /// </summary>
    public static string? UrlPubliqueOuNull(this IFileStorage stockage, string? cle) =>
        cle is null ? null : stockage.UrlPublique(cle);
}

/// <summary>Format unique des clés de fichier : GUID en 32 chiffres hexadécimaux minuscules + extension du type détecté.</summary>
public static partial class CleFichier
{
    /// <summary>Nouvelle clé pour un fichier du type donné, ex. <c>3f2a…9c.jpg</c>.</summary>
    public static string Nouvelle(TypeFichier type) => $"{Guid.NewGuid():N}.{type.Extension}";

    /// <summary>Vrai si <paramref name="cle"/> a exactement le format d'une clé générée par le serveur.</summary>
    public static bool EstValide(string? cle) => cle is not null && Format().IsMatch(cle);

    /// <summary>Type MIME servi pour la clé (d'après son extension), ou <c>null</c> si la clé est mal formée.</summary>
    public static string? TypeMime(string cle) =>
        EstValide(cle) ? TypeFichier.Acceptes.First(t => cle.EndsWith("." + t.Extension, StringComparison.Ordinal)).TypeMime : null;

    [GeneratedRegex("^[0-9a-f]{32}\\.(jpg|png|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex Format();
}
