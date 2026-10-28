import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { AnimalCarteDto } from '../../core/api/animal.dto';
import { CarteAnimal } from '../carte-animal/carte-animal';

/**
 * Grille de cartes animal (DESIGN.md § Layout) : 2 colonnes sur téléphone, 3 sur tablette, 4 sur ordinateur.
 * Partagée par l'accueil et le catalogue ; l'appelant choisit l'état vide à afficher à la place d'une liste vide.
 */
@Component({
  selector: 'app-grille-animaux',
  imports: [CarteAnimal],
  template: `
    <ul class="grille-animaux">
      @for (animal of animaux(); track animal.id) {
        <li><app-carte-animal [animal]="animal" /></li>
      }
    </ul>
  `,
  styleUrl: './grille-animaux.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GrilleAnimaux {
  readonly animaux = input.required<readonly AnimalCarteDto[]>();
}
