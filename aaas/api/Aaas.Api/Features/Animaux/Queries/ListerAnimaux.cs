using System.Globalization;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Queries;

/// <summary>
/// Catalogue public (<c>GET /api/animaux</c>) : animaux dont le statut effectif (AD-5) n'est pas Adopté, carte au statut
/// effectif, <c>creeLe</c> décroissant (<c>contrats-api.md</c> §2-§3).
/// <c>Limite</c> est la valeur brute du paramètre <c>limite</c> (entier 1..50, ou absent).
/// </summary>
public sealed record ListerAnimauxQuery(string? Limite);

public sealed class ListerAnimauxHandler(AppDbContext db, TimeProvider temps)
    : IQueryHandler<ListerAnimauxQuery, CollectionDto<AnimalCarteDto>>
{
    public const int LimiteMax = 50;
    public const string MessageLimite = "La limite doit être un nombre entier entre 1 et 50.";

    public async Task<Result<CollectionDto<AnimalCarteDto>>> HandleAsync(ListerAnimauxQuery query, CancellationToken cancellationToken)
    {
        int? limite = null;
        if (query.Limite is not null)
        {
            if (!int.TryParse(query.Limite, NumberStyles.None, CultureInfo.InvariantCulture, out var valeur)
                || valeur is < 1 or > LimiteMax)
            {
                return Result.Invalid(new Dictionary<string, string> { ["limite"] = MessageLimite });
            }
            limite = valeur;
        }

        // Statut effectif calculé dans la requête SQL (sous-requête sur les réservations), au même instant pour tout.
        var now = temps.GetUtcNow();
        var reservations = db.Set<Reservation>().AsNoTracking();
        var nonAdopte = AnimalRules.StatutEffectif<Animal, Reservation>(reservations, now)
            .Composer((_, statut) => statut != StatutAnimal.Adopte);

        var requete = db.Set<Animal>()
            .AsNoTracking()
            .Where(nonAdopte)
            .OrderByDescending(a => a.CreeLe)
            .ThenByDescending(a => a.Id)
            .Select(AnimalCarteDto.Projection(reservations, now));

        if (limite is { } n)
            requete = requete.Take(n);

        var items = await requete.ToListAsync(cancellationToken);
        return Result.Ok(new CollectionDto<AnimalCarteDto>(items, now));
    }
}
