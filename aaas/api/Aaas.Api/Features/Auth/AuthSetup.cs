using Aaas.Api.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Auth;

public static class AuthSetup
{
    /// <summary>
    /// Options <c>Jwt</c> et <c>Admin</c> validées au démarrage, hachage <see cref="PasswordHasher{TUser}"/> (sans Identity complet),
    /// JwtBearer HS256 (401 / 403 au format <see cref="ApiErrors"/>) et policy <see cref="GroupeAdmin.Politique"/>, déclarée ici seulement.
    /// </summary>
    public static IServiceCollection AddAuth(this IServiceCollection services)
    {
        services.AddOptions<JwtOptions>().BindConfiguration(JwtOptions.Section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
        services.AddOptions<AdminOptions>().BindConfiguration(AdminOptions.Section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminOptions>, AdminOptionsValidator>();
        // Garde de démarrage : Jwt (clé >= 32 octets) et Admin (email, mot de passe), sinon l'API refuse de démarrer.
        services.AddGardeDemarrage(sp => sp.VerifierConfigurationAuth());

        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<Jetons>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        // Paramètres lus via IOptions<JwtOptions> (validées) et TimeProvider (remplacé dans les tests).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>((bearer, jwt, time) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = Jetons.ParametresValidation(jwt.Value, time);
                bearer.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        await ApiErrors.Unauthorized().ExecuteAsync(context.HttpContext);
                    },
                    OnForbidden = context => ApiErrors.Forbidden().ExecuteAsync(context.HttpContext),
                };
            });

        services.AddAuthorizationBuilder()
            .AddPolicy(GroupeAdmin.Politique, policy =>
                policy.RequireClaim(ClaimsPrincipalExtensions.ClaimRole, nameof(Role.Admin)));

        return services;
    }

    /// <summary>
    /// Garde de démarrage : <c>Jwt</c> ou <c>Admin</c> invalide → <see cref="OptionsValidationException"/>, avant toute base.
    /// </summary>
    public static void VerifierConfigurationAuth(this IServiceProvider services)
    {
        _ = services.GetRequiredService<IOptions<JwtOptions>>().Value;
        _ = services.GetRequiredService<IOptions<AdminOptions>>().Value;
    }
}
