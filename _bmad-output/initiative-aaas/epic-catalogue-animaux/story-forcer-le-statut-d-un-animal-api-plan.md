---
title: "Forcer le statut d'un animal (API)"
type: 'feature'
ticket: '10'
created: '2026-11-03'
status: 'built'
baseline_revision: 'eb6bae04c1ae53af8ca0da55e0667323b5b9cc1f'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Hors du parcours de réservation, l'admin ne peut ni marquer un animal Adopté (adoption conclue en direct) ni le remettre Disponible (erreur, retour d'adoption) : `Animal.ForcerStatut` existe mais aucune route ne l'appelle (CAP-7, AD-4, AD-18).

**Approach:** Ajouter `POST /api/admin/animaux/{id}/statut` `{ statut, statutAttendu }`, qui appelle `Animal.ForcerStatut` après avoir vérifié que le statut effectif vaut toujours `statutAttendu` et qu'aucune réservation n'est active, et renvoie la fiche à jour.

## Boundaries & Constraints

**Always:** route du groupe admin (anonyme 401, Membre 403) ; corps `StatutAnimalDto` (§6) `{ statut, statutAttendu }`, propriétés nullables validées par le handler : `statut` absent ou `Reserve` → 400 `errors.statut` ; `statutAttendu` absent → 400 `errors.statutAttendu` (les trois valeurs y sont permises) ; valeur inconnue ou de casse différente → 400 (convertisseur global). Ordre des refus : 400, puis 404 `{ error }` (animal inconnu), puis 409 `modifie-ailleurs` si le statut effectif (AD-5, `AnimalRules.StatutEffectif` au même `now`) diffère de `statutAttendu`, puis 409 `reservation-active` si une réservation de l'animal est active (`ReservationRules.EstActive`). Transition uniquement par `Animal.ForcerStatut` (forcer Adopté pose `AdopteLe`, un Adopté forcé de nouveau garde sa date ; forcer Disponible l'efface). Écriture par `TransactionRunner` ; conflit `RowVersion` à l'enregistrement → 409 `modifie-ailleurs` (`RefusAnimal`). Succès → 200 `AnimalDetailDto` relue par la projection publiée après le commit.

**Never:** modifier `Animal.ForcerStatut`, les transitions ou le schéma (aucune migration) ; annuler, expirer ou modifier une réservation (epic réservations) ; écran admin (entrée 13).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Adopter | Disponible ; `{ Adopte, Disponible }` | 200, `statut: "Adopte"`, `adopteLe` = maintenant ; dans `/api/animaux/adoptes`, absent du catalogue | — |
| Remettre disponible | Adopté ; `{ Disponible, Adopte }` | 200, `statut: "Disponible"`, `adopteLe: null` | — |
| Adopté de nouveau | Adopté ; `{ Adopte, Adopte }` | 200, `adopteLe` inchangée | — |
| Réservé échu | stocké Réservé, réservation EnAttente échue ; `{ Adopte, Disponible }` | 200, Adopté | — |
| Réservé | `{ Reserve, Disponible }` | 400 `errors.statut` ; rien modifié | — |
| Corps incomplet | `{}` | 400 `errors.statut` et `errors.statutAttendu` | — |
| Attendu périmé | Adopté ; `{ Disponible, Disponible }` | 409 `modifie-ailleurs` ; rien modifié | — |
| Réservation active | réservation active ; `{ Adopte, Reserve }` | 409 `reservation-active` ; rien modifié | — |
| Inconnu | id 999999, corps valide | 404 `{ error }` | — |
| Concurrence | `RowVersion` modifié entre lecture et enregistrement | 409 `modifie-ailleurs` | — |
| Accès | Membre / anonyme | 403 / 401 | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/Animal.cs` -- `ForcerStatut(StatutAnimal, TimeProvider)` : lève `TransitionInterditeException` pour `Reserve` (la validation 400 l'en empêche) ; à ne pas modifier.
- `aaas/api/Aaas.Api/Features/Animaux/Commands/ModifierAnimal.cs` -- modèle exact à suivre : validation, `TransactionRunner.RunAsync(ReadCommitted)`, `catch (DbUpdateConcurrencyException)` → `RefusAnimal.ModifieAilleurs()`, puis fiche relue par `IQueryHandler<ObtenirAnimalQuery, AnimalDetailDto>`. Y ajouter à `RefusAnimal` le code `reservation-active` (message en français).
- `aaas/api/Aaas.Api/Common/Disponibilite/AnimalRules.cs`, `ReservationRules.cs` -- statut effectif et réservation active, traduits en SQL ; les évaluer en base dans la transaction (pas en mémoire), au même `now` (`TimeProvider`).
- `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalEcritureDto.cs` -- style du DTO nullable et du message `MessageChoix`.
- `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- groupe `admin` ; ajouter `POST /{id:int}/statut`.
- `aaas/api/Aaas.Api.Tests/AdminAnimauxTests.cs` -- `EnvoyerAsync`, `LireAsync`, `AnimalEnBaseAsync`, `ModificationConcurrente` (intercepteur RowVersion depuis une autre connexion, cas PUT en ReadCommitted) ; `Outils/Animaux.cs` (`Adopte()`, `Reserve()`), `Outils/Reservations.cs` (`CreerMembreAsync`, `Active`, `Echue`).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/StatutAnimalDto.cs` -- record `{ Statut?, StatutAttendu? }` et sa validation.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Commands/ForcerStatut.cs` -- commande et handler ; `RefusAnimal.ReservationActive()` dans `ModifierAnimal.cs`.
- [x] `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs` -- route.
- [x] `aaas/api/Aaas.Api.Tests/ForcerStatutTests.cs` -- une couverture par ligne de la matrice.

**Acceptance Criteria:**
- Given `dotnet build` et `dotnet test` sur `aaas/api/Aaas.sln`, then 0 avertissement et tous les tests passent (un ignoré, comme avant).

## Implementation Notes

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Animal supprimé entre le chargement suivi et la requête du statut effectif : `SingleAsync` lève, réponse 500 | low | patch | Corrigé : `SingleOrDefaultAsync` sur le statut rendu nullable (`Composer`), ligne absente → 404. Vérifié par lecture (course non reproduite en test). |
| 2 | Forcer le statut déjà en place n'émet aucun UPDATE : pas de contrôle `RowVersion` | low | rejected | Aucune donnée modifiée et la fiche renvoyée montre l'état réel ; l'empêcher demanderait un verrou ou un `RowVersion` forcé, sans gain pour l'admin. |
| 3 | Intercepteur `ModificationConcurrente` recopié au lieu d'être réutilisé (Code Map) | low | patch | Corrigé : une seule classe dans `Aaas.Api.Tests/Outils/ModificationConcurrente.cs`, utilisée par `AdminAnimauxTests` et `ForcerStatutTests`. |

## Design Notes

`modifie-ailleurs` est vérifié avant `reservation-active` : un admin qui voit « Disponible » alors qu'une réservation vient d'arriver reçoit « rechargez », puis, après rechargement (`statutAttendu: Reserve`), le refus explicite. Forcer un Réservé échu laisse sa réservation `EnAttente` échue telle quelle : le job d'expiration (epic réservations) devra tolérer un animal qui n'est plus Réservé au lieu d'appeler `Liberer()` aveuglément.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
