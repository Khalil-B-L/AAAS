using Aaas.Api.Common;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Features.Animaux.Commands;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Animaux.Queries;

namespace Aaas.Api.Features.Animaux;

/// <summary>Routes <c>/api/animaux</c> et <c>/api/admin/animaux</c> de <c>contrats-api.md</c> §4.</summary>
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

        // « Ils ont trouvé une famille » : segment littéral, que la contrainte entière de /{id:int} n'accepte pas.
        groupe.MapGet("/adoptes", async (
            IQueryHandler<ListerAdoptesQuery, CollectionDto<AnimalCarteDto>> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ListerAdoptesQuery(), cancellationToken)).ToHttpResult());

        // Fiche publique, tout statut ; un id non entier ne correspond à aucune route (404 ApiErrors).
        groupe.MapGet("/{id:int}", async (
            int id,
            IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ObtenirAnimalQuery(id), cancellationToken)).ToHttpResult());

        var admin = app.MapGroupeAdmin("/api/admin/animaux");

        // Liste admin : tous statuts, sans filtre ni pagination.
        admin.MapGet("", async (
            IQueryHandler<ListerAnimauxAdminQuery, CollectionDto<AnimalCarteDto>> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ListerAnimauxAdminQuery(), cancellationToken)).ToHttpResult());

        // Même fiche que la route publique, tout statut.
        admin.MapGet("/{id:int}", async (
            int id,
            IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ObtenirAnimalQuery(id), cancellationToken)).ToHttpResult());

        // Corps à propriétés nullables, tout validé par le handler ; un champ « statut » envoyé est ignoré.
        admin.MapPost("", async (
            AnimalEcritureDto corps,
            ICommandHandler<CreerAnimalCommand, AnimalDetailDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new CreerAnimalCommand(corps), cancellationToken))
                .ToHttpResult(StatusCodes.Status201Created));

        admin.MapPut("/{id:int}", async (
            int id,
            AnimalEcritureDto corps,
            ICommandHandler<ModifierAnimalCommand, AnimalDetailDto> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ModifierAnimalCommand(id, corps), cancellationToken)).ToHttpResult());

        admin.MapDelete("/{id:int}", async (
            int id,
            ICommandHandler<SupprimerAnimalCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new SupprimerAnimalCommand(id), cancellationToken)).ToHttpResult());

        // Upload multipart, un unique champ « fichier » par requête (AD-12). Le formulaire est lu ici (et non lié en
        // IFormFile) pour qu'un champ absent, répété ou un corps non multipart donne 400 errors.fichier.
        // API en JWT Bearer : pas d'antiforgery sur ces routes.
        admin.MapPost("/{id:int}/photos", async (
            int id,
            HttpRequest requete,
            ICommandHandler<AjouterPhotoCommand, PhotoDto> handler,
            CancellationToken cancellationToken) =>
        {
            IReadOnlyList<IFormFile> fichiers;
            try
            {
                fichiers = requete.HasFormContentType
                    ? (await requete.ReadFormAsync(cancellationToken)).Files.GetFiles(RegleFichier.Champ)
                    : [];
            }
            // Multipart mal formé (sans boundary, tronqué, corrompu) → 400 errors.fichier, pas 500.
            // BadHttpRequestException (ex. 413 corps trop gros de Kestrel) garde son propre statut.
            catch (Exception ex) when (ex is InvalidDataException or IOException && ex is not BadHttpRequestException)
            {
                return ((Result)RegleFichier.Refus(RegleFichier.MessageIllisible)).ToHttpResult();
            }
            return (await handler.HandleAsync(new AjouterPhotoCommand(id, fichiers), cancellationToken))
                .ToHttpResult(StatusCodes.Status201Created);
        }).DisableAntiforgery();

        admin.MapDelete("/{id:int}/photos/{photoId:int}", async (
            int id,
            int photoId,
            ICommandHandler<SupprimerPhotoCommand> handler,
            CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new SupprimerPhotoCommand(id, photoId), cancellationToken)).ToHttpResult());

        return app;
    }
}
