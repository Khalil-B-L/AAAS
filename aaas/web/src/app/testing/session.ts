/** Ouverture de session dans les specs (TestBed déjà configuré avec `provideHttpClientTesting`). */
import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Role } from '../core/api/auth.dto';
import { AuthService, CLE_JETON } from '../core/auth/auth.service';
import { membre, motDePasse, reponseAuth, utilisateurEnTantQue } from './utilisateurs';

/** Connecte Sonia avec le rôle donné par `POST /api/auth/connexion` ; `jeton` est celui que renvoie l'API. */
export async function connecterEnTantQue(role: Role, jeton = 'jeton-1'): Promise<void> {
  const http = TestBed.inject(HttpTestingController);
  const connexion = TestBed.inject(AuthService).connecter({
    email: membre.email,
    motDePasse,
  });
  http.expectOne('/api/auth/connexion').flush(reponseAuth(utilisateurEnTantQue(role), jeton));
  await connexion;
}

/** Jeton laissé par une session précédente (rechargement) ; sans jeton, rien n'est stocké. À appeler avant de créer `AuthService`. */
export function stockerJeton(jeton?: string): void {
  if (jeton) {
    localStorage.setItem(CLE_JETON, jeton);
  }
}
