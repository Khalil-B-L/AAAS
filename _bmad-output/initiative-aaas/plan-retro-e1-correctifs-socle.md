---
title: 'Correctifs du socle issus de la rétrospective de l''epic 1'
type: 'bugfix'
ticket: ''
created: '2026-10-15'
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
  - '{project-root}/_bmad-output/initiative-aaas/epic-socle-comptes/epic-socle-comptes-retrospective.md'
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** La rétrospective de l'epic 1 (action A3) a relevé sept défauts du socle à corriger avant l'epic 2 : `Site:Telephone` non validé en E.164 avant la base et renvoyé brut par `/api/site` (F10) ; le seed admin ne met pas à jour le téléphone ni l'identité du compte (F11) ; le mode email Journal exige une section Smtp valide (F12) ; `Jwt:ExpiresMinutes` sans borne haute (F16) ; les formulaires masquent le message `error` de l'API (F6) ; aucun test API ne fixe le 404 de `/api/auth/moi` (F7) ni aucun test front la chaîne de démarrage auth réelle (F8).

**Approach:** Un lot de correctifs ciblés, sans nouvelle fonctionnalité : la normalisation du téléphone passe dans `Common/` pour que `SiteOptions` valide et normalise `Site:Telephone` au démarrage (avant `Migrate()`) ; le seed aligne téléphone et identité ; le mode email effectif se décide sans valider la section Smtp ; `Jwt:ExpiresMinutes` borné à 1..60 ; les formulaires affichent `error` quand l'API en renvoie un ; deux tests de non-régression.

## Boundaries & Constraints

**Always:** règle téléphone inchangée (8 chiffres tunisiens → `+216…`, ou `+` puis 8 à 15 chiffres) ; une seule implémentation de la normalisation ; gardes de démarrage avant toute base ; messages en français nommant la clé de configuration ; le front choisit toujours d'abord selon `code` puis `errors` (AD-10) ; tous les tests existants restent verts.

**Never:** changement de route, de DTO ou de format JSON ; nouvelle dépendance ; correction des constats reportés par la rétrospective (F1–F5, F13–F15, F17–F27) ; écriture dans les fichiers de spec ou d'architecture.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Site national | `Site:Telephone = "72 000 000"` | démarrage OK ; `/api/site` → `"+21672000000"` | — |
| Site invalide | `Site:Telephone = "pas-un-numero"` | l'API refuse de démarrer, message nommant `Site:Telephone`, aucune base créée | — |
| Seed, téléphone changé | admin existant avec un autre téléphone | téléphone du compte = `Site:Telephone` normalisé | — |
| Seed, promotion | compte Membre existant = `Admin:Email` | rôle Admin, prénom « Administrateur », nom « AAAS », téléphone du site ; avertissement journalisé | — |
| Journal, Smtp invalide | `Email:Mode = Journal`, `Smtp:User` invalide | démarrage OK, mode Journal | — |
| Smtp effectif invalide | mode Smtp avec mot de passe, `Smtp:User` invalide | l'API refuse de démarrer (inchangé) | — |
| Durée JWT | `Jwt:ExpiresMinutes` = 0 ou 61 | l'API refuse de démarrer, message `Jwt:ExpiresMinutes` | — |
| Formulaire, erreur API | 500 `{ error: "Une erreur inattendue…" }` | alerte = ce message | sans corps lisible (réseau) : message réseau actuel |
| `/moi` compte disparu | jeton valide, compte absent | 404 `{ error }` (test API) | — |
| Démarrage réel du site | jeton en `localStorage`, `appConfig` | `/api/auth/moi` part avec `Bearer <jeton>`, l'en-tête affiche le prénom | — |
| `returnUrl` réel | `/connexion?returnUrl=%2Fmes-favoris` sous `appConfig`, connexion réussie | URL finale `/mes-favoris` | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Auth/Telephone.cs` -- normaliseur actuel ; à déplacer en `Common/Telephone.cs` (namespace `Aaas.Api.Common`), comme `AdresseEmail` en 1.4 ; usages : `Commands/Inscrire.cs`, `SeedAdmin.cs`, `AuthTests.cs`.
- `aaas/api/Aaas.Api/Features/Site/SiteOptions.cs`, `SiteEndpoints.cs` (`AddSiteOptions`), `Queries/ObtenirSite.cs` -- `[Required]` seulement, valeur renvoyée brute ; la garde `IOptions<SiteOptions>.Value` de `Program.cs` tourne déjà avant `Migrate()`.
- `aaas/api/Aaas.Api/Features/Auth/SeedAdmin.cs:33-66` -- normalise puis lève `InvalidOperationException` ; ne met à jour que rôle et mot de passe.
- `aaas/api/Aaas.Api/Common/Email/EmailSetup.cs:26-37` -- la fabrique lit `IOptions<SmtpOptions>.Value`, ce qui exécute `SmtpOptionsValidator`.
- `aaas/api/Aaas.Api/Features/Auth/JwtOptions.cs:38` -- seule contrainte `> 0`.
- `aaas/web/src/app/features/auth/erreurs-formulaire.ts:38` -- repli `MESSAGE_ENVOI_IMPOSSIBLE` pour tout le reste ; `core/api/erreur-api.ts` (`lireErreurApi`) à réutiliser.
- `aaas/api/Aaas.Api.Tests/AuthTests.cs` (`FabriquerJeton`, `GetMoiAsync`, `AssertErreurAsync`, `Seed_*`, `Demarrage_refuse_*`), `EmailTests.cs` (tests de mode), `SocleTests.cs`, `AaasFactory.cs` -- harnais à réutiliser.
- `aaas/web/src/app/app.spec.ts:73-94` (« App avec appConfig ») -- seul test sous `appConfig` ; `core/auth/auth.service.ts` (`CLE_JETON`).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Common/Telephone.cs` -- déplacer le normaliseur (comportement identique), mettre à jour ses usages.
- [x] `aaas/api/Aaas.Api/Features/Site/SiteOptions.cs`, `SiteEndpoints.cs` -- validateur : `Telephone.Normaliser` non nul, sinon échec nommant `Site:Telephone` ; `PostConfigure` qui remplace la valeur par sa forme E.164.
- [x] `aaas/api/Aaas.Api/Features/Auth/SeedAdmin.cs` -- utiliser `Site:Telephone` déjà normalisé (supprimer le contrôle local) ; mettre à jour le téléphone s'il diffère ; sur promotion d'un compte non Admin, prénom/nom du seed et avertissement.
- [x] `aaas/api/Aaas.Api/Common/Email/EmailSetup.cs` -- décider le mode effectif sur une lecture non validée de la section Smtp ; la validation Smtp ne s'applique qu'à la résolution de `SmtpEmailSender`.
- [x] `aaas/api/Aaas.Api/Features/Auth/JwtOptions.cs` -- `ExpiresMinutes` entre 1 et 60.
- [x] `aaas/api/Aaas.Api.Tests/` -- tests des lignes API de la matrice (site national et invalide, seed téléphone et promotion, Journal avec Smtp invalide, durée JWT 0 et 61, `/moi` 404 avec jeton d'un compte inexistant) ; adapter les tests existants de seed touchés.
- [x] `aaas/web/src/app/features/auth/erreurs-formulaire.ts` (+ spec) -- sans `code` ni `errors` mais corps API lisible : alerte = `error` ; sans corps : message réseau.
- [x] `aaas/web/src/app/app.spec.ts` -- deux tests sous `appConfig` : restauration avec `Bearer` et prénom dans l'en-tête ; `returnUrl` lié par la route après connexion.

**Acceptance Criteria:**
- Given `dotnet test` et `npx ng test --watch=false`, then tout passe, y compris les nouveaux tests.
- Given `npx ng build` et `dotnet build`, then aucun avertissement.
- Given le code de `Aaas.Api`, when on cherche `Telephone.Normaliser`, then une seule implémentation, dans `Common/`.

## Implementation Notes

- `Site:Telephone` : `SiteTelephonePostConfigure` (normalise, laisse une valeur invalide intacte) + `SiteOptionsValidator` (vide laissé à `[Required]` pour un seul message), enregistrés dans `AddSiteOptions` ; `ObtenirSite` inchangé, il lit la valeur déjà normalisée.
- Seed : promotion = avertissement (catégorie `SeedAdmin`) + prénom/nom du seed ; téléphone aligné à chaque démarrage s'il diffère.
- Email : mode effectif décidé sur `IConfiguration.GetSection("Smtp").Get<SmtpOptions>()` (non validé) ; `IOptions<SmtpOptions>.Value` n'est plus lu que par `SmtpEmailSender`.
- Tests : `Seed_refuse_le_demarrage_si_Site_Telephone_est_invalide` (AuthTests) remplacé par `SocleTests.Demarrage_refuse_si_Site_Telephone_est_invalide_avant_toute_base` (base dédiée jamais créée, vérifiée par `CanConnectAsync`) ; helper `Messages` devenu inutile, supprimé. Front : nouveau `erreurs-formulaire.spec.ts`, un cas 500 dans `connexion.spec.ts`.
- Résultats : `dotnet test` 129 réussis + 1 ignoré (122 avant) ; Vitest 152 (139 avant) ; `dotnet build` et `ng build` sans avertissement.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 1, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `EmailSetup.cs` : `GetSection("Smtp").Get<SmtpOptions>()` convertit tous les champs ; un `Smtp:Port` non numérique fait encore échouer le démarrage en mode Journal | low | patch | Contredit la ligne « Journal, Smtp invalide » de la matrice. Corrigé : seul `Smtp:Password` est lu pour décider du mode ; test transformé en théorie (`Smtp:User`, `Smtp:Port = abc`, `Smtp:TimeoutSecondes = dix`). Après correctif : `dotnet test` 131 réussis + 1 ignoré, build sans avertissement. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts ; build sans avertissement.
