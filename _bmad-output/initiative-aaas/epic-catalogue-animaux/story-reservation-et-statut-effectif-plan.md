---
title: 'Réservation et statut effectif'
type: 'feature'
ticket: '4'
created: '2026-10-27'
status: 'built'
baseline_revision: '929bbebb6fade1816beb5a31629012d7ebc0a26d'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md'
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
  - '{project-root}/_bmad-output/initiative-aaas/spec-aaas-adoption/state-machines.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Le statut stocké d'un animal ment dès qu'une réservation expire : un animal `Reserve` dont la réservation a passé ses 15 minutes doit apparaître Disponible partout, à la seconde (AD-5), et l'entité Réservation dont dépendent le catalogue, la fiche, l'admin et l'epic réservations n'existe pas encore.

**Approach:** Ajouter l'entité `Reservation` (slice `Features/Reservations`) et sa migration, puis les prédicats uniques du statut effectif dans `Common/Disponibilite/`, sans dépendance de slice et traduisibles en SQL, et faire projeter le statut effectif par `GET /api/animaux` (Adoptés effectifs exclus) ; un jeu de données de test « réservation » partagé sert ces tests et les stories suivantes.

## Boundaries & Constraints

**Always:** `Reservation` : `AnimalId`, `UserId`, `Statut` (`StatutReservation` : `EnAttente`, `Confirmee`, `Annulee`, `Expiree`), `CreeLe`, `ExpireLe`, `Logement` (`Appartement`, `Maison`), `Jardin`, `Experience` (texte facultatif), `RowVersion` ; suppression `Restrict` vers `Animal` et vers `User` (AD-7) ; index unique filtré sur `AnimalId` où `Statut = 'EnAttente'` (AD-6) ; énumérations en chaîne. Prédicats AD-5 uniques : `ReservationRules.EstActive(now)` (`EnAttente` et `ExpireLe > now`), `ReservationRules.StatutEffectif(now)` (`EnAttente` échue → `Expiree`), `AnimalRules.StatutEffectif(now)` (`Reserve` sans réservation active → `Disponible`, sinon statut stocké) ; ils s'expriment en `Expression<…>` traduites par EF Core en SQL (jamais évaluées en mémoire) ; `Common/` ne référence aucun namespace `Features` (les types qu'ils manipulent — énumérations de statut et interfaces minimales des entités — vivent dans `Common/Disponibilite/` ; déplacer `StatutAnimal` y est permis). `now` vient toujours de `TimeProvider`. `GET /api/animaux` : statut effectif dans la carte, Adoptés effectifs exclus, tri et `limite` inchangés.

**Never:** créer, confirmer, annuler ou expirer une réservation par l'API (epic réservations) ; `ReservationExpiration` et le `BackgroundService` ; écrire `Animal.Statut` hors des méthodes de l'entité ; évaluation côté client d'un prédicat ; filtres de catalogue (entrée 5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Réservation active | animal `Reserve`, réservation `EnAttente`, `ExpireLe` = now + 5 min | carte `statut: "Reserve"` ; `EstActive` vrai | — |
| Réservation échue | même animal, horloge avancée de 15 min (`ExpireLe` ≤ now) | carte `statut: "Disponible"` ; réservation effective `Expiree` | — |
| Réservé sans réservation | animal `Reserve`, aucune réservation | carte `statut: "Disponible"` | — |
| Confirmée / annulée | réservation `Confirmee` ou `Annulee` | jamais active ; statut effectif = statut stocké | — |
| Adopté | animal `Adopte` | absent de `GET /api/animaux` | — |
| Deux réservations EnAttente sur un animal | insertion de la seconde | refusée par la base (index unique filtré) | exception d'unicité (2601/2627) |
| Suppression bloquée | supprimer un animal ou un compte qui a une réservation | refusée par la base (Restrict) | exception de contrainte |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/` (`Animal.cs`, `Enumerations.cs` avec `StatutAnimal`, `Contrats/AnimalCarteDto.cs` avec `Projection` statique, `Queries/ListerAnimaux.cs`) -- la projection devient dépendante de `now` (méthode qui renvoie l'`Expression`) ; garder le tri `CreeLe` puis `Id` et la validation de `limite`.
- `aaas/api/Aaas.Api/Features/Auth/User.cs` -- cible du `Restrict` sur `UserId`.
- `aaas/api/Aaas.Api/Data/AppDbContext.cs` -- conventions ; `ApplyConfigurationsFromAssembly` ; migration à nommer `Reservations_Reservation`.
- `aaas/api/Aaas.Api/Common/` -- `Common/Data/SqlErreurs` (2601/2627) pour le test d'unicité ; aucune dépendance vers `Features` (règle confirmée par la rétrospective, F20).
- `aaas/api/Aaas.Api.Tests/Outils/Animaux.cs` (`Nouveau`, `Reserve`, `Adopte`, `InsererAsync`, `ViderAnimauxAsync`) et `Outils/Comptes.cs` -- modèles du nouveau `Outils/Reservations.cs` ; `AaasFactory.Time` (`FakeTimeProvider` partagé, avancer seulement vers l'avant) ; `AnimauxTests.cs` (vide la table avant chaque test : il faudra vider les réservations d'abord).
- `aaas/web/src/app/core/api/reservation.dto.ts` -- types `StatutReservation` et `Logement` déjà présents ; aucun changement front attendu.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Common/Disponibilite/` -- énumérations de statut partagées, interfaces minimales (animal : `Id`, `Statut` ; réservation : `AnimalId`, `Statut`, `ExpireLe`), `ReservationRules` et `AnimalRules` en `Expression`.
- [x] `aaas/api/Aaas.Api/Features/Reservations/Reservation.cs`, `ReservationConfiguration.cs`, `Enumerations.cs` -- entité, index unique filtré, `Restrict`, `RowVersion`, longueur `Experience` (1000).
- [x] `aaas/api/Aaas.Api/Data/Migrations/*_Reservations_Reservation.cs` -- migration générée.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalCarteDto.cs`, `Queries/ListerAnimaux.cs` -- projection du statut effectif, exclusion des Adoptés effectifs.
- [x] `aaas/api/Aaas.Api.Tests/Outils/Reservations.cs` -- jeu de données « réservation » partagé (active, échue, confirmée, annulée ; membre de test créé au besoin ; vidage des réservations avant les animaux).
- [x] `aaas/api/Aaas.Api.Tests/DisponibiliteTests.cs` (+ ajustements d'`AnimauxTests.cs`, `DemoAnimauxTests.cs`) -- une couverture par ligne de la matrice, prédicats exécutés en SQL sur LocalDB (une requête EF qui les utilise, pas un appel en mémoire).

**Acceptance Criteria:**
- Given `dotnet test`, then tout passe, y compris les tests existants des animaux et de la démonstration.
- Given le code de `Common/`, when on cherche `Aaas.Api.Features`, then aucune occurrence.
- Given les journaux EF d'un test du statut effectif, then la requête SQL contient le calcul (aucun avertissement d'évaluation côté client).

## Implementation Notes

- La forme générique sur interfaces (`IAnimalStatut`, `IReservationStatut`, contrainte `class`) est traduite telle quelle par EF Core 10 : aucune variante nécessaire.
- Composition sans `Invoke` : `Common/ExpressionComposition.Composer` insère le corps de `AnimalRules.StatutEffectif` dans la projection (`AnimalCarteDto.Projection(reservations, now)`) et dans le filtre « non Adopté effectif » de `ListerAnimaux` ; la règle reste écrite une seule fois.
- `Reservation` porte `Confirmer()` / `Annuler()` (gardes EnAttente) pour que le jeu de données de test passe par l'entité ; aucune route ne les appelle.
- Index nommés : unique filtré `IX_Reservations_AnimalId_EnAttente` + index simple `IX_Reservations_AnimalId` (sinon la clé étrangère n'aurait que l'index filtré).
- `Outils/Animaux.ViderAnimauxAsync` vide d'abord les réservations : `AnimauxTests` et `DemoAnimauxTests` n'ont pas eu à changer leur nettoyage ; seul le test de la carte `Reserve` crée une réservation active.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 4, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `Reservation.Experience` : le commentaire promet `null` si vide, le constructeur stocke la valeur brute | low | patch | Corrigé : trim, vide → `null` (AD-22) ; `ReservationTests` (4 cas). |
| 2 | Filtre « non Adopté » calculé : l index `(Statut, CreeLe)` n est plus utilisable, sous-requête répétée dans WHERE et SELECT | low | rejected | Résultat correct (la règle ne change que Reserve → Disponible) ; coût négligeable aux volumes actuels ; à reconsidérer au Refactor sweep (2.15). |
| 3 | `Reservation` lève une `InvalidOperationException` générique sur transition interdite | low | patch | Corrigé : `TransitionReservationInterditeException` (comme `TransitionInterditeException` d Animal), distinguable pour l epic réservations ; test ajouté. |
| 4 | Ordre des `using` dans `DemoAnimauxTests.cs` | low | patch | Corrigé. |

Après correctifs : `dotnet build` 0 avertissement ; `dotnet test` 177 réussis + 1 ignoré.

## Design Notes

Le statut effectif se calcule dans la requête, par sous-requête sur les réservations de l'animal ; une forme attendue (à adapter si EF Core l'exige, en le notant ici) :

```csharp
// Common/Disponibilite/AnimalRules.cs
public static Expression<Func<TAnimal, StatutAnimal>> StatutEffectif<TAnimal, TResa>(IQueryable<TResa> reservations, DateTimeOffset now)
    where TAnimal : IAnimalStatut where TResa : IReservationStatut =>
    a => a.Statut == StatutAnimal.Reserve
         && !reservations.Any(r => r.AnimalId == a.Id && r.Statut == StatutReservation.EnAttente && r.ExpireLe > now)
        ? StatutAnimal.Disponible
        : a.Statut;
```

Si EF Core ne traduit pas l'accès par interface générique, la variante acceptable garde la même règle en un seul endroit de `Common/` (par exemple des expressions sur des valeurs primitives composées par la slice) ; ne jamais dupliquer la règle dans une slice.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 avertissement.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts, un ignoré.
- `grep -rn "Aaas.Api.Features" aaas/api/Aaas.Api/Common` -- expected: aucune ligne.
