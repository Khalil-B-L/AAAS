import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';

/** Pages membre : sans session → Connexion, puis retour à la page demandée. */
export const membreGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.restaurer();
  return auth.estConnecte() || versConnexion(router, state.url);
};

/** Espace admin : sans session → Connexion ; Membre → Accès refusé. */
export const adminGuard: CanActivateFn = async (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  await auth.restaurer();
  if (!auth.estConnecte()) {
    return versConnexion(router, state.url);
  }
  return auth.estAdmin() || router.parseUrl('/acces-refuse');
};

function versConnexion(router: Router, returnUrl: string): UrlTree {
  return router.createUrlTree(['/connexion'], { queryParams: { returnUrl } });
}
