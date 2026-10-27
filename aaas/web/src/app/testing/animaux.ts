/** Fixtures animal partagées par les specs. */
import { AnimalCarteDto, CollectionDto } from '../core/api/animal.dto';

/** Heure serveur des réponses de collection des specs. */
export const serverNow = '2026-10-15T10:00:00Z';

/** Caramel, chatte jeune Disponible, sans photo : la carte de référence des specs. */
export const caramel: AnimalCarteDto = {
  id: 7,
  nom: 'Caramel',
  espece: 'Chat',
  sexe: 'Femelle',
  age: 'Jeune',
  taille: 'Moyenne',
  statut: 'Disponible',
  photoPrincipaleUrl: null,
};

/** Rex, chien adulte Réservé, avec photo. */
export const rex: AnimalCarteDto = {
  id: 12,
  nom: 'Rex',
  espece: 'Chien',
  sexe: 'Male',
  age: 'Adulte',
  taille: 'Grande',
  statut: 'Reserve',
  photoPrincipaleUrl: '/fichiers/rex.jpg',
};

/** Carte dérivée de Caramel. */
export function carteAnimal(modifs: Partial<AnimalCarteDto> = {}): AnimalCarteDto {
  return { ...caramel, ...modifs };
}

/** Les 4 derniers pensionnaires de l'accueil. */
export const derniersPensionnaires: AnimalCarteDto[] = [
  caramel,
  rex,
  carteAnimal({ id: 3, nom: 'Mimi', age: 'Bebe' }),
  carteAnimal({ id: 4, nom: 'Filou', espece: 'Chien', sexe: 'Male', age: 'Senior' }),
];

/** Réponse de collection `{ items, serverNow }`. */
export function collection<T>(items: T[]): CollectionDto<T> {
  return { items, serverNow };
}
