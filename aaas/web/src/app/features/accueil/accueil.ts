import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Accueil : bandeau compact puis les animaux, sans hero plein écran.
 * La grille des derniers pensionnaires (epic 2) et les actualités (epic 4) viendront
 * remplir les emplacements prévus ici.
 */
@Component({
  selector: 'app-accueil',
  imports: [RouterLink],
  templateUrl: './accueil.html',
  styleUrl: './accueil.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Accueil {
  /** Emplacement Actualités : la section n'est rendue que s'il y a des données (epic 4). */
  protected readonly actualites = signal<readonly unknown[]>([]);
}
