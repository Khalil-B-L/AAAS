import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SiteService } from '../../core/site.service';
import { TelephonePipe } from '../telephone.pipe';

let compteur = 0;

/** Gros bouton orange « Appeler l'association » ; rien tant que le site n'est pas chargé. */
@Component({
  selector: 'app-bouton-appel',
  imports: [TelephonePipe],
  templateUrl: './bouton-appel.html',
  styleUrl: './bouton-appel.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoutonAppel {
  protected readonly site = inject(SiteService).site;
  protected readonly idNumero = `bouton-appel-numero-${++compteur}`;
}
