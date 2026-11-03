using Aaas.Api.Common.Disponibilite;

namespace Aaas.Api.Features.Animaux.Contrats;

/// <summary>
/// Corps de <c>POST /api/admin/animaux/:id/statut</c> (<c>contrats-api.md</c> §6) : statut voulu (<c>Disponible</c> ou
/// <c>Adopte</c>) et statut effectif affiché à l'admin (<c>statutAttendu</c>, les trois valeurs permises). Tout est
/// nullable et validé par le handler (<see cref="Valider"/>) ; une valeur d'énumération inconnue ou de casse différente
/// est refusée en 400 par le convertisseur global avant le handler.
/// </summary>
public sealed record StatutAnimalDto(StatutAnimal? Statut, StatutAnimal? StatutAttendu)
{
    public const string MessageStatut = "Choisissez Disponible ou Adopté.";

    /// <summary>Erreurs par champ (clés JSON camelCase), vide si valide.</summary>
    public IReadOnlyDictionary<string, string> Valider()
    {
        var errors = new Dictionary<string, string>();

        // Réservé ne se force jamais : il ne s'obtient que par une réservation (AD-4).
        if (Statut is null or StatutAnimal.Reserve)
            errors["statut"] = MessageStatut;

        if (StatutAttendu is null)
            errors["statutAttendu"] = AnimalEcritureDto.MessageChoix;

        return errors;
    }
}
