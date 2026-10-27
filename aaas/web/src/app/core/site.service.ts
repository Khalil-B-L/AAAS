import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { SiteDto } from './api/site.dto';

/**
 * Valeurs du site (AD-14), chargées une fois au démarrage depuis `GET /api/site`.
 * Un échec n'empêche pas l'application de démarrer : `site()` reste `null`.
 */
@Injectable({ providedIn: 'root' })
export class SiteService {
  private readonly http = inject(HttpClient);
  private readonly _site = signal<SiteDto | null>(null);

  readonly site = this._site.asReadonly();

  async charger(): Promise<void> {
    try {
      this._site.set(await firstValueFrom(this.http.get<SiteDto>('/api/site')));
    } catch (erreur) {
      console.error('Impossible de charger les informations du site (/api/site).', erreur);
    }
  }
}
