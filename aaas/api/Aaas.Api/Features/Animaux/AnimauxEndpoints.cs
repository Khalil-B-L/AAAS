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

        // Filtres §2 lus en texte : une valeur invalide (entier non valide, énumération inconnue ou de casse différente,
        // paramètre répété) doit donner 400 errors.<param>, pas un 400 de liaison.
        groupe.MapGet("", async (
            string? limite,
            string? espece,
            string? sexe,
            string? age,
            string? taille,
            string? enfants,
            string? chiens,
            string? chats,
            IQueryHandler<ListerAnimauxQuery, CollectionDto<AnimalCarteDto>> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(
                new ListerAnimauxQuery(limite, espece, sexe, age, taille, enfants, chiens, chats),
                cancellationToken)).ToHttpResult());

        return app;
    }
}
