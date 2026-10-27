import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnimalCarteDto } from '../../core/api/animal.dto';
import { AnimauxService } from '../../core/animaux.service';
import { CarteAnimal } from '../../shared/carte-animal/carte-animal';

/** Nombre de cartes de « Nos derniers pensionnaires ». */
export const NOMBRE_PENSIONNAIRES = 4;

/**
 * Accueil : bandeau compact puis les derniers pensionnaires, sans hero plein écran.
 * Les actualités (epic 4) viendront remplir l'emplacement prévu ici.
 */
@Component({
  selector: 'app-accueil',
  imports: [RouterLink, CarteAnimal],
  templateUrl: './accueil.html',
  styleUrl: './accueil.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Accueil {
  private readonly animaux = inject(AnimauxService);

  /** `null` pendant le chargement ; liste vide si aucun animal ou si l'API a échoué. */
  protected readonly pensionnaires = signal<readonly AnimalCarteDto[] | null>(null);

  /** Emplacement Actualités : la section n'est rendue que s'il y a des données (epic 4). */
  protected readonly actualites = signal<readonly unknown[]>([]);

  constructor() {
    void this.chargerPensionnaires();
  }

  private async chargerPensionnaires(): Promise<void> {
    try {
      const reponse = await this.animaux.lister(NOMBRE_PENSIONNAIRES);
      this.pensionnaires.set(reponse.items);
    } catch (erreur) {
      console.error('Impossible de charger les derniers pensionnaires (/api/animaux).', erreur);
      this.pensionnaires.set([]);
    }
  }
}
