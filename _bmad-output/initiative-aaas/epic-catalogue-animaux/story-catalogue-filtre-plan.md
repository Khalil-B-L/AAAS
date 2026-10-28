---
title: 'Catalogue filtré'
type: 'feature'
ticket: '5'
created: '2026-10-28'
status: 'built'
baseline_revision: '57f7886bc2a946efc1583bc9b3bae92cb363cc31'
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
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La page Adopter n'affiche que son titre : un visiteur ne peut ni parcourir tous les animaux ni les filtrer par espèce, âge, taille, sexe ou ententes (CAP-2), alors que les boutons Miaou ! / Wouf ! de l'accueil y mènent déjà avec `?espece=`.

**Approach:** Ajouter à `GET /api/animaux` les filtres de `contrats-api.md` §2 sur le statut effectif, puis construire la page `/adopter` : puces de filtre reflétées dans l'URL, grille de cartes, compteur, « Effacer les filtres », états vides et lien vers « Ils ont trouvé une famille ».

## Boundaries & Constraints

**Always:** paramètres exacts de §2 (`espece`, `sexe`, `age`, `taille`, `enfants`, `chiens`, `chats`, `limite`), une valeur par paramètre, casse exacte des énumérations, filtres combinés en ET ; `enfants=Oui` (de même `chiens`, `chats`) garde les ententes `Oui` **ou** `AVoir`, et toute autre valeur que `Oui` pour ces trois paramètres est refusée ; valeur inconnue → 400 `ApiErrors` avec `errors.<param>` (clé = nom du paramètre) ; filtres appliqués sur le statut effectif (Adoptés jamais renvoyés) ; tri `creeLe` décroissant inchangé ; mêmes noms de paramètres dans l'URL du site (query params) ; filtres appliqués immédiatement au clic, sans bouton « Rechercher » ; libellés depuis `shared/libelles.ts` ; carte animal partagée ; jetons CSS de DESIGN.md (puces : bordure `border-control`, active pleine `primary`) ; rangée de puces défilante horizontalement sur téléphone, sur plusieurs lignes sur ordinateur ; grille 2 / 3 / 4 colonnes comme l'accueil ; accessibilité (puces = boutons à état `aria-pressed`, compteur annoncé, cibles ≥ 48 px).

**Never:** filtre ou recherche sur l'accueil ; pagination (Deferred) ; filtre par statut côté visiteur ; photos (entrée 6) ; fiche (entrée 8) ; page des adoptés (entrée 14 ; seul le lien est posé ici).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Un filtre | `?espece=Chat` | seuls les chats non adoptés | — |
| Filtres combinés | `?espece=Chien&taille=Grande&sexe=Male` | intersection | — |
| Entente | `?enfants=Oui` | ententeEnfants `Oui` ou `AVoir` ; jamais `Non` | — |
| Valeur inconnue | `?espece=Lapin`, `?enfants=AVoir`, `?age=jeune` | 400 `errors.espece` / `errors.enfants` / `errors.age` | — |
| Réservé expiré | animal Reserve sans réservation active, `?espece=` correspondant | renvoyé, statut `Disponible` | — |
| Arrivée depuis Miaou ! | `/adopter?espece=Chat` | puce Chat active, compteur « n animaux » | — |
| Clic sur une puce | puce Taille Grande | URL mise à jour, liste rechargée, compteur à jour, « Effacer les filtres » visible | — |
| Aucun résultat | filtres sans correspondance | phrase rappelant les filtres actifs + « Effacer les filtres » | — |
| Catalogue vide | aucun animal non adopté, sans filtre | « Tous nos protégés ont trouvé une famille pour l'instant ! » + lien | API en échec : même état, erreur journalisée |
| Effacer | « Effacer les filtres » | URL `/adopter`, tous les animaux | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimaux.cs` -- `ListerAnimauxQuery(string? Limite)`, validation texte de `limite` (1..50, `NumberStyles.None`) → `Result.Invalid(errors.limite)`, filtre « non Adopté » et projection via `AnimalCarteDto.Projection(reservations, now)` + `ExpressionComposition.Composer` ; tri `CreeLe` puis `Id`. Étendre la requête aux filtres sans dupliquer la règle du statut effectif.
- `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- paramètres lus en texte (`string?`) pour garder des 400 `errors.<param>` ; même principe pour les nouveaux filtres.
- `aaas/api/Aaas.Api/Features/Animaux/Enumerations.cs`, `Common/Disponibilite/Statuts.cs` -- énumérations (`Espece`, `Sexe`, `TrancheAge`, `Taille`, `Entente`).
- `aaas/api/Aaas.Api.Tests/AnimauxTests.cs`, `DisponibiliteTests.cs`, `Outils/Animaux.cs` (`Nouveau(...)` avec espèce, sexe, âge, taille ; ententes fixes : à paramétrer), `Outils/Reservations.cs` -- harnais à réutiliser.
- `aaas/web/src/app/features/animaux/animaux.routes.ts` (`CATALOGUE_ROUTES` = page vide) ; `features/accueil/` (grille 2/3/4 colonnes et état vide « Tous nos protégés… » à réutiliser plutôt que recopier) ; `core/animaux.service.ts` (appel `limite`) ; `core/api/animal.dto.ts` ; `shared/carte-animal/`, `shared/libelles.ts` ; `testing/animaux.ts`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimaux.cs`, `AnimauxEndpoints.cs` -- filtres §2 lus en texte, validés (casse exacte, `Oui` seul pour les ententes), combinés en ET sur la requête du statut effectif.
- [x] `aaas/api/Aaas.Api.Tests/Outils/Animaux.cs`, `CatalogueTests.cs` -- ententes paramétrables dans la fabrique ; une couverture par ligne API de la matrice (chaque filtre seul, combinaisons, ententes Oui/AVoir/Non, 400 par paramètre, Réservé expiré, jamais d'Adopté).
- [x] `aaas/web/src/app/core/animaux.service.ts` -- `lister(filtres)` qui transmet les paramètres §2.
- [x] `aaas/web/src/app/shared/grille-animaux/` -- grille de cartes partagée (2/3/4 colonnes) et état vide « Catalogue vide », réutilisés par l'accueil et le catalogue.
- [x] `aaas/web/src/app/features/animaux/catalogue/`, `animaux.routes.ts` -- page `/adopter` : puces par groupe (Espèce, Âge, Taille, Sexe, Ententes Enfants / Chiens / Chats), état dans les query params, compteur, « Effacer les filtres », « Aucun résultat », lien « Ils ont trouvé une famille » en bas de page ; titre de page.
- [x] Tests Vitest -- service, grille, page (lecture de `?espece=Chat`, clic → URL et rechargement, compteur, effacer, aucun résultat, catalogue vide, échec API) ; accueil toujours vert.

**Acceptance Criteria:**
- Given `dotnet test`, `npx ng test --watch=false`, `dotnet build` et `npx ng build`, then tout passe, sans avertissement ni dépassement de budget.
- Given l'API en Development avec les animaux de démonstration, when on ouvre `/adopter?espece=Chat` puis on ajoute « Âge : Bébé », then seule Mimi reste et le compteur indique 1 animal (vérification humaine).

## Implementation Notes

- API : toutes les erreurs de filtre sont collectées (une clé `errors.<param>` par paramètre invalide). Énumérations lues par nom exact (ordinal) : entier, casse différente, valeur vide ou paramètre répété (lié en `"A,B"`) → 400. Messages : `MessageEnumeration<T>()` (« Valeur attendue parmi : … ») et `MessageEntente`.
- Front : les types d'énumération de `animal.dto.ts` dérivent de tableaux (`ESPECES`, `SEXES`, `TRANCHES_AGE`, `TAILLES`) qui donnent l'ordre des puces ; `FiltresCatalogue` + `PARAMETRES_CATALOGUE` (ordre §2) pour le service. L'accueil appelle `lister({ limite: 4 })`.
- Page : une valeur inconnue dans l'URL est ignorée (jamais transmise à l'API) ; rechargement par `switchMap` (réponse dépassée ignorée) ; API en échec → « Catalogue vide » sans compteur, erreur journalisée, quels que soient les filtres. « Effacer les filtres » vit dans la barre du compteur, visible dès qu'un filtre est actif (donc aussi dans l'état « Aucun résultat »). Le lien bas de page reste affiché même quand l'état « Catalogue vide » porte déjà le sien.
- `app.routes.spec.ts` : `afterEach` répond à toute requête `/api/animaux` (accueil et Adopter).

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | « (Oui ou À voir) » des puces d entente seulement lisible par les lecteurs d écran | low | patch | EXPERIENCE.md décrit « Enfants / Chiens / Chats : Oui ou À voir » ; un visiteur voyant ne pouvait pas savoir qu « Enfants » garde aussi les « À voir ». Corrigé : précision visible, en légende discrète. `ng test` 181 réussis, `ng build` sans avertissement. |
| 2 | Après une panne de l API, aucun moyen de réessayer sans changer de filtre ou recharger la page | low | rejected | Cas rare ; état « catalogue vide » accepté par le plan et testé ; un bouton de nouvel essai ajouterait de la complexité. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.
