using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Animaux.Queries;

namespace Aaas.Api.Features.Animaux.Commands;

/// <summary>Création d'un animal (<c>POST /api/admin/animaux</c>) : toujours Disponible, créé maintenant.</summary>
public sealed record CreerAnimalCommand(AnimalEcritureDto Champs);

/// <summary>
/// Valide les champs, insère l'animal dans la transaction (<see cref="TransactionRunner"/>), puis relit la fiche par
/// la projection publiée (<see cref="AnimalDetailDto.Projection"/>) après le commit.
/// </summary>
public sealed class CreerAnimalHandler(
    AppDbContext db,
    TransactionRunner transactions,
    TimeProvider temps,
    IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto> fiche)
    : ICommandHandler<CreerAnimalCommand, AnimalDetailDto>
{
    public async Task<Result<AnimalDetailDto>> HandleAsync(CreerAnimalCommand command, CancellationToken cancellationToken)
    {
        var errors = command.Champs.Valider();
        if (errors.Count > 0)
            return Result.Invalid(errors);

        Animal? animal = null;
        var resultat = await transactions.RunAsync(IsolationLevel.ReadCommitted, _ =>
        {
            animal = new Animal(temps);
            command.Champs.Appliquer(animal);
            db.Set<Animal>().Add(animal);
            return Task.FromResult(Result.Ok());
        }, cancellationToken);

        if (resultat.Echec is { } echec)
            return echec;
        // Id renseigné par le SaveChanges du runner : la fiche est relue après le commit.
        return await fiche.HandleAsync(new ObtenirAnimalQuery(animal!.Id), cancellationToken);
    }
}
