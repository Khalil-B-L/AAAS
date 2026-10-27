using System.Net;
using System.Text.Json;

namespace Aaas.Api.Tests.Outils;

/// <summary>Comptes de test : corps d'inscription et de connexion, inscription et connexion qui renvoient le jeton.</summary>
public static class Comptes
{
    public const string MotDePasse = "MotDePasse1";

    public static string NouvelEmail() => $"membre-{Guid.NewGuid():N}@exemple.fr";

    public static string Inscription(string email, string telephone = "72 123 456") =>
        JsonSerializer.Serialize(new { prenom = "Camille", nom = "Martin", email, telephone, motDePasse = MotDePasse });

    public static string Connexion(string email, string motDePasse) =>
        JsonSerializer.Serialize(new { email, motDePasse });

    public static async Task<string> InscrireAsync(this AaasFactory factory, string email)
    {
        var response = await factory.PostJsonAsync("/api/auth/inscription", Inscription(email));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        return doc.RootElement.GetProperty("jeton").GetString()!;
    }

    public static async Task<string> ConnecterAsync(this AaasFactory factory, string email, string motDePasse)
    {
        var response = await factory.PostJsonAsync("/api/auth/connexion", Connexion(email, motDePasse));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        return doc.RootElement.GetProperty("jeton").GetString()!;
    }
}
