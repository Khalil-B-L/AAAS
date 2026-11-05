import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import {
  AnimalCarteDto,
  AnimalDetailDto,
  AnimalEcritureDto,
  CollectionDto,
  PhotoDto,
  StatutAnimal,
  StatutAnimalDto,
} from './api/animal.dto';

/**
 * Client de la gestion des animaux côté admin (`/api/admin/animaux`, contrats-api.md §4) : liste, fiche, création,
 * modification, statut forcé, suppression et photos. L'appelant décide quoi faire d'un échec (`HttpErrorResponse`).
 */
@Injectable({ providedIn: 'root' })
export class AdminAnimauxService {
  private readonly http = inject(HttpClient);

  /** Tous les animaux, tous statuts, du plus récent au plus ancien (`GET /api/admin/animaux`). */
  lister(): Promise<CollectionDto<AnimalCarteDto>> {
    return firstValueFrom(this.http.get<CollectionDto<AnimalCarteDto>>('/api/admin/animaux'));
  }

  /** Fiche complète d'un animal (`GET /api/admin/animaux/:id`) ; id inconnu → 404. */
  obtenir(id: number): Promise<AnimalDetailDto> {
    return firstValueFrom(this.http.get<AnimalDetailDto>(`/api/admin/animaux/${id}`));
  }

  /** Crée l'animal, statut Disponible (`POST /api/admin/animaux` → 201) ; champ invalide → 400 `errors`. */
  creer(animal: AnimalEcritureDto): Promise<AnimalDetailDto> {
    return firstValueFrom(this.http.post<AnimalDetailDto>('/api/admin/animaux', animal));
  }

  /**
   * Remplace les 12 champs d'édition (`PUT /api/admin/animaux/:id` → 200) ; 400 `errors`, 404, 409
   * `modifie-ailleurs` si l'animal a changé entre-temps.
   */
  modifier(id: number, animal: AnimalEcritureDto): Promise<AnimalDetailDto> {
    return firstValueFrom(this.http.put<AnimalDetailDto>(`/api/admin/animaux/${id}`, animal));
  }

  /**
   * Force le statut (`POST /api/admin/animaux/:id/statut` → 200 `AnimalDetailDto`) ; `statutAttendu` = statut effectif
   * affiché. 404 ; 409 `reservation-active` (réservation en cours) ou `modifie-ailleurs` (statut changé entre-temps).
   */
  forcerStatut(
    id: number,
    statut: StatutAnimalDto['statut'],
    statutAttendu: StatutAnimal,
  ): Promise<AnimalDetailDto> {
    const corps: StatutAnimalDto = { statut, statutAttendu };
    return firstValueFrom(
      this.http.post<AnimalDetailDto>(`/api/admin/animaux/${id}/statut`, corps),
    );
  }

  /**
   * Supprime l'animal, sa fiche et ses photos (`DELETE /api/admin/animaux/:id` → 204). Rejette avec une
   * `HttpErrorResponse` 409 `animal-a-des-reservations` s'il a des réservations, 404 s'il n'existe plus.
   */
  async supprimer(id: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`/api/admin/animaux/${id}`));
  }

  /**
   * Ajoute une photo en dernière position (`POST /api/admin/animaux/:id/photos`, multipart, champ `fichier` → 201).
   * Type, taille et nombre sont vérifiés par l'API : refus → 400 `errors.fichier`.
   */
  ajouterPhoto(id: number, fichier: File): Promise<PhotoDto> {
    const corps = new FormData();
    corps.append('fichier', fichier);
    return firstValueFrom(this.http.post<PhotoDto>(`/api/admin/animaux/${id}/photos`, corps));
  }

  /** Retire une photo (`DELETE /api/admin/animaux/:id/photos/:photoId` → 204). */
  async supprimerPhoto(id: number, photoId: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`/api/admin/animaux/${id}/photos/${photoId}`));
  }
}
