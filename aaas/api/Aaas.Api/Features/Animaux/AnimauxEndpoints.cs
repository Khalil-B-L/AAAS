using Aaas.Api.Common;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Animaux.Queries;

namespace Aaas.Api.Features.Animaux;

/// <summary>Routes publiques <c>/api/animaux</c> de <c>contrats-api.md</c> §4.</summary>
public static class AnimauxEndpoints
{
    public static IEndpointRouteBuilder MapAnimauxEndpoints(this IEndpointRouteBuilder app)
    {
        var groupe = app.MapGroup("/api/animaux");

        // limite lue en texte : une valeur non entière doit donner 400 errors.limite, pas un 400 de liaison.
        groupe.MapGet("", async (
            string? limite,
            IQueryHandler<ListerAnimauxQuery, CollectionDto<AnimalCarteDto>> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ListerAnimauxQuery(limite), cancellationToken)).ToHttpResult());

        return app;
    }
}
