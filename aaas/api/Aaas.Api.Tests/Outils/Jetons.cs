using System.Text;
using Aaas.Api.Features.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Aaas.Api.Tests.Outils;

/// <summary>Jetons JWT côté tests (nommé ainsi pour ne pas masquer <see cref="Features.Auth.Jetons"/> de l'API).</summary>
public static class JetonsDeTest
{
    public static JsonWebToken LireJeton(string jeton) => new(jeton);

    /// <summary>Jeton signé hors API (claims choisis), valide à l'heure courante du <see cref="AaasFactory.Time"/>.</summary>
    public static string FabriquerJeton(this AaasFactory factory, Dictionary<string, object> claims, string cle = AaasFactory.JwtKey)
    {
        var jwt = factory.Services.GetRequiredService<IOptions<JwtOptions>>().Value;
        var now = factory.Time.GetUtcNow().UtcDateTime;
        return new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(10),
            Claims = claims,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cle)), SecurityAlgorithms.HmacSha256),
        });
    }
}
