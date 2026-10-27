using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Common.Email;
using Aaas.Api.Common.Json;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Aaas.Api.Features.Auth;
using Aaas.Api.Features.Site;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configuration lue à la résolution (et non ici) pour que les tests puissent la surcharger.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Aaas")
        ?? throw new InvalidOperationException("ConnectionStrings:Aaas est absente de la configuration.")));

// L'heure ne se lit que par TimeProvider (AD-11) ; les tests le remplacent par un FakeTimeProvider.
builder.Services.AddSingleton(TimeProvider.System);

// Briques communes : handlers (AD-2), transactions (AD-6), JSON (AD-11, AD-19).
builder.Services.AddHandlers();
builder.Services.AddScoped<TransactionRunner>();
builder.Services.AddJsonConventions();

builder.Services.AddSiteOptions();

// Comptes (AD-8) : JWT, policy Admin, hachage des mots de passe.
builder.Services.AddAuth();

// Emails (AD-13) : un seul IEmailSender vers Admin:NotificationEmail, Smtp ou Journal selon Email:Mode.
builder.Services.AddEmail();

var app = builder.Build();

// Gardes de démarrage déclarées par les slices (Site, Jwt, Admin, Email, dans cet ordre) :
// configuration invalide => OptionsValidationException avant de toucher la base.
app.Services.VerifierConfiguration();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Compte admin créé ou mis à jour depuis la configuration, après les migrations.
await SeedAdmin.ExecuterAsync(app.Services);

// Format d'erreur unique (AD-10) : en tête du pipeline.
app.UseApiErrors();

// Après UseApiErrors : les 401 / 403 du JwtBearer suivent aussi le format unique.
app.UseAuthentication();
app.UseAuthorization();

app.MapSiteEndpoints();
app.MapAuthEndpoints();
app.MapAnimauxEndpoints();

// En dernier : toute autre route sous /api → 404 { error }.
app.MapApiFallback();

app.Run();

public partial class Program;
