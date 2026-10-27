import { Routes } from '@angular/router';
import { AccesRefuse } from './acces-refuse/acces-refuse';
import { Connexion } from './connexion/connexion';
import { Inscription } from './inscription/inscription';

/** Monté sur `/connexion`. */
export const CONNEXION_ROUTES: Routes = [
  { path: '', component: Connexion, title: 'Connexion · AAAS' },
];

/** Monté sur `/inscription`. */
export const INSCRIPTION_ROUTES: Routes = [
  { path: '', component: Inscription, title: 'Créer mon compte · AAAS' },
];

/** Monté sur `/acces-refuse`. */
export const ACCES_REFUSE_ROUTES: Routes = [
  { path: '', component: AccesRefuse, title: "Cette page est réservée à l'association. · AAAS" },
];
