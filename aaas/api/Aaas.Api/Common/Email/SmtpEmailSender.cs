using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Aaas.Api.Common.Email;

/// <summary>
/// Mode <see cref="ModeEmail.Smtp"/> : MailKit en StartTls, expéditeur <c>Smtp:User</c>, destinataire <c>Admin:NotificationEmail</c>,
/// texte brut UTF-8, <c>Reply-To</c> si fourni. Tout l'envoi tient dans <c>Smtp:TimeoutSecondes</c> ; un échec est journalisé
/// en <c>Error</c> (jamais le mot de passe) puis relancé. Pas de nouvel essai.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> smtp,
    IOptions<NotificationOptions> notification,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _smtp = smtp.Value;
    private readonly string _destinataire = AdresseEmail.Normaliser(notification.Value.NotificationEmail);

    public async Task EnvoyerAsync(MessageEmail message, CancellationToken cancellationToken)
    {
        // Message mal formé : ArgumentException avant toute connexion, hors journalisation des échecs SMTP.
        MessageEmail.Verifier(message);
        var mime = Construire(message);
        var delai = TimeSpan.FromSeconds(_smtp.TimeoutSecondes);
        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(delai);
        using var client = CreerClient();
        client.Timeout = (int)delai.TotalMilliseconds;

        try
        {
            await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls, limite.Token);
            // User et Password validés au démarrage en mode Smtp effectif (SmtpOptionsValidator, repli Journal).
            await client.AuthenticateAsync(_smtp.User ?? string.Empty, _smtp.Password ?? string.Empty, limite.Token);
            await client.SendAsync(mime, limite.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Annulation demandée par l'appelant : pas un échec SMTP.
            throw;
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                var depassement = new TimeoutException($"Envoi SMTP interrompu après {_smtp.TimeoutSecondes} s.", ex);
                JournaliserEchec(depassement, message);
                throw depassement;
            }
            JournaliserEchec(ex, message);
            throw;
        }

        // L'email est accepté par le serveur : un échec du QUIT n'est pas un échec d'envoi.
        try
        {
            await client.DisconnectAsync(true, limite.Token);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Email « {Sujet} » envoyé à {Destinataire}, mais la déconnexion de {Host}:{Port} a échoué.",
                message.Sujet, _destinataire, _smtp.Host, _smtp.Port);
        }
    }

    /// <summary>Fabrique du client MailKit ; remplacée seulement par les tests (serveur SMTP local à certificat auto-signé).</summary>
    internal Func<SmtpClient> CreerClient { get; init; } = () => new SmtpClient();

    private void JournaliserEchec(Exception exception, MessageEmail message) =>
        logger.LogError(exception, "Échec de l'envoi de l'email « {Sujet} » à {Destinataire} via {Host}:{Port}.",
            message.Sujet, _destinataire, _smtp.Host, _smtp.Port);

    private MimeMessage Construire(MessageEmail message)
    {
        var mime = new MimeMessage();
        mime.From.Add(MailboxAddress.Parse(AdresseEmail.Normaliser(_smtp.User)));
        mime.To.Add(MailboxAddress.Parse(_destinataire));
        if (!string.IsNullOrWhiteSpace(message.RepondreA))
            mime.ReplyTo.Add(MailboxAddress.Parse(message.RepondreA.Trim()));
        mime.Subject = message.Sujet;
        var corps = new TextPart(TextFormat.Plain);
        corps.SetText(Encoding.UTF8, message.Texte);
        mime.Body = corps;
        return mime;
    }
}
