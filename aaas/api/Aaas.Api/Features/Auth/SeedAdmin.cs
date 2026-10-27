using Aaas.Api.Common;
using Aaas.Api.Data;
using Aaas.Api.Features.Site;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aaas.Api.Features.Auth;

/// <summary>
/// Crée ou met à jour le compte Admin au démarrage (AD-1, AD-8), après les migrations. Idempotent : retrouve
/// le compte par email normalisé, force <see cref="Role.Admin"/>, rehache le mot de passe s'il ne vérifie plus
/// et remet <see cref="Role.Membre"/> tout autre compte Admin (un seul admin). Le téléphone du compte suit <c>Site:Telephone</c>
/// (déjà en E.164) ; un compte promu prend le prénom et le nom du seed.
/// Seule voie de création d'un Admin.
/// </summary>
public static class SeedAdmin
{
    public const string Prenom = "Administrateur";
    public const string Nom = "AAAS";

    public static async Task ExecuterAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher<User>>();
        var admin = sp.GetRequiredService<IOptions<AdminOptions>>().Value;
        var site = sp.GetRequiredService<IOptions<SiteOptions>>().Value;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(SeedAdmin));

        var email = AdresseEmail.Normaliser(admin.Email);
        var motDePasse = admin.Password!;
        // Déjà validé et normalisé en E.164 par les options du site (garde de démarrage, avant Migrate()).
        var telephone = site.Telephone;

        // Un seul admin : tout autre compte Admin (ancien Admin:Email) redevient Membre.
        var anciensAdmins = await db.Set<User>()
            .Where(u => u.Role == Role.Admin && u.Email != email)
            .ToListAsync(cancellationToken);
        foreach (var ancien in anciensAdmins)
            ancien.Role = Role.Membre;
        var modifie = anciensAdmins.Count > 0;

        var user = await db.Set<User>().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);
        if (user is null)
        {
            user = new User
            {
                Prenom = Prenom,
                Nom = Nom,
                Email = email,
                Telephone = telephone,
                Role = Role.Admin,
                CreeLe = sp.GetRequiredService<TimeProvider>().GetUtcNow(),
            };
            user.MotDePasseHash = hasher.HashPassword(user, motDePasse);
            db.Set<User>().Add(user);
            modifie = true;
        }
        else
        {
            if (user.Role != Role.Admin)
            {
                // Promotion d'un compte existant (inscrit comme Membre) : il prend l'identité du compte admin.
                logger.LogWarning(
                    "Le compte {UserId} (rôle {Role}) correspond à Admin:Email : il devient Admin, prénom et nom remplacés par ceux du compte admin.",
                    user.Id, user.Role);
                user.Role = Role.Admin;
                user.Prenom = Prenom;
                user.Nom = Nom;
                modifie = true;
            }
            if (user.Telephone != telephone)
            {
                user.Telephone = telephone;
                modifie = true;
            }
            if (hasher.VerifyHashedPassword(user, user.MotDePasseHash, motDePasse) != PasswordVerificationResult.Success)
            {
                user.MotDePasseHash = hasher.HashPassword(user, motDePasse);
                modifie = true;
            }
        }

        if (modifie)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Compte admin {UserId} à jour ; {Retrogrades} ancien(s) admin(s) remis Membre.",
                user.Id, anciensAdmins.Count);
        }
    }
}
