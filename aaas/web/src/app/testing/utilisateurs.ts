/** Fixtures utilisateur partagées par les specs. */
import { AuthReponseDto, InscriptionDto, Role, UtilisateurDto } from '../core/api/auth.dto';

/** Mot de passe de Sonia dans les saisies des specs. */
export const motDePasse = 'motdepasse';

/** Sonia, Membre : l'utilisatrice de référence des specs. */
export const membre: UtilisateurDto = {
  id: 3,
  prenom: 'Sonia',
  nom: 'Ben Ali',
  email: 'sonia@exemple.tn',
  telephone: '+21672000000',
  role: 'Membre',
};

/** Compte Admin distinct de Sonia. */
export const admin: UtilisateurDto = { ...membre, id: 1, prenom: 'Admin', role: 'Admin' };

/** Sonia avec le rôle donné. */
export function utilisateurEnTantQue(role: Role): UtilisateurDto {
  return { ...membre, role };
}

/** Saisie d'inscription de Sonia (téléphone tel que tapé, avant normalisation par l'API). */
export const saisieInscription: InscriptionDto = {
  prenom: membre.prenom,
  nom: membre.nom,
  email: membre.email,
  telephone: '+216 72 000 000',
  motDePasse,
};

/** Réponse d'inscription ou de connexion (`AuthReponseDto`). */
export function reponseAuth(
  utilisateur: UtilisateurDto = membre,
  jeton = 'jeton-1',
): AuthReponseDto {
  return { jeton, expireLe: '2026-10-15T10:00:00Z', utilisateur };
}
