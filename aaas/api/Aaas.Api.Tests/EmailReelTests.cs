using Aaas.Api.Common.Email;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Tests;

/// <summary>
/// Envoi réel via Gmail avec les user-secrets de <c>Aaas.Api</c> : ignoré sauf si <c>AAAS_EMAIL_REEL=1</c>.
/// Vérification humaine : l'email arrive dans la boîte <c>Admin:NotificationEmail</c>.
/// </summary>
[Collection(ApiCollection.Name)]
public class EmailReelTests(AaasFactory factory)
{
    public const string Variable = "AAAS_EMAIL_REEL";

    [Fact]
    public async Task EmailReel_envoie_un_email_de_test_a_Admin_NotificationEmail()
    {
        Assert.SkipUnless(Environment.GetEnvironmentVariable(Variable) == "1",
            $"Envoi réel désactivé : définir {Variable}=1 (avec Smtp:Password en user-secrets) pour l'exécuter.");

        // appsettings.json de l'API puis ses user-secrets, sans la configuration en mémoire des tests.
        var racine = factory.Services.GetRequiredService<IWebHostEnvironment>().ContentRootPath;
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(racine, "appsettings.json"), optional: false)
            .AddUserSecrets<Program>()
            .Build();
        var smtp = configuration.GetSection(SmtpOptions.Section).Get<SmtpOptions>() ?? new SmtpOptions();
        var notification = configuration.GetSection(NotificationOptions.Section).Get<NotificationOptions>() ?? new NotificationOptions();
        Assert.True(smtp.PossedeMotDePasse, "Smtp:Password est absent des user-secrets de Aaas.Api.");
        var validationSmtp = new SmtpOptionsValidator().Validate(null, smtp);
        Assert.True(validationSmtp.Succeeded, validationSmtp.FailureMessage);
        var validationNotification = new NotificationOptionsValidator().Validate(null, notification);
        Assert.True(validationNotification.Succeeded, validationNotification.FailureMessage);

        using var loggerFactory = LoggerFactory.Create(_ => { });
        var sender = new SmtpEmailSender(Options.Create(smtp), Options.Create(notification), loggerFactory.CreateLogger<SmtpEmailSender>());

        await sender.EnvoyerAsync(new MessageEmail(
            $"AAAS — email de test {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "Ceci est un email de test envoyé par Aaas.Api.Tests (EmailReel).\nAccents : é è à ç ô.",
            smtp.User), TestContext.Current.CancellationToken);
    }
}
