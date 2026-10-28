using Aaas.Api.Common.Disponibilite;
using Aaas.Api.Data;
using Aaas.Api.Features.Animaux;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Aaas.Api.Tests.Outils;

/// <summary>
/// Jeu de données de test « animal » partagé : fabrique d'animaux (date de création choisie sans toucher à l'horloge
/// de la factory), passage par les vraies transitions (AD-4), insertion et nettoyage de la table.
/// </summary>
public static class Animaux
{
    /// <summary>Date de création par défaut : l'heure initiale de la factory.</summary>
    public static readonly DateTimeOffset CreeLeParDefaut = new(2026, 1, 15, 10, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Animal Disponible, valide pour la base ; <paramref name="creeLe"/> fixe sa date de création.</summary>
    public static Animal Nouveau(
        string nom = "Caramel",
        DateTimeOffset? creeLe = null,
        Espece espece = Espece.Chat,
        Sexe sexe = Sexe.Femelle,
        TrancheAge age = TrancheAge.Jeune,
        Taille taille = Taille.Moyenne) =>
        new(new FakeTimeProvider(creeLe ?? CreeLeParDefaut))
        {
            Nom = nom,
            Espece = espece,
            Sexe = sexe,
            Age = age,
            Taille = taille,
            Description = $"{nom} attend sa famille.",
            Vaccine = true,
            Sterilise = false,
            Puce = true,
            EntenteEnfants = Entente.Oui,
            EntenteChiens = Entente.AVoir,
            EntenteChats = Entente.Non,
        };

    /// <summary>Passe l'animal Réservé par <see cref="Animal.Reserver"/>.</summary>
    public static Animal Reserve(this Animal animal)
    {
        animal.Reserver();
        return animal;
    }

    /// <summary>Passe l'animal Adopté par <see cref="Animal.ForcerStatut"/> ; <c>AdopteLe</c> = <paramref name="adopteLe"/>.</summary>
    public static Animal Adopte(this Animal animal, DateTimeOffset? adopteLe = null)
    {
        animal.ForcerStatut(StatutAnimal.Adopte, new FakeTimeProvider(adopteLe ?? CreeLeParDefaut));
        return animal;
    }

    /// <summary>
    /// <paramref name="nombre"/> animaux Disponibles créés à une minute d'intervalle à partir de <paramref name="depuis"/>,
    /// nommés <c>{prefixe}1</c> (le plus ancien) à <c>{prefixe}n</c> (le plus récent).
    /// </summary>
    public static Animal[] Serie(int nombre, string prefixe = "Animal", DateTimeOffset? depuis = null) =>
        Enumerable.Range(1, nombre)
            .Select(i => Nouveau($"{prefixe}{i}", (depuis ?? CreeLeParDefaut).AddMinutes(i)))
            .ToArray();

    /// <summary>Insère les animaux en base ; leurs <c>Id</c> sont renseignés au retour.</summary>
    public static async Task InsererAsync(this AaasFactory factory, params Animal[] animaux)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<Animal>().AddRange(animaux);
        await db.SaveChangesAsync(Ct);
    }

    /// <summary>
    /// Vide la table des animaux (la base est partagée par toute la collection de tests), après leurs réservations
    /// qui en bloquent la suppression (<c>Restrict</c>, AD-7).
    /// </summary>
    public static async Task ViderAnimauxAsync(this AaasFactory factory)
    {
        await factory.ViderReservationsAsync();
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<Animal>().ExecuteDeleteAsync(Ct);
    }
}
