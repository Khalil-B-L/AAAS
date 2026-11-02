using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Fichiers;

/// <summary>Enregistrement du stockage (AD-12) et route publique <c>GET /fichiers/:cle</c> (<c>contrats-api.md</c> §4).</summary>
public static class FichiersEndpoints
{
    public static IServiceCollection AddStockageFichiers(this IServiceCollection services)
    {
        services.AddOptions<StorageOptions>()
            .BindConfiguration(StorageOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        // Garde de démarrage : Storage:PhotosPath absent ou invalide => OptionsValidationException avant de toucher la base.
        services.AddGardeDemarrage(sp => _ = sp.GetRequiredService<IOptions<StorageOptions>>().Value);
        // Singleton : l'instance est capturée par la projection de carte (AD-1) sans varier d'une requête à l'autre.
        services.AddSingleton<IFileStorage, DisqueFileStorage>();
        return services;
    }

    /// <summary>
    /// Sert un fichier par sa clé, via <see cref="IFileStorage"/> uniquement (pas d'<c>UseStaticFiles</c>) ;
    /// clé mal formée ou fichier absent → 404 <c>{ error }</c>.
    /// </summary>
    public static IEndpointRouteBuilder MapFichiersEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/fichiers/{cle}", (string cle, IFileStorage stockage, HttpContext context) =>
        {
            if (CleFichier.TypeMime(cle) is not { } typeMime || stockage.Ouvrir(cle) is not { } flux)
                return ApiErrors.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.Stream(flux, typeMime);
        });
        return app;
    }
}
