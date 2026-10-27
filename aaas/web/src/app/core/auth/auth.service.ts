import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom, timeout } from 'rxjs';
import { AuthReponseDto, ConnexionDto, InscriptionDto, UtilisateurDto } from '../api/auth.dto';

/** Clé du jeton dans `localStorage` (AD-9). */
export const CLE_JETON = 'aaas.jeton';

/** Attente maximale de `GET /api/auth/moi` au démarrage. */
export const DELAI_RESTAURATION_MS = 10_000;

/**
 * Session côté client (AD-9) : seul propriétaire du jeton (`localStorage`) et de l'utilisateur (signal).
 * Le rôle vient toujours de `UtilisateurDto`, jamais du contenu du jeton.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly _utilisateur = signal<UtilisateurDto | null>(null);
  private jetonCourant: string | null = lireJeton();
  private restauration: Promise<void> | null = null;
  private finRestauration: (() => void) | null = null;

  readonly utilisateur = this._utilisateur.asReadonly();
  readonly estConnecte = computed(() => this._utilisateur() !== null);
  readonly estAdmin = computed(() => this._utilisateur()?.role === 'Admin');

  /** Jeton à envoyer en `Authorization: Bearer` (lu par l'intercepteur seulement). */
  get jeton(): string | null {
    return this.jetonCourant;
  }

  /** Crée un compte Membre et ouvre sa session. Rejette avec l'`HttpErrorResponse` en cas d'échec. */
  async inscrire(dto: InscriptionDto): Promise<void> {
    this.ouvrirSession(
      await firstValueFrom(this.http.post<AuthReponseDto>('/api/auth/inscription', dto)),
    );
  }

  /** Ouvre la session ; un refus (400) ne touche pas à la session en cours. */
  async connecter(dto: ConnexionDto): Promise<void> {
    this.ouvrirSession(
      await firstValueFrom(this.http.post<AuthReponseDto>('/api/auth/connexion', dto)),
    );
  }

  /** Déconnexion locale : jeton effacé, utilisateur oublié. */
  deconnecter(): void {
    this.jetonCourant = null;
    ecrireJeton(null);
    this._utilisateur.set(null);
    this.conclureRestauration();
  }

  /**
   * Restaure la session depuis le jeton stocké (`GET /api/auth/moi`), une seule fois.
   * Se résout toujours, au plus tard après {@link DELAI_RESTAURATION_MS} ; 401 ou 404 → jeton effacé.
   */
  restaurer(): Promise<void> {
    this.restauration ??= new Promise<void>((resolve) => {
      this.finRestauration = resolve;
      void this.chargerSession().finally(resolve);
    });
    return this.restauration;
  }

  /** Une connexion ou une déconnexion explicite tranche : les gardes n'attendent plus `/moi`. */
  private conclureRestauration(): void {
    this.restauration ??= Promise.resolve();
    this.finRestauration?.();
  }

  private async chargerSession(): Promise<void> {
    const jeton = this.jetonCourant;
    if (!jeton) {
      return;
    }
    try {
      const utilisateur = await firstValueFrom(
        this.http.get<UtilisateurDto>('/api/auth/moi').pipe(timeout(DELAI_RESTAURATION_MS)),
      );
      if (this.jetonCourant === jeton) {
        this._utilisateur.set(utilisateur);
      }
    } catch (erreur) {
      // 401 (jeton expiré ou refusé) ou 404 (compte disparu) → visiteur, jeton effacé.
      // Réseau ou délai dépassé : visiteur, jeton gardé.
      if (
        erreur instanceof HttpErrorResponse &&
        (erreur.status === 401 || erreur.status === 404) &&
        this.jetonCourant === jeton
      ) {
        this.deconnecter();
      }
    }
  }

  private ouvrirSession(reponse: AuthReponseDto): void {
    this.jetonCourant = reponse.jeton;
    ecrireJeton(reponse.jeton);
    this._utilisateur.set(reponse.utilisateur);
    this.conclureRestauration();
  }
}

function lireJeton(): string | null {
  try {
    return localStorage.getItem(CLE_JETON);
  } catch {
    return null;
  }
}

function ecrireJeton(jeton: string | null): void {
  try {
    if (jeton) {
      localStorage.setItem(CLE_JETON, jeton);
    } else {
      localStorage.removeItem(CLE_JETON);
    }
  } catch {
    // Stockage indisponible (navigation privée stricte) : la session reste en mémoire.
  }
}
