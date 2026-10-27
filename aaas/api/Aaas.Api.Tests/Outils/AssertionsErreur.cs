using System.Net;
using System.Text.Json;
using Aaas.Api.Common;

namespace Aaas.Api.Tests.Outils;

/// <summary>Format d'erreur unique de l'API (AD-10) : <c>{ error[, code] }</c> et <c>{ error, errors }</c>.</summary>
public static class AssertionsErreur
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Vérifie le corps <c>{ error[, code] }</c> (JSON, aucune autre propriété) et renvoie le texte brut.</summary>
    public static async Task<string> AssertErreurAsync(HttpResponseMessage response, string message, string? code = null)
    {
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadAsStringAsync(Ct);
        using var doc = JsonDocument.Parse(json);
        var racine = doc.RootElement;

        Assert.Equal(code is null ? ["error"] : ["error", "code"], racine.EnumerateObject().Select(p => p.Name));
        Assert.Equal(message, racine.GetProperty("error").GetString());
        if (code is not null)
            Assert.Equal(code, racine.GetProperty("code").GetString());
        return json;
    }

    /// <summary>Vérifie un 400 <c>{ error, errors }</c> et renvoie les erreurs par champ.</summary>
    public static async Task<Dictionary<string, string>> AssertValidationAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = await RequetesApi.LireJsonAsync(response);
        Assert.Equal(["error", "errors"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(ApiErrors.MessageValidation, doc.RootElement.GetProperty("error").GetString());
        return doc.RootElement.GetProperty("errors").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
    }
}
