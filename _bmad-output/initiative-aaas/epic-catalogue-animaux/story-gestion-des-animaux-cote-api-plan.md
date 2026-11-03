---
title: 'Gestion des animaux côté API'
type: 'feature'
ticket: '9'
created: '2026-11-03'
status: 'built'
baseline_revision: '68562fdb1d44bc6b2fae9e659f78222317c20b91'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** L'admin ne peut ni lister tous les animaux, ni en créer, modifier ou supprimer par l'API : seuls les animaux de démonstration et l'ajout de photos existent (CAP-7), et les écrans admin (entrées 11 à 13) n'ont rien à appeler.

**Approach:** Ajouter au groupe admin `/api/admin/animaux` la liste (tous statuts), la fiche, la création (statut Disponible), la modification sans statut et la suppression, refusée en 409 `animal-a-des-reservations` dès qu'une réservation existe, les fichiers des photos étant supprimés après le commit.

## Boundaries & Constraints

**Always:** routes et réponses de `contrats-api.md` §4 : `GET ""` → 200 `{ items: AnimalCarteDto[], serverNow }` tous statuts, `creeLe` décroissant puis `Id` décroissant ; `GET /{id:int}` → 200 `AnimalDetailDto` ; `POST ""` → 201 `AnimalDetailDto` statut `Disponible` ; `PUT /{id:int}` → 200 `AnimalDetailDto` ; `DELETE /{id:int}` → 204 ; id inconnu → 404 `{ error }`. Groupe `MapGroupeAdmin` (anonyme 401, Membre 403). `AnimalEcritureDto` = exactement les 12 champs du §6, jamais de statut (un champ `statut` envoyé est ignoré). Validation par le handler, `errors.<champ>` en camelCase : `nom` (trimé, requis, ≤ 50), `description` (trimée, requise, ≤ 2000), énumérations et booléens requis (absents → `errors.<champ>`) ; valeur d'énumération inconnue ou de casse différente → 400 (convertisseur global). Réponses construites par les projections publiées (`AnimalCarteDto.Projection`, `AnimalDetailDto.Projection`), en dernière étape, statut effectif. Écritures par `TransactionRunner`. Conflit `RowVersion` à l'enregistrement d'une modification ou d'une suppression → 409 `modifie-ailleurs`. Suppression : refusée en 409 `animal-a-des-reservations` si au moins une réservation existe, quel que soit son statut ; sinon lignes de photos supprimées en cascade et fichiers supprimés via `IFileStorage` après le commit (échec journalisé, la suppression reste faite).

**Never:** forcer un statut (entrée 10) ; toucher aux routes de photos existantes, à `Animal.Statut` ou à ses transitions ; écrans admin (entrées 11 à 13) ; pagination ou filtres sur la liste admin ; nouvelle migration (aucun changement de schéma).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Liste | 1 Disponible, 1 Réservé actif, 1 Adopté | les 3, `creeLe` décroissant, statuts effectifs | — |
| Création | DTO valide (nom « Caramel »), `statut: "Adopte"` en trop | 201, `statut: "Disponible"`, `photos: []`, `creeLe` = maintenant ; visible au catalogue | — |
| Création invalide | nom vide, description de 2001 caractères, `sexe` absent | 400, `errors.nom`, `errors.description`, `errors.sexe` ; rien en base | — |
| Énumération invalide | `espece: "chat"` | 400 | — |
| Modification | PUT sur un animal Adopté | 200, champs mis à jour, statut et `adopteLe` inchangés | — |
| Inconnu | GET, PUT, DELETE `/api/admin/animaux/999999` | 404 `{ error }` | — |
| Suppression | animal avec 2 photos, sans réservation | 204 ; animal et photos absents ; fichiers supprimés ; `GET /fichiers/<clé>` → 404 | échec de suppression du fichier : journalisé, 204 |
| Réservations | animal avec une réservation Annulée (ou active) | 409 `animal-a-des-reservations`, rien supprimé | — |
| Accès | Membre / anonyme sur chaque route | 403 / 401 | — |
| Concurrence | `RowVersion` modifié entre lecture et enregistrement (PUT ou DELETE) | 409 `modifie-ailleurs` | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- groupe `admin` (`MapGroupeAdmin`) qui porte déjà les routes photos ; y ajouter les 5 routes. Corps JSON lié comme `InscriptionDto` (`Features/Auth/AuthEndpoints.cs:15`, record à propriétés nullables, tout validé par le handler).
- `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAdoptes.cs` -- modèle de requête de liste (statut effectif, projection en dernier, `CollectionDto(items, now)`). `ObtenirAnimal.cs` -- réutilisable tel quel pour `GET /api/admin/animaux/{id}` (même fiche, tout statut).
- `aaas/api/Aaas.Api/Features/Animaux/Commands/SupprimerPhoto.cs` -- modèle de suppression : ligne dans la transaction, fichier après le commit, échec journalisé. `AjouterPhoto.cs` -- usage de `TransactionRunner` avec `Result<T>`.
- `aaas/api/Aaas.Api/Features/Auth/Commands/Inscrire.cs:25-53` -- style de validation (`Dictionary` d'erreurs, messages en français, `Result.Invalid(errors)`).
- `aaas/api/Aaas.Api/Features/Animaux/Animal.cs` -- `NomMax` 50, `DescriptionMax` 2000, setters publics des champs d'édition, `RowVersion` ; `PhotoAnimal` en `Cascade`, `Reservation` en `Restrict` (`AnimalConfiguration`, `Features/Reservations`).
- `aaas/api/Aaas.Api/Common/Data/TransactionRunner.cs` -- ne capture pas `DbUpdateConcurrencyException` : le handler l'attrape autour de `RunAsync` et renvoie `Result.Conflict("modifie-ailleurs", …)`.
- `aaas/api/Aaas.Api/Common/Result.cs` (`NotFound`, `Conflict`, `Invalid`), `ResultHttpExtensions.cs` -- 201 via `ToHttpResult(StatusCodes.Status201Created)`.
- `aaas/api/Aaas.Api.Tests/Outils/` -- `Animaux.Nouveau/InsererAsync/ViderAnimauxAsync`, `Reservations.CreerMembreAsync/Active/Nouvelle(...).Annulee()`, `Fichiers.EnvoyerPhotoAsync`, `Comptes.ConnecterAsync`, `AssertionsErreur.AssertErreurAsync/AssertValidationAsync` ; `PhotosTests.cs:437-460` -- `SaveChangesInterceptor` injecté par `WithWebHostBuilder` (modèle pour simuler le conflit `RowVersion` et l'échec de suppression de fichier) ; `GroupeAdminTests.cs` -- 401/403.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalEcritureDto.cs` -- record des 12 champs du §6 (nullables) et validation partagée création / modification.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Queries/ListerAnimauxAdmin.cs` -- liste tous statuts, tri `creeLe` puis `Id` décroissants.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Commands/CreerAnimal.cs`, `ModifierAnimal.cs`, `SupprimerAnimal.cs` -- commandes ; réponse relue par `AnimalDetailDto.Projection` après le commit.
- [x] `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- 5 routes admin.
- [x] `aaas/api/Aaas.Api.Tests/AdminAnimauxTests.cs` -- une couverture par ligne de la matrice ; accès Membre/anonyme sur les 5 routes.

**Acceptance Criteria:**
- Given `dotnet build` et `dotnet test` sur `aaas/api/Aaas.sln`, then 0 avertissement et tous les tests passent (un ignoré, comme avant).
- Given un animal créé par `POST /api/admin/animaux`, when on appelle `GET /api/animaux`, then il y apparaît avec le statut Disponible.

## Implementation Notes

- `AnimalEcritureDto.Valider()` / `Appliquer(Animal)` portent la validation partagée ; les refus 409 sont dans `RefusAnimal` (`Commands/ModifierAnimal.cs`).
- Création et modification relisent la fiche via `ObtenirAnimalHandler` (projection `AnimalDetailDto.Projection`) après le commit.
- Ajout hors carte du code : `SqlErreurs.EstViolationCleEtrangere` (SQL 547) ; la suppression traduit une violation de la clé étrangère `Restrict` (réservation créée entre la vérification et le `DELETE`) en 409 `animal-a-des-reservations`.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `SupprimerAnimal` en ReadCommitted : une photo validée par `AjouterPhoto` entre la lecture des photos et la suppression est supprimée en cascade, mais son fichier reste sur le disque sans journal | low | patch | Corrigé : transaction en `Serializable` (comme `AjouterPhoto`, interblocage rejoué par le runner). Effet : le test RowVersion du DELETE modifie la ligne dans la même transaction (un écrivain concurrent attend désormais le verrou). |
| 2 | Repli SQL 547 (réservation insérée entre la vérification et la suppression) sans test | low | patch | Corrigé : test `Reservation_inseree_entre_la_verification_et_la_suppression_renvoie_409` (intercepteur, insertion dans la transaction du handler). |
| 3 | `PUT` d'un id inconnu avec un corps invalide → 400 au lieu de 404 | low | rejected | Validation avant lecture, comme l'inscription ; la matrice teste l'id inconnu avec un corps valide ; le formulaire admin n'envoie pas de corps invalide vers un animal supprimé. |

## Design Notes

Description requise : le formulaire admin d'EXPERIENCE.md (§ ajout d'un animal) la fait remplir, la colonne est non nulle et la fiche l'affiche toujours. Un `PUT` ne porte pas de `RowVersion` (contrat §6) : `modifie-ailleurs` ne couvre que le conflit survenu pendant la transaction (forçage de statut ou suppression concurrents).

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
