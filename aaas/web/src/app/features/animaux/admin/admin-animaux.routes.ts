import { Routes } from '@angular/router';
import { routePageVide } from '../../../shared/page-vide/page-vide';
import { AdminAnimauxListe } from './liste/liste';

/**
 * Monté sur `/admin/animaux` : liste ; formulaires d'ajout et de modification à venir (pages vides). Fichier à part
 * des routes publiques pour que le code admin ait son propre chunk, jamais téléchargé par un visiteur.
 */
export const ADMIN_ANIMAUX_ROUTES: Routes = [
  { path: '', component: AdminAnimauxListe, title: 'Admin · Animaux · AAAS' },
  routePageVide('nouveau', 'Admin · Ajouter un animal'),
  routePageVide(':id', 'Admin · Modifier un animal'),
];
