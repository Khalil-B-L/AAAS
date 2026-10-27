using System.Net;
using Aaas.Api.Features.Auth.Commands;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;
using static Aaas.Api.Tests.Outils.JetonsDeTest;
using static Aaas.Api.Tests.Outils.RequetesApi;

namespace Aaas.Api.Tests;

/// <summary>Comptes côté API (story 3) : inscription.</summary>
[Collection(ApiCollection.Name)]
public class InscriptionTests(AaasFactory factory)
{
    [Fact]
    public async Task Inscription_complete_renvoie_201_membre_et_telephone_E164()
    {
        var email = NouvelEmail();

        var response = await factory.PostJsonAsync("/api/auth/inscription", Inscription(email, telephone: "72 123 456"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        var racine = doc.RootElement;
        Assert.Equal(["jeton", "expireLe", "utilisateur"], racine.EnumerateObject().Select(p => p.Name));
        Assert.False(string.IsNullOrEmpty(racine.GetProperty("jeton").GetString()));
        Assert.Equal(factory.Time.GetUtcNow().AddMinutes(60), racine.GetProperty("expireLe").GetDateTimeOffset());
        Assert.EndsWith("Z", racine.GetProperty("expireLe").GetString());

        var utilisateur = racine.GetProperty("utilisateur");
        Assert.Equal(["id", "prenom", "nom", "email", "telephone", "role"], utilisateur.EnumerateObject().Select(p => p.Name));
        Assert.Equal("Camille", utilisateur.GetProperty("prenom").GetString());
        Assert.Equal("Martin", utilisateur.GetProperty("nom").GetString());
        Assert.Equal(email, utilisateur.GetProperty("email").GetString());
        Assert.Equal("+21672123456", utilisateur.GetProperty("telephone").GetString());
        Assert.Equal("Membre", utilisateur.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Inscription_publique_ignore_un_role_envoye_et_cree_un_membre()
    {
        var corps = $$"""{"prenom":"Eve","nom":"Durand","email":"{{NouvelEmail()}}","telephone":"72123456","motDePasse":"{{MotDePasse}}","role":"Admin"}""";

        var response = await factory.PostJsonAsync("/api/auth/inscription", corps);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        Assert.Equal("Membre", doc.RootElement.GetProperty("utilisateur").GetProperty("role").GetString());
        Assert.Equal("Membre", LireJeton(doc.RootElement.GetProperty("jeton").GetString()!).GetClaim("role").Value);
    }

    [Theory]
    [InlineData("\"telephone\":\"\",")]
    [InlineData("\"telephone\":\"   \",")]
    [InlineData("\"telephone\":null,")]
    [InlineData("")]
    public async Task Inscription_sans_telephone_renvoie_400_errors_telephone(string champTelephone)
    {
        var corps = $$"""{"prenom":"Camille","nom":"Martin","email":"{{NouvelEmail()}}",{{champTelephone}}"motDePasse":"{{MotDePasse}}"}""";

        var response = await factory.PostJsonAsync("/api/auth/inscription", corps);

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["telephone"], errors.Keys);
        Assert.Equal("Indiquez votre numéro de téléphone.", errors["telephone"]);
    }

    [Theory]
    [InlineData("72.12 34-56", "+21672123456")]
    [InlineData("  98765432 ", "+21698765432")]
    [InlineData("29-555-000", "+21629555000")]
    [InlineData("+32 470 12 34 56", "+32470123456")]
    [InlineData("+33.6.12.34.56.78", "+33612345678")]
    [InlineData("+1 202-555-0143", "+12025550143")]
    public async Task Telephone_accepte_est_stocke_et_renvoye_en_E164(string saisie, string attendu)
    {
        var email = NouvelEmail();

        var response = await factory.PostJsonAsync("/api/auth/inscription", Inscription(email, telephone: saisie));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var doc = await LireJsonAsync(response);
        Assert.Equal(attendu, doc.RootElement.GetProperty("utilisateur").GetProperty("telephone").GetString());

        // Relu depuis la base par /moi : c'est bien la forme stockée.
        var moi = await factory.GetMoiAsync(doc.RootElement.GetProperty("jeton").GetString());
        using var docMoi = await LireJsonAsync(moi);
        Assert.Equal(attendu, docMoi.RootElement.GetProperty("telephone").GetString());
    }

    [Theory]
    [InlineData("7212345")]            // 7 chiffres
    [InlineData("721234567")]          // 9 chiffres
    [InlineData("02123456")]           // premier chiffre nul
    [InlineData("06 12 34 56 78")]     // ancienne forme française, plus reconnue
    [InlineData("72 12 34 5a")]
    [InlineData("72/12/34/56")]       // séparateur non toléré
    [InlineData("+1234567")]           // 7 chiffres
    [InlineData("+1234567890123456")]  // 16 chiffres
    [InlineData("+0612345678")]        // indicatif commençant par 0
    [InlineData("++33612345678")]
    [InlineData("33612345678")]        // international sans +
    [InlineData("+33 (0)6 12 34 56 78")]
    public async Task Telephone_de_format_inconnu_renvoie_400_errors_telephone(string saisie)
    {
        var response = await factory.PostJsonAsync("/api/auth/inscription", Inscription(NouvelEmail(), telephone: saisie));

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["telephone"], errors.Keys);
        Assert.StartsWith("Numéro de téléphone invalide", errors["telephone"]);
    }

    [Fact]
    public async Task Email_deja_pris_avec_casse_ou_espaces_differents_renvoie_400_errors_email()
    {
        var email = NouvelEmail();
        Assert.Equal(HttpStatusCode.Created, (await factory.PostJsonAsync("/api/auth/inscription", Inscription(email))).StatusCode);

        var response = await factory.PostJsonAsync("/api/auth/inscription", Inscription($"  {email.ToUpperInvariant()} "));

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["email"], errors.Keys);
        Assert.Equal(InscrireHandler.MessageEmailPris, errors["email"]);
    }

    [Fact]
    public async Task Inscriptions_concurrentes_du_meme_email_un_seul_compte_l_autre_400_errors_email()
    {
        var email = NouvelEmail();

        var reponses = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => factory.PostJsonAsync("/api/auth/inscription", Inscription(email))));

        Assert.Single(reponses, r => r.StatusCode == HttpStatusCode.Created);
        foreach (var refus in reponses.Where(r => r.StatusCode != HttpStatusCode.Created))
        {
            var errors = await AssertValidationAsync(refus);
            Assert.Equal(InscrireHandler.MessageEmailPris, errors["email"]);
        }
    }

    [Fact]
    public async Task Champs_invalides_renvoient_400_errors_par_champ_en_camelCase()
    {
        var corps = """{"prenom":"  ","nom":"","email":"pas-un-email","telephone":"72123456","motDePasse":"court"}""";

        var response = await factory.PostJsonAsync("/api/auth/inscription", corps);

        var errors = await AssertValidationAsync(response);
        Assert.Equal(["email", "motDePasse", "nom", "prenom"], errors.Keys.Order());
        Assert.Equal("Indiquez votre prénom.", errors["prenom"]);
        Assert.Equal("Indiquez votre nom.", errors["nom"]);
        Assert.Equal("Adresse email invalide (ex. : nom@domaine.fr).", errors["email"]);
        Assert.Equal("Le mot de passe doit contenir au moins 8 caractères.", errors["motDePasse"]);
    }
}
