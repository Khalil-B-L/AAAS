using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Email;

public static class EmailSetup
{
    /// <summary>Environnement des tests : toujours <see cref="ModeEmail.Journal"/> (AD-17).</summary>
    public const string EnvironnementTesting = "Testing";

    /// <summary>
    /// Options <c>Email</c>, <c>Admin:NotificationEmail</c> et <c>Smtp</c> ; un seul <see cref="IEmailSender"/>,
    /// choisi par <see cref="ModeEffectif"/>.
    /// </summary>
    public static IServiceCollection AddEmail(this IServiceCollection services)
    {
        services.AddOptions<EmailOptions>().BindConfiguration(EmailOptions.Section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddOptions<NotificationOptions>().BindConfiguration(NotificationOptions.Section).ValidateOnStart();
        services.AddSingleton<IValidateOptions<NotificationOptions>, NotificationOptionsValidator>();
        // Smtp n'est validée que si le mode effectif est Smtp (à la résolution de SmtpEmailSender) ;
        // un mode Journal démarre même avec une section Smtp invalide.
        services.AddOptions<SmtpOptions>().BindConfiguration(SmtpOptions.Section);
        services.AddSingleton<IValidateOptions<SmtpOptions>, SmtpOptionsValidator>();
        // Garde de démarrage : Email:Mode, Admin:NotificationEmail et, en mode Smtp effectif, la section Smtp.
        // Smtp sans Smtp:Password => Journal avec un avertissement : l'API démarre toujours.
        services.AddGardeDemarrage(sp => sp.VerifierConfigurationEmail());

        services.AddSingleton<SmtpEmailSender>();
        services.AddSingleton<JournalEmailSender>();
        services.AddSingleton<IEmailSender>(sp =>
        {
            // Lecture non validée de Smtp : le mode effectif ne dépend que du mot de passe ; la validation Smtp
            // ne s'exécute qu'à la résolution de SmtpEmailSender (IOptions<SmtpOptions>.Value), donc en mode Smtp effectif.
            // Seul Smtp:Password est lu : une autre clé Smtp invalide (même non numérique) ne bloque pas le mode Journal.
            var smtpNonValide = new SmtpOptions
            {
                Password = sp.GetRequiredService<IConfiguration>()[$"{SmtpOptions.Section}:{nameof(SmtpOptions.Password)}"],
            };
            var mode = ModeEffectif(
                sp.GetRequiredService<IHostEnvironment>(),
                sp.GetRequiredService<IOptions<EmailOptions>>().Value,
                smtpNonValide,
                sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(EmailSetup).FullName!));
            return mode == ModeEmail.Smtp
                ? sp.GetRequiredService<SmtpEmailSender>()
                : sp.GetRequiredService<JournalEmailSender>();
        });
        return services;
    }

    /// <summary>
    /// <c>Testing</c> → Journal ; <c>Smtp</c> sans <c>Smtp:Password</c> → Journal avec un avertissement (l'API démarre toujours) ;
    /// sinon le mode demandé.
    /// </summary>
    public static ModeEmail ModeEffectif(IHostEnvironment environnement, EmailOptions email, SmtpOptions smtp, ILogger logger)
    {
        if (environnement.IsEnvironment(EnvironnementTesting))
            return ModeEmail.Journal;
        if (email.ModeDemande == ModeEmail.Smtp && !smtp.PossedeMotDePasse)
        {
            logger.LogWarning(
                "Email:Mode = Smtp mais Smtp:Password est absent (dotnet user-secrets set \"Smtp:Password\" \"...\") : "
                + "les emails sont écrits dans le journal (mode Journal), aucun n'est envoyé.");
            return ModeEmail.Journal;
        }
        return email.ModeDemande;
    }

    /// <summary>
    /// Garde de démarrage : <c>Email:Mode</c>, <c>Admin:NotificationEmail</c> et, en mode Smtp effectif, la section <c>Smtp</c>
    /// invalides → <see cref="OptionsValidationException"/>, avant toute base. Émet aussi l'avertissement de repli.
    /// </summary>
    public static void VerifierConfigurationEmail(this IServiceProvider services)
    {
        _ = services.GetRequiredService<IOptions<EmailOptions>>().Value;
        _ = services.GetRequiredService<IOptions<NotificationOptions>>().Value;
        _ = services.GetRequiredService<IEmailSender>();
    }
}
