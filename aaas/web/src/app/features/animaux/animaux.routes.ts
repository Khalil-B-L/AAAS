import { Routes } from '@angular/router';
import { RACINE_VERS_ACCUEIL, routePageVide } from '../../shared/page-vide/page-vide';

/** Monté sur `/adopter`. */
export const CATALOGUE_ROUTES: Routes = [routePageVide('', 'Adopter')];

/** Monté sur `/animaux`. */
export const FICHE_ROUTES: Routes = [RACINE_VERS_ACCUEIL, routePageVide(':id', 'Fiche animal')];

/** Monté sur `/ils-ont-trouve-une-famille`. */
export const ADOPTES_ROUTES: Routes = [routePageVide('', 'Ils ont trouvé une famille')];

/** Monté sur `/admin/animaux`. */
export const ADMIN_ANIMAUX_ROUTES: Routes = [
  routePageVide('', 'Admin · Animaux'),
  routePageVide('nouveau', 'Admin · Ajouter un animal'),
  routePageVide(':id', 'Admin · Modifier un animal'),
];
