import { Routes } from '@angular/router';
import { RACINE_VERS_ACCUEIL, routePageVide } from '../../shared/page-vide/page-vide';
import { Adoptes } from './adoptes/adoptes';
import { Catalogue } from './catalogue/catalogue';
import { Fiche } from './fiche/fiche';

/** Monté sur `/adopter` : catalogue filtré, filtres en query params (contrats-api.md §7). */
export const CATALOGUE_ROUTES: Routes = [
  { path: '', component: Catalogue, title: 'Adopter · AAAS' },
];

/**
 * Monté sur `/animaux` : fiche `/animaux/:id`. Sans `title` de route : la fiche pose elle-même « [Nom] · AAAS »
 * une fois l'animal chargé.
 */
export const FICHE_ROUTES: Routes = [RACINE_VERS_ACCUEIL, { path: ':id', component: Fiche }];

/** Monté sur `/ils-ont-trouve-une-famille` : animaux adoptés. */
export const ADOPTES_ROUTES: Routes = [
  { path: '', component: Adoptes, title: 'Ils ont trouvé une famille · AAAS' },
];

/** Monté sur `/admin/animaux`. */
export const ADMIN_ANIMAUX_ROUTES: Routes = [
  routePageVide('', 'Admin · Animaux'),
  routePageVide('nouveau', 'Admin · Ajouter un animal'),
  routePageVide(':id', 'Admin · Modifier un animal'),
];
