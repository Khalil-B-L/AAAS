import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AnimalCarteDto, CollectionDto } from './api/animal.dto';

/** Client de `GET /api/animaux` (contrats-api.md §2, §4). L'appelant décide quoi faire d'un échec. */
@Injectable({ providedIn: 'root' })
export class AnimauxService {
  private readonly http = inject(HttpClient);

  /** Animaux non adoptés, du plus récent au plus ancien ; `limite` (1..50) garde les n plus récents. */
  lister(limite?: number): Promise<CollectionDto<AnimalCarteDto>> {
    const params = limite === undefined ? undefined : new HttpParams().set('limite', limite);
    return firstValueFrom(this.http.get<CollectionDto<AnimalCarteDto>>('/api/animaux', { params }));
  }
}
