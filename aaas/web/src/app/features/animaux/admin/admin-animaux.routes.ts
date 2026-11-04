import { Routes, UrlMatchResult, UrlSegment } from '@angular/router';
import { AdminAnimalFormulaire } from './formulaire/formulaire';
import { AdminAnimauxListe } from './liste/liste';

/**
 * `nouveau` (ajout, sans paramètre) ou `:id` (modification) sur une seule route : après la création, l'adresse passe
 * de `/admin/animaux/nouveau` à `/admin/animaux/:id` en réutilisant le même formulaire, sans interrompre l'envoi
 * des photos.
 */
export function nouveauOuId(segments: UrlSegment[]): UrlMatchResult | null {
  if (segments.length !== 1) {
    return null;
  }
  const [segment] = segments;
  return segment.path === 'nouveau'
    ? { consumed: segments }
    : { consumed: segments, posParams: { id: segment } };
}

/**
 * Monté sur `/admin/animaux` : liste, puis formulaire d'ajout et de modification. Le formulaire pose lui-même son
 * titre d'onglet (« Admin · Modifier [prénom] · AAAS »), d'où l'absence de `title` de route. Fichier à part des
 * routes publiques pour que le code admin ait son propre chunk, jamais téléchargé par un visiteur.
 */
export const ADMIN_ANIMAUX_ROUTES: Routes = [
  { path: '', component: AdminAnimauxListe, title: 'Admin · Animaux · AAAS' },
  { matcher: nouveauOuId, component: AdminAnimalFormulaire },
];
