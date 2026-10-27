using Microsoft.AspNetCore.Diagnostics;

namespace Aaas.Api.Common;

/// <summary>Mécanismes uniques d'erreur hors <c>ToHttpResult</c> (AD-10).</summary>
public static class ErreursHttp
{
    /// <summary>
    /// À placer en tête du pipeline : exception non gérée → 500 <c>{ error }</c> sans détail
    /// (l'exception est journalisée par le middleware), puis toute réponse d'erreur sans corps
    /// (400, 404, 405, 415…) → <c>{ error }</c> avec le message par défaut du statut.
    /// </summary>
    public static IApplicationBuilder UseApiErrors(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            ExceptionHandler = async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                // Requête mal formée levée par la liaison des paramètres (ThrowOnBadRequest en Development).
                var statut = exception is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError;
                await ApiErrors.Erreur(statut).ExecuteAsync(context);
            },
        });

        app.UseStatusCodePages(context => ApiErrors.Erreur(context.HttpContext.Response.StatusCode)
            .ExecuteAsync(context.HttpContext));

        return app;
    }

    /// <summary>À mapper en dernier : toute route inconnue sous <c>/api</c> → 404 <c>{ error }</c>.</summary>
    public static IEndpointRouteBuilder MapApiFallback(this IEndpointRouteBuilder app)
    {
        app.MapFallback("/api/{**chemin}", () => ApiErrors.NotFound());
        return app;
    }
}
