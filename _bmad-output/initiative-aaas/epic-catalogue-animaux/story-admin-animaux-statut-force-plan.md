---
title: 'Admin animaux : statut forcé'
type: 'feature'
ticket: '13'
created: '2026-11-04'
status: 'built'
baseline_revision: 'd1bf461adc3048c614646a3107a54cf8182a7311'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** L'admin ne peut pas, depuis le site, marquer un animal Adopté (adoption conclue hors réservation) ni le remettre Disponible : le formulaire n'a pas de statut, alors que `POST /api/admin/animaux/:id/statut` existe (entrée 10, CAP-7).

**Approach:** Ajouter au formulaire, en modification, un choix segmenté Disponible / Réservé / Adopté et un bouton qui applique le statut choisi par `POST …/statut { statut, statutAttendu }`, et recharger le formulaire avec le message de l'API sur 409 `reservation-active` ou `modifie-ailleurs`.

## Boundaries & Constraints

**Always:** en modification seulement ; en création, mention fixe « Statut : Disponible » (l'API crée toujours Disponible). Choix segmenté (même style que les autres choix du formulaire, libellés de `LIBELLES_STATUT_ANIMAL`) présélectionné sur le statut effectif chargé ; « Réservé » toujours désactivé (jamais forçable) ; un animal Réservé affiche ses trois options désactivées et la phrase « Réservé : traitez d'abord sa réservation dans Admin · Réservations. ». Changer la sélection n'appelle rien : un bouton « Changer le statut », actif seulement si la sélection diffère du statut chargé, envoie `statut` = sélection et `statutAttendu` = statut chargé (une flèche du clavier dans le groupe ne force donc jamais un statut). Succès → statut, pastille et titre mis à jour depuis l'`AnimalDetailDto` renvoyé, message « Statut enregistré : [libellé]. ». 409 `reservation-active` ou `modifie-ailleurs` → formulaire rechargé (`GET`, comme le conflit du `PUT`) puis alerte avec le message de l'API ; 404 → « Animal introuvable » ; autre échec → alerte générique, sélection remise sur le statut chargé, erreur journalisée. Bouton occupé pendant l'appel (pas de double envoi). Le statut ne passe jamais par le `PUT` ni par « Enregistrer l'animal ». Cibles ≥ 48 px, statut écrit, ton factuel.

**Never:** dialogue de confirmation pour un changement de statut (non destructif, réversible) ; modification de l'API ; changement du comportement des champs et des photos.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Adopter | Disponible ; choisir Adopté, « Changer le statut » | `POST …/statut { statut: "Adopte", statutAttendu: "Disponible" }` ; Adopté affiché, message | — |
| Remettre disponible | Adopté ; choisir Disponible, valider | `{ Disponible, Adopte }` ; Disponible affiché | — |
| Sélection inchangée | statut chargé sélectionné | bouton désactivé, aucun appel | — |
| Clavier | flèche dans le groupe | sélection change, aucun appel | — |
| Réservé | statut chargé Réservé | trois options désactivées, phrase vers Admin · Réservations, pas de bouton actif | — |
| Création | `/admin/animaux/nouveau` | « Statut : Disponible », pas de choix | — |
| Réservation active | 409 `reservation-active` | `GET` rejoué, formulaire rechargé, alerte avec le message de l'API | — |
| Modifié ailleurs | 409 `modifie-ailleurs` | idem | — |
| Échec réseau | 500 | alerte générique, sélection remise sur le statut chargé | erreur journalisée |

</frozen-after-approval>

## Code Map

- `aaas/web/src/app/features/animaux/admin/formulaire/formulaire.ts` -- `animal` (signal `AnimalDetailDto`, statut effectif chargé), `charger(id)` (rechargement, déjà utilisé par le conflit du `PUT`, `MESSAGE_CONFLIT`), `alerte`/`message`, `lireErreurApi` ; ajouter la sélection de statut, l'état occupé et `changerStatut()`. Le fichier fait ~680 lignes : une petite section dédiée, sans remanier le reste.
- `formulaire.html` -- choix segmentés existants (`.choix-segmente`, `.segment`, radios, ~l.121) à réutiliser pour le statut ; `formulaire.css` -- budget de style du composant (4 kB) : préférer les classes globales de `src/styles.css`.
- `aaas/web/src/app/core/admin-animaux.service.ts` -- ajouter `forcerStatut(id, statut, statutAttendu): Promise<AnimalDetailDto>`.
- `aaas/web/src/app/shared/libelles.ts` (`LIBELLES_STATUT_ANIMAL`), `shared/pastille-statut/` ; `core/api/animal.dto.ts` (`StatutAnimal`, ajouter `StatutAnimalDto`).
- `formulaire.spec.ts`, `core/admin-animaux.service.spec.ts` -- modèles de tests (service simulé, `HttpTestingController`).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/web/src/app/core/api/animal.dto.ts`, `core/admin-animaux.service.ts` + spec -- `StatutAnimalDto`, `forcerStatut`.
- [x] `aaas/web/src/app/features/animaux/admin/formulaire/` + spec -- choix de statut et bouton, chaque ligne de la matrice.

**Acceptance Criteria:**
- Given `npx ng test --watch=false` et `npx ng build` dans `aaas/web`, then tout passe sans avertissement ni dépassement de budget.
- Given l'API en Development et `npm start`, connecté en admin, when on force un animal Adopté, then il quitte `/adopter` et apparaît dans « Ils ont trouvé une famille » ; remis Disponible, il revient au catalogue (vérification humaine, `hitl`).

## Implementation Notes

- Section « Statut » placée entre le titre et le formulaire, hors du `<form>` (radios sans `formControlName`, bouton `type="button"`) : Entrée dans le formulaire ne peut jamais forcer un statut. Pastille dans la légende.
- `statutChoisi` = `linkedSignal` sur `animal()` : revient au statut chargé à chaque chargement, `PUT` ou statut réussi. Succès du statut → `animal` et titre mis à jour sans `setValue` : les champs saisis non enregistrés sont conservés.
- Messages du statut (`alerteStatut`, `messageStatut`) affichés dans la section, pas au-dessus de « Enregistrer l'animal ».
- Style `.segment:has(:disabled)` ajouté aux classes globales (`styles.css`) ; `formulaire.css` reste sous le budget de 4 kB.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | « Changer le statut » (POST) et « Enregistrer l'animal » (PUT) peuvent partir ensemble : le PUT peut recevoir 409 `modifie-ailleurs` (champs saisis perdus) ou sa réponse tardive remettre l'ancien statut après « Statut enregistré » | medium | patch | Corrigé : chaque bouton désactivé et chaque méthode sans effet pendant l'autre appel ; 2 tests. |
| 2 | Messages du statut conservés après un `PUT` réussi ou un rechargement de conflit du `PUT` : « Statut enregistré : Adopté. » à côté d'une pastille Disponible | low | patch | Corrigé : `afficher()` efface `alerteStatut` et `messageStatut` (le 409 du statut pose son alerte après `charger()`) ; test. |
| 3 | 409 du statut puis `GET` de rechargement en échec : alerte de chargement près d'« Enregistrer », choix refusé gardé, bouton actif | low | rejected | Deux échecs consécutifs, rare ; un nouvel essai renvoie au pire un nouveau 409 sans rien modifier. Le corriger ajoute une branche. |
| 4 | Un choix non appliqué est remis sur le statut chargé après un `PUT` réussi, sans message | low | rejected | Comportement voulu : le choix suit le statut chargé (`linkedSignal`) ; rien n'est envoyé, rien n'est perdu côté données. |

## Verification

**Commands:**
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.

**Manual checks (if no CLI):**
- Parcours du deuxième critère d'acceptation.
