import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { EnTete } from './layout/en-tete/en-tete';
import { PiedDePage } from './layout/pied-de-page/pied-de-page';

@Component({
  imports: [RouterOutlet, EnTete, PiedDePage],
  selector: 'app-root',
  styleUrl: './app.css',
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {
  private readonly router = inject(Router);

  /** Pattes en filigrane : jamais sur les écrans admin de travail (DESIGN.md). */
  protected readonly pattesVisibles = toSignal(
    this.router.events.pipe(
      filter((evenement) => evenement instanceof NavigationEnd),
      map((fin) => !estAdmin(fin.urlAfterRedirects)),
    ),
    { initialValue: !estAdmin(this.router.url) },
  );
}

function estAdmin(url: string): boolean {
  const chemin = url.split(/[?#]/)[0];
  return chemin === '/admin' || chemin.startsWith('/admin/');
}
