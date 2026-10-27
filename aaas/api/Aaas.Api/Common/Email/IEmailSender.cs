using MimeKit;

namespace Aaas.Api.Common.Email;

/// <summary>Email à destination de l'admin : le destinataire n'est jamais fourni par l'appelant (<c>Admin:NotificationEmail</c>).</summary>
/// <param name="Sujet">Sujet de l'email.</param>
/// <param name="Texte">Corps en texte brut.</param>
/// <param name="RepondreA">Adresse <c>Reply-To</c> facultative (par exemple l'email d'un visiteur).</param>
public sealed record MessageEmail(string Sujet, string Texte, string? RepondreA = null)
{
    /// <summary>
    /// Vérification commune aux deux modes, avant toute connexion : <see cref="RepondreA"/>, s'il est fourni, doit être
    /// une adresse email valide (<see cref="AdresseEmail"/> et analysable par MimeKit) ; sinon <see cref="ArgumentException"/>.
    /// </summary>
    public static void Verifier(MessageEmail message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (string.IsNullOrWhiteSpace(message.RepondreA))
            return;
        var adresse = message.RepondreA.Trim();
        if (!AdresseEmail.EstValide(AdresseEmail.Normaliser(adresse)) || !MailboxAddress.TryParse(adresse, out _))
            throw new ArgumentException(
                $"{nameof(RepondreA)} n'est pas une adresse email valide.", $"{nameof(message)}.{nameof(RepondreA)}");
    }
}

/// <summary>
/// Seul point d'envoi d'email de l'API (AD-13), toujours vers <c>Admin:NotificationEmail</c>.
/// Un échec lève une exception : la politique (journaliser ou 503) appartient à l'appelant.
/// <see cref="ArgumentException"/> si <see cref="MessageEmail.RepondreA"/> est mal formé, dans les deux modes.
/// </summary>
public interface IEmailSender
{
    Task EnvoyerAsync(MessageEmail message, CancellationToken cancellationToken);
}
