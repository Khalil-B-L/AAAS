using Aaas.Api.Common;
using Aaas.Api.Features.Auth.Commands;
using Aaas.Api.Features.Auth.Contrats;
using Aaas.Api.Features.Auth.Queries;

namespace Aaas.Api.Features.Auth;

/// <summary>Routes <c>/api/auth/*</c> de <c>contrats-api.md</c> §4.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/auth");

        groupe.MapPost("/inscription", async (
            InscriptionDto corps,
            ICommandHandler<InscrireCommand, AuthReponseDto> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new InscrireCommand(corps.Prenom, corps.Nom, corps.Email, corps.Telephone, corps.MotDePasse);
            return (await handler.HandleAsync(command, cancellationToken)).ToHttpResult(StatusCodes.Status201Created);
        });

        groupe.MapPost("/connexion", async (
            ConnexionDto corps,
            ICommandHandler<ConnecterCommand, AuthReponseDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ConnecterCommand(corps.Email, corps.MotDePasse), cancellationToken)).ToHttpResult());

        groupe.MapGet("/moi", async (
            HttpContext context,
            IQueryHandler<ObtenirMoiQuery, UtilisateurDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ObtenirMoiQuery(context.User.UserId()), cancellationToken)).ToHttpResult())
            .RequireAuthorization();

        return app;
    }
}
