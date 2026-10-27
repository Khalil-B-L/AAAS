namespace Aaas.Api.Features.Auth;

/// <summary>Rôle d'un compte (<c>contrats-api.md</c> §1). L'inscription publique crée toujours un <see cref="Membre"/>.</summary>
public enum Role
{
    Membre,
    Admin,
}

/// <summary>Compte du site (AD-8). Seule la slice Auth écrit cette entité.</summary>
public class User
{
    public const int PrenomMax = 100;
    public const int NomMax = 100;
    public const int EmailMax = Common.AdresseEmail.Max;

    /// <summary>E.164 : « + » suivi d'au plus 15 chiffres.</summary>
    public const int TelephoneMax = 16;

    public const int MotDePasseHashMax = 256;

    public int Id { get; set; }

    public string Prenom { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    /// <summary>Email normalisé (trim, minuscules), unique.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Téléphone au format E.164 (ex. <c>+33612345678</c>).</summary>
    public string Telephone { get; set; } = string.Empty;

    /// <summary>Hachage <c>PasswordHasher&lt;User&gt;</c> ; jamais exposé.</summary>
    public string MotDePasseHash { get; set; } = string.Empty;

    public Role Role { get; set; } = Role.Membre;

    public DateTimeOffset CreeLe { get; set; }
}
