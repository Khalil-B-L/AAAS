import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, distinctUntilChanged, from, map, of, switchMap } from 'rxjs';
import { AnimalDetailDto, Entente } from '../../../core/api/animal.dto';
import { AnimauxService } from '../../../core/animaux.service';
import { DateFrPipe } from '../../../shared/date-fr.pipe';
import {
  CRITERES_SANTE,
  LIBELLES_ENTENTE,
  LIBELLES_ESPECE,
  LIBELLES_PUCE_TAILLE,
  LIBELLES_RETOUR_ESPECE,
  LIBELLES_SEXE,
  LIBELLES_TITRE_ENTENTES,
  LIBELLES_TRANCHE_AGE,
  libelleSante,
} from '../../../shared/libelles';
import { Galerie } from './galerie/galerie';

/**
 * Titres d'onglet : la route n'en déclare pas, pour que la stratégie de titre du routeur n'écrase pas celui posé ici
 * (« [Nom] · AAAS » une fois la fiche chargée).
 */
const TITRE_CHARGEMENT = 'Fiche animal · AAAS';
const TITRE_INTROUVABLE = 'Animal introuvable · AAAS';

/** Plus grand identifiant transmis à l'API (`int` côté serveur). */
const ID_MAX = 2_147_483_647;

/** État de la page : en chargement, fiche affichée, animal introuvable (404 ou id invalide), ou API muette. */
type Etat =
  | { readonly type: 'chargement' }
  | { readonly type: 'fiche'; readonly animal: AnimalDetailDto }
  | { readonly type: 'introuvable' }
  | { readonly type: 'erreur' };

interface CaseSante {
  readonly libelle: string;
  readonly present: boolean;
}

interface LigneEntente {
  readonly icone: string;
  readonly libelle: string;
  readonly entente: Entente;
  readonly valeur: string;
}

/** Identifiant de route valide (entier positif représentable côté API), sinon `null`. */
function lireId(texte: string | null): number | null {
  if (texte === null || !/^\d{1,10}$/.test(texte)) {
    return null;
  }
  const id = Number(texte);
  return id >= 1 && id <= ID_MAX ? id : null;
}

/**
 * Fiche animal (`/animaux/:id`, CAP-3) : galerie, caractéristiques, description, santé accordée au sexe, ententes,
 * retour vers le catalogue de l'espèce. Disponible : emplacement d'action vide (epic réservations) ; Réservé :
 * « Déjà réservé » désactivé ; Adopté : mention « A trouvé une famille », sans bouton. Titre d'onglet
 * « [Nom] · AAAS ». Le cœur favori (epic contenus) n'a qu'un emplacement vide dans la galerie.
 */
@Component({
  selector: 'app-fiche',
  imports: [RouterLink, Galerie, DateFrPipe],
  templateUrl: './fiche.html',
  styleUrl: './fiche.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Fiche {
  private readonly animaux = inject(AnimauxService);
  private readonly route = inject(ActivatedRoute);
  private readonly titre = inject(Title);

  protected readonly etat = signal<Etat>({ type: 'chargement' });

  protected readonly animal = computed(() => {
    const etat = this.etat();
    return etat.type === 'fiche' ? etat.animal : null;
  });

  /** Puces de caractéristiques : espèce, âge, sexe, taille. */
  protected readonly faits = computed(() => {
    const animal = this.animal();
    return animal
      ? [
          LIBELLES_ESPECE[animal.espece],
          LIBELLES_TRANCHE_AGE[animal.age],
          LIBELLES_SEXE[animal.sexe],
          LIBELLES_PUCE_TAILLE[animal.taille],
        ]
      : [];
  });

  protected readonly sante = computed((): CaseSante[] => {
    const animal = this.animal();
    return animal
      ? CRITERES_SANTE.map((critere) => ({
          libelle: libelleSante(critere, animal.sexe, animal[critere]),
          present: animal[critere],
        }))
      : [];
  });

  protected readonly titreEntentes = computed(() => {
    const animal = this.animal();
    return animal ? LIBELLES_TITRE_ENTENTES[animal.sexe] : '';
  });

  protected readonly ententes = computed((): LigneEntente[] => {
    const animal = this.animal();
    if (!animal) {
      return [];
    }
    const ligne = (icone: string, libelle: string, entente: Entente): LigneEntente => ({
      icone,
      libelle,
      entente,
      valeur: LIBELLES_ENTENTE[entente],
    });
    return [
      ligne('👧', 'Les enfants', animal.ententeEnfants),
      ligne('🐶', 'Les chiens', animal.ententeChiens),
      ligne('🐱', 'Les chats', animal.ententeChats),
    ];
  });

  /** « ← Les chats » / « ← Les chiens » vers Adopter filtré sur l'espèce. */
  protected readonly libellesRetour = LIBELLES_RETOUR_ESPECE;

  constructor() {
    this.route.paramMap
      .pipe(
        map((params) => params.get('id')),
        distinctUntilChanged(),
        switchMap((texte) => {
          const id = lireId(texte);
          if (id === null) {
            return of<Etat>({ type: 'introuvable' });
          }
          this.etat.set({ type: 'chargement' });
          this.titre.setTitle(TITRE_CHARGEMENT);
          return from(this.animaux.obtenir(id)).pipe(
            map((animal): Etat => ({ type: 'fiche', animal })),
            catchError((erreur: unknown) => {
              if (erreur instanceof HttpErrorResponse && erreur.status === 404) {
                return of<Etat>({ type: 'introuvable' });
              }
              console.error(`Impossible de charger la fiche (/api/animaux/${id}).`, erreur);
              return of<Etat>({ type: 'erreur' });
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((etat) => {
        this.etat.set(etat);
        if (etat.type === 'fiche') {
          this.titre.setTitle(`${etat.animal.nom} · AAAS`);
        } else if (etat.type === 'introuvable') {
          this.titre.setTitle(TITRE_INTROUVABLE);
        }
      });
  }
}
