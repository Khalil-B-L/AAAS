namespace Aaas.Api.Common;

/// <summary>
/// Seul pont <see cref="Result"/> → HTTP (AD-2). Le statut de succès est choisi par l'endpoint
/// (ex. <c>ToHttpResult(StatusCodes.Status201Created)</c>), jamais par le handler.
/// </summary>
public static class ResultHttpExtensions
{
    /// <summary>Succès → <paramref name="statutSucces"/> (200 par défaut) avec la valeur en JSON ; échec → <see cref="ApiErrors"/>.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result, int statutSucces = StatusCodes.Status200OK) =>
        result.Echec is { } echec ? VersHttp(echec) : Results.Json(result.Valeur, statusCode: statutSucces);

    /// <summary>Succès → 204 ; échec → <see cref="ApiErrors"/>.</summary>
    public static IResult ToHttpResult(this Result result) =>
        result.Echec is { } echec ? VersHttp(echec) : Results.NoContent();

    private static IResult VersHttp(Echec echec) => echec.Errors is { } errors
        ? Results.Json(new ErrorResponse(echec.Message, errors, echec.Code), statusCode: echec.Statut)
        : ApiErrors.Erreur(echec.Statut, echec.Message, echec.Code);
}
