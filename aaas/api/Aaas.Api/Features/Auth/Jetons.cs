using Aaas.Api.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aaas.Api.Features.Auth;

public sealed record JetonEmis(string Jeton, DateTimeOffset ExpireLe);

/// <summary>Émission et paramètres de validation du JWT HS256 (AD-8). L'heure vient de <see cref="TimeProvider"/>.</summary>
public sealed class Jetons(IOptions<JwtOptions> options, TimeProvider time)
{
    public static readonly TimeSpan Tolerance = TimeSpan.FromSeconds(30);

    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new() { SetDefaultTimesOnTokenCreation = false };

    /// <summary>Jeton de <c>Jwt:ExpiresMinutes</c> minutes, claims <c>sub</c>, <c>email</c>, <c>role</c>, <c>jti</c>.</summary>
    public JetonEmis Emettre(User user)
    {
        var maintenant = time.GetUtcNow();
        var expireLe = maintenant.AddMinutes(_options.ExpiresMinutes);
        var descripteur = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = maintenant.UtcDateTime,
            NotBefore = maintenant.UtcDateTime,
            Expires = expireLe.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [ClaimsPrincipalExtensions.ClaimId] = user.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                [ClaimsPrincipalExtensions.ClaimEmail] = user.Email,
                [ClaimsPrincipalExtensions.ClaimRole] = user.Role.ToString(),
                [ClaimsPrincipalExtensions.ClaimJti] = Guid.NewGuid().ToString("N"),
            },
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(_options.KeyBytes), SecurityAlgorithms.HmacSha256),
        };
        return new JetonEmis(_handler.CreateToken(descripteur), expireLe);
    }

    /// <summary>
    /// Validation : émetteur, audience, signature HS256 uniquement, durée de vie vérifiée sur <see cref="TimeProvider"/>
    /// (tolérance 30 s), <c>sub</c> comme nom et <c>role</c> comme rôle.
    /// </summary>
    public static TokenValidationParameters ParametresValidation(JwtOptions options, TimeProvider time) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(options.KeyBytes),
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = Tolerance,
        LifetimeValidator = (notBefore, expires, _, _) =>
        {
            var maintenant = time.GetUtcNow().UtcDateTime;
            return expires is { } fin && fin.ToUniversalTime() > maintenant - Tolerance
                && (notBefore is not { } debut || debut.ToUniversalTime() <= maintenant + Tolerance);
        },
        NameClaimType = ClaimsPrincipalExtensions.ClaimId,
        RoleClaimType = ClaimsPrincipalExtensions.ClaimRole,
    };
}
