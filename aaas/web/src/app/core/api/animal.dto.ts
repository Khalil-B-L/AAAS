/** Contrats animal (contrats-api.md §1, §2 et §6), repris à l'identique de l'API. */

/** Valeurs de l'énumération `Espece`, dans l'ordre de l'API. */
export const ESPECES = ['Chat', 'Chien'] as const;
export type Espece = (typeof ESPECES)[number];

/** Valeurs de l'énumération `Sexe`. */
export const SEXES = ['Male', 'Femelle'] as const;
export type Sexe = (typeof SEXES)[number];

/** Valeurs de l'énumération `TrancheAge` : saisie par l'admin, jamais calculée. */
export const TRANCHES_AGE = ['Bebe', 'Jeune', 'Adulte', 'Senior'] as const;
export type TrancheAge = (typeof TRANCHES_AGE)[number];

/** Valeurs de l'énumération `Taille`. */
export const TAILLES = ['Petite', 'Moyenne', 'Grande'] as const;
export type Taille = (typeof TAILLES)[number];

/** Énumération `Entente`. */
export type Entente = 'Oui' | 'AVoir' | 'Non';

/** Énumération `StatutAnimal`. */
export type StatutAnimal = 'Disponible' | 'Reserve' | 'Adopte';

/** Carte animal : seul format de carte de l'API. */
export interface AnimalCarteDto {
  id: number;
  nom: string;
  espece: Espece;
  sexe: Sexe;
  age: TrancheAge;
  taille: Taille;
  /** Statut effectif. */
  statut: StatutAnimal;
  /** URL relative de la photo principale, `null` sans photo. */
  photoPrincipaleUrl: string | null;
}

/**
 * Filtres de `GET /api/animaux` (contrats-api.md §2), mêmes noms que les paramètres ; combinés en ET.
 * Une entente à `Oui` garde les animaux dont l'entente vaut Oui ou À voir.
 */
export interface FiltresCatalogue {
  espece?: Espece;
  sexe?: Sexe;
  age?: TrancheAge;
  taille?: Taille;
  enfants?: 'Oui';
  chiens?: 'Oui';
  chats?: 'Oui';
  /** 1..50 : les n plus récents. */
  limite?: number;
}

/** Paramètres de `FiltresCatalogue`, dans l'ordre de contrats-api.md §2 (ordre des query params). */
export const PARAMETRES_CATALOGUE = [
  'espece',
  'sexe',
  'age',
  'taille',
  'enfants',
  'chiens',
  'chats',
  'limite',
] as const satisfies readonly (keyof FiltresCatalogue)[];

/** Enveloppe de toute collection de l'API (AD-22). */
export interface CollectionDto<T> {
  items: T[];
  /** Heure du serveur, date UTC suffixée `Z` ; à lire par `parseUtc`. */
  serverNow: string;
}
