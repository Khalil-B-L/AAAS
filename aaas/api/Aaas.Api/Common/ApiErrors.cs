using System.Text.Json.Serialization;

namespace Aaas.Api.Common;

/// <summary>
/// Corps unique de toute réponse d'erreur (AD-10) : <c>{ error }</c>, plus <c>errors</c> (400 par champ)
/// et/ou <c>code</c> (refus métier, <c>contrats-api.md</c> §5). Les propriétés nulles sont omises.
/// </summary>
public sealed record ErrorResponse(
    string Error,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyDictionary<string, string>? Errors = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Code = null);

/// <summary>Seules fabriques de réponses d'erreur HTTP de l'API (AD-10).</summary>
public static class ApiErrors
{
    public const string MessageValidation = "Certains champs sont invalides.";
    public const string MessageRequeteInvalide = "La requête est invalide.";
    public const string MessageNonConnecte = "Connectez-vous pour continuer.";
    public const string MessageInterdit = "Cette page est réservée à l'association.";
    public const string MessageIntrouvable = "La ressource demandée est introuvable.";
    public const string MessageMethodeNonAutorisee = "Cette méthode n'est pas autorisée pour cette adresse.";
    public const string MessageConflit = "L'opération est en conflit avec l'état actuel.";
    public const string MessageTypeNonPrisEnCharge = "Le type de contenu de la requête n'est pas pris en charge.";
    public const string MessageInterne = "Une erreur inattendue est survenue. Veuillez réessayer plus tard.";
    public const string MessageIndisponible = "Le service est momentanément indisponible. Veuillez réessayer plus tard.";
    public const string MessageErreur = "La requête n'a pas pu aboutir.";

    /// <summary>Message français par défaut d'un statut d'erreur.</summary>
    public static string MessageParDefaut(int statut) => statut switch
    {
        StatusCodes.Status400BadRequest => MessageRequeteInvalide,
        StatusCodes.Status401Unauthorized => MessageNonConnecte,
        StatusCodes.Status403Forbidden => MessageInterdit,
        StatusCodes.Status404NotFound => MessageIntrouvable,
        StatusCodes.Status405MethodNotAllowed => MessageMethodeNonAutorisee,
        StatusCodes.Status409Conflict => MessageConflit,
        StatusCodes.Status415UnsupportedMediaType => MessageTypeNonPrisEnCharge,
        StatusCodes.Status500InternalServerError => MessageInterne,
        StatusCodes.Status503ServiceUnavailable => MessageIndisponible,
        _ => MessageErreur,
    };

    /// <summary>Réponse d'erreur générique ; message par défaut du statut si absent.</summary>
    public static IResult Erreur(int statut, string? message = null, string? code = null) =>
        Results.Json(new ErrorResponse(message ?? MessageParDefaut(statut), Code: code), statusCode: statut);

    public static IResult BadRequest(string? message = null) => Erreur(StatusCodes.Status400BadRequest, message);

    /// <summary>400 avec erreurs par champ (clés = noms JSON camelCase).</summary>
    public static IResult Validation(IReadOnlyDictionary<string, string> errors, string? message = null) =>
        Results.Json(new ErrorResponse(message ?? MessageValidation, errors), statusCode: StatusCodes.Status400BadRequest);

    public static IResult Unauthorized(string? message = null) => Erreur(StatusCodes.Status401Unauthorized, message);

    public static IResult Forbidden(string? message = null) => Erreur(StatusCodes.Status403Forbidden, message);

    public static IResult NotFound(string? message = null) => Erreur(StatusCodes.Status404NotFound, message);

    /// <summary>Refus métier : statut + <c>code</c> stable de <c>contrats-api.md</c> §5.</summary>
    public static IResult Refus(int statut, string code, string message) => Erreur(statut, message, code);

    public static IResult Interne() => Erreur(StatusCodes.Status500InternalServerError);
}
