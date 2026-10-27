import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { StatutAnimal } from '../../core/api/animal.dto';
import { LIBELLES_STATUT_ANIMAL } from '../libelles';

/** Pastille de statut d'un animal : couleur + mot, jamais la couleur seule (DESIGN.md § Pastilles de statut). */
@Component({
  selector: 'app-pastille-statut',
  template: `{{ libelle() }}`,
  styleUrl: './pastille-statut.css',
  host: { '[class]': "'pastille pastille-' + statut()" },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PastilleStatut {
  readonly statut = input.required<StatutAnimal>();

  protected readonly libelle = computed(() => LIBELLES_STATUT_ANIMAL[this.statut()]);
}
