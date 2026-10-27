/** Contrats d'authentification (contrats-api.md §6) et format d'erreur unique de l'API (AD-10). */

/** Énumération `Role` (contrats-api.md §1). */
export type Role = 'Membre' | 'Admin';

/** Corps de `POST /api/auth/inscription`. */
export interface InscriptionDto {
  prenom: string;
  nom: string;
  email: string;
  /** Obligatoire ; normalisé en E.164 par l'API. */
  telephone: string;
  motDePasse: string;
}

/** Corps de `POST /api/auth/connexion`. */
export interface ConnexionDto {
  email: string;
  motDePasse: string;
}

/** Compte exposé par l'API ; seule source du rôle côté client (AD-9). */
export interface UtilisateurDto {
  id: number;
  prenom: string;
  nom: string;
  email: string;
  telephone: string;
  role: Role;
}

/** Réponse d'inscription (201) et de connexion (200). */
export interface AuthReponseDto {
  jeton: string;
  /** Date UTC suffixée `Z` ; à lire par `parseUtc` si besoin. */
  expireLe: string;
  utilisateur: UtilisateurDto;
}

/** Corps de toute réponse d'erreur : `{ error, errors?, code? }` (AD-10). */
export interface ErreurApi {
  error: string;
  /** Erreurs par champ (400), clés = noms JSON camelCase. */
  errors?: Record<string, string>;
  /** Code de refus métier stable (contrats-api.md §5). */
  code?: string;
}
