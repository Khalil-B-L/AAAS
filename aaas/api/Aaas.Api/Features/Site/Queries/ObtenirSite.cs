using Aaas.Api.Common;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Site.Queries;

/// <summary>Valeurs du site (AD-14) : téléphone et réseaux sociaux de la configuration.</summary>
public sealed record ObtenirSiteQuery;

public sealed class ObtenirSiteHandler(IOptions<SiteOptions> options) : IQueryHandler<ObtenirSiteQuery, SiteDto>
{
    public Task<Result<SiteDto>> HandleAsync(ObtenirSiteQuery query, CancellationToken cancellationToken)
    {
        var site = options.Value;
        return Task.FromResult(Result.Ok(new SiteDto(site.Telephone, site.Facebook, site.Instagram)));
    }
}
