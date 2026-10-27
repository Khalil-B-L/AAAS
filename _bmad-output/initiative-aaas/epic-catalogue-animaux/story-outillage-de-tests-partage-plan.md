---
title: 'Outillage de tests partagé'
type: 'refactor'
ticket: '1'
created: '2026-10-15'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/epic-socle-comptes/epic-socle-comptes-retrospective.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Les tests de l'epic 1 recopient les mêmes helpers dans plusieurs classes et specs (rétrospective F22, F27), et les gardes de démarrage de l'API suivent trois styles différents (F23) ; l'epic 2 va ajouter une dizaine de stories qui en auraient besoin à leur tour.

**Approach:** Refactor sans changement de comportement : regrouper les helpers de test de l'API et du front dans des fichiers d'outillage partagés, sortir le faux serveur SMTP et scinder `AuthTests` par sujet, et ramener les gardes de démarrage à un seul point d'entrée appelé par `Program.cs`.

## Boundaries & Constraints

**Always:** même nombre de tests, mêmes noms de tests, mêmes assertions ; aucun changement de code de production autre que les gardes de démarrage, dont l'ordre (avant `Migrate()`) et les messages restent identiques ; conventions de nommage français du projet.

**Never:** nouvelle dépendance ; nouveau test de fonctionnalité ; correction de bug au passage (s'il en apparaît un, le noter dans Implementation Notes) ; toucher aux constats reportés F1–F5, F13–F15, F17–F21, F24–F26.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Suite API | `dotnet test` | même total (réussis + 1 ignoré) qu'avant | — |
| Suite front | `npx ng test --watch=false` | même total qu'avant | — |
| Garde de démarrage | une config invalide (Site, Jwt, Admin, Email) | même `OptionsValidationException`, même message, avant toute base | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api.Tests/AuthTests.cs` (671 lignes) -- helpers privés : `Redemarrer` (522), `RestaurerAdminParDefaut` (531), `LireUserAsync` (536), `JsonContent` (594), `PostJsonAsync` (596), `GetMoiAsync` (599), `FabriquerJeton` (628), `LireJsonAsync` (644), `AssertValidationAsync` (647), `AssertErreurAsync` (656), `TrouverValidation` (664) ; mélange intégration HTTP, gardes de démarrage, seed et tests unitaires (`Telephone_Normaliser`).
- `aaas/api/Aaas.Api.Tests/CommonTests.cs:309,313` -- `PostJsonAsync`, `AssertErreurAsync` (avec `code`, signature différente).
- `aaas/api/Aaas.Api.Tests/EmailTests.cs` (481 lignes) -- `Demarrer` (379), `TrouverValidation` (435), `FauxServeurSmtp` (209, ~100 lignes, TLS), classe `EmailReelTests` (449) dans le même fichier.
- `aaas/api/Aaas.Api.Tests/SocleTests.cs:117` -- `TrouverValidation`.
- `aaas/api/Aaas.Api.Tests/AaasFactory.cs`, `JournalDeTest.cs`, `EndpointsDeTest.cs` -- harnais existant, à garder.
- `aaas/api/Aaas.Api/Program.cs:37-42` -- trois gardes : `IOptions<SiteOptions>.Value` en ligne, `VerifierConfigurationAuth()`, `VerifierConfigurationEmail()` ; `Features/Auth/AuthSetup.cs:52`, `Common/Email/EmailSetup.cs:62`.
- Front : fixture utilisateur « Sonia » recopiée dans `app.routes.spec.ts`, `app.spec.ts`, `core/auth/auth.interceptor.spec.ts`, `auth.service.spec.ts`, `gardes.spec.ts`, `features/auth/connexion/connexion.spec.ts`, `inscription/inscription.spec.ts`, `layout/en-tete/en-tete.spec.ts` ; « connecter en tant que » dans `app.routes.spec.ts:32`, `gardes.spec.ts:50`, `auth.interceptor.spec.ts:38` ; helpers de formulaire (`rendre`, `stable`, `champ`, `saisirEtEnvoyer`) dupliqués entre `connexion.spec.ts` et `inscription.spec.ts`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api.Tests/Outils/` -- `ValidationDemarrage.cs` (`TrouverValidation`, démarrage refusé avec config en mémoire), `HoteDeTest.cs` (redémarrage d'hôte avec environnement et config), `RequetesApi.cs` (JSON, `PostJsonAsync`, GET avec jeton, `LireJsonAsync`), `AssertionsErreur.cs` (une seule `AssertErreurAsync(message, code?)` et `AssertValidationAsync`), `Jetons.cs` (`FabriquerJeton`), `FauxServeurSmtp.cs` (sorti d'`EmailTests`).
- [x] `aaas/api/Aaas.Api.Tests/` -- remplacer les copies par les helpers ; scinder `AuthTests.cs` en fichiers par sujet (inscription, connexion et session, groupe admin, seed et gardes, normalisation) ; `EmailReelTests` dans son propre fichier.
- [x] `aaas/api/Aaas.Api/Common/GardesDemarrage.cs`, `Program.cs` -- un seul `VerifierConfiguration()` appelé avant `Migrate()`, qui déclenche les gardes Site, Jwt, Admin et Email (les méthodes de slice restent la source de leurs options).
- [x] `aaas/web/src/app/testing/` -- `utilisateurs.ts` (fixtures Membre et Admin), `session.ts` (connecter en tant que, avec ou sans jeton), `formulaires.ts` (helpers communs aux specs de formulaire) ; remplacer les copies dans les specs listées.

**Acceptance Criteria:**
- Given les suites avant et après, when on compare les totaux, then ils sont identiques (API : réussis + 1 ignoré ; front : réussis).
- Given le code de test, when on cherche `TrouverValidation`, `PostJsonAsync`, `AssertErreurAsync` ou la fixture « Sonia », then chacun n'est défini qu'une fois.
- Given `Program.cs`, then une seule ligne de garde de configuration avant `Migrate()`.

## Implementation Notes

- **Totaux** : API avant 131 réussis + 1 ignoré (132), après identique ; noms de méthodes de test identiques (comparaison des listes `--list-tests` sans le nom de classe). Front avant 152 (18 fichiers), après 152 (18 fichiers), libellés `describe > it` identiques. `dotnet build` 0 avertissement, `npx ng build` sans avertissement, `tsc -p tsconfig.spec.json` propre.
- **Gardes de démarrage** : `Common/GardesDemarrage.cs` ne référence aucune slice (sens des dépendances F20 conservé) : chaque slice déclare sa garde à l'enregistrement de ses options (`services.AddGardeDemarrage(...)` dans `AddSiteOptions`, `AddAuth`, `AddEmail`) et `VerifierConfiguration()` les exécute dans l'ordre d'enregistrement (Site, Jwt/Admin, Email), soit l'ordre d'avant. `VerifierConfigurationAuth()` / `VerifierConfigurationEmail()` restent les sources de leurs options ; mêmes appels, donc mêmes exceptions et messages. `Program.cs` n'a plus qu'une ligne de garde avant `Migrate()`.
- **Outils API** (`Aaas.Api.Tests/Outils/`, namespace `Aaas.Api.Tests.Outils`) : extensions sur `AaasFactory` (`factory.PostJsonAsync`, `factory.GetAvecJetonAsync`, `factory.GetMoiAsync`, `factory.Redemarrer`, `factory.Configurer`, `factory.RestaurerAdminParDefaut`, `factory.DemarrerAvecConfigurationInvalide`, `factory.FabriquerJeton`). La classe de `Jetons.cs` s'appelle `JetonsDeTest` pour ne pas masquer `Features.Auth.Jetons`. Fichier en plus de la liste : `Comptes.cs` (`NouvelEmail`, corps d'inscription/connexion, `InscrireAsync`, `ConnecterAsync`), nécessaire pour scinder `AuthTests` sans recopier ces helpers dans chaque fichier. `DemarrerAvecConfigurationInvalide` renvoie l'exception (nullable) : l'`Assert.NotNull` reste dans chaque test.
- **Scission d'`AuthTests`** : `InscriptionTests`, `ConnexionEtSessionTests`, `GroupeAdminTests`, `SeedEtGardesTests`, `TelephoneTests` (unitaire, hors collection `api` puisqu'il n'utilise pas la factory) ; `EmailReelTests.cs` sorti d'`EmailTests.cs`. Le client de test `ClientQuitEnEchec` reste dans `EmailTests` (un seul usage).
- **Outils front** (`src/app/testing/`) : exclus de `tsconfig.app.json` et inclus dans `tsconfig.spec.json`. `utilisateurs.ts` : `membre` (Sonia), `admin`, `utilisateurEnTantQue(role)`, `saisieInscription`, `reponseAuth()` ; `session.ts` : `connecterEnTantQue(role, jeton?)` et `stockerJeton(jeton?)` (session restaurée au rechargement) ; `formulaires.ts` : `rendreFormulaire`, `stable`, `champ`, `saisirEtEnvoyer`. La page vide de `formulaires.ts` porte un sélecteur explicite : sans lui, Angular signalait NG0912 (collision d'ID avec le `PageTest` d'`app.spec.ts`).
- Quelques valeurs non vérifiées par les tests ont changé en passant par les fixtures communes (identifiants envoyés à `connecter`, `id` 1 → 3 dans `app.routes.spec.ts`, jeton `'j'` → `'jeton-1'` dans `gardes.spec.ts`) ; aucune assertion ne les lit.
- Aucun bug rencontré.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 5, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `SiteEndpoints.cs:15` : commentaire « garde de Program » périmé | low | patch | La garde est désormais déclarée dans la méthode elle-même. Mettre le commentaire à jour. |
| 2 | `CommonTests.cs:126` : hôte dérivé encore construit en ligne | low | patch | Seule copie restante hors `Outils/HoteDeTest.cs`. Utiliser `HoteDeTest`. |
| 3 | `app.spec.ts:125-151` : `localStorage.setItem` et boucle de saisie recopiés | low | patch | `stockerJeton` et `saisirEtEnvoyer` existent dans `testing/`. Les utiliser (sélecteur de formulaire adapté si besoin). |
| 4 | Identifiants de Sonia tapés en dur dans `auth.service.spec.ts` et `connexion.spec.ts` | low | patch | Dériveraient sans erreur si la fixture change. Lire `membre.email` et le mot de passe de la fixture. |
| 5 | `TelephoneTests` sort de la collection `api` (exécution parallèle) | low | rejected | Test de fonction pure, aucun état partagé ; totaux et noms inchangés ; noté dans Implementation Notes. |

## Verification

**Commands:**
- Avant tout changement, noter les totaux de `dotnet test aaas/api/Aaas.sln` et de `npx ng test --watch=false` (dans `aaas/web`).
- Après : mêmes commandes -- expected: mêmes totaux, tout vert ; `dotnet build` et `npx ng build` sans avertissement.
