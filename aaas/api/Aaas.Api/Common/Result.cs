namespace Aaas.Api.Common;

/// <summary>
/// Échec d'un cas d'usage : statut HTTP, message français, code de refus métier ou erreurs par champ.
/// Converti en HTTP uniquement par <see cref="ResultHttpExtensions"/>.
/// </summary>
public sealed record Echec(int Statut, string Message, string? Code = null, IReadOnlyDictionary<string, string>? Errors = null);

/// <summary>
/// Résultat sans valeur d'un cas d'usage (succès → 204). Porte aussi les fabriques d'échec communes :
/// <c>return Result.NotFound(...)</c> se convertit implicitement en <see cref="Result"/> comme en <see cref="Result{T}"/>.
/// </summary>
public sealed class Result
{
    private static readonly Result Succes = new(null);

    private Result(Echec? echec) => Echec = echec;

    /// <summary>Échec, ou <c>null</c> en cas de succès.</summary>
    public Echec? Echec { get; }

    public bool EstSucces => Echec is null;

    public static Result Ok() => Succes;

    public static Result<T> Ok<T>(T valeur) => Result<T>.Ok(valeur);

    /// <summary>404, message par défaut si absent.</summary>
    public static Echec NotFound(string? message = null) =>
        new(StatusCodes.Status404NotFound, message ?? ApiErrors.MessageIntrouvable);

    /// <summary>409 refus métier avec code stable (<c>contrats-api.md</c> §5).</summary>
    public static Echec Conflict(string code, string message) =>
        new(StatusCodes.Status409Conflict, message, code);

    /// <summary>400 avec erreurs par champ (clés = noms JSON camelCase).</summary>
    public static Echec Invalid(IReadOnlyDictionary<string, string> errors) =>
        new(StatusCodes.Status400BadRequest, ApiErrors.MessageValidation, Errors: errors);

    /// <summary>400 refus métier avec code stable (ex. <c>identifiants-invalides</c>).</summary>
    public static Echec Invalid(string code, string message) =>
        new(StatusCodes.Status400BadRequest, message, code);

    public static Echec Forbidden() => new(StatusCodes.Status403Forbidden, ApiErrors.MessageInterdit);

    /// <summary>503 refus avec code stable (ex. <c>envoi-impossible</c>).</summary>
    public static Echec Unavailable(string code, string message) =>
        new(StatusCodes.Status503ServiceUnavailable, message, code);

    public static implicit operator Result(Echec echec) => new(echec ?? throw new ArgumentNullException(nameof(echec)));
}

/// <summary>Résultat d'un cas d'usage (AD-2) : valeur en cas de succès, <see cref="Echec"/> sinon. Un refus métier n'est jamais une exception.</summary>
public sealed class Result<T>
{
    private Result(T? valeur, Echec? echec)
    {
        Valeur = valeur;
        Echec = echec;
    }

    /// <summary>Valeur en cas de succès.</summary>
    public T? Valeur { get; }

    /// <summary>Échec, ou <c>null</c> en cas de succès.</summary>
    public Echec? Echec { get; }

    public bool EstSucces => Echec is null;

    public static Result<T> Ok(T valeur) => new(valeur, null);

    public static Result<T> NotFound(string? message = null) => Result.NotFound(message);

    public static Result<T> Conflict(string code, string message) => Result.Conflict(code, message);

    public static Result<T> Invalid(IReadOnlyDictionary<string, string> errors) => Result.Invalid(errors);

    public static Result<T> Invalid(string code, string message) => Result.Invalid(code, message);

    public static Result<T> Forbidden() => Result.Forbidden();

    public static Result<T> Unavailable(string code, string message) => Result.Unavailable(code, message);

    public static implicit operator Result<T>(Echec echec) => new(default, echec ?? throw new ArgumentNullException(nameof(echec)));
}
