import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { returnUrlSur } from './return-url';

/**
 * Intercepteur unique (AD-9) : `Authorization: Bearer` sur les appels `/api` ;
 * 401 hors `/api/auth/*` → déconnexion locale puis Connexion avec `returnUrl` ; 403 → Accès refusé.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const versApi = estApi(req.url);
  const jeton = versApi ? auth.jeton : null;
  const requete = jeton ? req.clone({ setHeaders: { Authorization: `Bearer ${jeton}` } }) : req;

  return next(requete).pipe(
    catchError((erreur: unknown) => {
      if (versApi && erreur instanceof HttpErrorResponse) {
        // Un 401 sur un ancien jeton (session rouverte entre-temps) ne ferme pas la nouvelle session.
        const jetonToujoursCourant = !jeton || jeton === auth.jeton;
        if (erreur.status === 401 && !req.url.startsWith('/api/auth/') && jetonToujoursCourant) {
          auth.deconnecter();
          void router.navigate(['/connexion'], {
            queryParams: { returnUrl: returnUrlSur(router.url) },
          });
        } else if (erreur.status === 403) {
          void router.navigateByUrl('/acces-refuse');
        }
      }
      return throwError(() => erreur);
    }),
  );
};

function estApi(url: string): boolean {
  return url === '/api' || url.startsWith('/api/') || url.startsWith('/api?');
}
