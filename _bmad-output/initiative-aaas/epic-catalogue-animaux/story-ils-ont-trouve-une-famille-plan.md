---
title: 'Ils ont trouvé une famille'
type: 'feature'
ticket: '14'
created: '2026-11-02'
status: 'built'
baseline_revision: 'd52ff425e873914e989101a78b64b61b6e79f142'
route: 'full'
route_source: 'auto'
risk: 'low'
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

**Problem:** Les animaux adoptés disparaissent du catalogue mais n'apparaissent nulle part : la page « Ils ont trouvé une famille », liée depuis le catalogue et le pied de page, est vide (CAP-11).

**Approach:** Exposer `GET /api/animaux/adoptes` (statut effectif Adopté, `adopteLe` décroissant) et construire la page `/ils-ont-trouve-une-famille` avec la grille de cartes partagée, dont chaque carte ouvre la fiche.

## Boundaries & Constraints

**Always:** réponse `{ items: AnimalCarteDto[], serverNow }` (§4), projection de carte publiée réutilisée telle quelle et laissée en dernière étape de la requête ; statut effectif `Adopte` via `Common/Disponibilite` ; tri `adopteLe` décroissant puis `Id` décroissant (§3) ; route déclarée sans entrer en conflit avec `GET /api/animaux/{id:int}` ; page avec la grille partagée (`shared/grille-animaux`), titre « Ils ont trouvé une famille », titre d'onglet « Ils ont trouvé une famille · AAAS » ; état vide propre à la page (phrase courte + lien vers Adopter, pas le message « Catalogue vide ») ; erreur API : même état vide, erreur journalisée.

**Never:** filtres ou pagination sur cette page ; changement de la fiche (déjà conforme pour un Adopté) ; écriture de statut.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Adoptés | 3 Adoptés (`adopteLe` différents), 2 Disponibles, 1 Réservé | `GET /api/animaux/adoptes` → les 3 Adoptés, `adopteLe` décroissant, `statut: "Adopte"` | — |
| Aucun adopté | aucun Adopté | `{ items: [] }` ; page : phrase + lien « Voir les animaux à adopter » | — |
| Page | 2 adoptés | 2 cartes avec pastille Adopté ; chaque carte ouvre `/animaux/:id` | API en échec : état vide, erreur journalisée |
| Démonstration | Development | Simba et Kira affichés | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimaux.cs` -- modèle : requête du statut effectif (`AnimalRules.StatutEffectif`, `ExpressionComposition.Composer`), projection `AnimalCarteDto.Projection(...)` en dernière étape, `CollectionDto`, `serverNow` du même `now`.
- `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- groupe public `/api/animaux` (`""`, `/{id:int}`) : ajouter `/adoptes`.
- `aaas/api/Aaas.Api.Tests/Outils/Animaux.cs` (`Adopte(adopteLe)` via `ForcerStatut`), `CatalogueTests.cs` -- modèles de tests.
- `aaas/web/src/app/features/animaux/animaux.routes.ts` (`ADOPTES_ROUTES` = page vide) ; `features/animaux/catalogue/` (modèle de page) ; `shared/grille-animaux/` ; `core/animaux.service.ts` ; `testing/animaux.ts`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAdoptes.cs`, `AnimauxEndpoints.cs` -- requête et route `GET /api/animaux/adoptes`.
- [x] `aaas/api/Aaas.Api.Tests/AdoptesTests.cs` -- chaque ligne API de la matrice (seuls les Adoptés, tri, liste vide) et non-régression de `GET /api/animaux/{id}`.
- [x] `aaas/web/src/app/core/animaux.service.ts` -- `listerAdoptes()`.
- [x] `aaas/web/src/app/features/animaux/adoptes/`, `animaux.routes.ts` -- page avec grille partagée, état vide propre, titres.
- [x] Tests Vitest -- service et page (cartes, lien vers la fiche, état vide, échec API).

**Acceptance Criteria:**
- Given `dotnet test`, `npx ng test --watch=false`, `dotnet build` et `npx ng build`, then tout passe sans avertissement.

## Implementation Notes

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Test de démonstration : noms triés avant comparaison, l ordre réel n est pas vérifié | low | patch | Corrigé : attend `["Simba", "Kira"]` dans l ordre de l API. `dotnet test` 291 réussis + 1 ignoré. |
| 2 | Commentaire de `grille-animaux.ts` : ne cite pas la nouvelle page | low | patch | Corrigé. |
| 3 | Test « pas de filtre ni de pagination » ne détecterait pas un lien de pagination | low | rejected | Le test du service vérifie déjà que l appel part sans paramètre ; aucun défaut actuel. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.
