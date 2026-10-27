---
title: 'Animal, transitions et derniers pensionnaires'
type: 'feature'
ticket: '2'
created: '2026-10-27'
status: 'built'
baseline_revision: '99884028f0622deb3fb9b3eb2f5438e8298a0e91'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md'
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Aucun animal n'existe dans l'application : pas d'entité, pas de route, pas de carte ; l'accueil n'a qu'un titre « Nos derniers pensionnaires » vide, et les stories suivantes de l'epic n'ont rien sur quoi s'appuyer.

**Approach:** Tracer bullet de l'epic : entité `Animal` (slice `Features/Animaux`) avec ses quatre transitions AD-4 et sa migration, `AnimalCarteDto` et sa projection publiés dans `Contrats/`, `GET /api/animaux?limite=` en `IQueryHandler`, puis côté site les libellés, la carte animal et la pastille de statut partagées, affichées dans la section de l'accueil.

## Boundaries & Constraints

**Always:** énumérations et valeurs exactes de `contrats-api.md` §1, stockées en chaîne, sérialisées par le convertisseur global ; `Animal.Statut` à setter privé, modifié seulement par `Reserver()` (Disponible→Reserve), `Liberer()` (Reserve→Disponible), `MarquerAdopte()` (Reserve→Adopte, pose `AdopteLe`) et `ForcerStatut(statut)` (Disponible ou Adopte seulement ; Adopte pose `AdopteLe`, Disponible l'efface), qui lèvent `TransitionInterditeException` hors de leur état source ; `CreeLe` et `AdopteLe` par `TimeProvider` ; `RowVersion` ; noms du glossaire §6 (`AnimalCarteDto` : `id, nom, espece, sexe, age, taille, statut, photoPrincipaleUrl`) ; collection `{ items, serverNow }` ; requête `AsNoTracking` projetée en DTO ; libellés uniquement dans `shared/libelles.ts` ; jetons CSS de DESIGN.md seulement.

**Never:** statut effectif ni entité Réservation (entrée 4) ; filtres de catalogue (entrée 5) ; photos et `IFileStorage` (entrée 6 : `photoPrincipaleUrl` vaut `null` ici) ; fiche `/animaux/:id` (entrée 8, la carte y pointe déjà) ; routes admin ; données de démonstration (entrée 3).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Derniers pensionnaires | 6 animaux non adoptés | `GET /api/animaux?limite=4` → 4 cartes, `creeLe` décroissant, `{ items, serverNow }` | — |
| Adoptés exclus | un animal `Adopte` | absent de `GET /api/animaux` | — |
| Sans limite | `GET /api/animaux` | tous les non adoptés | — |
| Limite invalide | `limite=0`, `51`, `abc` | 400 `errors.limite` | — |
| Transition interdite | `Liberer()` sur Disponible, `ForcerStatut(Reserve)` | `TransitionInterditeException` | — |
| Accueil avec animaux | API renvoie 4 cartes | 4 cartes : nom, « Espèce · âge · sexe », pastille de statut, dégradé (pas de photo), lien `/animaux/:id` ; Réservé : photo désaturée | — |
| Accueil vide | `items: []` | message d'état vide, lien « Voir tous les animaux → » conservé | API en échec : même état vide, erreur journalisée |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Auth/` (`User.cs`, `UserConfiguration.cs`, `Queries/ObtenirMoi.cs`, `Contrats/UtilisateurDto.cs` avec `Projection`) -- modèle de slice : entité + `IEntityTypeConfiguration`, handler CQRS, projection `Expression` réutilisable.
- `aaas/api/Aaas.Api/Features/Site/SiteEndpoints.cs` -- modèle d'endpoint public + `ToHttpResult()`.
- `aaas/api/Aaas.Api/Data/AppDbContext.cs` -- conventions (enum → chaîne, `DateTimeOffset` UTC, `DateTime` interdit) ; migration à nommer `Animaux_Animal` (`dotnet ef migrations add`, manifeste `aaas/api/.config/dotnet-tools.json`).
- `aaas/api/Aaas.Api/Common/` -- `Result`, `IQueryHandler`, `ApiErrors`, convertisseurs JSON : à réutiliser.
- `aaas/api/Aaas.Api/Program.cs` -- ajouter `MapAnimauxEndpoints()` avant `MapApiFallback()`.
- `aaas/api/Aaas.Api.Tests/Outils/` (helpers de la story 2.1 : `RequetesApi`, `AssertionsErreur`, `HoteDeTest`) -- y ajouter le jeu de données de test « animal » partagé (`Outils/Animaux.cs`).
- `aaas/web/src/app/features/accueil/accueil.html:32-36` -- section et commentaire d'emplacement à remplacer ; `accueil.ts` ; `core/site.service.ts` (modèle de chargement et d'échec).
- `aaas/web/src/app/shared/` (`telephone.pipe`, `bouton-appel`) et `src/styles.css` (jetons, `--shadow`, `--rounded-xl`, dégradé `--color-photo-placeholder-*`) ; `src/app/testing/` (fixtures front).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Animal.cs`, `Enumerations.cs`, `AnimalConfiguration.cs`, `TransitionInterditeException.cs` -- entité, énums §1 (`Espece`, `Sexe`, `TrancheAge`, `Taille`, `Entente`, `StatutAnimal`), longueurs (`Nom` 50, `Description` 2000), transitions AD-4.
- [x] `aaas/api/Aaas.Api/Data/Migrations/*_Animaux_Animal.cs` -- migration générée.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalCarteDto.cs` -- DTO + projection `Expression` publiée (seul format de carte, AD-1).
- [x] `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimaux.cs`, `AnimauxEndpoints.cs`, `Program.cs` -- `GET /api/animaux?limite=` (1..50), Adoptés exclus, tri `creeLe` décroissant, `serverNow` via `TimeProvider`.
- [x] `aaas/api/Aaas.Api.Tests/Outils/Animaux.cs`, `AnimauxTests.cs`, `AnimalTests.cs` -- fabrique d'animaux de test partagée ; tests de la matrice (API) et des transitions (unitaires).
- [x] `aaas/web/src/app/core/api/animal.dto.ts`, `core/animaux.service.ts` -- interfaces §6 et appel `GET /api/animaux?limite=`.
- [x] `aaas/web/src/app/shared/libelles.ts`, `shared/pastille-statut/`, `shared/carte-animal/` -- libellés de §1, pastille de statut (animal), carte animal conforme à DESIGN.md (désaturation Réservé, dégradé sans photo, lien `/animaux/:id`).
- [x] `aaas/web/src/app/features/accueil/` -- grille des 4 derniers pensionnaires (2 colonnes téléphone, 3 tablette, 4 ordinateur), état vide ; fixtures front d'animaux dans `src/app/testing/animaux.ts` ; tests Vitest de la carte, de la pastille et de l'accueil.

**Acceptance Criteria:**
- Given `dotnet test` et `npx ng test --watch=false`, then tout passe, y compris les nouveaux tests.
- Given `dotnet build` et `npx ng build`, then aucun avertissement.
- Given le code de `src/app`, when on cherche une couleur hex ou un libellé de statut en dur hors de `styles.css` et `shared/libelles.ts`, then aucune occurrence.

## Implementation Notes

- `ForcerStatut(statut, temps)` refuse seulement la cible `Reserve` ; aucun état source exigé, car un `Reserve` stocké dont la réservation est échue est effectivement Disponible (AD-5) et le refus `reservation-active` appartient à la commande admin. `MarquerAdopte` et `ForcerStatut` reçoivent le `TimeProvider` ; le constructeur `Animal(TimeProvider)` pose `CreeLe`.
- Enveloppe `{ items, serverNow }` : `Common/CollectionDto<T>` (générique, sans règle métier), réutilisable par les autres collections.
- `limite` est lié en texte et validé par le handler (entier sans signe, 1..50) : `abc`, `-1`, `2.5` ou vide → 400 `errors.limite` (et non un 400 de liaison sans `errors`). Tri secondaire `Id` décroissant pour un ordre stable à `CreeLe` égal. Index `(Statut, CreeLe)`.
- Jeu de test `Outils/Animaux.cs` : `CreeLe` fixé par un `FakeTimeProvider` propre à l'animal (l'horloge partagée de la factory n'est pas avancée) ; `AnimauxTests` vide la table avant chaque test (base partagée par la collection).
- Front : `libelles.ts` couvre toutes les énumérations de §1 ; `Logement` et `StatutReservation` sont déclarés dans `core/api/reservation.dto.ts` (sans DTO de réservation). Jetons ajoutés à `styles.css` : `--size-card-photo(-desktop)`, `--filter-photo-reserve`. État vide : texte de EXPERIENCE.md (« Tous nos protégés… ») avec lien vers Ils ont trouvé une famille, en plus de « Voir tous les animaux → ».
- `app.routes.spec.ts` : l'`afterEach` répond `{ items: [] }` à `GET /api/animaux?limite=4`, désormais émis par toute navigation vers l'accueil.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 0, false 1, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `Animal.ForcerStatut(Adopte)` sur un animal déjà Adopté écrase `AdopteLe` | medium | patch | Aucun garde sur l'état courant : un admin qui renvoie « Adopté » (entrée 13, statutAttendu = Adopte) remonterait l'animal en tête de « Ils ont trouvé une famille » avec une fausse date (tri `adopteLe`, §3). Correctif : garder `AdopteLe` quand l'animal est déjà Adopté ; test ajouté. |
| 2 | Critère « aucun libellé de statut en dur » : littéraux dans des specs, une fixture et deux commentaires | false | rejected | Le critère vise l'affichage : le code de production n'a aucun libellé hors `shared/libelles.ts`. Les specs comparent au texte réellement affiché, ce qui est le rôle d'une assertion ; les commentaires ne sont pas rendus. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement ni dépassement de budget.
