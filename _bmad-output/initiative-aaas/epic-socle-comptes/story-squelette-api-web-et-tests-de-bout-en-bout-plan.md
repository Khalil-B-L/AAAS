---
title: 'Squelette API, web et tests de bout en bout'
type: 'feature'
ticket: '1'
created: '2026-10-14'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Le dépôt n'a ni `aaas/api` ni `aaas/web` ; aucun epic ne peut construire tant qu'un projet ne démarre pas, ne crée pas sa base et n'a pas de harnais de tests.

**Approach:** Tracer bullet : scaffolder l'API .NET 10 (+ projet de tests xunit.v3) et l'application Angular 22, relier les deux par le proxy `ng serve`, et faire traverser une valeur réelle (`Site:Telephone`) de la configuration de l'API jusqu'au pied de page Angular, avec base LocalDB migrée au démarrage et tests qui passent des deux côtés.

## Boundaries & Constraints

**Always:** versions de la Stack de l'architecture, sauf .NET (décision 2026-10-14 : SDK 10.0.401 du poste sans `global.json`, runtime et paquets Microsoft en 10.0.12) ; secrets jamais dans `appsettings*.json` (`UserSecretsId` présent, aucun secret requis par cette story) ; `TimeProvider.System` enregistré en DI ; options `SiteOptions` liées à la section `Site`, validées au démarrage (`ValidateOnStart`) ; valeurs placeholder marquées d'un commentaire `// PLACEHOLDER` dans `appsettings.json` ; front en chemins relatifs uniquement ; Angular standalone, zoneless, `OnPush`, signals ; tests backend sur base LocalDB jetable supprimée en fin de run, environnement `Testing`, config en mémoire.

**Never:** CORS ; `UseStaticFiles` ; `UseHttpsRedirection` ; zone.js ; NgRx ou librairie de composants ; Common (`Result`, `ApiErrors`, handlers, convertisseurs JSON — story 2) ; entité `User`, JWT (story 3) ; habillage, jetons CSS, routes Angular (story 5) ; entité dans `AppDbContext`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Site OK | `GET /api/site` | 200 `{ telephone, facebook, instagram }` (valeurs de la section `Site`) | — |
| Config manquante | `Site:Telephone` absent ou vide | L'API refuse de démarrer (`OptionsValidationException`) | Message nommant la clé |
| Premier démarrage | Base `Aaas` absente | Base créée et migration `Socle_Initial` appliquée | — |
| API injoignable | `/api/site` en échec côté front | Pied de page rendu sans numéro, pas d'erreur bloquante | Erreur journalisée en console |

</frozen-after-approval>

## Code Map

- `server/FitClub.Api/Program.cs` -- modèle : DbContext résolu depuis `IConfiguration` (surchargeable par les tests), `Database.Migrate()` dans un scope au démarrage, `public partial class Program;`. Ne pas reprendre `UseHttpsRedirection` ni les fichiers statiques.
- `server/FitClub.Api.Tests/FitClubFactory.cs` -- modèle de factory : base `…Tests_{Guid:N}` sur `(localdb)\MSSQLLocalDB`, `UseEnvironment("Testing")`, `AddInMemoryCollection`, `EnsureDeletedAsync` en fin de run, `ICollectionFixture`. À porter en xunit.v3 (`IAsyncLifetime` renvoie `ValueTask`) et y ajouter `FakeTimeProvider`.
- `server/FitClub.Api/Properties/launchSettings.json` -- FitClub occupe 7166/5178 ; AAAS prend `https://localhost:7180;http://localhost:5180`.
- `server/dotnet-tools.json` -- modèle de manifeste local `dotnet-ef`.
- Ne rien modifier sous `server/` ni `gym-app/`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.sln`, `aaas/api/Aaas.Api/Aaas.Api.csproj` -- `dotnet new web`, net10.0, Nullable, `UserSecretsId` ; paquets EF Core SqlServer/Design 10.0.12 -- socle API.
- [x] `aaas/api/.config/dotnet-tools.json` -- manifeste local `dotnet-ef` 10.0.12 -- migrations reproductibles.
- [x] `aaas/api/Aaas.Api/Properties/launchSettings.json` -- profil `https` 7180/5180, `Development` -- port fixe pour le proxy.
- [x] `aaas/api/Aaas.Api/appsettings.json`, `appsettings.Development.json` -- `ConnectionStrings:Aaas` (base `Aaas` sur LocalDB), section `Site` (`Telephone` E.164, `Facebook`, `Instagram`) avec `// PLACEHOLDER` -- AD-14.
- [x] `aaas/api/Aaas.Api/Data/AppDbContext.cs` -- contexte sans entité, `ApplyConfigurationsFromAssembly` -- AD-7 prêt pour les slices.
- [x] `aaas/api/Aaas.Api/Data/Migrations/*_Socle_Initial.cs` -- migration générée par `dotnet ef migrations add Socle_Initial` -- historique de migrations amorcé.
- [x] `aaas/api/Aaas.Api/Features/Site/SiteOptions.cs`, `SiteDto.cs`, `SiteEndpoints.cs` -- options validées (`[Required]` + `ValidateDataAnnotations` + `ValidateOnStart`) ; `MapGet("/api/site")` → `SiteDto` -- slice Site.
- [x] `aaas/api/Aaas.Api/Program.cs` -- DbContext, `TimeProvider.System`, options Site, `Migrate()` au démarrage, `MapSiteEndpoints()`, `public partial class Program;`.
- [x] `aaas/api/Aaas.Api.Tests/Aaas.Api.Tests.csproj` -- xunit.v3 4.0.2, xunit.runner.visualstudio 3.1.5, Microsoft.NET.Test.Sdk 18.11.0, Mvc.Testing 10.0.12, TimeProvider.Testing 10.11.0 -- harnais.
- [x] `aaas/api/Aaas.Api.Tests/AaasFactory.cs` -- factory LocalDB jetable, `Testing`, config en mémoire (dont `Site:*` de test), `FakeTimeProvider` exposé en propriété et remplaçant `TimeProvider` -- harnais réutilisé par tous les epics.
- [x] `aaas/api/Aaas.Api.Tests/SocleTests.cs` -- tests : `/api/site` renvoie les valeurs de test ; aucune migration en attente ; `TimeProvider` résolu est le `FakeTimeProvider` ; démarrage refusé sans `Site:Telephone` -- matrice I/O.
- [x] `aaas/web/` -- `npx @angular/cli@22.2.2 new web --style=css --ssr=false --skip-git` (zoneless par défaut, Vitest) -- socle front.
- [x] `aaas/web/proxy.conf.json`, `aaas/web/angular.json` -- `/api` et `/fichiers` → `https://localhost:7180`, `secure: false`, branché sur `serve` -- AD-17.
- [x] `aaas/web/src/app/core/api/site.dto.ts`, `core/site.service.ts` -- interface `SiteDto` (§6) ; service à signal `site()` chargé une fois via `provideAppInitializer`, échec avalé et journalisé -- AD-14.
- [x] `aaas/web/src/app/app.ts`, `app.html`, `app.config.ts` -- `provideHttpClient`, initializer ; pied de page minimal affichant `site()?.telephone`.
- [x] `aaas/web/src/app/core/site.service.spec.ts`, `app.spec.ts` -- Vitest : le service charge `/api/site` ; le pied de page affiche le numéro ; échec HTTP → pas de numéro, pas d'exception.
- [x] `_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md` -- table Stack : SDK 10.0.401, runtime et paquets Microsoft 10.0.12, avec la date de la décision -- l'architecture reflète le poste réel.
- [x] `aaas/README.md` -- commandes de démarrage (`dotnet run --launch-profile https`, `npm start`, tests).

**Acceptance Criteria:**
- Given LocalDB disponible et la base `Aaas` absente, when `dotnet run --launch-profile https` dans `aaas/api/Aaas.Api`, then l'API démarre, la base `Aaas` existe avec la migration `Socle_Initial` appliquée.
- Given l'API lancée, when `npm start` dans `aaas/web` puis ouverture de `http://localhost:4200`, then le pied de page affiche le numéro placeholder de `appsettings.json`, servi via le proxy.
- Given aucune base de test, when `dotnet test` dans `aaas/api`, then tous les tests passent et aucune base `AaasTests_*` ne subsiste.
- Given `aaas/web`, when `npx ng test --watch=false`, then tous les tests Vitest passent.

## Implementation Notes

- `Aaas.Api.Tests.csproj` : `IsTestingPlatformApplication=false`. xunit.v3 4.0.2 active Microsoft.Testing.Platform v2, que le `dotnet test` (VSTest) du SDK 10 refuse sans opt-in dans un `global.json` ; le plan exclut `global.json`, on reste donc sur VSTest via xunit.runner.visualstudio 3.1.5.
- `Program.cs` : en plus de `ValidateOnStart`, `IOptions<SiteOptions>.Value` est lu juste après `Build()` (garde comme `JwtOptions` de FitClub) pour que la config invalide échoue avant `Migrate()` et ne crée aucune base.
- Manifeste d'outils déplacé dans `aaas/api/.config/dotnet-tools.json` (`dotnet new tool-manifest` le crée à la racine).
- Table Stack : `xunit.runner.visualstudio` 3.1.5 et `TimeProvider.Testing` 10.11.0 également épinglés ("dernier au scaffold").
- Squelette `ng new` conservé : `provideRouter` avec `routes` vide et `<router-outlet />` (aucune route ajoutée).

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 4, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `app.config.ts` : l'initializer attend `charger()` sans timeout ; une API qui accepte la connexion sans répondre bloque le rendu | medium | patch | `provideAppInitializer` attend la promesse, `HttpClient` n'a pas de timeout par défaut : contredit la ligne « API injoignable » de la matrice. Correctif : ne pas attendre le chargement dans l'initializer. |
| 2 | `app.spec.ts` : aucun test ne passe par `appConfig` / `provideAppInitializer` | low | patch | Supprimer l'initializer laisse les 4 tests verts. Correctif : un test qui démarre avec les providers d'`appConfig` et vérifie la requête `/api/site`. |
| 3 | `SiteOptions.cs` : le message de validation nomme la propriété, pas la clé `Site:Telephone` | low | patch | Message par défaut de `[Required]` : « The Telephone field is required. ». Correctif : `ErrorMessage` nommant `Site:Telephone` (idem Facebook / Instagram), test qui exige `Site:Telephone`. |
| 4 | `AaasFactory.DisposeAsync` : `EnsureDeletedAsync` hors `try/finally` | low | rejected | Ne survient que si LocalDB tombe en cours de run, cas improbable au quotidien ; le correctif ajoute une garde. |
| 5 | `AaasFactory.DisposeAsync` démarre l'hôte si aucun test ne l'a fait (run filtré) | low | rejected | Réel mais sans effet visible (base créée puis supprimée) ; suivre l'état de l'hôte ajouterait de la complexité. |
| 6 | Pas de test pour une clé `Site:Telephone` absente (seulement vide) | low | rejected | La propriété vaut `string.Empty` par défaut : absente et vide suivent le même chemin `[Required]`, déjà testé. |

## Design Notes

`/api/site` est un endpoint direct sur `IOptions<SiteOptions>` : `IQueryHandler` et `Result<T>` n'existent qu'à la story 2, qui convertira cet endpoint. Pas de `SeedAdmin`, pas d'entité : la migration `Socle_Initial` est vide mais crée `__EFMigrationsHistory`, ce qui prouve la chaîne.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 erreur, 0 avertissement nullable.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts.
- `npx ng test --watch=false` (dans `aaas/web`) -- expected: tous verts.
- `npx ng build` (dans `aaas/web`) -- expected: succès.

**Manual checks (if no CLI):**
- `curl -k https://localhost:7180/api/site` puis `curl http://localhost:4200/api/site` -- même JSON.
