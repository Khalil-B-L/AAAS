using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Queries;

/// <summary>« Ils ont trouvé une famille » (<c>GET /api/animaux/adoptes</c>) : sans filtre ni pagination.</summary>
public sealed record ListerAdoptesQuery;

/// <summary>
/// Animaux dont le statut effectif (AD-5) est Adopté, carte au statut effectif, <c>adopteLe</c> décroissant puis
/// <c>Id</c> décroissant (<c>contrats-api.md</c> §3).
/// </summary>
public sealed class ListerAdoptesHandler(AppDbContext db, TimeProvider temps, IFileStorage stockage)
    : IQueryHandler<ListerAdoptesQuery, CollectionDto<AnimalCarteDto>>
{
    public async Task<Result<CollectionDto<AnimalCarteDto>>> HandleAsync(ListerAdoptesQuery query, CancellationToken cancellationToken)
    {
        // Statut effectif calculé dans la requête SQL, au même instant que serverNow.
        var now = temps.GetUtcNow();
        var reservations = db.Set<Reservation>().AsNoTracking();
        var adopte = AnimalRules.StatutEffectif<Animal, Reservation>(reservations, now)
            .Composer((_, statut) => statut == StatutAnimal.Adopte);

        // Projection en dernière opération : l'URL de la photo principale est construite côté client.
        var items = await db.Set<Animal>().AsNoTracking()
            .Where(adopte)
            .OrderByDescending(a => a.AdopteLe)
            .ThenByDescending(a => a.Id)
            .Select(AnimalCarteDto.Projection(reservations, now, stockage))
            .ToListAsync(cancellationToken);
        return Result.Ok(new CollectionDto<AnimalCarteDto>(items, now));
    }
}
