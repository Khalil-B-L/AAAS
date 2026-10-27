using System.Text;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Auth;

/// <summary>Section <c>Jwt</c>. <c>Jwt:Key</c> vient uniquement des user-secrets (tests : configuration en mémoire).</summary>
public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public const int MinKeyBytes = 32;

    /// <summary>Durée maximale d'un jeton (AD-8 : 60 min).</summary>
    public const int MaxExpiresMinutes = 60;

    public string Issuer { get; set; } = "Aaas";

    public string Audience { get; set; } = "Aaas";

    /// <summary>Clé HS256, au moins <see cref="MinKeyBytes"/> octets en UTF-8.</summary>
    public string? Key { get; set; }

    public int ExpiresMinutes { get; set; } = 60;

    public byte[] KeyBytes => Encoding.UTF8.GetBytes(Key ?? string.Empty);
}

/// <summary>Garde de démarrage : clé absente ou trop courte, durée hors 1..60 min → l'API refuse de démarrer.</summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        var echecs = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Key))
            echecs.Add("Jwt:Key est absente de la configuration (dotnet user-secrets set \"Jwt:Key\" \"...\").");
        else if (Encoding.UTF8.GetByteCount(options.Key) < JwtOptions.MinKeyBytes)
            echecs.Add($"Jwt:Key est trop courte ({JwtOptions.MinKeyBytes} octets minimum).");
        if (string.IsNullOrWhiteSpace(options.Issuer))
            echecs.Add("Jwt:Issuer est vide.");
        if (string.IsNullOrWhiteSpace(options.Audience))
            echecs.Add("Jwt:Audience est vide.");
        if (options.ExpiresMinutes is < 1 or > JwtOptions.MaxExpiresMinutes)
            echecs.Add($"Jwt:ExpiresMinutes doit être compris entre 1 et {JwtOptions.MaxExpiresMinutes}.");
        return echecs.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(echecs);
    }
}
