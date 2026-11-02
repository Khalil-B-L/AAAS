---
title: 'Fiche animal'
type: 'feature'
ticket: '8'
created: '2026-11-02'
status: 'built'
baseline_revision: 'b1a1e6a843f3682ce5894cae70beff7d803d34e3'
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

**Problem:** Toucher une carte animal ouvre une page vide : un visiteur ne peut voir ni les photos, ni la description, ni la santé, ni les ententes, ni le statut réel d'un animal (CAP-3), et l'admin (entrée 9) n'a pas de DTO de détail à réutiliser.

**Approach:** Publier `AnimalDetailDto` et sa projection dans `Contrats/`, exposer `GET /api/animaux/:id` (tout statut, statut effectif), puis construire la page `/animaux/:id` conforme à DESIGN.md et EXPERIENCE.md : galerie, caractéristiques, santé, ententes, retour vers le catalogue de l'espèce, états Réservé et Adopté, et emplacements laissés vides pour le bouton de réservation (epic réservations) et le cœur favori (epic contenus).

## Boundaries & Constraints

**Always:** `AnimalDetailDto` = champs de la carte (`id`, `nom`, `espece`, `sexe`, `age`, `taille`, `statut` effectif, `photoPrincipaleUrl`) + `description`, `vaccine`, `sterilise`, `puce`, `ententeEnfants`, `ententeChiens`, `ententeChats`, `creeLe`, `adopteLe` (`null` sauf Adopté), `photos` (`PhotoDto[]` triées par `ordre`, URLs relatives) — DTO plat, seule imbrication `PhotoDto` (§6) ; une seule projection, publiée dans `Contrats/`, réutilisée telle quelle par l'entrée 9 ; statut effectif par `Common/Disponibilite` (jamais recalculé) ; `GET /api/animaux/:id` renvoie aussi un animal Adopté ; id inconnu → 404 `{ error }` ; dates par `parseUtc` et le pipe de date partagé ; libellés depuis `shared/libelles.ts`, accordés au sexe pour la santé (« Vaccinée » / « Vacciné », « Non stérilisée »…) ; pastille de statut partagée ; jetons CSS de DESIGN.md ; mobile-first, fiche en deux colonnes (galerie / informations) à partir de 1024 px ; accessibilité de EXPERIENCE.md (texte alternatif « Photo n de [prénom] », vignettes et points actionnables au clavier et ≥ 48 px de cible, ententes et santé lisibles sans la couleur).

**Never:** bouton « Je veux l'adopter » actif, questionnaire ou invitation à se connecter (epic réservations : seul un emplacement vide est posé pour un animal Disponible) ; cœur favori fonctionnel (epic contenus : emplacement vide) ; routes admin ; page des adoptés (entrée 14).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Fiche Disponible | `GET /api/animaux/:id` d'un animal avec 3 photos | 200 `AnimalDetailDto`, `photos` triées par `ordre`, `adopteLe: null` ; page : galerie « 1 / 3 », puces, santé, ententes, pastille Disponible, emplacement d'action vide | — |
| Réservé effectif | animal Reserve avec réservation active | `statut: "Reserve"` ; page : bouton « Déjà réservé » désactivé | — |
| Réservé expiré | animal Reserve sans réservation active | `statut: "Disponible"` | — |
| Adopté | animal Adopté | 200, `adopteLe` renseigné ; page : pastille Adopté, mention « A trouvé une famille », aucun bouton | — |
| Sans photo | animal sans photo | `photos: []`, `photoPrincipaleUrl: null` ; page : dégradé de remplacement, pas de compteur | — |
| Inconnu | `GET /api/animaux/999999` ou `/animaux/abc` | API 404 `{ error }` ; page : message « animal introuvable » + lien vers Adopter | — |
| Galerie | clic sur la 3e vignette, ou point 2, ou balayage | photo affichée change, compteur à jour, vignette active cerclée | — |
| Retour | chien | lien « ← Les chiens » vers `/adopter?espece=Chien` | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalCarteDto.cs` -- modèle de projection : statut effectif via `AnimalRules.StatutEffectif` + `ExpressionComposition.Composer`, URL de photo construite en mémoire via `IFileStorage` (la projection doit rester la dernière étape de la requête, cf. Implementation Notes de la story 2.6) ; `Contrats/PhotoDto.cs`.
- `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimaux.cs`, `AnimauxEndpoints.cs` (groupe public `/api/animaux`) -- y ajouter `ObtenirAnimal` et `GET /{id:int}`.
- `aaas/api/Aaas.Api/Features/Animaux/Animal.cs` (`Photos`), `PhotoAnimal.cs` -- données.
- `aaas/api/Aaas.Api.Tests/` -- `Outils/Animaux.cs`, `Outils/Reservations.cs`, `Outils/Fichiers.cs` (envoi de photo de test), `CatalogueTests.cs`, `PhotosTests.cs` -- harnais à réutiliser.
- `aaas/web/src/app/features/animaux/animaux.routes.ts` (`FICHE_ROUTES` : `:id` = page vide) ; `features/animaux/catalogue/` (style de page) ; `core/animaux.service.ts`, `core/api/animal.dto.ts` ; `shared/carte-animal/` (lien `/animaux/:id`, dégradé de remplacement), `shared/pastille-statut/`, `shared/libelles.ts`, `shared/date-fr.pipe.ts`, `core/parse-utc.ts` ; `testing/animaux.ts`.
- `_bmad-output/initiative-aaas/ux-aaas-adoption/mockups/ecrans-cles.html` §1 -- maquette de la fiche (DESIGN.md prime sur ses données fictives).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalDetailDto.cs` -- DTO et projection publiée (photos triées, statut effectif, `adopteLe`).
- [x] `aaas/api/Aaas.Api/Features/Animaux/Queries/ObtenirAnimal.cs`, `AnimauxEndpoints.cs` -- `GET /api/animaux/{id:int}`, 404 si absent.
- [x] `aaas/api/Aaas.Api.Tests/FicheAnimalTests.cs` -- chaque ligne API de la matrice.
- [x] `aaas/web/src/app/core/api/animal.dto.ts`, `core/animaux.service.ts` -- `AnimalDetailDto`, `PhotoDto`, `obtenir(id)`.
- [x] `aaas/web/src/app/shared/libelles.ts` -- libellés de santé accordés au sexe.
- [x] `aaas/web/src/app/features/animaux/fiche/` (`fiche.ts/html/css`, `galerie/`) et `animaux.routes.ts` -- page `/animaux/:id`, titre de page « [Nom] · AAAS », galerie (photo, compteur, points, vignettes, balayage), puces, cases santé, lignes d'entente, retour vers l'espèce, emplacements vides (action, favori), états Réservé / Adopté / introuvable.
- [x] Tests Vitest -- service, galerie (clic vignette, point, compteur, texte alternatif), fiche (Disponible, Réservé, Adopté, sans photo, introuvable, retour).

**Acceptance Criteria:**
- Given `dotnet test`, `npx ng test --watch=false`, `dotnet build` et `npx ng build`, then tout passe sans avertissement ni dépassement de budget.
- Given l'API en Development avec les animaux de démonstration, when on ouvre la carte de Rex depuis l'accueil, then la fiche montre son illustration, ses caractéristiques, sa santé, ses ententes et « ← Les chiens » (vérification humaine facultative).

## Implementation Notes

- Projection `AnimalDetailDto.Projection` : même modèle que la carte (statut effectif composé, clés lues en SQL, URLs construites côté client, y compris dans la sous-collection `photos`) ; `adopteLe` n'est renvoyée que si le statut effectif est Adopté.
- La route `:id` de `FICHE_ROUTES` ne déclare pas de `title` : la fiche pose « Fiche animal · AAAS » pendant le chargement, puis « [Nom] · AAAS » ou « Animal introuvable · AAAS », sans être écrasée par la stratégie de titre du routeur. La ligne `/animaux/7` du tableau des pages de `app.routes.spec.ts` est remplacée par un test dédié.
- Un id non entier, nul ou hors `int` (`abc`, `0`, `-3`, `1.5`, `99999999999`) donne « Animal introuvable » sans appel à l'API ; un échec autre que 404 affiche « Fiche indisponible » (alerte) avec le lien vers Adopter.
- Emplacements vides : `<ng-content>` en haut à droite de la galerie (cœur favori) et bloc `.action` vide pour un animal Disponible (bouton de réservation).
- Nouveaux jetons dans `styles.css` (tailles de galerie, vignette, points, favori, puces, marque santé, mention) ; libellés `LIBELLES_PUCE_TAILLE`, `CRITERES_SANTE`/`LIBELLES_SANTE`/`libelleSante`, `LIBELLES_TITRE_ENTENTES` dans `shared/libelles.ts`.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 2, low 1, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Vignette active : la règle `.vignette[aria-current='true']` (spécificité 0,2,0) écrase l'anneau de focus global ; anneaux des autres vignettes rognés par `overflow-x: auto` (padding 4 px < 5 px) | medium | patch | Contredit « vignettes actionnables au clavier » (focus visible, EXPERIENCE.md). Corriger pour que le focus reste visible sur la vignette active et ne soit pas rogné. |
| 2 | Balayage : un défilement vertical avec dérive horizontale ≥ seuil change de photo | medium | patch | `terminerBalayage` ne compare que les X ; sur téléphone, faire défiler la page sur la photo changerait la photo. Ne considérer qu'un geste principalement horizontal ; test du cas vertical. |
| 3 | « Les chats » / « Les chiens » codés en dur dans `fiche.ts`, repli implicite sur Chat | low | patch | Contredit « libellés depuis `shared/libelles.ts` ». Déplacer dans `libelles.ts` en `Record<Espece, string>`. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.
