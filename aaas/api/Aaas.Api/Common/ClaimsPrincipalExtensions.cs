using System.Security.Claims;

namespace Aaas.Api.Common;

/// <summary>
/// Seul code qui lit un claim (AD-8). Les noms de claims émis et validés sont déclarés ici une fois.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Identifiant du compte (<c>sub</c>), aussi <c>NameClaimType</c> du JwtBearer.</summary>
    public const string ClaimId = "sub";

    public const string ClaimEmail = "email";

    /// <summary>Rôle du compte (<c>Membre</c> / <c>Admin</c>), aussi <c>RoleClaimType</c> du JwtBearer.</summary>
    public const string ClaimRole = "role";

    public const string ClaimJti = "jti";

    /// <summary>
    /// Identifiant du compte connecté, lu uniquement dans le claim <c>sub</c>. À n'appeler que sur une route
    /// qui exige l'authentification : un jeton validé porte toujours un <c>sub</c> entier.
    /// </summary>
    public static int UserId(this ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimId), out var id)
            ? id
            : throw new InvalidOperationException("Aucun identifiant de compte dans le jeton (route sans RequireAuthorization ?).");
}
