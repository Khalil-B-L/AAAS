using Aaas.Api.Common;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Auth;

/// <summary>
/// Compte admin créé ou mis à jour au démarrage (section <c>Admin</c>). <c>Admin:Password</c> vient uniquement
/// des user-secrets (tests : configuration en mémoire).
/// </summary>
public sealed class AdminOptions
{
    public const string Section = "Admin";
    public const int MotDePasseMin = 8;

    public string? Email { get; set; }

    public string? Password { get; set; }
}

/// <summary>Garde de démarrage : email admin invalide ou mot de passe trop court → l'API refuse de démarrer.</summary>
public sealed class AdminOptionsValidator : IValidateOptions<AdminOptions>
{
    public ValidateOptionsResult Validate(string? name, AdminOptions options)
    {
        var echecs = new List<string>();
        if (!AdresseEmail.EstValide(AdresseEmail.Normaliser(options.Email)))
            echecs.Add("Admin:Email est absente ou n'est pas une adresse email valide.");
        if (string.IsNullOrEmpty(options.Password))
            echecs.Add("Admin:Password est absent de la configuration (dotnet user-secrets set \"Admin:Password\" \"...\").");
        else if (options.Password.Length < AdminOptions.MotDePasseMin)
            echecs.Add($"Admin:Password est trop court ({AdminOptions.MotDePasseMin} caractères minimum).");
        return echecs.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(echecs);
    }
}
