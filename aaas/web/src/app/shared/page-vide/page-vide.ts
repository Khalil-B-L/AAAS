import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Route } from '@angular/router';

/** Page d'attente d'un écran pas encore construit : affiche seulement son titre. */
@Component({
  selector: 'app-page-vide',
  template: `<h1 class="titre-page">{{ titre() }}</h1>`,
  styles: `
    .titre-page {
      margin: var(--spacing-7) var(--gutter);
      color: var(--color-primary);
      font-size: var(--font-size-title-message);
      font-weight: var(--font-weight-title-message);
      line-height: var(--line-height-title-message);
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageVide {
  /** Lié depuis `data.titre` de la route (`withComponentInputBinding`). */
  readonly titre = input.required<string>();
}

/** Route d'une page vide : titre affiché et titre de l'onglet. */
export function routePageVide(path: string, titre: string): Route {
  return { path, component: PageVide, title: `${titre} · AAAS`, data: { titre } };
}

/**
 * À placer dans un groupe de routes chargé à la demande sans page à sa racine
 * (`/animaux`, `/nous-aider`…) : sinon le parent « réussit » à vide au lieu de tomber sur `**`.
 */
export const RACINE_VERS_ACCUEIL: Route = { path: '', pathMatch: 'full', redirectTo: '/' };
