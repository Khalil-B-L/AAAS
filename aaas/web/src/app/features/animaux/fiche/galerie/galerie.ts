import { ChangeDetectionStrategy, Component, computed, input, linkedSignal } from '@angular/core';
import { PhotoDto, StatutAnimal } from '../../../../core/api/animal.dto';
import { PastilleStatut } from '../../../../shared/pastille-statut/pastille-statut';

/** Balayage horizontal minimal (px) pour changer de photo. */
const SEUIL_BALAYAGE = 40;

/**
 * Galerie de la fiche (DESIGN.md § Galerie photo, EXPERIENCE.md § Galerie photo) : grande photo (ou dégradé de
 * remplacement) avec la pastille de statut, compteur « n / total », points et vignettes actionnables au clavier,
 * balayage sur téléphone. Chaque photo a le texte alternatif « Photo n de [prénom] ». Le contenu projeté (bouton
 * favori) est posé en haut à droite de la photo.
 */
@Component({
  selector: 'app-galerie',
  imports: [PastilleStatut],
  templateUrl: './galerie.html',
  styleUrl: './galerie.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Galerie {
  /** Triées par `ordre`. */
  readonly photos = input.required<readonly PhotoDto[]>();
  readonly nom = input.required<string>();
  readonly statut = input.required<StatutAnimal>();

  /** Index de la photo affichée ; revient à la première quand les photos changent. */
  protected readonly index = linkedSignal({ source: this.photos, computation: () => 0 });

  protected readonly photo = computed(() => this.photos()[this.index()] ?? null);

  private debutBalayage: { readonly x: number; readonly y: number } | null = null;

  protected texteAlternatif(index: number): string {
    return `Photo ${index + 1} de ${this.nom()}`;
  }

  protected afficher(index: number): void {
    this.index.set(index);
  }

  protected debuterBalayage(event: TouchEvent): void {
    const toucher = event.changedTouches[0];
    this.debutBalayage = toucher ? { x: toucher.clientX, y: toucher.clientY } : null;
  }

  /**
   * Geste surtout horizontal (|dx| ≥ seuil et |dx| > |dy|, pour ne pas confondre avec le défilement de la page) :
   * vers la gauche photo suivante, vers la droite précédente, sans boucler.
   */
  protected terminerBalayage(event: TouchEvent): void {
    const fin = event.changedTouches[0];
    const debut = this.debutBalayage;
    this.debutBalayage = null;
    if (debut === null || fin === undefined) {
      return;
    }
    const dx = fin.clientX - debut.x;
    const dy = fin.clientY - debut.y;
    if (Math.abs(dx) < SEUIL_BALAYAGE || Math.abs(dx) <= Math.abs(dy)) {
      return;
    }
    const cible = this.index() + (dx < 0 ? 1 : -1);
    if (cible >= 0 && cible < this.photos().length) {
      this.index.set(cible);
    }
  }
}
