import { Routes } from '@angular/router';
import { adminGuard, membreGuard } from './core/auth/gardes';

/**
 * Routes Angular de contrats-api.md §7 (AD-21), chargées à la demande par fonctionnalité.
 * Pages membre derrière `membreGuard`, espace admin derrière `adminGuard` (AD-9).
 */
export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadChildren: () => import('./features/accueil/accueil.routes').then((m) => m.ACCUEIL_ROUTES),
  },
  {
    path: 'adopter',
    loadChildren: () => import('./features/animaux/animaux.routes').then((m) => m.CATALOGUE_ROUTES),
  },
  {
    path: 'animaux/:id/reserver',
    canActivate: [membreGuard],
    loadChildren: () =>
      import('./features/reservations/reservations.routes').then((m) => m.QUESTIONNAIRE_ROUTES),
  },
  {
    path: 'animaux',
    loadChildren: () => import('./features/animaux/animaux.routes').then((m) => m.FICHE_ROUTES),
  },
  {
    path: 'reservations',
    canActivate: [membreGuard],
    loadChildren: () =>
      import('./features/reservations/reservations.routes').then(
        (m) => m.RESERVATION_ENVOYEE_ROUTES,
      ),
  },
  {
    path: 'ils-ont-trouve-une-famille',
    loadChildren: () => import('./features/animaux/animaux.routes').then((m) => m.ADOPTES_ROUTES),
  },
  {
    path: 'actualites',
    loadChildren: () =>
      import('./features/actualites/actualites.routes').then((m) => m.ACTUALITES_ROUTES),
  },
  {
    path: 'nous-aider',
    loadChildren: () =>
      import('./features/nous-aider/nous-aider.routes').then((m) => m.NOUS_AIDER_ROUTES),
  },
  {
    path: 'contact',
    loadChildren: () => import('./features/contact/contact.routes').then((m) => m.CONTACT_ROUTES),
  },
  {
    path: 'connexion',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.CONNEXION_ROUTES),
  },
  {
    path: 'inscription',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.INSCRIPTION_ROUTES),
  },
  {
    path: 'mes-reservations',
    canActivate: [membreGuard],
    loadChildren: () =>
      import('./features/reservations/reservations.routes').then((m) => m.MES_RESERVATIONS_ROUTES),
  },
  {
    path: 'mes-favoris',
    canActivate: [membreGuard],
    loadChildren: () => import('./features/favoris/favoris.routes').then((m) => m.FAVORIS_ROUTES),
  },
  {
    path: 'admin',
    canActivate: [adminGuard],
    children: [
      // `/admin` seul : la page Réservations (une adresse inconnue tombe sur le `**` racine).
      { path: '', pathMatch: 'full', redirectTo: 'reservations' },
      {
        path: 'reservations',
        loadChildren: () =>
          import('./features/reservations/reservations.routes').then(
            (m) => m.ADMIN_RESERVATIONS_ROUTES,
          ),
      },
      {
        path: 'animaux',
        loadChildren: () =>
          import('./features/animaux/animaux.routes').then((m) => m.ADMIN_ANIMAUX_ROUTES),
      },
      {
        path: 'actualites',
        loadChildren: () =>
          import('./features/actualites/actualites.routes').then((m) => m.ADMIN_ACTUALITES_ROUTES),
      },
    ],
  },
  {
    path: 'acces-refuse',
    loadChildren: () => import('./features/auth/auth.routes').then((m) => m.ACCES_REFUSE_ROUTES),
  },
  { path: '**', redirectTo: '' },
];
