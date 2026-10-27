---
title: 'Briques communes backend'
type: 'feature'
ticket: '2'
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
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Les slices des epics suivants ont besoin d'un mécanisme unique pour les cas d'usage, les erreurs, les transactions, le temps et le JSON ; sans lui, chaque slice inventerait le sien (AD-2, AD-6, AD-10, AD-11, AD-19, AD-21).

**Approach:** Poser dans `Aaas.Api/Common/` et `Data/` les briques transverses (handlers, `Result<T>` + `ToHttpResult`, format d'erreur unique, `TransactionRunner`, conventions EF, convertisseurs JSON, `LiensFront`), convertir `/api/site` en `IQueryHandler`, et prouver le tout par des tests d'intégration sur des endpoints déclarés uniquement dans les tests.

## Boundaries & Constraints

**Always:** toute réponse d'erreur (400, 404, 405, 415, 500, refus métier) a le corps `{ "error": "…" }`, plus `errors` (400 par champ) et/ou `code` (refus métier), propriétés nulles omises, messages en français ; dates écrites en UTC avec `Z` ; énumérations écrites et lues en chaîne, même casse, entier ou valeur inconnue → 400 ; aucun détail d'exception dans un 500 ; l'heure ne se lit que par `TimeProvider`.

**Never:** MediatR ou librairie de pipeline ; `Results.NotFound()`, `Results.Problem()`, `ProblemDetails` ; `EnableRetryOnFailure` ; endpoint de test dans le code de production ; entité, JWT, `ClaimsPrincipalExtensions` (story 3) ; enveloppe `{ items, serverNow }` et trim des textes (stories qui les utilisent).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Route API inconnue | `GET /api/inexistant` | 404 `{ error }` JSON | — |
| Exception non gérée | endpoint qui lève | 500 `{ error }` sans détail | exception journalisée |
| Date non UTC | `DateTimeOffset` en +02:00 | JSON `"…Z"` à l'instant UTC équivalent | — |
| Énumération | valeur `EnAttente` | JSON `"EnAttente"` | — |
| Énumération invalide en entrée | `"Inconnue"` ou `1` | 400 `{ error }` | — |
| Refus métier | `Result.Conflict("code", "msg")` | 409 `{ error: "msg", code: "code" }` | — |
| Validation | `Result.Invalid({ champ: msg })` | 400 `{ error, errors: { champ } }` | — |
| Transaction | travail renvoie un échec ou lève | rien n'est commité | interblocage 1205 : 3 essais, `ChangeTracker.Clear()` entre eux |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Program.cs` -- pipeline actuel : DbContext, `TimeProvider.System`, `AddSiteOptions`, garde options, `Migrate()`, `MapSiteEndpoints()`. Garder la garde et l'ordre migration → endpoints.
- `aaas/api/Aaas.Api/Features/Site/SiteEndpoints.cs` -- endpoint direct sur `IOptions<SiteOptions>`, à convertir (commentaire « story 2 » à retirer). Garder `AddSiteOptions`.
- `aaas/api/Aaas.Api/Data/AppDbContext.cs` -- contexte vide avec `ApplyConfigurationsFromAssembly` ; ajouter les conventions.
- `aaas/api/Aaas.Api.Tests/AaasFactory.cs`, `SocleTests.cs` -- harnais existant (collection `api`, `FakeTimeProvider` `Time`) ; à réutiliser, pas à dupliquer.
- `server/FitClub.Api/Common/ApiErrors.cs` -- modèle `ErrorResponse(Error, Errors)` à étendre avec `Code`.
- `server/FitClub.Api/Sessions/SessionEndpoints.cs:56-76,122` et `Auth/AuthEndpoints.cs:123` -- modèle de boucle sur 1205 (`SqlException { Number: 1205 }`, aussi en `InnerException`) et de détection 2601/2627. Lecture seule.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Common/Result.cs` -- `Result<T>` : `Ok`, `NotFound(message)`, `Conflict(code, message)`, `Invalid(errors)`, `Invalid(code, message)`, `Forbidden()`, `Unavailable(code, message)` (503) ; `Result` sans valeur pour 204.
- [x] `aaas/api/Aaas.Api/Common/ResultHttpExtensions.cs` -- `ToHttpResult()` (200, ou statut de succès passé en paramètre pour 201) et `ToHttpResult()` du `Result` sans valeur (204) ; échecs via `ApiErrors` -- conversion unique (AD-2).
- [x] `aaas/api/Aaas.Api/Common/ApiErrors.cs` -- `ErrorResponse(Error, Errors?, Code?)` (nuls omis), fabriques par statut, messages par défaut en français (404, 403, 500…).
- [x] `aaas/api/Aaas.Api/Common/Handlers.cs` -- `ICommandHandler<TCommand, TReponse>` / `IQueryHandler<TQuery, TReponse>` (`Task<Result<TReponse>> HandleAsync(…, CancellationToken)`) et `AddHandlers()` qui enregistre en scoped toutes les implémentations de l'assembly.
- [x] `aaas/api/Aaas.Api/Common/ErreursHttp.cs` -- `UseApiErrors()` (gestionnaire d'exceptions → 500, pages de statut → `{ error }` pour toute réponse d'erreur sans corps) et `MapApiFallback()` (`/api/{**chemin}` → 404).
- [x] `aaas/api/Aaas.Api/Common/Data/TransactionRunner.cs` -- scoped, `RunAsync<T>(IsolationLevel, Func<CancellationToken, Task<Result<T>>>)` : transaction, `SaveChanges` + commit seulement si succès, rollback sinon, 3 essais sur 1205 ; `SqlErreurs.EstInterblocage` / `EstViolationUnicite` publics pour les handlers.
- [x] `aaas/api/Aaas.Api/Common/Json/UtcDateTimeOffsetConverter.cs`, `Common/Json/JsonConfiguration.cs` -- écriture UTC `Z`, lecture refusant une date sans fuseau ; `ConfigureHttpJsonOptions` avec ce convertisseur et `JsonStringEnumConverter(allowIntegerValues: false)` (AD-11, AD-19).
- [x] `aaas/api/Aaas.Api/Common/LiensFront.cs` -- constantes et méthodes pour toutes les routes de `contrats-api.md` §7 (`FicheAnimal(id)`, `ReservationEnvoyee(id)`, `AdminReservations`…).
- [x] `aaas/api/Aaas.Api/Data/AppDbContext.cs` -- `ConfigureConventions` : `DateTimeOffset` → `datetimeoffset` avec conversion UTC à l'écriture, `Enum` → chaîne ; une propriété `DateTime` lève à la construction du modèle.
- [x] `aaas/api/Aaas.Api/Features/Site/Queries/ObtenirSite.cs`, `SiteEndpoints.cs` -- `ObtenirSiteQuery` + handler → `Result<SiteDto>`, endpoint via `ToHttpResult()`.
- [x] `aaas/api/Aaas.Api/Program.cs` -- `AddHandlers()`, `TransactionRunner`, JSON, `UseApiErrors()` en tête, `MapApiFallback()` en dernier.
- [x] `aaas/api/Aaas.Api.Tests/EndpointsDeTest.cs` -- `IStartupFilter` enregistré par la factory qui ajoute, en aval du pipeline de `Program`, des endpoints `/tests/…` (lève, date + enum, corps avec enum, chaque variante de `Result`, travail `TransactionRunner`).
- [x] `aaas/api/Aaas.Api.Tests/CommonTests.cs` -- une couverture par ligne de la matrice ; conventions vérifiées sur le modèle d'un contexte dérivé d'`AppDbContext` ; transaction via une table créée en SQL brut dans la base jetable ; `LiensFront` aligné sur §7.

**Acceptance Criteria:**
- Given l'API, when `GET /api/site`, then la réponse est inchangée (200, `SiteDto`) et passe par `IQueryHandler` + `ToHttpResult()`.
- Given `Aaas.Api` hors tests, when on cherche `Results.NotFound`, `Results.Problem` ou `ProblemDetails`, then aucune occurrence.
- Given `dotnet test`, when la suite tourne, then tous les tests passent, y compris ceux de la story 1.1.

## Implementation Notes

- Fabriques d'échec sur `Result` (non générique) renvoyant un `Echec`, converti implicitement en `Result` ou `Result<T>` ; `Result<T>` expose les mêmes fabriques. Ajouts pour que les stories suivantes n'étendent pas `Common` : `ICommandHandler<TCommand>` (→ `Result`, 204) et `TransactionRunner.RunAsync` pour `Result`.
- `TransactionRunner` vide aussi le `ChangeTracker` quand le travail renvoie un échec (pas de fuite dans un `SaveChanges` ultérieur).
- `UseApiErrors` traduit `BadHttpRequestException` (levée en Development par `ThrowOnBadRequest`) en son statut (400…) au lieu de 500 ; test dédié.
- `AaasFactory` capture le journal (`JournalDeTest`) pour vérifier qu'une exception non gérée est journalisée.
- 2026-10-14 — `JsonStringEnumConverter(allowIntegerValues: false)` remplacé par `Common/Json/EnumEnChaineConverter` : sa lecture ignore la casse (`"enAttente"` accepté, vérifié par test), ce qui viole la contrainte « même casse ». Le convertisseur maison écrit le nom C# et ne lit que le nom exact ; entier, `"1"` ou casse différente → 400.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 4, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `MapApiFallback` accepte toutes les méthodes : une mauvaise méthode sur une route `/api` existante renvoie 404 au lieu de 405 | low | rejected | Réel (sonde du relecteur : `POST /api/site` → 404 `{ error }`), mais le corps reste au format unique et le front n'appelle que les verbes du contrat §4 ; obtenir 405 demanderait de filtrer le fallback par route connue (complexité ajoutée). |
| 2 | `TransactionRunner` ne vide pas le `ChangeTracker` quand le travail ou `SaveChangesAsync` lève (hors 1205) : une violation d'unicité laisse les entités `Added` suivies dans le scope | low | patch | Le handler qui rattrape 2601/2627 (AD-6) garde des entités rejetées qu'un `SaveChanges` ultérieur du même scope réinsérerait. Correctif : vider le `ChangeTracker` sur toute exception, puis relancer. |
| 3 | AD-19 et `contrats-api.md` §1 nomment encore `JsonStringEnumConverter` | low | patch | Le code utilise `EnumEnChaineConverter` (écart justifié : casse). Correctif : mettre à jour le texte d'AD-19 et de §1. |
| 4 | `InterdireDateTimeConvention` ignore les types complexes EF | low | rejected | Aucun type complexe prévu par l'architecture ; couvrir ce cas ajouterait une boucle pour un cas que personne ne rencontre aujourd'hui. |

## Design Notes

`ToHttpResult` est le seul pont `Result` → HTTP ; le statut de succès est choisi par l'endpoint (`ToHttpResult(StatusCodes.Status201Created)`), jamais par le handler. `Unavailable` et `Invalid(code, message)` existent dès maintenant pour `envoi-impossible` (503) et `identifiants-invalides` (400), afin que les stories 3 et Contact n'étendent pas `Common`.

Endpoints de test : un `IStartupFilter` dont le middleware appelle d'abord `next(app)` puis `UseRouting`/`UseEndpoints` place les routes `/tests/…` en aval du gestionnaire d'exceptions de `Program`, hors du fallback `/api/**`, avec les options JSON globales :

```csharp
public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
{
    next(app);
    app.UseRouting();
    app.UseEndpoints(e => e.MapGet("/tests/exception", () => { throw new InvalidOperationException("détail secret"); }));
};
```

La boucle sur 1205 n'est pas testable sans provoquer un vrai interblocage (`SqlException` n'a pas de constructeur public, `THROW` refuse les numéros < 50000) : couverte par la revue, pas par un test.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 erreur, 0 avertissement.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts.
- `grep -rnE "Results\.(NotFound|Problem)|ProblemDetails" aaas/api/Aaas.Api --include=*.cs` -- expected: aucune ligne.
