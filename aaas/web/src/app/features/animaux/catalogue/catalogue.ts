import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, from, map, of, switchMap } from 'rxjs';
import { AnimalCarteDto } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { CatalogueVide } from '../../../shared/grille-animaux/catalogue-vide';
import { GrilleAnimaux } from '../../../shared/grille-animaux/grille-animaux';
import {
  GROUPES_PUCES,
  Puce,
  basculer,
  estActive,
  lireFiltres,
  memesFiltres,
  rappels,
  versQueryParams,
} from './filtres';

/** Résultat du dernier chargement ; `echec` : l'API n'a pas répondu (erreur journalisée). */
interface Resultat {
  readonly animaux: readonly AnimalCarteDto[];
  readonly echec: boolean;
}

/**
 * Page Adopter (`/adopter`, CAP-2) : puces de filtre dont l'état vit dans les query params (mêmes noms que
 * contrats-api.md §2), appliquées au clic ; grille des animaux non adoptés, compteur annoncé, « Effacer les filtres »,
 * états « Aucun résultat » et « Catalogue vide », lien vers « Ils ont trouvé une famille ».
 */
@Component({
  selector: 'app-catalogue',
  imports: [RouterLink, GrilleAnimaux, CatalogueVide],
  templateUrl: './catalogue.html',
  styleUrl: './catalogue.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Catalogue {
  private readonly animaux = inject(AnimauxService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly groupes = GROUPES_PUCES;

  /** Filtres actifs, lus dans l'URL. */
  protected readonly filtres = toSignal(this.route.queryParamMap.pipe(map(lireFiltres)), {
    requireSync: true,
  });

  /** Rappels des filtres actifs ; vide si aucun filtre. */
  protected readonly filtresActifs = computed(() => rappels(this.filtres()));

  /** `null` jusqu'au premier chargement ; le résultat précédent reste affiché pendant un rechargement. */
  protected readonly resultat = signal<Resultat | null>(null);

  protected readonly compteur = computed(() => {
    const nombre = this.resultat()?.animaux.length ?? 0;
    return `${nombre} ${nombre > 1 ? 'animaux' : 'animal'}`;
  });

  constructor() {
    // Un chargement par jeu de filtres distinct ; une réponse dépassée par un nouveau clic est ignorée.
    this.route.queryParamMap
      .pipe(
        map(lireFiltres),
        distinctUntilChanged(memesFiltres),
        switchMap((filtres) =>
          from(this.animaux.lister(filtres)).pipe(
            map((reponse): Resultat => ({ animaux: reponse.items, echec: false })),
            catchError((erreur: unknown) => {
              console.error('Impossible de charger le catalogue (/api/animaux).', erreur);
              return of<Resultat>({ animaux: [], echec: true });
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((resultat) => this.resultat.set(resultat));
  }

  protected estActive(puce: Puce): boolean {
    return estActive(this.filtres(), puce);
  }

  protected basculer(puce: Puce): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: versQueryParams(basculer(this.filtres(), puce)),
    });
  }

  protected effacer(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: {} });
  }
}
