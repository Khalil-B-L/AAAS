using System.Data;
using Aaas.Api.Common;
using Aaas.Api.Common.Data;
using Aaas.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Aaas.Api.Tests;

public enum StatutTest
{
    EnAttente,
    Confirmee,
}

public sealed record DateEtEnum(DateTimeOffset Quand, StatutTest Statut, string? Absent);

public sealed record CorpsAvecEnum(StatutTest Statut);

public sealed record CorpsAvecDate(DateTimeOffset Quand);

/// <summary>
/// Endpoints <c>/tests/…</c> ajoutés en aval du pipeline de <c>Program</c> (donc derrière <c>UseApiErrors</c>,
/// hors du fallback <c>/api/**</c>, avec les options JSON globales). N'existent jamais en production.
/// </summary>
public sealed class EndpointsDeTest : IStartupFilter
{
    public const string TableTransaction = "TestsTransaction";

    public static readonly DateTimeOffset DateNonUtc = new(2026, 1, 15, 12, 0, 0, TimeSpan.FromHours(2));

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        next(app);
        app.UseRouting();
        // Ce routage est propre aux routes de test : l'autorisation doit suivre pour /tests/admin.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(Mapper);
    };

    private static void Mapper(IEndpointRouteBuilder e)
    {
        // Route du groupe admin partagé : jamais en production (contrats-api.md §4).
        e.MapGroupeAdmin("/tests/admin").MapGet("", () => Result.Ok("admin").ToHttpResult());

        e.MapGet("/tests/exception", IResult () => throw new InvalidOperationException("détail secret"));

        e.MapGet("/tests/date-enum", () =>
            Result.Ok(new DateEtEnum(DateNonUtc, StatutTest.EnAttente, null)).ToHttpResult());

        e.MapPost("/tests/enum", (CorpsAvecEnum corps) => Result.Ok(corps).ToHttpResult());

        e.MapPost("/tests/date", (CorpsAvecDate corps) => Result.Ok(corps).ToHttpResult());

        e.MapGet("/tests/result/{variante}", (string variante) => variante switch
        {
            "ok" => Result<string>.Ok("valeur").ToHttpResult(),
            "cree" => Result.Ok("valeur").ToHttpResult(StatusCodes.Status201Created),
            "introuvable" => Result<string>.NotFound().ToHttpResult(),
            "introuvable-message" => Result<string>.NotFound("Cet animal est introuvable.").ToHttpResult(),
            "conflit" => Result<string>.Conflict("animal-indisponible", "Trop tard.").ToHttpResult(),
            "invalide" => Result<string>.Invalid(new Dictionary<string, string> { ["nom"] = "Le nom est obligatoire." }).ToHttpResult(),
            "invalide-code" => Result<string>.Invalid("identifiants-invalides", "Email ou mot de passe incorrect.").ToHttpResult(),
            "interdit" => Result<string>.Forbidden().ToHttpResult(),
            "indisponible" => Result<string>.Unavailable("envoi-impossible", "Envoi impossible.").ToHttpResult(),
            "sans-valeur" => Result.Ok().ToHttpResult(),
            "sans-valeur-echec" => ((Result)Result.Conflict("reservation-echue", "Réservation échue.")).ToHttpResult(),
            _ => throw new ArgumentOutOfRangeException(nameof(variante)),
        });

        // Insère une ligne puis réussit, échoue (Result) ou lève : seule la réussite doit être commitée.
        e.MapPost("/tests/transaction/{issue}", async (string issue, string valeur, TransactionRunner runner, AppDbContext db, CancellationToken ct) =>
        {
            var result = await runner.RunAsync(IsolationLevel.Serializable, async token =>
            {
                await db.Database.ExecuteSqlAsync($"INSERT INTO dbo.TestsTransaction (Valeur) VALUES ({valeur})", token);
                return issue switch
                {
                    "succes" => Result.Ok(valeur),
                    "echec" => Result.Conflict("test-echec", "Échec du travail."),
                    _ => throw new InvalidOperationException("Le travail lève."),
                };
            }, ct);
            return result.ToHttpResult();
        });
    }
}
