---
title: 'Admin animaux : formulaire et photos'
type: 'feature'
ticket: '12'
created: '2026-11-04'
status: 'built'
baseline_revision: '5a60ea3377967cc8f09a3df33194e612fb20b86f'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `/admin/animaux/nouveau` et `/admin/animaux/:id` sont des pages vides : l'admin ne peut ni créer un animal, ni le modifier, ni gérer ses photos depuis le site (CAP-7), alors que l'API (entrées 6 et 9) est prête.

**Approach:** Un seul composant de formulaire pour l'ajout et la modification, sur `POST`/`PUT`/`GET /api/admin/animaux`, avec une zone d'ajout photo qui envoie les photos une à une, dans l'ordre, sur `POST /api/admin/animaux/:id/photos`, et la suppression de l'animal par le dialogue partagé de l'entrée 11.

## Boundaries & Constraints

**Always:** formulaire une colonne (720 px max sur ordinateur), maquette `ecrans-cles.html` §6 et DESIGN.md (champs, choix segmentés, zone d'ajout photo, grille de 3 vignettes 88 px, × de retrait, pastille « Principale » sur la première par `ordre`) : nom, espèce, sexe, ententes Enfants / Chiens / Chats (Oui / À voir / Non) en choix segmentés ; âge et taille en listes ; vaccin, stérilisation, puce en cases à cocher ; description (zone de texte) ; libellés de `shared/libelles.ts`. Champs vérifiés avant envoi (nom et description requis, 50 et 2000 caractères max, chaque choix requis) ; erreurs de l'API (`errors.<champ>`) affichées sous le champ. Bouton « Enregistrer l'animal ». Photos : `<input type="file" accept="image/jpeg,image/png,image/webp" multiple>` sans `capture` (le téléphone propose appareil ou galerie), texte alternatif « Photo n de [prénom] ». Création : photos choisies gardées en attente avec aperçu ; à l'enregistrement, `POST` animal puis envoi des photos une par une dans l'ordre choisi ; l'adresse devient `/admin/animaux/:id` sans quitter la page (mode modification). Modification : chargement par `GET /api/admin/animaux/:id`, nouvelles photos envoyées une par une dès leur choix, `PUT` à l'enregistrement. Chaque envoi en échec reste affiché avec le message de l'API (`errors.fichier`) et un bouton « Renvoyer » qui ne renvoie que celle-là ; les envois suivants continuent. Retrait × : photo en attente → retirée localement ; photo enregistrée → dialogue partagé « Retirer la photo n de [prénom] ? » puis `DELETE …/photos/:photoId` (décision 2026-11-04 : toute suppression admin passe par le dialogue). Supprimer l'animal : dialogue partagé, phrase de l'entrée 11 ; 204 → liste ; 409 → alerte avec le message de l'API. 409 `modifie-ailleurs` au `PUT` → formulaire rechargé avec une alerte. Animal inconnu → « Animal introuvable » + lien vers la liste. Lien « ← Animaux » vers la liste. Titres d'onglet « Admin · Ajouter un animal · AAAS » / « Admin · Modifier [prénom] · AAAS ». Cibles ≥ 48 px, vouvoiement, ton factuel.

**Never:** statut ou forçage de statut dans le formulaire (entrée 13) ; réordonner les photos ; contrôle de type ou de taille côté front autre que `accept` (l'API décide) ; nouvelle dépendance ; changement d'API.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Création | champs valides + 3 photos choisies | `POST` puis 3 `POST …/photos` successifs dans l'ordre ; adresse `/admin/animaux/:id` ; la 1re marquée Principale | — |
| Champs manquants | nom vide, sexe non choisi | aucun appel ; messages sous Nom et Sexe | — |
| Erreur API | 400 `errors.description` | message sous Description | — |
| Photo refusée | 2e photo → 400 `errors.fichier` | 1re et 3e envoyées ; 2e affichée en échec avec le message et « Renvoyer » | « Renvoyer » ne renvoie que la 2e |
| Modification | `GET` puis changement du nom | `PUT` avec les 12 champs ; message « Modifications enregistrées. » | — |
| Ajout en modification | 2 photos choisies sur un animal existant | 2 envois successifs immédiats | — |
| Conflit | `PUT` → 409 `modifie-ailleurs` | alerte, formulaire rechargé depuis l'API | — |
| Inconnu | `GET` → 404 | « Animal introuvable » + lien vers la liste | — |
| Supprimer | bouton Supprimer → dialogue → confirmer | `DELETE` ; 204 → `/admin/animaux` | 409 → alerte |

</frozen-after-approval>

## Code Map

- `aaas/web/src/app/features/animaux/admin/admin-animaux.routes.ts` -- remplacer les deux `routePageVide` (`nouveau`, `:id`) par le formulaire ; le titre « Modifier [prénom] » est posé par le composant (comme la fiche, sans `title` de route).
- `aaas/web/src/app/core/admin-animaux.service.ts` -- compléter : `obtenir`, `creer`, `modifier`, `ajouterPhoto(id, fichier)` (`FormData`, champ `fichier`), `supprimerPhoto(id, photoId)`.
- `aaas/web/src/app/features/animaux/admin/liste/` -- modèle (alertes, `lireErreurApi`, usage du dialogue `shared/dialogue-confirmation/` avec `origine`, phrase de suppression).
- `aaas/web/src/app/features/auth/erreurs-formulaire.ts` -- `interpreterErreur(erreur, champsConnus)` : messages par champ et alerte ; `features/auth/inscription/` -- modèle de formulaire (champs, messages sous les champs).
- `aaas/web/src/app/features/animaux/fiche/fiche.ts` -- modèle du titre d'onglet posé par le composant et de l'id de route invalide.
- `aaas/web/src/app/core/api/animal.dto.ts` (`ESPECES`, `SEXES`, `TRANCHES_AGE`, `TAILLES`, `AnimalDetailDto`, `PhotoDto`) ; `shared/libelles.ts` ; `src/styles.css` (jetons ; zone photo `warning-soft` / `accent`).
- `_bmad-output/initiative-aaas/ux-aaas-adoption/mockups/ecrans-cles.html:392-436` -- maquette §6 (DESIGN.md et EXPERIENCE.md priment).
- `aaas/web/src/app/testing/` (`formulaires.ts`, `animaux.ts` `ficheCaramel`/`ficheAnimal`) ; `app.routes.spec.ts` (lignes `/admin/animaux/nouveau` et `/admin/animaux/7` à adapter).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/web/src/app/core/admin-animaux.service.ts` + spec -- méthodes ci-dessus.
- [x] `aaas/web/src/app/features/animaux/admin/formulaire/` + spec -- formulaire, zone photo, file d'envoi séquentielle, chaque ligne de la matrice.
- [x] `aaas/web/src/app/features/animaux/admin/admin-animaux.routes.ts`, `app.routes.spec.ts` -- routes.

**Acceptance Criteria:**
- Given `npx ng test --watch=false` et `npx ng build` dans `aaas/web`, then tout passe sans avertissement ni dépassement de budget.
- Given l'API en Development et `npm start`, connecté en admin sur téléphone puis sur ordinateur, when on crée un animal avec 3 photos puis on le modifie, then il apparaît au catalogue avec sa photo principale et ses modifications (vérification humaine, `hitl`).

## Implementation Notes

- Route unique `{ matcher: nouveauOuId }` pour `nouveau` et `:id` : après la création, `router.navigate([..., id], { replaceUrl: true })` réutilise le même composant (même `routeConfig`), donc la file d'envoi des photos continue sans rechargement. Pas de `title` de route : le composant pose « Admin · Ajouter un animal · AAAS », « Admin · Modifier [prénom] · AAAS » ou « Admin · Animal introuvable · AAAS ».
- Ordre des vignettes : photos enregistrées par `ordre`, puis photos locales dans l'ordre du choix ; « Principale » sur la première. Une photo en échec passe donc après celles envoyées ensuite (même ordre que côté API).
- Styles génériques « choix segmenté » et « case à cocher en ligne » ajoutés à `src/styles.css` (réutilisables par le questionnaire) pour rester sous le budget de 4 kB par composant.
- 409 `modifie-ailleurs` : message propre au front (le message de l'API invite à recharger, ce que le formulaire fait déjà) puis `GET` ; les photos locales en cours sont conservées.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Vignette 88 px : l'étiquette « En attente » (haut gauche, ~81 px) passe sous le rond blanc de la × (haut droite) | low | patch | Corrigé : étiquettes d'état et « Principale » empilées en bas à gauche, × seule en haut à droite. |
| 2 | Retrait d'une photo (en attente, ou enregistrée après le dialogue) : le bouton d'origine disparaît, le focus tombe sur `<body>` | low | patch | Corrigé : focus sur le champ « Prendre / choisir des photos » après le retrait ; tests. |
| 3 | Quitter la page pendant l'envoi : les photos restantes sont abandonnées sans avertissement | medium | patch | Corrigé : statut visible `role="status"` « Envoi des photos : n sur total… Restez sur cette page. » pendant l'envoi ; test. |

## Verification

**Commands:**
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.

**Manual checks (if no CLI):**
- Parcours du deuxième critère d'acceptation.
