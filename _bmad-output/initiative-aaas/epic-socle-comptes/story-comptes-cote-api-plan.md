---
title: 'Comptes côté API'
type: 'feature'
ticket: '3'
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

**Problem:** Aucun compte n'existe : personne ne peut s'inscrire ni se connecter, et les epics suivants n'ont ni identité membre ni rôle Admin pour protéger leurs routes (CAP-1, AD-8).

**Approach:** Slice `Features/Auth` : entité `User` (prénom, nom, email unique, téléphone E.164 obligatoire, rôle), inscription, connexion et `/api/auth/moi` en handlers CQRS, JWT HS256 avec claims `sub`/`email`/`role`/`jti`, policy `Admin` et groupe `/api/admin` partagés, 401/403 au format `ApiErrors`, compte admin créé ou mis à jour au démarrage depuis la configuration.

## Boundaries & Constraints

**Always:** `PasswordHasher<User>` (sans Identity complet) ; clé JWT ≥ 32 octets vérifiée au démarrage, l'API refuse de démarrer sinon ; `Jwt:Key` et `Admin:Password` uniquement en user-secrets (tests : config en mémoire) ; `MapInboundClaims = false`, `NameClaimType = "sub"`, `RoleClaimType = "role"` ; l'identifiant ne se lit que par `ClaimsPrincipalExtensions.UserId()` ; policy `Admin` = `RequireClaim("role", "Admin")` déclarée une fois ; inscription publique → toujours `Membre` ; email normalisé (trim, minuscules) ; heure via `TimeProvider` ; DTO et routes de `contrats-api.md` §4 et §6 ; erreurs via `Result` / `ApiErrors` uniquement.

**Décisions (2026-10-14) :** email déjà utilisé à l'inscription → 400 `errors.email` « Un compte existe déjà avec cet email. » (aucun nouveau code au §5), y compris en cas d'inscription concurrente (violation d'unicité) ; téléphone : l'API accepte un numéro français à 10 chiffres commençant par 0 (espaces, points ou tirets tolérés) ou un numéro international `+` suivi de 8 à 15 chiffres (mêmes séparateurs tolérés), et le stocke en E.164 (`06 12 34 56 78` → `+33612345678`) ; tout autre format → 400 `errors.telephone`.

**Never:** ASP.NET Core Identity complet ; rôle lu ailleurs que dans le claim `role` ; création d'un Admin depuis le site ; mot de passe ou jeton journalisé ; route absente de §4 dans le code de production (la route admin de test vit dans les tests) ; front (story 6).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Inscription OK | `InscriptionDto` complet | 201 `AuthReponseDto`, rôle `Membre`, téléphone stocké en E.164 | — |
| Sans téléphone | `telephone` vide ou absent | 400 `errors.telephone` | — |
| Téléphone français | `"06.12 34-56 78"` | stocké et renvoyé `+33612345678` | format inconnu → 400 `errors.telephone` |
| Email déjà pris | email existant (casse ou espaces différents) | 400 `errors.email` | — |
| Champs invalides | email mal formé, mot de passe < 8, prénom/nom vides | 400 `errors` par champ (camelCase) | — |
| Connexion OK | identifiants justes | 200 `AuthReponseDto` | — |
| Identifiants faux | email inconnu ou mot de passe faux | 400 `code: identifiants-invalides`, même message dans les deux cas | — |
| Session | `GET /api/auth/moi` avec jeton | 200 `UtilisateurDto` | sans jeton ou jeton invalide → 401 `{ error }` |
| Membre sur admin | jeton Membre sur une route du groupe admin | 403 `{ error: "Cette page est réservée à l'association." }` | — |
| Seed admin | `Admin:Email` / `Admin:Password` configurés | compte Admin créé, ou existant remis Admin avec ce mot de passe | config absente → l'API refuse de démarrer |

</frozen-after-approval>

## Code Map

- `server/FitClub.Api/Auth/AuthSetup.cs`, `JwtOptions.cs` (validator), `TokenService.cs` -- modèle : options JwtBearer configurées via `IOptions<JwtOptions>`, `OnChallenge` + `HandleResponse`, garde 32 octets, `JsonWebTokenHandler`, `ValidAlgorithms` HS256, `ClockSkew` 30 s. Écarts : claims `role`, `NameClaimType = "sub"`, `RoleClaimType = "role"`, `OnForbidden`, réponses via `ApiErrors` (options JSON globales) et non `new JsonSerializerOptions`.
- `server/FitClub.Api/Auth/AuthEndpoints.cs` -- modèle de validation champ par champ, normalisation email, hachage factice contre l'énumération d'emails, `SuccessRehashNeeded`, unicité concurrente. Écarts : handlers CQRS, `identifiants-invalides` en 400.
- `server/FitClub.Api.Tests/JwtKeyGuardTests.cs` -- modèle du test de refus de démarrage sans clé.
- `aaas/api/Aaas.Api/Common/` -- `Result`, `ApiErrors` (`MessageNonConnecte`, `MessageInterdit`), `IQueryHandler` / `ICommandHandler`, `SqlErreurs.EstViolationUnicite`, `TransactionRunner` : à réutiliser, ne pas dupliquer.
- `aaas/api/Aaas.Api/Program.cs` -- garde `IOptions<SiteOptions>.Value` puis `Migrate()` puis `UseApiErrors()` ; insérer garde JWT, seed admin après `Migrate()`, `UseAuthentication/UseAuthorization` après `UseApiErrors()`.
- `aaas/api/Aaas.Api.Tests/AaasFactory.cs`, `EndpointsDeTest.cs` -- config en mémoire à compléter (`Jwt:*`, `Admin:*`) ; le filtre de test ajoute ses routes après son propre `UseRouting`, donc il doit aussi appeler `UseAuthentication`/`UseAuthorization` pour sa route admin.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Aaas.Api.csproj` -- `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12.
- [x] `aaas/api/Aaas.Api/Features/Auth/User.cs`, `UserConfiguration.cs` -- entité (`Prenom`, `Nom`, `Email` unique, `Telephone`, `MotDePasseHash`, `Role` enum `Membre`/`Admin`, `CreeLe`), longueurs max.
- [x] `aaas/api/Aaas.Api/Data/Migrations/*_Auth_User.cs` -- `dotnet ef migrations add Auth_User`.
- [x] `aaas/api/Aaas.Api/Features/Auth/JwtOptions.cs`, `AdminOptions.cs`, `Jetons.cs` -- options validées au démarrage (clé ≥ 32 octets, `Admin:Email` valide, `Admin:Password` ≥ 8) ; émission du jeton (60 min, claims `sub`, `email`, `role`, `jti`).
- [x] `aaas/api/Aaas.Api/Features/Auth/Telephone.cs` -- normalisation et validation E.164 (règle des Décisions).
- [x] `aaas/api/Aaas.Api/Features/Auth/Contrats/` -- `InscriptionDto`, `ConnexionDto`, `AuthReponseDto`, `UtilisateurDto` (§6).
- [x] `aaas/api/Aaas.Api/Features/Auth/Commands/Inscrire.cs`, `Commands/Connecter.cs`, `Queries/ObtenirMoi.cs` -- handlers ; email déjà pris selon les Décisions (pré-vérification + `SqlErreurs.EstViolationUnicite`).
- [x] `aaas/api/Aaas.Api/Features/Auth/SeedAdmin.cs` -- crée ou met à jour le compte Admin (`Prenom` « Administrateur », `Nom` « AAAS », `Telephone` = `Site:Telephone`).
- [x] `aaas/api/Aaas.Api/Features/Auth/AuthSetup.cs`, `AuthEndpoints.cs` -- JwtBearer (`OnChallenge` 401, `OnForbidden` 403 via `ApiErrors`), policy `Admin`, routes §4.
- [x] `aaas/api/Aaas.Api/Common/ClaimsPrincipalExtensions.cs`, `Common/GroupeAdmin.cs` -- `UserId()` ; `MapGroupeAdmin(prefixe)` = groupe exigeant la policy `Admin`, réutilisé par les slices.
- [x] `aaas/api/Aaas.Api/Program.cs` -- garde JWT + Admin, seed après `Migrate()`, authentification/autorisation, `MapAuthEndpoints()`.
- [x] `aaas/api/Aaas.Api/appsettings.json`, `aaas/README.md` -- `Jwt:Issuer/Audience/ExpiresMinutes`, `Admin:Email` (placeholder marqué) ; README : `dotnet user-secrets set` pour `Jwt:Key` et `Admin:Password`.
- [x] `aaas/api/Aaas.Api.Tests/AaasFactory.cs`, `EndpointsDeTest.cs` -- secrets de test en mémoire ; route `/tests/admin` via `MapGroupeAdmin`.
- [x] `aaas/api/Aaas.Api.Tests/AuthTests.cs` -- une couverture par ligne de la matrice, plus : refus de démarrage sans clé ou clé courte, rôle lu par la policy, jeton expiré via `FakeTimeProvider` → 401.

**Acceptance Criteria:**
- Given une API démarrée avec `Jwt:Key` et `Admin:*` en user-secrets, when on se connecte avec l'admin seedé puis on appelle `GET /api/auth/moi`, then le rôle renvoyé est `Admin`.
- Given le code de production, when on cherche une lecture de claim hors `ClaimsPrincipalExtensions`, then aucune.
- Given `dotnet test`, when la suite tourne, then tous les tests passent, y compris ceux des stories 1.1 et 1.2.

## Implementation Notes

- Durée de vie du jeton validée par un `LifetimeValidator` qui lit `TimeProvider` (tolérance 30 s) : sans lui, les jetons émis à l'heure du `FakeTimeProvider` seraient jugés sur l'horloge réelle, et le test d'expiration serait impossible.
- Noms de claims (`sub`, `email`, `role`, `jti`) déclarés une fois dans `ClaimsPrincipalExtensions` ; nom de la policy dans `GroupeAdmin.Politique`, policy déclarée uniquement dans `AuthSetup`. `/api/auth/moi` reçoit `HttpContext` (pas de `using System.Security.Claims` hors de `ClaimsPrincipalExtensions`, pour que la vérification grep reste nette).
- Téléphone : en plus de la règle des Décisions, `00…` (préfixe international) et un indicatif `+0…` sont refusés, puisque le résultat ne serait pas un E.164 valide.
- Règles email (normalisation, forme) regroupées dans `Features/Auth/AdresseEmail.cs`, partagé par l'inscription, la connexion, `AdminOptionsValidator` et le seed.
- `GET /api/auth/moi` pour un `sub` qui ne correspond à aucun compte → 404 (`Result` n'a pas de 401 ; aucune route ne supprime de compte aujourd'hui).
- Le seed lit `Site:Telephone` tel quel (déjà E.164 par configuration).
- Vérifié : `dotnet build` 0 erreur 0 avertissement ; `dotnet test` 93/93 ; grep des lectures de claims → seulement `Common/ClaimsPrincipalExtensions.cs`. Critère 1 (admin seedé → `/moi` rôle `Admin`) couvert par test avec configuration en mémoire, pas par un `dotnet run` avec de vrais user-secrets.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 2, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `SeedAdmin` copie `Site:Telephone` sans validation E.164 | medium | patch | `SiteOptions.Telephone` n'a que `[Required]` ; `"+33 6 00 00 00 00"` (17 car.) fait planter le seed sur `nvarchar(16)`, `"01 23 45 67 89"` est stocké hors E.164. Correctif : passer par `Telephone.Normaliser`, échec de démarrage explicite si `null`. |
| 2 | Changer `Admin:Email` laisse l'ancien compte Admin | medium | patch | Le seed ne rétrograde personne ; scénario réel (base démarrée avec le placeholder puis email remplacé). L'architecture ne prévoit qu'un admin (Deferred : « si plusieurs admins existent un jour »). Correctif : le seed repasse `Membre` tout autre compte `Admin`. |
| 3 | `Seed_est_idempotent` ne vérifie pas l'idempotence | low | patch | N'assert que la connexion. Correctif : vérifier un seul compte admin et `MotDePasseHash` inchangé après redémarrage. |
| 4 | AC1 vérifié seulement avec config en mémoire | low | rejected | Pas un défaut du code : le chemin réel (user-secrets, Development) est un contrôle manuel signalé à l'utilisateur. |
| 5 | `Jeton_expire…` avance l'horloge partagée de la collection sans la remettre | low | rejected | Aucun test actuel ne dépend de l'instant initial ; `FakeTimeProvider` ne recule pas, isoler demanderait une factory dédiée. À garder en tête pour les tests d'expiration de l'epic 3. |

## Design Notes

Le hachage factice sur email inconnu (FitClub) est conservé : même temps de réponse, même message, pas d'énumération d'emails par la connexion. Le seed est idempotent : il trouve l'utilisateur par email normalisé, force `Role = Admin` et rehache le mot de passe s'il ne vérifie plus.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 erreur, 0 avertissement.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts.
- `grep -rnE "FindFirst|\.Claims" aaas/api/Aaas.Api --include=*.cs` -- expected: seulement `Common/ClaimsPrincipalExtensions.cs`.
