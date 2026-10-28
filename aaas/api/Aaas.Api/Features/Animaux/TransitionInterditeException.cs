using Aaas.Api.Common.Disponibilite;

namespace Aaas.Api.Features.Animaux;

/// <summary>
/// Transition de statut appelée hors de son état source (AD-4). Erreur de programmation, jamais un refus
/// métier : les commandes vérifient l'état (statut effectif, réservation active) avant d'appeler la transition.
/// </summary>
public sealed class TransitionInterditeException(StatutAnimal depuis, string transition)
    : InvalidOperationException($"Transition « {transition} » interdite depuis le statut {depuis}.")
{
    public StatutAnimal Depuis { get; } = depuis;

    public string Transition { get; } = transition;
}
