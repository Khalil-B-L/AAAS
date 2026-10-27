namespace Aaas.Api.Common;

/// <summary>
/// Normalisation d'un téléphone (compte, <c>Site:Telephone</c>) en E.164 (décision 2026-10-14 : numéros tunisiens) : numéro tunisien
/// à 8 chiffres, le premier non nul (<c>72 000 000</c> → <c>+21672000000</c>), ou numéro international <c>+</c>
/// suivi de 8 à 15 chiffres ; espaces, points et tirets tolérés comme séparateurs.
/// </summary>
public static class Telephone
{
    public const string IndicatifTunisie = "+216";

    private static readonly char[] Separateurs = [' ', '.', '-'];

    /// <summary>Numéro en E.164, ou <c>null</c> si la saisie ne suit aucun des deux formats acceptés.</summary>
    public static string? Normaliser(string? saisie)
    {
        if (string.IsNullOrWhiteSpace(saisie))
            return null;

        var compact = string.Concat(saisie.Trim().Split(Separateurs, StringSplitOptions.RemoveEmptyEntries));

        if (compact.StartsWith('+'))
        {
            var chiffres = compact[1..];
            // E.164 : un indicatif de pays ne commence jamais par 0.
            return chiffres.Length is >= 8 and <= 15 && SontDesChiffres(chiffres) && chiffres[0] != '0'
                ? "+" + chiffres
                : null;
        }

        // Numéro tunisien : 8 chiffres, le premier non nul (« 0… » n'existe pas en numérotation nationale tunisienne).
        return compact.Length == 8 && SontDesChiffres(compact) && compact[0] != '0'
            ? IndicatifTunisie + compact
            : null;
    }

    private static bool SontDesChiffres(string valeur) => valeur.All(char.IsAsciiDigit);
}
