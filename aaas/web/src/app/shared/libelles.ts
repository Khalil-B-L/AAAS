/**
 * Seule source des libellés affichés des énumérations (contrats-api.md §1, AD-19).
 * Libellés invariables : « Réservé » même pour une femelle (state-machines.md).
 */
import { Entente, Espece, Sexe, StatutAnimal, Taille, TrancheAge } from '../core/api/animal.dto';
import { Role } from '../core/api/auth.dto';
import { Logement, StatutReservation } from '../core/api/reservation.dto';

export const LIBELLES_ESPECE: Readonly<Record<Espece, string>> = {
  Chat: 'Chat',
  Chien: 'Chien',
};

export const LIBELLES_SEXE: Readonly<Record<Sexe, string>> = {
  Male: 'Mâle',
  Femelle: 'Femelle',
};

export const LIBELLES_TRANCHE_AGE: Readonly<Record<TrancheAge, string>> = {
  Bebe: 'Bébé',
  Jeune: 'Jeune',
  Adulte: 'Adulte',
  Senior: 'Senior',
};

export const LIBELLES_TAILLE: Readonly<Record<Taille, string>> = {
  Petite: 'Petite',
  Moyenne: 'Moyenne',
  Grande: 'Grande',
};

export const LIBELLES_ENTENTE: Readonly<Record<Entente, string>> = {
  Oui: 'Oui',
  AVoir: 'À voir',
  Non: 'Non',
};

export const LIBELLES_LOGEMENT: Readonly<Record<Logement, string>> = {
  Appartement: 'Appartement',
  Maison: 'Maison',
};

export const LIBELLES_STATUT_ANIMAL: Readonly<Record<StatutAnimal, string>> = {
  Disponible: 'Disponible',
  Reserve: 'Réservé',
  Adopte: 'Adopté',
};

export const LIBELLES_STATUT_RESERVATION: Readonly<Record<StatutReservation, string>> = {
  EnAttente: 'En attente',
  Confirmee: 'Confirmée',
  Annulee: 'Annulée',
  Expiree: 'Expirée',
};

export const LIBELLES_ROLE: Readonly<Record<Role, string>> = {
  Membre: 'Membre',
  Admin: 'Admin',
};
