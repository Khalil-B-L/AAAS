namespace Aaas.Api.Common;

/// <summary>Groupe de routes réservé à l'association (AD-8), réutilisé par toutes les slices pour <c>/api/admin/…</c>.</summary>
public static class GroupeAdmin
{
    /// <summary>Nom de la policy <c>Admin</c>, déclarée une seule fois par la slice Auth.</summary>
    public const string Politique = "Admin";

    /// <summary>Groupe dont toutes les routes exigent la policy <see cref="Politique"/> (sans jeton → 401, rôle Membre → 403).</summary>
    public static RouteGroupBuilder MapGroupeAdmin(this IEndpointRouteBuilder app, string prefixe) =>
        app.MapGroup(prefixe).RequireAuthorization(Politique);
}
