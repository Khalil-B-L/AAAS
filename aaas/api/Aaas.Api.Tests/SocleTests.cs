using System.Net;
using System.Net.Http.Json;
using Aaas.Api.Data;
using Aaas.Api.Features.Site;
using Aaas.Api.Tests.Outils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests;

[Collection(ApiCollection.Name)]
public class SocleTests(AaasFactory factory)
{
    [Fact]
    public async Task Site_renvoie_les_valeurs_de_configuration()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/site", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var site = await response.Content.ReadFromJsonAsync<SiteDto>(TestContext.Current.CancellationToken);
        Assert.Equal(new SiteDto(AaasFactory.SiteTelephone, AaasFactory.SiteFacebook, AaasFactory.SiteInstagram), site);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("\"telephone\":", json);
        Assert.Contains("\"facebook\":", json);
        Assert.Contains("\"instagram\":", json);
    }

    [Fact]
    public async Task Aucune_migration_en_attente_au_demarrage()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.Contains(await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken),
            m => m.EndsWith("_Socle_Initial", StringComparison.Ordinal));
    }

    [Fact]
    public void TimeProvider_resolu_est_le_FakeTimeProvider()
    {
        var time = factory.Services.GetRequiredService<TimeProvider>();

        Assert.IsType<FakeTimeProvider>(time);
        Assert.Same(factory.Time, time);
    }

    [Fact]
    public void Demarrage_refuse_sans_Site_Telephone()
    {
        var validation = factory.DemarrerAvecConfigurationInvalide(new() { ["Site:Telephone"] = "" });

        Assert.NotNull(validation);
        Assert.Contains("Site:Telephone", validation.Message);
    }

    [Fact]
    public async Task Site_Telephone_national_est_renvoye_en_E164()
    {
        try
        {
            using var national = factory.Configurer(new() { ["Site:Telephone"] = "72 000 000" });

            var site = await national.CreateClient().GetFromJsonAsync<SiteDto>("/api/site", TestContext.Current.CancellationToken);

            Assert.Equal("+21672000000", site?.Telephone);
        }
        finally
        {
            // Le seed rejoué a aligné le téléphone de l'admin : on le remet sur la configuration de la factory.
            factory.RestaurerAdminParDefaut();
        }
    }

    [Fact]
    public async Task Demarrage_refuse_si_Site_Telephone_est_invalide_avant_toute_base()
    {
        var baseJamaisCreee = $"AaasTests_{Guid.NewGuid():N}";
        var connexion = $@"Server=(localdb)\MSSQLLocalDB;Database={baseJamaisCreee};Trusted_Connection=True;TrustServerCertificate=True";

        var validation = factory.DemarrerAvecConfigurationInvalide(new()
        {
            ["Site:Telephone"] = "pas-un-numero",
            ["ConnectionStrings:Aaas"] = connexion,
        });

        Assert.NotNull(validation);
        Assert.Contains("Site:Telephone", validation.Message);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.SetConnectionString(connexion);
        Assert.False(await db.Database.CanConnectAsync(TestContext.Current.CancellationToken));
    }
}
