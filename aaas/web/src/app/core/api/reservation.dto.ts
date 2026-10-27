/** Énumérations de réservation (contrats-api.md §1) ; les DTO de réservation viendront avec leur epic. */

/** Énumération `Logement`. */
export type Logement = 'Appartement' | 'Maison';

/** Énumération `StatutReservation`. */
export type StatutReservation = 'EnAttente' | 'Confirmee' | 'Annulee' | 'Expiree';
