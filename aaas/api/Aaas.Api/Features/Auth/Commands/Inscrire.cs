using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Data;
using Aaas.Api.Features.Auth.Contrats;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Auth.Commands;

/// <summary>Inscription publique : crée toujours un <see cref="Role.Membre"/> et renvoie son jeton.</summary>
public sealed record InscrireCommand(string? Prenom, string? Nom, string? Email, string? Telephone, string? MotDePasse);

public sealed class InscrireHandler(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    Jetons jetons,
    TimeProvider time) : ICommandHandler<InscrireCommand, AuthReponseDto>
{
    public const int MotDePasseMin = 8;
    public const int MotDePasseMax = 128;
    public const string MessageEmailPris = "Un compte existe déjà avec cet email.";

    public async Task<Result<AuthReponseDto>> HandleAsync(InscrireCommand command, CancellationToken cancellationToken)
    {
        var prenom = (command.Prenom ?? string.Empty).Trim();
        var nom = (command.Nom ?? string.Empty).Trim();
        var email = AdresseEmail.Normaliser(command.Email);
        var telephone = Telephone.Normaliser(command.Telephone);
        var motDePasse = command.MotDePasse ?? string.Empty;

        var errors = new Dictionary<string, string>();
        if (prenom.Length == 0)
            errors["prenom"] = "Indiquez votre prénom.";
        else if (prenom.Length > User.PrenomMax)
            errors["prenom"] = $"Prénom trop long ({User.PrenomMax} caractères maximum).";
        if (nom.Length == 0)
            errors["nom"] = "Indiquez votre nom.";
        else if (nom.Length > User.NomMax)
            errors["nom"] = $"Nom trop long ({User.NomMax} caractères maximum).";
        if (email.Length == 0)
            errors["email"] = "Indiquez votre adresse email.";
        else if (!AdresseEmail.EstValide(email))
            errors["email"] = "Adresse email invalide (ex. : nom@domaine.fr).";
        if (string.IsNullOrWhiteSpace(command.Telephone))
            errors["telephone"] = "Indiquez votre numéro de téléphone.";
        else if (telephone is null)
            errors["telephone"] = "Numéro de téléphone invalide (ex. : 72 000 000 ou +33 6 12 34 56 78).";
        if (motDePasse.Length < MotDePasseMin)
            errors["motDePasse"] = $"Le mot de passe doit contenir au moins {MotDePasseMin} caractères.";
        else if (motDePasse.Length > MotDePasseMax)
            errors["motDePasse"] = $"Mot de passe trop long ({MotDePasseMax} caractères maximum).";
        if (errors.Count > 0)
            return Result.Invalid(errors);

        if (await db.Set<User>().AnyAsync(u => u.Email == email, cancellationToken))
            return EmailPris();

        var user = new User
        {
            Prenom = prenom,
            Nom = nom,
            Email = email,
            Telephone = telephone!,
            Role = Role.Membre,
            CreeLe = time.GetUtcNow(),
        };
        user.MotDePasseHash = hasher.HashPassword(user, motDePasse);
        db.Set<User>().Add(user);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlErreurs.EstViolationUnicite(ex))
        {
            // Inscription concurrente avec le même email : même réponse que la pré-vérification.
            db.ChangeTracker.Clear();
            return EmailPris();
        }

        var jeton = jetons.Emettre(user);
        return Result.Ok(new AuthReponseDto(jeton.Jeton, jeton.ExpireLe, UtilisateurDto.De(user)));
    }

    private static Echec EmailPris() => Result.Invalid(new Dictionary<string, string> { ["email"] = MessageEmailPris });
}
