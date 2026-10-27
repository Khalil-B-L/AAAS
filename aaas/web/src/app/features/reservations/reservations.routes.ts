import { Routes } from '@angular/router';
import { RACINE_VERS_ACCUEIL, routePageVide } from '../../shared/page-vide/page-vide';

/** Monté sur `/animaux/:id/reserver`. */
export const QUESTIONNAIRE_ROUTES: Routes = [routePageVide('', 'Réserver')];

/** Monté sur `/reservations`. */
export const RESERVATION_ENVOYEE_ROUTES: Routes = [
  RACINE_VERS_ACCUEIL,
  routePageVide(':id/envoyee', 'Réservation envoyée'),
];

/** Monté sur `/mes-reservations`. */
export const MES_RESERVATIONS_ROUTES: Routes = [routePageVide('', 'Mes réservations')];

/** Monté sur `/admin/reservations`. */
export const ADMIN_RESERVATIONS_ROUTES: Routes = [routePageVide('', 'Admin · Réservations')];
