import { ParamMap, Params } from '@angular/router';
import {
  ESPECES,
  FiltresCatalogue,
  SEXES,
  TAILLES,
  TRANCHES_AGE,
} from '../../../core/api/animal.dto';
import {
  LIBELLES_ENTENTE,
  LIBELLES_ESPECE,
  LIBELLES_SEXE,
  LIBELLES_TAILLE,
  LIBELLES_TRANCHE_AGE,
} from '../../../shared/libelles';

/** Paramètre filtrable depuis la page Adopter : ceux de contrats-api.md §2, sans `limite`. */
export type ParametreFiltre = Exclude<keyof FiltresCatalogue, 'limite'>;

/** Filtres actifs de la page Adopter, lus dans les query params (mêmes noms qu'au §2). */
export type Filtres = Pick<FiltresCatalogue, ParametreFiltre>;

/** Puce de filtre : bascule `parametre=valeur`. */
export interface Puce {
  readonly parametre: ParametreFiltre;
  readonly valeur: string;
  /** Texte de la puce. */
  readonly libelle: string;
  /** Rappel du filtre dans la phrase « Aucun résultat ». */
  readonly rappel: string;
}

/** Groupe de puces, une valeur active au plus par paramètre. */
export interface GroupePuces {
  readonly id: string;
  readonly libelle: string;
  /** Précision lue par les technologies d'assistance après le libellé. */
  readonly precision?: string;
  readonly puces: readonly Puce[];
}

function puces<T extends string>(
  parametre: ParametreFiltre,
  valeurs: readonly T[],
  libelles: Readonly<Record<T, string>>,
  rappel: (libelle: string) => string = (libelle) => libelle,
): Puce[] {
  return valeurs.map((valeur) => ({
    parametre,
    valeur,
    libelle: libelles[valeur],
    rappel: rappel(libelles[valeur]),
  }));
}

function entente(parametre: 'enfants' | 'chiens' | 'chats', libelle: string): Puce {
  return { parametre, valeur: 'Oui', libelle, rappel: `entente avec les ${libelle.toLowerCase()}` };
}

/** Groupes de puces d'EXPERIENCE.md § Filtres du catalogue : espèce, âge, taille, sexe, ententes. */
export const GROUPES_PUCES: readonly GroupePuces[] = [
  { id: 'espece', libelle: 'Espèce', puces: puces('espece', ESPECES, LIBELLES_ESPECE) },
  { id: 'age', libelle: 'Âge', puces: puces('age', TRANCHES_AGE, LIBELLES_TRANCHE_AGE) },
  {
    id: 'taille',
    libelle: 'Taille',
    puces: puces('taille', TAILLES, LIBELLES_TAILLE, (libelle) => `taille ${libelle}`),
  },
  { id: 'sexe', libelle: 'Sexe', puces: puces('sexe', SEXES, LIBELLES_SEXE) },
  {
    id: 'ententes',
    libelle: 'Ententes',
    precision: `${LIBELLES_ENTENTE.Oui} ou ${LIBELLES_ENTENTE.AVoir}`,
    puces: [entente('enfants', 'Enfants'), entente('chiens', 'Chiens'), entente('chats', 'Chats')],
  },
];

const PUCES: readonly Puce[] = GROUPES_PUCES.flatMap((groupe) => groupe.puces);

/**
 * Filtres actifs d'une URL : seules les valeurs connues sont retenues (une valeur inconnue est ignorée plutôt que
 * de provoquer un 400 de l'API) ; un paramètre répété garde sa première valeur.
 */
export function lireFiltres(params: ParamMap): Filtres {
  const filtres: Record<string, string> = {};
  for (const puce of PUCES) {
    if (params.get(puce.parametre) === puce.valeur) {
      filtres[puce.parametre] = puce.valeur;
    }
  }
  return filtres as Filtres;
}

/** Query params de `filtres`, dans l'ordre des puces. */
export function versQueryParams(filtres: Filtres): Params {
  const params: Params = {};
  for (const puce of PUCES) {
    if (filtres[puce.parametre] === puce.valeur) {
      params[puce.parametre] = puce.valeur;
    }
  }
  return params;
}

/** `filtres` avec la puce activée, ou désactivée si elle l'était déjà (une valeur par paramètre). */
export function basculer(filtres: Filtres, puce: Puce): Filtres {
  const suivant: Record<string, string | undefined> = { ...filtres };
  suivant[puce.parametre] = filtres[puce.parametre] === puce.valeur ? undefined : puce.valeur;
  return suivant as Filtres;
}

/** La puce est-elle active dans `filtres` ? */
export function estActive(filtres: Filtres, puce: Puce): boolean {
  return filtres[puce.parametre] === puce.valeur;
}

/** Rappels des filtres actifs, dans l'ordre des puces. */
export function rappels(filtres: Filtres): string[] {
  return PUCES.filter((puce) => estActive(filtres, puce)).map((puce) => puce.rappel);
}

/** Deux jeux de filtres identiques ? */
export function memesFiltres(a: Filtres, b: Filtres): boolean {
  return PUCES.every((puce) => estActive(a, puce) === estActive(b, puce));
}
