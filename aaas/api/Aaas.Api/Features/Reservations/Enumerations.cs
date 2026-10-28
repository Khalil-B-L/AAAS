namespace Aaas.Api.Features.Reservations;

// Énumérations de contrats-api.md §1 (AD-19) propres à la slice Reservations ; StatutReservation vit dans
// Common/Disponibilite (prédicats du statut effectif, AD-5).

/// <summary>Logement déclaré dans le questionnaire de réservation.</summary>
public enum Logement
{
    Appartement,
    Maison,
}
