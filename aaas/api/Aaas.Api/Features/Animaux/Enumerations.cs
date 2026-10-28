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

// StatutAnimal vit dans Common/Disponibilite : les prédicats du statut effectif (AD-5) le partagent sans dépendre d'une slice.
