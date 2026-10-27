using Microsoft.Extensions.Options;

namespace Aaas.Api.Common.Email;

/// <summary>Mode <see cref="ModeEmail.Journal"/> : l'email est écrit dans le journal en <c>Information</c>, rien n'est envoyé.</summary>
public sealed class JournalEmailSender(IOptions<NotificationOptions> notification, ILogger<JournalEmailSender> logger) : IEmailSender
{
    public Task EnvoyerAsync(MessageEmail message, CancellationToken cancellationToken)
    {
        MessageEmail.Verifier(message);
        logger.LogInformation(
            "Email (mode Journal, non envoyé) à {Destinataire}, répondre à {RepondreA}, sujet « {Sujet} » :\n{Texte}",
            AdresseEmail.Normaliser(notification.Value.NotificationEmail),
            message.RepondreA ?? "(aucun)",
            message.Sujet,
            message.Texte);
        return Task.CompletedTask;
    }
}
