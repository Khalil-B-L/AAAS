using Aaas.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Aaas.Api.Features.Animaux;

/// <summary>
/// Jeu de démonstration (décision 2026-10-15) : 4 chats et 4 chiens créés au démarrage en environnement
/// <c>Development</c> seulement, et uniquement si aucun animal n'existe (un animal supprimé par l'admin n'est pas
/// recréé). Jamais en <c>Testing</c>. Sans photo (ajoutées par l'entrée 7) ; aucun Réservé (epic réservations).
/// </summary>
public static class DemoAnimaux
{
    private sealed record Modele(
        string Nom, Espece Espece, Sexe Sexe, TrancheAge Age, Taille Taille,
        Entente Enfants, Entente Chiens, Entente Chats,
        bool Vaccine, bool Sterilise, bool Puce, bool Adopte, string Description);

    // Ordre de la liste = du plus récent au plus ancien : l'accueil montre les 4 premiers non adoptés
    // (Caramel, Rex, Mimi, Filou : deux chats, deux chiens).
    private static readonly Modele[] Modeles =
    [
        new("Caramel", Espece.Chat, Sexe.Femelle, TrancheAge.Jeune, Taille.Petite, Entente.Oui, Entente.AVoir, Entente.Oui,
            true, true, true, false, "Caramel est une chatte douce et joueuse, trouvée près du port de Nabeul."),
        new("Rex", Espece.Chien, Sexe.Male, TrancheAge.Adulte, Taille.Grande, Entente.AVoir, Entente.Oui, Entente.Non,
            true, true, true, false, "Rex est un grand chien affectueux qui a besoin de longues promenades."),
        new("Mimi", Espece.Chat, Sexe.Femelle, TrancheAge.Bebe, Taille.Petite, Entente.Oui, Entente.Non, Entente.Oui,
            true, false, false, false, "Mimi a six mois ; elle adore les jouets à plumes et les siestes au soleil."),
        new("Filou", Espece.Chien, Sexe.Male, TrancheAge.Jeune, Taille.Moyenne, Entente.Oui, Entente.Oui, Entente.AVoir,
            true, false, true, false, "Filou, un an, est plein d'énergie et apprend vite."),
        new("Pacha", Espece.Chat, Sexe.Male, TrancheAge.Senior, Taille.Moyenne, Entente.AVoir, Entente.Non, Entente.AVoir,
            true, true, true, false, "Pacha, chat âgé et très calme, cherche un foyer paisible pour ses vieux jours."),
        new("Bella", Espece.Chien, Sexe.Femelle, TrancheAge.Bebe, Taille.Petite, Entente.Oui, Entente.Oui, Entente.Oui,
            false, false, false, false, "Bella, chiot de trois mois, sociable avec tout le monde."),
        new("Simba", Espece.Chat, Sexe.Male, TrancheAge.Adulte, Taille.Moyenne, Entente.Oui, Entente.Oui, Entente.Oui,
            true, true, true, true, "Simba a trouvé sa famille à Hammamet."),
        new("Kira", Espece.Chien, Sexe.Femelle, TrancheAge.Senior, Taille.Grande, Entente.Oui, Entente.AVoir, Entente.Non,
            true, true, true, true, "Kira coule des jours heureux dans sa nouvelle famille."),
    ];

    /// <summary>Nombre d'animaux du jeu de démonstration.</summary>
    public static int Nombre => Modeles.Length;

    public static async Task ExecuterAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        if (!sp.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return;

        var db = sp.GetRequiredService<AppDbContext>();
        if (await db.Set<Animal>().AnyAsync(cancellationToken))
            return;

        var maintenant = sp.GetRequiredService<TimeProvider>().GetUtcNow();
        for (var i = 0; i < Modeles.Length; i++)
        {
            var m = Modeles[i];
            // Dates de création échelonnées d'une heure : le 1er de la liste est le plus récent (tri creeLe décroissant).
            var temps = new TempsFixe(maintenant.AddHours(-(i + 1)));
            var animal = new Animal(temps)
            {
                Nom = m.Nom,
                Espece = m.Espece,
                Sexe = m.Sexe,
                Age = m.Age,
                Taille = m.Taille,
                Description = m.Description,
                Vaccine = m.Vaccine,
                Sterilise = m.Sterilise,
                Puce = m.Puce,
                EntenteEnfants = m.Enfants,
                EntenteChiens = m.Chiens,
                EntenteChats = m.Chats,
            };
            if (m.Adopte)
                animal.ForcerStatut(StatutAnimal.Adopte, temps);
            db.Set<Animal>().Add(animal);
        }

        await db.SaveChangesAsync(cancellationToken);
        sp.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DemoAnimaux))
            .LogInformation("Jeu de démonstration créé : {Nombre} animaux (Development).", Modeles.Length);
    }

    /// <summary>Horloge figée sur une date donnée, pour dater chaque animal de démonstration.</summary>
    private sealed class TempsFixe(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
