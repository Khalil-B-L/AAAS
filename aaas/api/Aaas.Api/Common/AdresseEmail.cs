using System.Text.RegularExpressions;

namespace Aaas.Api.Common;

/// <summary>Normalisation et forme d'une adresse email (comptes, configuration, emails sortants).</summary>
public static partial class AdresseEmail
{
    /// <summary>Longueur maximale d'une adresse (aussi la taille de la colonne <c>User.Email</c>).</summary>
    public const int Max = 256;

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex Forme();

    /// <summary>Trim + minuscules ; chaîne vide si absente.</summary>
    public static string Normaliser(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>Adresse normalisée non vide, d'au plus <see cref="Max"/> caractères, de forme <c>nom@domaine.ext</c>.</summary>
    public static bool EstValide(string emailNormalise) =>
        emailNormalise.Length is > 0 and <= Max && Forme().IsMatch(emailNormalise);
}
