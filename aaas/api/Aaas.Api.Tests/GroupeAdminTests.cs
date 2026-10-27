using System.Net;
using Aaas.Api.Common;
using Aaas.Api.Tests.Outils;
using static Aaas.Api.Tests.Outils.AssertionsErreur;
using static Aaas.Api.Tests.Outils.Comptes;

namespace Aaas.Api.Tests;

/// <summary>Comptes côté API (story 3) : groupe admin et policy.</summary>
[Collection(ApiCollection.Name)]
public class GroupeAdminTests(AaasFactory factory)
{
    [Fact]
    public async Task Membre_sur_le_groupe_admin_renvoie_403_au_format_unique()
    {
        var jeton = await factory.InscrireAsync(NouvelEmail());

        var response = await factory.GetAvecJetonAsync("/tests/admin", jeton);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertErreurAsync(response, "Cette page est réservée à l'association.");
    }

    [Fact]
    public async Task Anonyme_sur_le_groupe_admin_renvoie_401()
    {
        var response = await factory.GetAvecJetonAsync("/tests/admin", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await AssertErreurAsync(response, ApiErrors.MessageNonConnecte);
    }

    [Fact]
    public async Task Admin_seede_accede_au_groupe_admin()
    {
        var jeton = await factory.ConnecterAsync(AaasFactory.AdminEmail, AaasFactory.AdminPassword);

        var response = await factory.GetAvecJetonAsync("/tests/admin", jeton);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task La_policy_lit_le_role_dans_le_claim_role_et_nulle_part_ailleurs()
    {
        var roleClaimTypesUri = factory.FabriquerJeton(new Dictionary<string, object>
        {
            ["sub"] = "1",
            ["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] = "Admin",
        });
        var roleClaim = factory.FabriquerJeton(new Dictionary<string, object> { ["sub"] = "1", ["role"] = "Admin" });

        Assert.Equal(HttpStatusCode.Forbidden, (await factory.GetAvecJetonAsync("/tests/admin", roleClaimTypesUri)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await factory.GetAvecJetonAsync("/tests/admin", roleClaim)).StatusCode);
    }
}
