import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SiteService } from '../../core/site.service';
import { TelephonePipe } from '../../shared/telephone.pipe';

/** Pied de page public : numéro et réseaux de `SiteService`, liens Nous aider et Contact. */
@Component({
  selector: 'app-pied-de-page',
  imports: [RouterLink, TelephonePipe],
  templateUrl: './pied-de-page.html',
  styleUrl: './pied-de-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PiedDePage {
  protected readonly site = inject(SiteService).site;
}
