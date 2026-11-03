import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AnimalCarteDto, CollectionDto } from './api/animal.dto';

/**
 * Client de la gestion des animaux côté admin (`/api/admin/animaux`, contrats-api.md §4). L'appelant décide quoi
 * faire d'un échec. Le formulaire (création, modification, photos) complètera ce service.
 */
@Injectable({ providedIn: 'root' })
export class AdminAnimauxService {
  private readonly http = inject(HttpClient);

  /** Tous les animaux, tous statuts, du plus récent au plus ancien (`GET /api/admin/animaux`). */
  lister(): Promise<CollectionDto<AnimalCarteDto>> {
    return firstValueFrom(this.http.get<CollectionDto<AnimalCarteDto>>('/api/admin/animaux'));
  }

  /**
   * Supprime l'animal, sa fiche et ses photos (`DELETE /api/admin/animaux/:id` → 204). Rejette avec une
   * `HttpErrorResponse` 409 `animal-a-des-reservations` s'il a des réservations, 404 s'il n'existe plus.
   */
  async supprimer(id: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`/api/admin/animaux/${id}`));
  }
}
