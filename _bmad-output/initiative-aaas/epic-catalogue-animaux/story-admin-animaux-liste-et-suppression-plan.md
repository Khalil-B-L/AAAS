---
title: 'Admin animaux : liste et suppression'
type: 'feature'
ticket: '11'
created: '2026-11-03'
status: 'built'
baseline_revision: '81cf2b6261873cfecc18edd043699c8d064eaba7'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `/admin/animaux` est une page vide : l'admin ne voit pas ses animaux (tous statuts), ne peut pas ouvrir leur formulaire ni en supprimer un (CAP-7), et le dialogue de confirmation exigé avant toute action admin destructive n'existe pas encore.

**Approach:** Construire la page liste sur `GET /api/admin/animaux` (une ligne par animal avec accès au formulaire et bouton Supprimer, bouton « Ajouter un animal ») et créer dans `shared/` le dialogue de confirmation réutilisable, appelé avant `DELETE /api/admin/animaux/:id`, avec le message du refus 409 `animal-a-des-reservations`.

## Boundaries & Constraints

**Always:** liste dans l'ordre de l'API (`creeLe` décroissant), tous statuts, chaque ligne : vignette 64 px (`photoPrincipaleUrl` ou dégradé de remplacement), prénom, espèce · âge (libellés de `shared/libelles.ts`), pastille de statut partagée, lien vers `/admin/animaux/:id` (formulaire, entrée 12) et bouton « Supprimer » (contour `danger`) ; en tête « Ajouter un animal » → `/admin/animaux/nouveau` ; titre « Animaux », onglet « Admin · Animaux · AAAS ». Dialogue partagé (`shared/`) conforme à DESIGN.md (titre « Êtes-vous sûr ? », une phrase, bouton d'action `button-danger` puis « Retour » en contour, pleine largeur, sur voile) et EXPERIENCE.md : focus sur « Retour » à l'ouverture, Tab / Maj+Tab piégés dans le dialogue, Échap et « Retour » ferment sans rien faire, focus rendu à l'élément d'origine à la fermeture ; réutilisable par réservations et actualités (phrase et libellé d'action fournis par l'appelant). Suppression : phrase « Supprimer [prénom] ? Sa fiche et ses photos seront effacées. », action « Supprimer » ; succès 204 → ligne retirée sans recharger ; 409 `animal-a-des-reservations` → alerte d'erreur au-dessus de la liste (message de l'API), ligne conservée ; 404 → liste rechargée ; autre échec → alerte d'erreur générique, erreur journalisée. Liste vide → phrase courte + « Ajouter un animal » ; échec du chargement → alerte d'erreur, erreur journalisée. Ton factuel, vouvoiement, sans pattes en filigrane ; mobile-first, colonne centrée de 720 px max sur ordinateur, cibles ≥ 48 px, statut écrit.

**Never:** formulaire d'ajout ou de modification (entrée 12) ; forçage de statut (entrée 13) ; nouvelle dépendance (pas d'`@angular/cdk`) ; filtre, tri ou pagination côté front ; changement d'API.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Liste | API : Caramel Disponible, Rex Réservé, Simba Adopté | 3 lignes dans cet ordre, pastilles Disponible / Réservé / Adopté, liens vers `/admin/animaux/:id` | — |
| Vide | `items: []` | phrase + « Ajouter un animal » | — |
| Chargement en échec | 500 | alerte d'erreur | erreur journalisée |
| Ouvrir le dialogue | clic « Supprimer » sur Rex | dialogue « Supprimer Rex ? … », focus sur « Retour » | — |
| Retour / Échap | dans le dialogue | fermé, aucun appel API, focus sur le bouton « Supprimer » de Rex | — |
| Piège du focus | Tab depuis le dernier bouton, Maj+Tab depuis le premier | le focus reste dans le dialogue | — |
| Confirmer | « Supprimer » | `DELETE /api/admin/animaux/:id` ; 204 → dialogue fermé, ligne Rex retirée | — |
| A des réservations | 409 `animal-a-des-reservations` | dialogue fermé, alerte avec le message de l'API, Rex toujours listé | — |
| Déjà supprimé | 404 | dialogue fermé, liste rechargée | — |

</frozen-after-approval>

## Code Map

- `aaas/web/src/app/features/animaux/animaux.routes.ts` -- `ADMIN_ANIMAUX_ROUTES` : remplacer la page vide `''` par la liste ; laisser `nouveau` et `:id` en `routePageVide` (entrée 12).
- `aaas/web/src/app/core/animaux.service.ts` -- modèle de client (`firstValueFrom`, l'appelant gère l'échec) ; créer à côté `core/admin-animaux.service.ts` (`lister()`, `supprimer(id)`), que l'entrée 12 complétera.
- `aaas/web/src/app/features/animaux/adoptes/` -- modèle de page (signal `null` pendant le chargement, échec journalisé) ; `core/api/erreur-api.ts` (`lireErreurApi` pour le message et le `code` du 409) ; `core/api/animal.dto.ts` (`AnimalCarteDto`, `CollectionDto`).
- `aaas/web/src/app/shared/pastille-statut/`, `shared/carte-animal/carte-animal.css` (dégradé de remplacement de la photo), `shared/libelles.ts`, `src/styles.css` (jetons, alerte, boutons ; ajouter les jetons manquants plutôt que des valeurs en dur).
- `aaas/web/src/app/app.routes.spec.ts:52` -- la ligne `/admin/animaux` du tableau des pages vides doit suivre le nouveau titre (« Animaux » affiché, onglet inchangé).
- `aaas/web/src/app/testing/` -- `animaux.ts` (`caramel`, `rex`, `carteAnimal`, `collection`), `session.ts` ; tests HTTP avec `HttpTestingController` comme `core/animaux.service.spec.ts`.
- jsdom 30 sous Vitest : vérifier que l'API choisie pour le dialogue (`<dialog>` natif ou superposition maison) se teste dans jsdom ; sinon, superposition maison avec `role="dialog"`, `aria-modal="true"`, `aria-labelledby`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/web/src/app/shared/dialogue-confirmation/` -- composant ou service du dialogue + spec (focus initial, piège Tab / Maj+Tab, Échap, Retour, confirmation, focus rendu).
- [x] `aaas/web/src/app/core/admin-animaux.service.ts` + spec -- `GET` et `DELETE /api/admin/animaux`.
- [x] `aaas/web/src/app/features/animaux/admin/liste/` + spec -- page liste et suppression, chaque ligne de la matrice.
- [x] `aaas/web/src/app/features/animaux/animaux.routes.ts`, `app.routes.spec.ts` -- route.

**Acceptance Criteria:**
- Given `npx ng test --watch=false` et `npx ng build` dans `aaas/web`, then tout passe sans avertissement ni dépassement de budget.
- Given l'API en Development et `npm start`, connecté en admin sur téléphone puis sur ordinateur, when on supprime un animal créé pour l'essai, then le dialogue s'ouvre sur « Retour », Échap le ferme, « Supprimer » le retire de la liste et du catalogue (vérification humaine, `hitl`).

## Implementation Notes

- Dialogue : superposition maison (`role="dialog"`, `aria-modal`, `aria-labelledby` / `aria-describedby`) ; jsdom 30 n'implémente pas `HTMLDialogElement.showModal`. Composant `DialogueConfirmation` affiché sous `@if` par l'appelant (entrées `phrase`, `action`, `occupe`, `origine` ; sorties `confirme`, `retour`). Le focus qui sort du dialogue (clic sur le voile) est ramené sur « Retour ». Pendant le `DELETE`, le dialogue reste ouvert, boutons `aria-disabled`, Échap ignorée (pas de double envoi).
- Empilement : `.contenu` (`app.css`) perd son `z-index: 1`, qui créait un contexte d'empilement sous l'en-tête (`z-index: 2`) et le pied de page ; le voile (`position: fixed`, `z-index: 3`) passe désormais au-dessus des deux. Les pattes restent sous le contenu (ordre du document).
- Jetons ajoutés à `styles.css` : `--width-column` (720 px, repris par `.page-formulaire`), `--width-dialog`, `--size-thumb-list` ; classes globales `.bouton-danger` (button-danger) et `.bouton-contour-danger` (contour danger, réutilisable pour « Annuler » des réservations).
- `app.routes.spec.ts` : table `onglets` pour les pages dont le titre d'onglet diffère du titre affiché (`/admin/animaux` : « Animaux » / « Admin · Animaux · AAAS ») ; `/api/admin/animaux` ajouté aux requêtes vidées en fin de test.
- Après revue : Échap et Tab écoutés sur le document (focus éventuellement sur `<body>`), boîte en `tabindex="-1"`, `mousedown` sur le voile sans effet sur le focus ; boutons `aria-disabled` stylés comme `button-disabled` ; `ADMIN_ANIMAUX_ROUTES` déplacé dans `features/animaux/admin/admin-animaux.routes.ts` (chunk à part) ; rechargement en échec → liste retirée, rechargement réussi → alerte de chargement effacée.
- Après un 204, le bouton d'origine a disparu avec la ligne : le focus n'est pas rendu (retombe sur le document).

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 2, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Clic sur le voile ou le texte du dialogue : focus sur `<body>` sans `focusin`, Échap (écouté sur `.dialogue`) ne ferme plus | medium | patch | Contredit EXPERIENCE.md (Échap ferme). Corrigé : Échap et Tab écoutés sur le document, `tabindex="-1"` sur la boîte, `mousedown` du voile ramène le focus sur « Retour » ; 4 tests. |
| 2 | `ADMIN_ANIMAUX_ROUTES` dans `animaux.routes.ts` : code admin dans le chunk public | medium | patch | Corrigé : `features/animaux/admin/admin-animaux.routes.ts` ; le build émet un chunk `admin-animaux-routes` séparé. |
| 3 | Rechargement après 404 en échec : liste périmée affichée sous l'alerte ; alerte de chargement non effacée après un rechargement réussi | low | patch | Corrigé dans `charger()` ; test du 404 suivi d'un échec. |
| 4 | Boutons du dialogue occupés (`aria-disabled`) visuellement actifs | low | patch | Corrigé : style du bouton désactivé de DESIGN.md. |

## Verification

**Commands:**
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.

**Manual checks (if no CLI):**
- Parcours du deuxième critère d'acceptation, sur téléphone (ou vue mobile du navigateur) et sur ordinateur.
