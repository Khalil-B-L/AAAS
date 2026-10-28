import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AnimalCarteDto,
  CollectionDto,
  FiltresCatalogue,
  PARAMETRES_CATALOGUE,
} from './api/animal.dto';

/** Client de `GET /api/animaux` (contrats-api.md §2, §4). L'appelant décide quoi faire d'un échec. */
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
}
