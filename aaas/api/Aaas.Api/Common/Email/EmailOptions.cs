using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Email;

/// <summary>Implémentations possibles de <see cref="IEmailSender"/>.</summary>
public enum ModeEmail
{
    /// <summary>MailKit vers le serveur SMTP (<c>Smtp:*</c>).</summary>
    Smtp,

    /// <summary>L'email est écrit dans le journal, aucune connexion SMTP.</summary>
    Journal,
}

/// <summary>Section <c>Email</c> : choix de l'implémentation (<c>Email:Mode</c> = <c>Smtp</c> ou <c>Journal</c>).</summary>
public sealed class EmailOptions
{
    public const string Section = "Email";

    /// <summary>Lu en chaîne pour qu'une valeur inconnue soit refusée par le validateur avec un message nommant la clé.</summary>
    public string? Mode { get; set; }

    /// <summary>Mode demandé ; à lire seulement sur des options validées.</summary>
    public ModeEmail ModeDemande => Enum.Parse<ModeEmail>(Mode!);
}

/// <summary>
/// Destinataire unique des emails (<c>Admin:NotificationEmail</c>), lu dans la section <c>Admin</c>
/// à côté des options du compte admin (<c>Admin:Email</c>).
/// </summary>
public sealed class NotificationOptions
{
    public const string Section = "Admin";

    public string? NotificationEmail { get; set; }
}

/// <summary>
/// Section <c>Smtp</c> (Gmail par défaut). <c>Smtp:Password</c> (mot de passe d'application) vient uniquement
/// des user-secrets ; absent, l'API retombe sur le mode <see cref="ModeEmail.Journal"/>.
/// </summary>
public sealed class SmtpOptions
{
    public const string Section = "Smtp";

    public string Host { get; set; } = "smtp.gmail.com";

    public int Port { get; set; } = 587;

    /// <summary>Compte Gmail, aussi expéditeur des emails.</summary>
    public string? User { get; set; }

    public string? Password { get; set; }

    /// <summary>Délai maximal d'un envoi complet (connexion, authentification, envoi).</summary>
    public int TimeoutSecondes { get; set; } = 10;

    public bool PossedeMotDePasse => !string.IsNullOrWhiteSpace(Password);
}

/// <summary>Garde de démarrage : <c>Email:Mode</c> absent ou inconnu → l'API refuse de démarrer.</summary>
public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options) =>
        options.Mode is nameof(ModeEmail.Smtp) or nameof(ModeEmail.Journal)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"Email:Mode est absent ou inconnu (« {options.Mode} ») : valeurs admises {nameof(ModeEmail.Smtp)} ou {nameof(ModeEmail.Journal)}.");
}

/// <summary>Garde de démarrage : <c>Admin:NotificationEmail</c> absente ou mal formée → l'API refuse de démarrer.</summary>
public sealed class NotificationOptionsValidator : IValidateOptions<NotificationOptions>
{
    public ValidateOptionsResult Validate(string? name, NotificationOptions options) =>
        AdresseEmail.EstValide(AdresseEmail.Normaliser(options.NotificationEmail))
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Admin:NotificationEmail est absente ou n'est pas une adresse email valide.");
}

/// <summary>
/// Paramètres SMTP, vérifiés seulement quand le mode effectif est <see cref="ModeEmail.Smtp"/>.
/// Le mot de passe n'apparaît jamais dans un message.
/// </summary>
public sealed class SmtpOptionsValidator : IValidateOptions<SmtpOptions>
{
    public const int TimeoutMaxSecondes = 60;

    public ValidateOptionsResult Validate(string? name, SmtpOptions options)
    {
        var echecs = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Host))
            echecs.Add("Smtp:Host est absent de la configuration.");
        if (options.Port is < 1 or > 65535)
            echecs.Add("Smtp:Port doit être compris entre 1 et 65535.");
        if (!AdresseEmail.EstValide(AdresseEmail.Normaliser(options.User)))
            echecs.Add("Smtp:User est absent ou n'est pas une adresse email valide.");
        if (options.TimeoutSecondes is < 1 or > TimeoutMaxSecondes)
            echecs.Add($"Smtp:TimeoutSecondes doit être compris entre 1 et {TimeoutMaxSecondes}.");
        return echecs.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(echecs);
    }
}
