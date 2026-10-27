using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Auth.Contrats;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Auth.Queries;

/// <summary>Compte connecté (restauration de session côté front, AD-9). <c>UserId</c> vient de <c>ClaimsPrincipalExtensions.UserId()</c>.</summary>
public sealed record ObtenirMoiQuery(int UserId);

public sealed class ObtenirMoiHandler(AppDbContext db) : IQueryHandler<ObtenirMoiQuery, UtilisateurDto>
{
    public async Task<Result<UtilisateurDto>> HandleAsync(ObtenirMoiQuery query, CancellationToken cancellationToken)
    {
        var utilisateur = await db.Set<User>()
            .AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(UtilisateurDto.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return utilisateur is null ? Result.NotFound("Ce compte est introuvable.") : Result.Ok(utilisateur);
    }
}
