using Aaas.Api.Common;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Queries;

/// <summary>Fiche publique d'un animal (<c>GET /api/animaux/:id</c>), quel que soit son statut (Adopté compris).</summary>
public sealed record ObtenirAnimalQuery(int Id);

/// <summary>Fiche au statut effectif (AD-5), photos triées par <c>Ordre</c> ; animal inconnu → 404.</summary>
public sealed class ObtenirAnimalHandler(AppDbContext db, TimeProvider temps, IFileStorage stockage)
    : IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto>
{
    public async Task<Result<AnimalDetailDto>> HandleAsync(ObtenirAnimalQuery query, CancellationToken cancellationToken)
    {
        var now = temps.GetUtcNow();
        var reservations = db.Set<Reservation>().AsNoTracking();

        // Projection en dernière opération : les URLs des photos sont construites côté client.
        var fiche = await db.Set<Animal>().AsNoTracking()
            .Where(a => a.Id == query.Id)
            .Select(AnimalDetailDto.Projection(reservations, now, stockage))
            .SingleOrDefaultAsync(cancellationToken);

        return fiche is null ? Result<AnimalDetailDto>.NotFound() : Result.Ok(fiche);
    }
}
