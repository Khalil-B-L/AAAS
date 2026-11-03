import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AnimalCarteDto,
  AnimalDetailDto,
  CollectionDto,
  FiltresCatalogue,
  PARAMETRES_CATALOGUE,
} from './api/animal.dto';

/** Client de `GET /api/animaux`, `GET /api/animaux/adoptes` et `GET /api/animaux/:id` (contrats-api.md §2, §4). L'appelant décide quoi faire d'un échec. */
@Injectable({ providedIn: 'root' })
export class AnimauxService {
  private readonly http = inject(HttpClient);

  /**
   * Animaux non adoptés, du plus récent au plus ancien, filtrés par `filtres` (paramètres §2, ordre §2) ;
   * `limite` (1..50) garde les n plus récents. Un filtre absent n'est pas transmis.
   */
  lister(filtres: FiltresCatalogue = {}): Promise<CollectionDto<AnimalCarteDto>> {
    let params = new HttpParams();
    for (const parametre of PARAMETRES_CATALOGUE) {
      const valeur = filtres[parametre];
      if (valeur !== undefined) {
        params = params.set(parametre, valeur);
      }
    }
    return firstValueFrom(this.http.get<CollectionDto<AnimalCarteDto>>('/api/animaux', { params }));
  }

  /** Animaux adoptés, les plus récemment adoptés en premier (`GET /api/animaux/adoptes`, §3), sans filtre. */
  listerAdoptes(): Promise<CollectionDto<AnimalCarteDto>> {
    return firstValueFrom(this.http.get<CollectionDto<AnimalCarteDto>>('/api/animaux/adoptes'));
  }

  /** Fiche d'un animal, quel que soit son statut ; un id inconnu rejette avec une `HttpErrorResponse` 404. */
  obtenir(id: number): Promise<AnimalDetailDto> {
    return firstValueFrom(this.http.get<AnimalDetailDto>(`/api/animaux/${id}`));
  }
}
