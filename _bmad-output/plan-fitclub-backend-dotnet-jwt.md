---
title: 'Backend C# .NET avec authentification JWT pour FitClub'
type: 'feature'
ticket: ''
created: '2026-10-13'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Les réservations FitClub vivent dans le localStorage de chaque navigateur et reposent sur un simple nom saisi : n'importe qui peut réserver ou annuler au nom d'un autre, et rien n'est partagé.

**Approach:** API ASP.NET Core (.NET 10) avec SQL Server LocalDB via EF Core, comptes membres (inscription / connexion) et authentification JWT Bearer. Le planning est public ; réserver et annuler exigent d'être connecté, et la réservation est liée au compte. L'API sert aussi le front `gym-app/` (même origine) ; le JWT est gardé dans le localStorage et envoyé en `Authorization: Bearer`.

## Boundaries & Constraints

**Always:** Mots de passe hachés avec `PasswordHasher<T>` (jamais stockés ni journalisés en clair). JWT HS256 signé par une clé de configuration `Jwt:Key` (≥ 32 octets), avec émetteur, audience et expiration (60 min) vérifiés. Au démarrage hors environnement Development, clé absente ou trop courte → l'API refuse de démarrer. Email normalisé (trim + minuscules) et unique. Messages d'erreur en français. Échec de connexion : même message que l'email existe ou non.

**Never:** Pas de rôle admin, pas de refresh token, pas de réinitialisation de mot de passe, pas de connexion externe. Pas d'ASP.NET Core Identity complet (UI, tables Identity). Pas de CORS. Le formulaire de contact reste côté navigateur. Ne pas toucher `.claude/`, `.agents/`, `_bmad/`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Inscription OK | Nom, email inédit, mot de passe ≥ 8 caractères | 201 + jeton + profil ; l'utilisateur est connecté | No error expected |
| Inscription invalide | Nom vide, email mal formé ou mot de passe < 8 | 400 | Message par champ fautif |
| Email déjà pris | « LEA@x.fr » alors que « lea@x.fr » existe | 409 | « Un compte existe déjà avec cet email » |
| Connexion OK | Bons identifiants | 200 + jeton | No error expected |
| Connexion refusée | Mauvais mot de passe ou email inconnu | 401 | « Email ou mot de passe incorrect » (identique dans les deux cas) |
| Planning anonyme | GET planning sans jeton | 200, places restantes, aucune réservation marquée « à moi » | No error expected |
| Réserver sans jeton / jeton expiré ou falsifié | POST réservation | 401 ; le front efface le jeton et propose la connexion | « Connectez-vous pour réserver » |
| Réservation OK | Connecté, places libres | 200, places −1, séance marquée réservée par moi | No error expected |
| Séance complète | 0 place restante | 409 | « Ce cours est complet » |
| Double réservation | Déjà inscrit à la séance | 409 | « Vous avez déjà réservé ce cours » |
| Séance inconnue | id inexistant | 404 | « Séance introuvable » |
| Annulation | Connecté et inscrit | 200, place libérée | Non inscrit → 404 « Vous n'avez pas réservé ce cours » |
| Isolation | B tente d'annuler la réservation de A | A reste inscrit | B reçoit 404 (il n'a pas de réservation) |

</frozen-after-approval>

## Code Map

- `gym-app/index.html`, `app.js`, `styles.css` -- front existant à adapter ; `contact.js` + `tests/contact.test.js` -- à garder tels quels.
- `gym-app/booking.js`, `gym-app/tests/booking.test.js` -- logique localStorage remplacée par l'API : à supprimer. Reprendre ses 12 séances (ids `lun-07`…, jour, heure, cours, coach, capacité) et ses messages d'erreur ; le nombre d'inscrits fictifs devient `PreBooked` (Spinning mercredi reste complet : capacité 3, `PreBooked` 3).
- Outils : SDK .NET 10.0.401, modèles `webapi`/`xunit`, NuGet joignable, `(localdb)\MSSQLLocalDB` présent. Pas de git.

## Tasks & Acceptance

**Execution:**
- [x] `server/FitClub.sln`, `server/FitClub.Api/` -- projet `webapi` net10.0, API minimales ; packages EF Core SqlServer + Design, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.Extensions.Identity.Core` ; outil local `dotnet-ef` (manifeste dans `server/`)
- [x] `server/FitClub.Api/Data/` -- `AppDbContext`, entités `User` (email unique), `Session` (id texte, `SortOrder`, `PreBooked`), `Booking` (index unique SessionId+UserId) ; seed des 12 séances via `HasData` ; migration initiale ; `Database.Migrate()` au démarrage
- [x] `server/FitClub.Api/Auth/` -- émission/validation JWT (claims `sub`, `email`, `name`), options liées à `Jwt:*`, garde de démarrage sur la clé ; endpoints `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`
- [x] `server/FitClub.Api/Sessions/` -- `GET /api/sessions` (public ; `bookedByMe` si jeton), `POST` et `DELETE /api/sessions/{id}/booking` (`[Authorize]`), vérification de capacité dans une transaction `Serializable` -- évite de dépasser la capacité sur la dernière place
- [x] `server/FitClub.Api/Program.cs`, `appsettings*.json` -- chaîne LocalDB `FitClub`, clé JWT de dev uniquement dans `appsettings.Development.json`, fichiers statiques servis depuis `Frontend:Path` (défaut `../../gym-app`) avec `index.html` par défaut
- [x] `server/FitClub.Api.Tests/` -- xUnit + `WebApplicationFactory` ; base LocalDB jetable `FitClubTests_<guid>` supprimée en fin de run ; un test par ligne de la matrice + garde de clé JWT
- [x] `gym-app/api.js` -- client `fetch` : jeton en localStorage, en-tête Bearer, sur 401 efface le jeton et émet un événement
- [x] `gym-app/index.html`, `app.js`, `styles.css` -- zone « Mon compte » (onglets Connexion / Inscription, erreurs par champ), en-tête « Connecté : <nom> · Déconnexion », planning chargé depuis l'API, champ « Votre nom » retiré ; supprimer `booking.js` et son test
- [x] `gym-app/README.md`, `server/README.md` -- lancer avec `dotnet run --project server/FitClub.Api`, ouvrir l'URL affichée, lancer les tests

**Acceptance Criteria:**
- Given l'API lancée, when on ouvre l'URL racine, then la page FitClub s'affiche avec le planning chargé depuis l'API et sans erreur console.
- Given un membre connecté, when il recharge la page, then il reste connecté et voit ses réservations.
- Given deux navigateurs connectés à deux comptes, when l'un réserve, then l'autre voit une place de moins après rechargement.
- Given la base `FitClub`, when on lit la table des utilisateurs, then aucun mot de passe n'apparaît en clair.

## Implementation Notes

- `Microsoft.Extensions.Identity.Core` not referenced explicitly: it ships in the ASP.NET Core shared framework (NU1510 warning), `PasswordHasher<T>` is used from there. Other packages pinned to 10.0.12; `dotnet-ef` 10.0.12 in `server/dotnet-tools.json`.
- JWT key guard is enforced in every environment (stricter than "hors Development"); Development gets its key from `appsettings.Development.json`, tests from in-memory config.
- `dotnet new sln` defaults to `.slnx` on SDK 10.0.401; created with `--format sln` to match `server/FitClub.sln`.
- Booking: Serializable transaction + retry (3 attempts) on SQL deadlock 1205; unique-index violation mapped to 409 duplicate. Login with unknown email still runs a hash verification (timing parity).
- Static files: the whole `gym-app/` folder is served (incl. `tests/`, `README.md`) -- public front code only.
- Verified: `dotnet test server/FitClub.sln` 25/25, `npm test` 3/3, `node --check` OK, curl smoke run against LocalDB `FitClub` (hash stored, not clear text). Browser manual check not performed.
- Vérification orchestrateur : `dotnet test` 25/25, `npm test` 3/3, `node --check` OK ; parcours Chrome contre l'API lancée (anonyme → invitation, inscription, réservation 8→7, complet, rechargement connecté, annulation, jeton expiré et jeton falsifié → déconnexion propre, 375 px sans débordement). Seuls messages console restants : les 409/401 attendus des scénarios d'erreur. Comptes de test supprimés de la base `FitClub`.
- Correctifs de revue (front) : contrôle de `exp` avant chaque requête dans `api.js`, jeton effacé si `/api/auth/me` échoue, `askLogin` appelé une seule fois sur 401, favicon `data:,`.

## Plan Change Log

## Review Triage Log

**Passe 1 (quick)** — high 0 · medium 1 · low 5 · false 2 · maybe-false 0 (+1 constat ajouté par l'orchestrateur, inclus)

| Constat | Verdict | Route | Preuve |
|---|---|---|---|
| Jeton expiré : `GET /api/sessions` étant public, l'en-tête reste « Connecté » mais les réservations disparaissent, sans invitation à se reconnecter | medium | patch | Confirmé : `api.js` n'efface le jeton que sur un 401 ; le test `Planning_avec_jeton_invalide_reste_public` montre que la liste ne renvoie jamais 401. Correctif : contrôle de `exp` côté client avant chaque requête. |
| Restauration au démarrage : si `/api/auth/me` échoue autrement qu'en 401, le visiteur est affiché déconnecté mais le jeton est conservé (« Annuler » visible) | low | patch | Confirmé `app.js:272-274` : `setUser(null)` sans `clearToken`, puis `loadSessions()` envoie toujours le Bearer. Correctif direct : effacer le jeton. |
| Requête automatique `/favicon.ico` en 404 → erreur console (AC 1) | low | patch | Reproduit : `curl /favicon.ico` → 404 et ligne « Failed to load resource 404 » dans le parcours Chrome. Correctif : `<link rel="icon" href="data:,">`. |
| `askLogin` appelé deux fois sur un 401 de réservation/annulation | low | patch | Confirmé `app.js:149-154` et `:252-253` ; sans effet fonctionnel, correctif = suppression d'un doublon. |
| Troisième deadlock (1205) non intercepté → 500 ; aucun gestionnaire global d'exceptions | low | rejeté | Réel mais improbable en usage courant (3 deadlocks consécutifs sur la même séance, ou panne SQL) ; le correctif ajoute un middleware/une branche. |
| Corps JSON absent ou invalide → 400 en anglais du binding des API minimales | low | rejeté | Réel, mais le front envoie toujours un JSON valide ; seuls des appels manuels à l'API le voient ; le correctif ajoute de la gestion d'erreur. |
| Tout `gym-app/` est servi publiquement | low | rejeté | Réel mais sans fuite : seuls des fichiers front publics ; un `.env` ne serait pas servi (`ServeUnknownFileTypes` est faux par défaut, extension inconnue). |
| Connexion sans borne de longueur de mot de passe → travail PBKDF2 non borné | false | rejeté | PBKDF2-HMAC réduit une clé plus longue que le bloc à son hachage une seule fois : le coût des itérations ne dépend pas de la longueur ; seul le coût linéaire de lecture du corps reste, comme pour tout endpoint. |
| Garde de clé JWT active aussi en Development, contrairement à la règle figée | false | rejeté | La règle exige le refus hors Development ; elle n'impose pas de démarrer sans clé en Development. Plus strict, sans contradiction ; Development fonctionne avec sa clé de dev. |

## Design Notes

Le jeton en localStorage est exposé en cas de XSS : le front n'insère que du texte (`textContent`), à conserver. Le `sub` du JWT est la seule source de l'identité pour réserver/annuler ; aucun id utilisateur ne vient du corps de la requête.

## Verification

**Commands:**
- `dotnet test server/FitClub.sln` -- expected: tous les tests passent
- `cd gym-app && npm test` -- expected: tests contact OK
- `node --check gym-app/api.js && node --check gym-app/app.js` -- expected: aucune erreur

**Manual checks (if no CLI):**
- `dotnet run --project server/FitClub.Api` puis navigateur : inscription, déconnexion, connexion, réservation, rechargement, annulation, cours complet ; réserver déconnecté → invitation à se connecter.
