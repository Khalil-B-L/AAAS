/** Contrats animal (contrats-api.md §1 et §6), repris à l'identique de l'API. */

/** Énumération `Espece`. */
export type Espece = 'Chat' | 'Chien';

/** Énumération `Sexe`. */
export type Sexe = 'Male' | 'Femelle';

/** Énumération `TrancheAge` : saisie par l'admin, jamais calculée. */
export type TrancheAge = 'Bebe' | 'Jeune' | 'Adulte' | 'Senior';

/** Énumération `Taille`. */
export type Taille = 'Petite' | 'Moyenne' | 'Grande';

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

/** Enveloppe de toute collection de l'API (AD-22). */
export interface CollectionDto<T> {
  items: T[];
  /** Heure du serveur, date UTC suffixée `Z` ; à lire par `parseUtc`. */
  serverNow: string;
}
