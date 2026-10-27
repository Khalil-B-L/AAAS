using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Auth.Contrats;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Auth.Commands;

/// <summary>
/// Connexion par email et mot de passe. Email inconnu et mot de passe faux donnent la même réponse
/// (400 <c>identifiants-invalides</c>) après un calcul de même coût : la connexion ne révèle pas les emails.
/// Commande, car elle peut réécrire le hachage (<see cref="PasswordVerificationResult.SuccessRehashNeeded"/>).
/// </summary>
public sealed record ConnecterCommand(string? Email, string? MotDePasse);

public sealed class ConnecterHandler(
    AppDbContext db,
    IPasswordHasher<User> hasher,
    Jetons jetons) : ICommandHandler<ConnecterCommand, AuthReponseDto>
{
    public const string CodeIdentifiantsInvalides = "identifiants-invalides";
    public const string MessageIdentifiantsInvalides = "Email ou mot de passe incorrect.";

    private static readonly User UserFactice = new();
    private static readonly Lazy<string> HashFactice =
        new(() => new PasswordHasher<User>().HashPassword(UserFactice, Guid.NewGuid().ToString()));

    public async Task<Result<AuthReponseDto>> HandleAsync(ConnecterCommand command, CancellationToken cancellationToken)
    {
        var email = AdresseEmail.Normaliser(command.Email);
        var motDePasse = command.MotDePasse ?? string.Empty;

        var errors = new Dictionary<string, string>();
        if (email.Length == 0)
            errors["email"] = "Indiquez votre adresse email.";
        if (motDePasse.Length == 0)
            errors["motDePasse"] = "Indiquez votre mot de passe.";
        if (errors.Count > 0)
            return Result.Invalid(errors);

        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            hasher.VerifyHashedPassword(UserFactice, HashFactice.Value, motDePasse);
            return IdentifiantsInvalides();
        }

        var verification = hasher.VerifyHashedPassword(user, user.MotDePasseHash, motDePasse);
        if (verification == PasswordVerificationResult.Failed)
            return IdentifiantsInvalides();
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.MotDePasseHash = hasher.HashPassword(user, motDePasse);
            await db.SaveChangesAsync(cancellationToken);
        }

        var jeton = jetons.Emettre(user);
        return Result.Ok(new AuthReponseDto(jeton.Jeton, jeton.ExpireLe, UtilisateurDto.De(user)));
    }

    private static Echec IdentifiantsInvalides() => Result.Invalid(CodeIdentifiantsInvalides, MessageIdentifiantsInvalides);
}
