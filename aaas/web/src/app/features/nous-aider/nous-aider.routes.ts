import { Routes } from '@angular/router';
import { RACINE_VERS_ACCUEIL, routePageVide } from '../../shared/page-vide/page-vide';

/** Monté sur `/nous-aider`. */
export const NOUS_AIDER_ROUTES: Routes = [
  RACINE_VERS_ACCUEIL,
  routePageVide('dons', 'Dons'),
  routePageVide('benevolat', 'Bénévolat'),
  routePageVide('famille-accueil', "Famille d'accueil"),
];
