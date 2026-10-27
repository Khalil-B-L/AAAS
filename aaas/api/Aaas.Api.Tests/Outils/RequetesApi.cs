using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Aaas.Api.Tests.Outils;

/// <summary>Appels HTTP vers l'API de la factory (corps JSON en texte, jeton Bearer optionnel).</summary>
public static class RequetesApi
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static StringContent JsonContent(string json) => new(json, Encoding.UTF8, "application/json");

    public static Task<HttpResponseMessage> PostJsonAsync(this AaasFactory factory, string url, string json) =>
        factory.CreateClient().PostAsync(url, JsonContent(json), Ct);

    /// <summary>GET avec <c>Authorization: Bearer &lt;jeton&gt;</c>, ou sans en-tête si <paramref name="jeton"/> est <c>null</c>.</summary>
    public static Task<HttpResponseMessage> GetAvecJetonAsync(this AaasFactory factory, string url, string? jeton)
    {
        var client = factory.CreateClient();
        if (jeton is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jeton);
        return client.GetAsync(url, Ct);
    }

    public static Task<HttpResponseMessage> GetMoiAsync(this AaasFactory factory, string? jeton) =>
        factory.GetAvecJetonAsync("/api/auth/moi", jeton);

    public static async Task<JsonDocument> LireJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
}
