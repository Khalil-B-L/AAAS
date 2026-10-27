using Aaas.Api.Common;
using Aaas.Api.Features.Site.Queries;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Site;

public static class SiteEndpoints
{
    public static IServiceCollection AddSiteOptions(this IServiceCollection services)
    {
        services.AddOptions<SiteOptions>()
            .BindConfiguration(SiteOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        // Site:Telephone : normalisé en E.164 (PostConfigure) puis validé, avant toute base (garde déclarée ci-dessous
        // par AddGardeDemarrage, exécutée par VerifierConfiguration avant Migrate()).
        services.AddSingleton<IPostConfigureOptions<SiteOptions>, SiteTelephonePostConfigure>();
        services.AddSingleton<IValidateOptions<SiteOptions>, SiteOptionsValidator>();
        // Garde de démarrage : section Site invalide => OptionsValidationException avant de toucher la base.
        services.AddGardeDemarrage(sp => _ = sp.GetRequiredService<IOptions<SiteOptions>>().Value);
        return services;
    }

    public static IEndpointRouteBuilder MapSiteEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/site", async (IQueryHandler<ObtenirSiteQuery, SiteDto> handler, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new ObtenirSiteQuery(), cancellationToken)).ToHttpResult());
        return app;
    }
}
