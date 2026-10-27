using System.Linq.Expressions;

namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Carte animal (<c>contrats-api.md</c> §6) : seul format de carte de l'API (AD-1), réutilisé tel quel par les autres slices.
/// <c>PhotoPrincipaleUrl</c> reste <c>null</c> tant que le stockage des photos n'existe pas.
/// </summary>
public sealed record AnimalCarteDto(
    int Id,
    string Nom,
    Espece Espece,
    Sexe Sexe,
    TrancheAge Age,
    Taille Taille,
    StatutAnimal Statut,
    string? PhotoPrincipaleUrl)
{
    /// <summary>Projection unique <see cref="Animal"/> → carte, traduisible en SQL.</summary>
    public static readonly Expression<Func<Animal, AnimalCarteDto>> Projection =
        a => new AnimalCarteDto(a.Id, a.Nom, a.Espece, a.Sexe, a.Age, a.Taille, a.Statut, null);
}
