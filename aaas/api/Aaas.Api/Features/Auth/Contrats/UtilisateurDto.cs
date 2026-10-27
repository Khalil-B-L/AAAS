using System.Linq.Expressions;

namespace Aaas.Api.Features.Auth.Contrats;

/// <summary>Compte exposé par l'API (<c>contrats-api.md</c> §6) ; jamais le hachage du mot de passe.</summary>
public sealed record UtilisateurDto(int Id, string Prenom, string Nom, string Email, string Telephone, Role Role)
{
    /// <summary>Projection unique <see cref="User"/> → DTO, traduisible en SQL.</summary>
    public static readonly Expression<Func<User, UtilisateurDto>> Projection =
        u => new UtilisateurDto(u.Id, u.Prenom, u.Nom, u.Email, u.Telephone, u.Role);

    private static readonly Func<User, UtilisateurDto> ProjectionCompilee = Projection.Compile();

    public static UtilisateurDto De(User user) => ProjectionCompilee(user);
}
