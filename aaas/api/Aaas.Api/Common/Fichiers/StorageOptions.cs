using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Fichiers;

/// <summary>Stockage des fichiers (AD-12), section <c>Storage</c> de la configuration.</summary>
public sealed class StorageOptions
{
    public const string Section = "Storage";

    /// <summary>
    /// Dossier des photos ; relatif au dossier de l'API (<c>ContentRootPath</c>) s'il n'est pas absolu.
    /// </summary>
    [Required(AllowEmptyStrings = false, ErrorMessage = "La clé de configuration Storage:PhotosPath est obligatoire.")]
    public string PhotosPath { get; set; } = string.Empty;
}

/// <summary>Garde de démarrage : <c>Storage:PhotosPath</c> renseigné mais pas un chemin utilisable → l'API refuse de démarrer.</summary>
public sealed class StorageOptionsValidator : IValidateOptions<StorageOptions>
{
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        // Valeur absente : déjà refusée par [Required], un seul message.
        if (string.IsNullOrWhiteSpace(options.PhotosPath))
            return ValidateOptionsResult.Success;
        try
        {
            _ = Path.GetFullPath(options.PhotosPath);
            return ValidateOptionsResult.Success;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return ValidateOptionsResult.Fail($"Storage:PhotosPath n'est pas un chemin de dossier valide ({ex.Message}).");
        }
    }
}
