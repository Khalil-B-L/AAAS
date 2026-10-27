using System.Net;
using Aaas.Api.Common;
using Aaas.Api.Features.Auth.Commands;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;
using static Aaas.Api.Tests.Outils.JetonsDeTest;
using static Aaas.Api.Tests.Outils.RequetesApi;

namespace Aaas.Api.Tests;

/// <summary>Comptes côté API (story 3) : connexion, session (/moi) et jeton.</summary>
[Collection(ApiCollection.Name)]
public class ConnexionEtSessionTests(AaasFactory factory)
{
    [Fact]
    public async Task Connexion_avec_identifiants_justes_renvoie_200_AuthReponseDto()
    {
        var email = NouvelEmail();
        await factory.InscrireAsync(email);

        var response = await factory.PostJsonAsync("/api/auth/connexion", Connexion($" {email.ToUpperInvariant()}", MotDePasse));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        Assert.Equal(["jeton", "expireLe", "utilisateur"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(email, doc.RootElement.GetProperty("utilisateur").GetProperty("email").GetString());
        Assert.Equal("Membre", doc.RootElement.GetProperty("utilisateur").GetProperty("role").GetString());
    }

    [Fact]
    public async Task Identifiants_faux_renvoient_400_identifiants_invalides_avec_le_meme_message()
    {
        var email = NouvelEmail();
        await factory.InscrireAsync(email);

        var inconnu = await factory.PostJsonAsync("/api/auth/connexion", Connexion(NouvelEmail(), MotDePasse));
        var mauvais = await factory.PostJsonAsync("/api/auth/connexion", Connexion(email, "MauvaisMotDePasse"));

        foreach (var response in new[] { inconnu, mauvais })
        {
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            using var doc = await LireJsonAsync(response);
            Assert.Equal(["error", "code"], doc.RootElement.EnumerateObject().Select(p => p.Name));
            Assert.Equal("identifiants-invalides", doc.RootElement.GetProperty("code").GetString());
            Assert.Equal(ConnecterHandler.MessageIdentifiantsInvalides, doc.RootElement.GetProperty("error").GetString());
        }
        Assert.DoesNotContain(factory.Journal.Entrees, e => e.Message.Contains("MauvaisMotDePasse") || e.Message.Contains(MotDePasse));
    }

    [Fact]
    public async Task Connexion_sans_email_ni_mot_de_passe_renvoie_400_errors_par_champ()
    {
        var response = await factory.PostJsonAsync("/api/auth/connexion", """{"email":" ","motDePasse":""}""");

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["email", "motDePasse"], errors.Keys.Order());
    }

    // --- Session ---------------------------------------------------------------------------------

    [Fact]
    public async Task Moi_avec_jeton_renvoie_200_UtilisateurDto()
    {
        var email = NouvelEmail();
        var jeton = await factory.InscrireAsync(email);

        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        Assert.Equal(["id", "prenom", "nom", "email", "telephone", "role"], doc.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(email, doc.RootElement.GetProperty("email").GetString());
        Assert.Equal("Membre", doc.RootElement.GetProperty("role").GetString());
        Assert.Equal(LireJeton(jeton).Subject, doc.RootElement.GetProperty("id").GetInt32().ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("pas-un-jeton")]
    public async Task Moi_sans_jeton_ou_avec_jeton_invalide_renvoie_401_au_format_unique(string? jeton)
    {
        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageNonConnecte);
    }

    [Fact]
    public async Task Moi_avec_le_jeton_d_un_compte_inexistant_renvoie_404_au_format_unique()
    {
        var jeton = factory.FabriquerJeton(new Dictionary<string, object> { ["sub"] = int.MaxValue.ToString(), ["role"] = "Membre" });

        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await AssertErreurAsync(response, "Ce compte est introuvable.");
    }

    [Fact]
    public async Task Jeton_signe_avec_une_autre_cle_renvoie_401()
    {
        var jeton = factory.FabriquerJeton(new Dictionary<string, object> { ["sub"] = "1", ["role"] = "Admin" },
            cle: "une-autre-cle-de-test-de-plus-de-32-octets!!");

        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Jeton_expire_selon_le_TimeProvider_renvoie_401()
    {
        var jeton = await factory.InscrireAsync(NouvelEmail());
        Assert.Equal(HttpStatusCode.OK, (await factory.GetMoiAsync(jeton)).StatusCode);

        factory.Time.Advance(TimeSpan.FromMinutes(59));
        Assert.Equal(HttpStatusCode.OK, (await factory.GetMoiAsync(jeton)).StatusCode);

        factory.Time.Advance(TimeSpan.FromMinutes(2));
        var response = await factory.GetMoiAsync(jeton);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageNonConnecte);
    }

    [Fact]
    public async Task Jeton_porte_sub_email_role_jti_et_dure_60_minutes()
    {
        var email = NouvelEmail();
        var jeton1 = LireJeton(await factory.InscrireAsync(email));
        var jeton2 = LireJeton(await factory.ConnecterAsync(email, MotDePasse));

        Assert.Equal("HS256", jeton1.Alg);
        Assert.Equal(email, jeton1.GetClaim("email").Value);
        Assert.Equal("Membre", jeton1.GetClaim("role").Value);
        Assert.True(int.TryParse(jeton1.Subject, out _));
        Assert.Equal(jeton1.Subject, jeton2.Subject);
        Assert.NotEqual(jeton1.Id, jeton2.Id);
        Assert.Equal(TimeSpan.FromMinutes(60), jeton1.ValidTo - jeton1.IssuedAt);
    }
}
