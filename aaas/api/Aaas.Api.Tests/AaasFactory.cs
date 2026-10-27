using Aaas.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests;

/// <summary>
/// API complète sur une base LocalDB jetable, supprimée en fin de run.
/// Environnement <c>Testing</c>, configuration en mémoire, heure pilotée par <see cref="Time"/>.
/// </summary>
public class AaasFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SiteTelephone = "+21672123456";
    public const string SiteFacebook = "https://www.facebook.com/aaas-test";
    public const string SiteInstagram = "https://www.instagram.com/aaas-test";

    /// <summary>Secrets de test (en production : user-secrets).</summary>
    public const string JwtKey = "cle-de-test-aaas-au-moins-32-octets-0123456789";
    public const string AdminEmail = "admin@aaas.test";
    public const string AdminPassword = "MotDePasseAdmin1";
    public const string NotificationEmail = "notifications@aaas.test";

    public string DatabaseName { get; } = $"AaasTests_{Guid.NewGuid():N}";

    public string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    /// <summary>Horloge des tests, remplace <see cref="TimeProvider"/> dans l'API.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));

    /// <summary>Journal capturé de l'API (vérifie par exemple qu'une exception non gérée est journalisée).</summary>
    public JournalDeTest Journal { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Aaas"] = ConnectionString,
            ["Site:Telephone"] = SiteTelephone,
            ["Site:Facebook"] = SiteFacebook,
            ["Site:Instagram"] = SiteInstagram,
            ["Jwt:Key"] = JwtKey,
            ["Admin:Email"] = AdminEmail,
            ["Admin:Password"] = AdminPassword,
            ["Admin:NotificationEmail"] = NotificationEmail,
        }));
        builder.ConfigureLogging(logging => logging.AddProvider(Journal));
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Time);
            // Endpoints /tests/... déclarés uniquement dans les tests, en aval du pipeline de Program.
            services.AddTransient<IStartupFilter, EndpointsDeTest>();
        });
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public override async ValueTask DisposeAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        }
        finally
        {
            // L'hôte est arrêté même si la suppression de la base échoue.
            await base.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<AaasFactory>
{
    public const string Name = "api";
}
