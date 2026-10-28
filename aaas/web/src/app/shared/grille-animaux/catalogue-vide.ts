import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * État « Catalogue vide » (EXPERIENCE.md § State Patterns) de l'accueil et du catalogue : aucun animal non adopté
 * (ou API en échec), avec le lien vers « Ils ont trouvé une famille ».
 */
@Component({
  selector: 'app-catalogue-vide',
  imports: [RouterLink],
  template: `
    <p class="etat-vide">
      Tous nos protégés ont trouvé une famille pour l'instant !
      <a routerLink="/ils-ont-trouve-une-famille">Ils ont trouvé une famille</a>
    </p>
  `,
  styles: `
    :host {
      display: block;
    }

    .etat-vide {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: var(--spacing-2);
      margin: 0;
      padding: 0 var(--gutter);
      text-align: center;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CatalogueVide {}
