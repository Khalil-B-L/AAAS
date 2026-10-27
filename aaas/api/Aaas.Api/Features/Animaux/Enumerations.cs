namespace Aaas.Api.Features.Animaux;

// Énumérations de contrats-api.md §1 (AD-19) : noms C# exacts, stockés et sérialisés en chaîne.
// Les libellés affichés vivent uniquement côté front (shared/libelles.ts).

public enum Espece
{
    Chat,
    Chien,
}

public enum Sexe
{
    Male,
    Femelle,
}

/// <summary>Saisie par l'admin et stockée, jamais calculée.</summary>
public enum TrancheAge
{
    Bebe,
    Jeune,
    Adulte,
    Senior,
}

public enum Taille
{
    Petite,
    Moyenne,
    Grande,
}

public enum Entente
{
    Oui,
    AVoir,
    Non,
}

/// <summary>Statut stocké d'un animal ; ne change que par les transitions de <see cref="Animal"/> (AD-4).</summary>
public enum StatutAnimal
{
    Disponible,
    Reserve,
    Adopte,
}
