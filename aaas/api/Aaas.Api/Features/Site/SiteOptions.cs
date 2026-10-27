using System.ComponentModel.DataAnnotations;
using Aaas.Api.Common;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Site;

/// <summary>Valeurs du site (AD-14), section <c>Site</c> de la configuration.</summary>
public sealed class SiteOptions
{
    public const string Section = "Site";

    /// <summary>
    /// Numéro de l'association, normalisé en E.164 au chargement des options (<see cref="SiteTelephonePostConfigure"/>) :
    /// <c>72 000 000</c> en configuration devient <c>+21672000000</c>.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "La clé de configuration Site:Telephone est obligatoire.")]
    public string Telephone { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "La clé de configuration Site:Facebook est obligatoire.")]
    public string Facebook { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false, ErrorMessage = "La clé de configuration Site:Instagram est obligatoire.")]
    public string Instagram { get; set; } = string.Empty;
}

/// <summary>Remplace <c>Site:Telephone</c> par sa forme E.164 ; une valeur invalide est laissée telle quelle pour le validateur.</summary>
public sealed class SiteTelephonePostConfigure : IPostConfigureOptions<SiteOptions>
{
    public void PostConfigure(string? name, SiteOptions options) =>
        options.Telephone = Telephone.Normaliser(options.Telephone) ?? options.Telephone;
}

/// <summary>Garde de démarrage : <c>Site:Telephone</c> renseigné mais pas un numéro valide → l'API refuse de démarrer.</summary>
public sealed class SiteOptionsValidator : IValidateOptions<SiteOptions>
{
    public ValidateOptionsResult Validate(string? name, SiteOptions options) =>
        // Valeur absente : déjà refusée par [Required], un seul message.
        string.IsNullOrWhiteSpace(options.Telephone) || Telephone.Normaliser(options.Telephone) is not null
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "Site:Telephone n'est pas un numéro valide (attendu : E.164, ex. +21672000000, ou 8 chiffres tunisiens, ex. 72 000 000).");
}
