using System.Net.Http.Headers;
using System.Text;

namespace Aaas.Api.Tests.Outils;

/// <summary>
/// Contenus de fichiers de test (JPEG, PNG, WebP minimaux valides, faux JPEG, fichier trop gros) et appels des routes
/// de photos (<c>multipart/form-data</c>, champ <c>fichier</c>).
/// </summary>
public static class Fichiers
{
    public const int CinqMo = 5 * 1024 * 1024;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>JPEG (signature FF D8 FF, en-tête JFIF) de <paramref name="taille"/> octets, terminé par FF D9.</summary>
    public static byte[] Jpeg(int taille = 200 * 1024)
    {
        byte[] entete = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, (byte)'J', (byte)'F', (byte)'I', (byte)'F', 0x00, 0x01, 0x01, 0x00];
        var contenu = new byte[taille];
        entete.CopyTo(contenu, 0);
        contenu[^2] = 0xFF;
        contenu[^1] = 0xD9;
        return contenu;
    }

    /// <summary>PNG 1×1 valide.</summary>
    public static byte[] Png() => Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==");

    /// <summary>WebP 1×1 valide (RIFF … WEBP VP8).</summary>
    public static byte[] WebP() => Convert.FromBase64String("UklGRiIAAABXRUJQVlA4IBYAAAAwAQCdASoBAAEADsD+JaQAA3AAAAAA");

    /// <summary>GIF 1×1 valide (type refusé).</summary>
    public static byte[] Gif() => Convert.FromBase64String("R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==");

    /// <summary>Début de PDF (type refusé).</summary>
    public static byte[] Pdf() => Encoding.ASCII.GetBytes("%PDF-1.4\n%âãÏÓ\n1 0 obj\n<<>>\nendobj\n%%EOF\n");

    /// <summary>Texte qui se fait passer pour un JPEG.</summary>
    public static byte[] Texte() => Encoding.UTF8.GetBytes("Ceci n'est pas une photo.");

    /// <summary>Corps multipart avec un fichier par entrée de <paramref name="fichiers"/>, sous le champ <paramref name="champ"/>.</summary>
    public static MultipartFormDataContent Formulaire(string champ, params (byte[] Contenu, string TypeMime)[] fichiers)
    {
        var formulaire = new MultipartFormDataContent();
        foreach (var (contenu, typeMime) in fichiers)
        {
            var partie = new ByteArrayContent(contenu);
            partie.Headers.ContentType = new MediaTypeHeaderValue(typeMime);
            formulaire.Add(partie, champ, "photo-du-client.bin");
        }
        return formulaire;
    }

    public static Task<HttpResponseMessage> EnvoyerPhotoAsync(this AaasFactory factory, int animalId, string? jeton,
        byte[] contenu, string typeMime = "image/jpeg") =>
        factory.EnvoyerAsync(animalId, jeton, Formulaire("fichier", (contenu, typeMime)));

    public static Task<HttpResponseMessage> EnvoyerAsync(this AaasFactory factory, int animalId, string? jeton, HttpContent corps) =>
        Client(factory, jeton).PostAsync($"/api/admin/animaux/{animalId}/photos", corps, Ct);

    public static Task<HttpResponseMessage> SupprimerPhotoAsync(this AaasFactory factory, int animalId, int photoId, string? jeton) =>
        Client(factory, jeton).DeleteAsync($"/api/admin/animaux/{animalId}/photos/{photoId}", Ct);

    /// <summary>Noms des fichiers présents dans le dossier de stockage de la factory.</summary>
    public static string[] FichiersStockes(this AaasFactory factory) =>
        Directory.Exists(factory.PhotosPath)
            ? Directory.GetFiles(factory.PhotosPath).Select(f => Path.GetFileName(f)).Order().ToArray()
            : [];

    private static HttpClient Client(AaasFactory factory, string? jeton)
    {
        var client = factory.CreateClient();
        if (jeton is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return client;
    }
}
