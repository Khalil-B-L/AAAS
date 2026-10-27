import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/** Page admin ouverte par un Membre (ou API en 403) : message et retour à l'accueil (CAP-1). */
@Component({
  selector: 'app-acces-refuse',
  imports: [RouterLink],
  template: `
    <section class="page-formulaire">
      <h1 class="titre-message">Cette page est réservée à l'association.</h1>
      <a class="bouton-pilule bouton-contour" routerLink="/">Retour à l'accueil</a>
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccesRefuse {}
