using System.Net.Http.Headers;

namespace Aaas.Api.Common.Fichiers;

/// <summary>Type de fichier accepté : extension de la clé, type MIME servi et signature (premiers octets).</summary>
public sealed record TypeFichier(string Extension, string TypeMime, Func<ReadOnlyMemory<byte>, bool> ASignature)
{
    public static readonly TypeFichier Jpeg = new("jpg", "image/jpeg", c => c.Span.StartsWith((ReadOnlySpan<byte>)[0xFF, 0xD8, 0xFF]));

    public static readonly TypeFichier Png = new("png", "image/png",
        c => c.Span.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));

    // RIFF <taille sur 4 octets> WEBP
    public static readonly TypeFichier WebP = new("webp", "image/webp",
        c => c.Length >= 12 && c.Span[..4].SequenceEqual("RIFF"u8) && c.Span[8..12].SequenceEqual("WEBP"u8));

    public static readonly IReadOnlyList<TypeFichier> Acceptes = [Jpeg, Png, WebP];
}

/// <summary>Fichier reçu et validé par <see cref="RegleFichier"/> : contenu complet et type détecté.</summary>
public sealed record FichierValide(byte[] Contenu, TypeFichier Type);

/// <summary>
/// Seule règle de validation des fichiers envoyés (AD-12) : un unique fichier non vide, JPEG, PNG ou WebP vérifié par
/// type MIME déclaré <b>et</b> signature, 5 Mo maximum ; 10 photos maximum par animal. Tout refus → 400 <c>errors.fichier</c>.
/// </summary>
public static class RegleFichier
{
    /// <summary>Nom du champ <c>multipart/form-data</c> et clé de <c>errors</c>.</summary>
    public const string Champ = "fichier";

    public const long TailleMax = 5 * 1024 * 1024;

    public const int PhotosMaxParAnimal = 10;

    public const string MessageAbsent = "Choisissez une photo à envoyer.";
    public const string MessagePlusieurs = "Envoyez une seule photo par requête.";
    public const string MessageTaille = "La photo ne doit pas dépasser 5 Mo.";
    public const string MessageType = "Seules les photos JPEG, PNG ou WebP sont acceptées.";
    public const string MessageIllisible = "Le formulaire envoyé est illisible : envoyez à nouveau la photo.";
    public const string MessageNombre = "Cet animal a déjà 10 photos : supprimez-en une avant d'en ajouter une autre.";

    /// <summary>Refus 400 <c>errors.fichier</c>.</summary>
    public static Echec Refus(string message) => Result.Invalid(new Dictionary<string, string> { [Champ] = message });

    /// <summary>
    /// Valide les fichiers reçus dans le champ <see cref="Champ"/> : un seul, non vide, au plus <see cref="TailleMax"/>,
    /// type MIME déclaré égal au type détecté par la signature. Renvoie le contenu lu, ou le refus.
    /// </summary>
    public static async Task<(FichierValide? Fichier, Echec? Refus)> ValiderAsync(
        IReadOnlyList<IFormFile> fichiers, CancellationToken cancellationToken)
    {
        if (fichiers.Count > 1)
            return (null, Refus(MessagePlusieurs));
        if (fichiers.Count == 0 || fichiers[0].Length == 0)
            return (null, Refus(MessageAbsent));

        var fichier = fichiers[0];
        if (fichier.Length > TailleMax)
            return (null, Refus(MessageTaille));

        var declare = MediaTypeHeaderValue.TryParse(fichier.ContentType, out var media) ? media.MediaType : null;
        var type = TypeFichier.Acceptes.FirstOrDefault(t => string.Equals(t.TypeMime, declare, StringComparison.OrdinalIgnoreCase));
        if (type is null)
            return (null, Refus(MessageType));

        var contenu = new byte[fichier.Length];
        await using (var flux = fichier.OpenReadStream())
            await flux.ReadExactlyAsync(contenu, cancellationToken);

        return type.ASignature(contenu) ? (new FichierValide(contenu, type), null) : (null, Refus(MessageType));
    }

    /// <summary>Refus si l'animal a déjà <see cref="PhotosMaxParAnimal"/> photos, <c>null</c> sinon.</summary>
    public static Echec? ValiderNombre(int photosExistantes) =>
        photosExistantes >= PhotosMaxParAnimal ? Refus(MessageNombre) : null;
}
