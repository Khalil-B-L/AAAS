using System.Net;
using Aaas.Api.Data;
using Aaas.Api.Features.Auth;
using Aaas.Api.Tests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Aaas.Api.Tests.Outils.Comptes;
using static Aaas.Api.Tests.Outils.JetonsDeTest;
using static Aaas.Api.Tests.Outils.RequetesApi;

namespace Aaas.Api.Tests;

/// <summary>Comptes côté API (story 3) : seed du compte admin et gardes de démarrage Jwt / Admin.</summary>
[Collection(ApiCollection.Name)]
public class SeedEtGardesTests(AaasFactory factory)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // --- Seed admin ------------------------------------------------------------------------------------

    [Fact]
    public async Task Admin_seede_se_connecte_et_moi_renvoie_le_role_Admin()
    {
        var jeton = await factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);

        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        Assert.Equal("Admin", doc.RootElement.GetProperty("role").GetString());
        Assert.Equal(SeedAdmin.Prenom, doc.RootElement.GetProperty("prenom").GetString());
        Assert.Equal(SeedAdmin.Nom, doc.RootElement.GetProperty("nom").GetString());
        Assert.Equal(AaasFactory.SiteTelephone, doc.RootElement.GetProperty("telephone").GetString());
    }

    [Fact]
    public async Task Seed_remet_Admin_un_compte_existant_avec_le_mot_de_passe_configure()
    {
        var email = NouvelEmail();
        await factory.InscrireAsync(email);
        const string nouveauMotDePasse = "NouveauMotDePasseAdmin";

        try
        {
            using var redemarree = factory.Redemarrer(new() { ["Admin:Email"] = $" {email.ToUpperInvariant()} ", ["Admin:Password"] = nouveauMotDePasse });
            var client = redemarree.CreateClient();

            var ancien = await client.PostAsync("/api/auth/connexion", JsonContent(Connexion(email, MotDePasse)), Ct);
            Assert.Equal(HttpStatusCode.BadRequest, ancien.StatusCode);

            var jeton = await factory.ConnecterAsync(email, nouveauMotDePasse);
            Assert.Equal("Admin", LireJeton(jeton).GetClaim("role").Value);
            Assert.Equal(HttpStatusCode.OK, (await factory.GetAvecJetonAsync("/tests/admin", jeton)).StatusCode);
        }
        finally
        {
            factory.RestaurerAdminParDefaut();
        }
    }

    [Fact]
    public async Task Changer_Admin_Email_remet_Membre_l_ancien_admin()
    {
        var emailA = NouvelEmail();
        var emailB = NouvelEmail();
        const string motDePasseAdmin = "MotDePasseAdminAB";

        try
        {
            using (factory.Redemarrer(new() { ["Admin:Email"] = emailA, ["Admin:Password"] = motDePasseAdmin }))
            {
            }
            Assert.Equal(Role.Admin, (await LireUserAsync(emailA)).Role);

            using (factory.Redemarrer(new() { ["Admin:Email"] = emailB, ["Admin:Password"] = motDePasseAdmin }))
            {
            }

            Assert.Equal(Role.Membre, (await LireUserAsync(emailA)).Role);
            Assert.Equal(Role.Admin, (await LireUserAsync(emailB)).Role);
            Assert.Equal(Role.Membre, (await LireUserAsync(AaasFactory.AdminEmail)).Role);

            var jetonA = await factory.ConnecterAsync(emailA, motDePasseAdmin);
            Assert.Equal("Membre", LireJeton(jetonA).GetClaim("role").Value);
            Assert.Equal(HttpStatusCode.Forbidden, (await factory.GetAvecJetonAsync("/tests/admin", jetonA)).StatusCode);
        }
        finally
        {
            factory.RestaurerAdminParDefaut();
        }
        Assert.Equal(Role.Membre, (await LireUserAsync(emailB)).Role);
        Assert.Equal(Role.Admin, (await LireUserAsync(AaasFactory.AdminEmail)).Role);
    }

    [Theory]
    [InlineData("72 000 000", "+21672000000")]
    [InlineData("+33 6 00 00 00 00", "+33600000000")]
    public async Task Seed_normalise_Site_Telephone_en_E164(string siteTelephone, string attendu)
    {
        var email = NouvelEmail();

        try
        {
            using (factory.Redemarrer(new() { ["Admin:Email"] = email, ["Site:Telephone"] = siteTelephone }))
            {
            }

            Assert.Equal(attendu, (await LireUserAsync(email)).Telephone);
        }
        finally
        {
            factory.RestaurerAdminParDefaut();
        }
    }

    // Site:Telephone invalide : refusé par les options du site avant toute base (SocleTests).

    [Fact]
    public async Task Seed_aligne_le_telephone_de_l_admin_existant_sur_Site_Telephone()
    {
        var avant = await LireUserAsync(AaasFactory.AdminEmail);
        Assert.Equal(AaasFactory.SiteTelephone, avant.Telephone);

        try
        {
            using (factory.Redemarrer(new() { ["Site:Telephone"] = "+33 6 00 00 00 00" }))
            {
            }

            var apres = await LireUserAsync(AaasFactory.AdminEmail);
            Assert.Equal(avant.Id, apres.Id);
            Assert.Equal("+33600000000", apres.Telephone);
        }
        finally
        {
            factory.RestaurerAdminParDefaut();
        }
        Assert.Equal(AaasFactory.SiteTelephone, (await LireUserAsync(AaasFactory.AdminEmail)).Telephone);
    }

    [Fact]
    public async Task Seed_promeut_un_Membre_avec_l_identite_du_compte_admin_et_un_avertissement()
    {
        var email = NouvelEmail();
        var inscription = await factory.PostJsonAsync("/api/auth/inscription", Inscription(email, telephone: "98 765 432"));
        Assert.Equal(HttpStatusCode.Created, inscription.StatusCode);
        var membre = await LireUserAsync(email);
        var avertissementsAvant = factory.Journal.Entrees.Count(EstAvertissementPromotion);

        try
        {
            using (factory.Redemarrer(new() { ["Admin:Email"] = email }))
            {
            }

            var promu = await LireUserAsync(email);
            Assert.Equal(membre.Id, promu.Id);
            Assert.Equal(Role.Admin, promu.Role);
            Assert.Equal(SeedAdmin.Prenom, promu.Prenom);
            Assert.Equal(SeedAdmin.Nom, promu.Nom);
            Assert.Equal(AaasFactory.SiteTelephone, promu.Telephone);
            Assert.True(factory.Journal.Entrees.Count(EstAvertissementPromotion) > avertissementsAvant);
        }
        finally
        {
            factory.RestaurerAdminParDefaut();
        }
    }

    private static bool EstAvertissementPromotion(JournalDeTest.Entree e) =>
        e.Niveau == Microsoft.Extensions.Logging.LogLevel.Warning && e.Categorie == typeof(SeedAdmin).FullName
        && e.Message.Contains("Admin:Email");

    [Fact]
    public async Task Seed_est_idempotent()
    {
        var avant = await LireUserAsync(AaasFactory.AdminEmail);

        using (factory.Redemarrer([]))
        {
        }

        using var scope = factory.Services.CreateScope();
        var comptes = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<User>().AsNoTracking()
            .Where(u => u.Email == AaasFactory.AdminEmail).ToListAsync(Ct);
        var apres = Assert.Single(comptes);
        Assert.Equal(avant.Id, apres.Id);
        Assert.Equal(Role.Admin, apres.Role);
        Assert.Equal(avant.MotDePasseHash, apres.MotDePasseHash);
    }

    private async Task<User> LireUserAsync(string email)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<User>().AsNoTracking()
            .SingleAsync(u => u.Email == email, Ct);
    }

    // --- Gardes de démarrage ----------------------------------------------------------------------------

    [Theory]
    [InlineData("Jwt:Key", "", "Jwt:Key est absente")]
    [InlineData("Jwt:Key", "trop-courte", "Jwt:Key est trop courte")]
    [InlineData("Jwt:Key", "0123456789012345678901234567890", "Jwt:Key est trop courte")]
    [InlineData("Admin:Email", "", "Admin:Email")]
    [InlineData("Admin:Email", "pas-un-email", "Admin:Email")]
    [InlineData("Admin:Password", "", "Admin:Password est absent")]
    [InlineData("Admin:Password", "court", "Admin:Password est trop court")]
    [InlineData("Jwt:ExpiresMinutes", "0", "Jwt:ExpiresMinutes")]
    [InlineData("Jwt:ExpiresMinutes", "61", "Jwt:ExpiresMinutes")]
    public void Demarrage_refuse_sans_configuration_Jwt_ou_Admin_valide(string cle, string valeur, string attendu)
    {
        var validation = factory.DemarrerAvecConfigurationInvalide(new() { [cle] = valeur });

        Assert.NotNull(validation);
        Assert.Contains(attendu, validation.Message);
    }
}
