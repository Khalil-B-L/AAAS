import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnimalCarteDto } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { GrilleAnimaux } from '../../../shared/grille-animaux/grille-animaux';

/**
 * Page « Ils ont trouvé une famille » (`/ils-ont-trouve-une-famille`, CAP-11) : grille des animaux adoptés, les plus
 * récemment adoptés en premier, sans filtre ni pagination ; chaque carte ouvre la fiche. Aucun adopté ou API en échec
 * (erreur journalisée) : phrase courte et lien vers Adopter.
 */
@Component({
  selector: 'app-adoptes',
  imports: [RouterLink, GrilleAnimaux],
  templateUrl: './adoptes.html',
  styleUrl: './adoptes.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Adoptes {
  private readonly animaux = inject(AnimauxService);

  /** `null` jusqu'à la réponse de l'API ; liste vide si aucun adopté ou en cas d'échec. */
  protected readonly adoptes = signal<readonly AnimalCarteDto[] | null>(null);

  constructor() {
    this.animaux.listerAdoptes().then(
      (reponse) => this.adoptes.set(reponse.items),
      (erreur: unknown) => {
        console.error('Impossible de charger les animaux adoptés (/api/animaux/adoptes).', erreur);
        this.adoptes.set([]);
      },
    );
  }
}
