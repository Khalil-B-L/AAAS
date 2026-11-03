using Aaas.Api.Common;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Queries;

/// <summary>Liste admin (<c>GET /api/admin/animaux</c>) : tous statuts, sans filtre ni pagination.</summary>
public sealed record ListerAnimauxAdminQuery;

/// <summary>
/// Tous les animaux, carte au statut effectif (AD-5), <c>creeLe</c> décroissant puis <c>Id</c> décroissant
/// (<c>contrats-api.md</c> §3).
/// </summary>
public sealed class ListerAnimauxAdminHandler(AppDbContext db, TimeProvider temps, IFileStorage stockage)
    : IQueryHandler<ListerAnimauxAdminQuery, CollectionDto<AnimalCarteDto>>
{
    public async Task<Result<CollectionDto<AnimalCarteDto>>> HandleAsync(ListerAnimauxAdminQuery query, CancellationToken cancellationToken)
    {
        var now = temps.GetUtcNow();
        var reservations = db.Set<Reservation>().AsNoTracking();

        // Projection en dernière opération : l'URL de la photo principale est construite côté client.
        var items = await db.Set<Animal>().AsNoTracking()
            .OrderByDescending(a => a.CreeLe)
            .ThenByDescending(a => a.Id)
            .Select(AnimalCarteDto.Projection(reservations, now, stockage))
            .ToListAsync(cancellationToken);
        return Result.Ok(new CollectionDto<AnimalCarteDto>(items, now));
    }
}
