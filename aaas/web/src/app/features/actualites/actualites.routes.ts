import { Routes } from '@angular/router';
import { routePageVide } from '../../shared/page-vide/page-vide';

/** Monté sur `/actualites`. */
export const ACTUALITES_ROUTES: Routes = [routePageVide('', 'Actualités')];

/** Monté sur `/admin/actualites`. */
export const ADMIN_ACTUALITES_ROUTES: Routes = [routePageVide('', 'Admin · Actualités')];
