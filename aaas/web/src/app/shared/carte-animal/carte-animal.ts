import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnimalCarteDto } from '../../core/api/animal.dto';
import { LIBELLES_ESPECE, LIBELLES_SEXE, LIBELLES_TRANCHE_AGE } from '../libelles';
import { PastilleStatut } from '../pastille-statut/pastille-statut';

/**
 * Carte animal sur `AnimalCarteDto` (DESIGN.md § Carte animal) : photo (ou dégradé de remplacement) avec la pastille
 * de statut, nom, méta « Espèce · âge · sexe » ; photo désaturée si Réservé ; mène à la fiche `/animaux/:id`.
 */
@Component({
  selector: 'app-carte-animal',
  imports: [RouterLink, PastilleStatut],
  templateUrl: './carte-animal.html',
  styleUrl: './carte-animal.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CarteAnimal {
  readonly animal = input.required<AnimalCarteDto>();

  protected readonly meta = computed(() => {
    const animal = this.animal();
    return [
      LIBELLES_ESPECE[animal.espece],
      LIBELLES_TRANCHE_AGE[animal.age],
      LIBELLES_SEXE[animal.sexe],
    ].join(' · ');
  });
}
