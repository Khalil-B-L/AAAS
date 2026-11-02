using System.Globalization;
using Aaas.Api.Common;
using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Common.Fichiers;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux.Contrats;
using Aaas.Api.Features.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux.Queries;

/// <summary>
/// Catalogue public (<c>GET /api/animaux</c>) : animaux dont le statut effectif (AD-5) n'est pas Adopté, filtrés par les
/// paramètres de <c>contrats-api.md</c> §2 (combinés en ET), carte au statut effectif, <c>creeLe</c> décroissant (§3).
/// Chaque propriété est la valeur brute du paramètre du même nom (en camelCase), ou <c>null</c> s'il est absent :
/// <c>Limite</c> entier 1..50 ; <c>Espece</c>, <c>Sexe</c>, <c>Age</c>, <c>Taille</c> nom exact de l'énumération ;
/// <c>Enfants</c>, <c>Chiens</c>, <c>Chats</c> uniquement <c>Oui</c> (entente <c>Oui</c> ou <c>AVoir</c>).
/// </summary>
public sealed record ListerAnimauxQuery(
    string? Limite = null,
    string? Espece = null,
    string? Sexe = null,
    string? Age = null,
    string? Taille = null,
    string? Enfants = null,
    string? Chiens = null,
    string? Chats = null);

public sealed class ListerAnimauxHandler(AppDbContext db, TimeProvider temps, IFileStorage stockage)
    : IQueryHandler<ListerAnimauxQuery, CollectionDto<AnimalCarteDto>>
{
    public const int LimiteMax = 50;
    public const string MessageLimite = "La limite doit être un nombre entier entre 1 et 50.";
    public const string MessageEntente = "Seule la valeur Oui est acceptée.";

    /// <summary>Message d'erreur d'un filtre d'énumération : les valeurs acceptées, casse exacte.</summary>
    public static string MessageEnumeration<T>() where T : struct, Enum =>
        $"Valeur attendue parmi : {string.Join(", ", Enum.GetNames<T>())}.";

    public async Task<Result<CollectionDto<AnimalCarteDto>>> HandleAsync(ListerAnimauxQuery query, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string>();

        int? limite = null;
        if (query.Limite is not null)
        {
            if (int.TryParse(query.Limite, NumberStyles.None, CultureInfo.InvariantCulture, out var valeur)
                && valeur is >= 1 and <= LimiteMax)
                limite = valeur;
            else
                errors["limite"] = MessageLimite;
        }

        var espece = LireEnumeration<Espece>(query.Espece, "espece", errors);
        var sexe = LireEnumeration<Sexe>(query.Sexe, "sexe", errors);
        var age = LireEnumeration<TrancheAge>(query.Age, "age", errors);
        var taille = LireEnumeration<Taille>(query.Taille, "taille", errors);
        var enfants = LireEntente(query.Enfants, "enfants", errors);
        var chiens = LireEntente(query.Chiens, "chiens", errors);
        var chats = LireEntente(query.Chats, "chats", errors);

        if (errors.Count > 0)
            return Result.Invalid(errors);

        // Statut effectif calculé dans la requête SQL (sous-requête sur les réservations), au même instant pour tout.
        var now = temps.GetUtcNow();
        var reservations = db.Set<Reservation>().AsNoTracking();
        var nonAdopte = AnimalRules.StatutEffectif<Animal, Reservation>(reservations, now)
            .Composer((_, statut) => statut != StatutAnimal.Adopte);

        var animaux = db.Set<Animal>().AsNoTracking().Where(nonAdopte);

        // Filtres combinés en ET ; une entente « Oui » demandée garde les animaux à l'entente Oui ou À voir.
        if (espece is { } e)
            animaux = animaux.Where(a => a.Espece == e);
        if (sexe is { } s)
            animaux = animaux.Where(a => a.Sexe == s);
        if (age is { } ag)
            animaux = animaux.Where(a => a.Age == ag);
        if (taille is { } t)
            animaux = animaux.Where(a => a.Taille == t);
        if (enfants)
            animaux = animaux.Where(a => a.EntenteEnfants == Entente.Oui || a.EntenteEnfants == Entente.AVoir);
        if (chiens)
            animaux = animaux.Where(a => a.EntenteChiens == Entente.Oui || a.EntenteChiens == Entente.AVoir);
        if (chats)
            animaux = animaux.Where(a => a.EntenteChats == Entente.Oui || a.EntenteChats == Entente.AVoir);

        IQueryable<Animal> tries = animaux
            .OrderByDescending(a => a.CreeLe)
            .ThenByDescending(a => a.Id);

        // Limite avant la projection : l'URL de la photo principale est construite côté client, en fin de requête.
        if (limite is { } n)
            tries = tries.Take(n);

        var items = await tries
            .Select(AnimalCarteDto.Projection(reservations, now, stockage))
            .ToListAsync(cancellationToken);
        return Result.Ok(new CollectionDto<AnimalCarteDto>(items, now));
    }

    /// <summary>
    /// Valeur d'énumération par son nom exact (même casse, AD-19) ; un entier, une liste à virgules (paramètre répété)
    /// ou un nom inconnu ajoute <c>errors[parametre]</c>.
    /// </summary>
    private static T? LireEnumeration<T>(string? texte, string parametre, Dictionary<string, string> errors)
        where T : struct, Enum
    {
        if (texte is null)
            return null;
        foreach (var valeur in Enum.GetValues<T>())
        {
            if (string.Equals(valeur.ToString(), texte, StringComparison.Ordinal))
                return valeur;
        }
        errors[parametre] = MessageEnumeration<T>();
        return null;
    }

    /// <summary>Filtre d'entente : absent → pas de filtre, <c>Oui</c> → filtre, toute autre valeur → <c>errors[parametre]</c>.</summary>
    private static bool LireEntente(string? texte, string parametre, Dictionary<string, string> errors)
    {
        if (texte is null)
            return false;
        if (string.Equals(texte, nameof(Entente.Oui), StringComparison.Ordinal))
            return true;
        errors[parametre] = MessageEntente;
        return false;
    }
}
