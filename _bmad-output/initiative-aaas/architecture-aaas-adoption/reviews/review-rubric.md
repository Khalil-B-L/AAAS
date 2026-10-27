# Revue « rubric » — architecture-aaas-adoption.md

- **Lens :** rubric walker (Good-spine checklist de `reviewer-gate.md`)
- **Date :** 2026-10-14
- **Spine relue :** `architecture-aaas-adoption.md` (status draft) + `.memlog.md`
- **Entrées confrontées :** `spec-aaas-adoption.md`, `state-machines.md`, `stack.md`, `EXPERIENCE.md`, code réel `server/FitClub.Api` (+ `FitClub.Api.Tests`)
- **La spine n'a pas été modifiée.**

## Verdict

Spine solide et bien structurée (12 CAP couverts, enveloppe opérationnelle explicite, Deferred sans fuite de divergence), mais **pas prête pour le handoff** : 4 findings high laissent deux unités diverger (frontière Animaux/Reservations autour de l'expiration, concurrence des transitions admin, sérialisation UTC des dates, claim de rôle JWT), et plusieurs tags `[ADOPTED]` prétendent reprendre de FitClub ce que le code n'a pas. Aucun critical.

## Synthèse par critère de la checklist

| Critère | Résultat |
| --- | --- |
| Vrais points de divergence couverts, aucun manqué | Partiel : statut effectif d'un animal côté lecture, concurrence admin/expiration, `DateTime.Kind`, nom du claim de rôle, représentation âge/taille manquent (H1–H4, M5) |
| Chaque Rule applicable et empêche sa divergence | Partiel : AD-1 contredit AD-4/AD-5 (H1) ; AD-11 non applicable tel quel avec `DateTime` + EF SQL Server (H3) ; AD-8 incomplet (H4) |
| Rien dans Deferred ne laisse diverger deux unités | OK (seule réserve : contrat de liste sans pagination, L7) |
| Tech nommée vérifiée et actuelle | OK pour .NET 10 / EF 10.0.12 / JwtBearer 10.0.12 / xUnit 2.9.3 (vérifiés dans les csproj FitClub) ; Angular « 22 » non épinglé, librairie SMTP ouverte (L1) |
| Ratifie FitClub sans le contredire | Partiel : AD-6, AD-10 (base), JWT HS256/60 min/garde de clé, `sub`, TimeProvider, factory de tests confirmés ; AD-2, AD-7, AD-8 tagués ADOPTED mais introduisent du neuf ou s'écartent de FitClub sans le dire (M1, L2, L3) |
| Couvre CAP-1..12 | OK : les 12 CAP sont mappées ; voir M5 (CAP-2), M6 (CAP-7) pour les trous de décision |
| Aucune dimension silencieuse (dont enveloppe opérationnelle) | Globalement OK (AD-17, secrets, journalisation, tests, Deferred hébergement/CI/backups/monitoring) ; manque le mode « sans SMTP » dev/test et le cadrage du BackgroundService en test (M8) |

## Vérification des AD [ADOPTED] contre le code réel de FitClub

| AD | Prétention | Constat dans `server/FitClub.Api` | Verdict |
| --- | --- | --- | --- |
| AD-2 `[ADOPTED pour CQRS]` | CQRS via handlers ; aucun endpoint ne touche `AppDbContext` | FitClub n'a aucun handler : `AuthEndpoints` et `SessionEndpoints` injectent `AppDbContext` directement dans les endpoints. Le tag renvoie à la contrainte de la spec, pas à FitClub. | Écart non déclaré (M1) |
| AD-6 `[ADOPTED, modèle FitClub]` | Serializable + nouvel essai borné sur interblocage | Confirmé : `SessionEndpoints.TryBook` en `IsolationLevel.Serializable`, `MaxDeadlockRetries = 3`, `ChangeTracker.Clear()`, détection SQL 1205 ; violation d'unicité 2601/2627 via `AuthEndpoints.IsUniqueViolation`. L'index FitClub n'est pas filtré (nouveau mais cohérent). | Ratifié |
| AD-7 `[ADOPTED, FitClub]` | Un `AppDbContext`, `IEntityTypeConfiguration` + `ApplyConfigurationsFromAssembly`, `Migrate()` au démarrage, enums en chaîne, ids `int` | Confirmé : contexte unique, `Database.Migrate()` dans `Program.cs`, LocalDB `(localdb)\MSSQLLocalDB`. **Non** : FitClub configure tout en ligne dans `OnModelCreating`, n'a aucun enum, et `Session.Id` est une `string`. | Partiellement adopté (M1) |
| AD-8 `[ADOPTED]` | PasswordHasher, HS256, clé ≥ 32 o vérifiée au démarrage, 60 min, rôle en claim, policy `Admin`, seed admin | Confirmé : `PasswordHasher<User>`, HS256, `JwtOptionsValidator` (32 octets) + `ValidateOnStart` + garde dans `Program.cs`, `ExpiresMinutes = 60`, `sub` seul identifiant. **Absent** : aucun rôle, aucun claim de rôle (claims émis : sub, email, name, jti), aucune policy, aucun seed de compte, pas de téléphone. `MapInboundClaims = false` et aucun `RoleClaimType` configuré. | Partiellement adopté ; trou bloquant (H4) |
| AD-10 `[ADOPTED, FitClub ApiErrors]` | `{ error }` / `{ error, errors }`, messages FR, + champ `code` | Confirmé pour la forme (`ErrorResponse(Error, Errors)`, 400 « Certains champs sont invalides. », 401 personnalisé via `OnChallenge`). `code` est une extension (à ajouter au record). **Pas de `OnForbidden`** : un 403 FitClub a un corps vide. | Ratifié + extension ; trou 403 (M2) |
| AD-11 `[ADOPTED pour TimeProvider]` | Heure via `TimeProvider`, stockage UTC | Confirmé : `services.AddSingleton(TimeProvider.System)`, `clock.GetUtcNow().UtcDateTime`. FitClub ne renvoie jamais au front une date relue en base, donc le problème `DateTimeKind.Unspecified` n'y apparaît pas (H3). Les tests FitClub ne remplacent pas `TimeProvider` (L2). | Ratifié pour TimeProvider ; règle « Z » non garantie (H3) |
| Conventions « Tests (modèle FitClub) » | xUnit + WebApplicationFactory + LocalDB jetable | Confirmé : `FitClubFactory` crée `FitClubTests_{guid}` et `EnsureDeletedAsync` en fin de run, environnement `Testing`, config en mémoire. | Ratifié |
| Conventions « Configuration et secrets » | Jamais de secret dans `appsettings*.json` | FitClub met une clé `DEV-ONLY` dans `appsettings.Development.json`. AAAS s'en écarte (choix défendable) sans le dire. | Écart non déclaré (L3) |

## Findings

### Critical

Aucun.

### High

**H1 — Frontière Animaux / Reservations incohérente autour de l'expiration (AD-1 vs AD-3 vs AD-4 vs AD-5).**
- AD-1 : « Seule la slice propriétaire écrit ses entités » et « une slice n'appelle jamais les handlers d'une autre slice » ; mais AD-4/AD-5 font écrire `Animal.Statut` par les commandes de Reservations (`Animal.Reserver()`, `Liberer()`, `MarquerAdopte()`).
- AD-3 dit que l'expiration paresseuse est « une commande appelée avant la lecture » ; AD-5 dit que les lectures utilisent le prédicat `ReservationRules.EstActive(now)` et que la commande ne précède que réserver/confirmer/annuler. Les deux lectures sont possibles.
- Conséquence : les requêtes Animaux (catalogue CAP-2, fiche CAP-3, accueil) ont besoin du statut effectif. Un développeur lira `Animal.Statut` brut (animal affiché Réservé jusqu'à 30 s après l'échéance, ce qui casse « Réserver à nouveau » à 00:00 dans EXPERIENCE) ; un autre appellera la commande `ExpirerReservationsEchues` de Reservations depuis Animaux, ce qu'AD-1 interdit ; un troisième recopiera le prédicat. `ReservationRules` est une règle métier de Reservations, et AD-1 l'interdit dans `Common/`.
- **Correction :** (a) dans AD-1, déclarer l'exception explicitement : « `Animal.Statut` n'est écrit que par les méthodes de l'entité Animal (AD-4) ; les commandes de Reservations peuvent les appeler ». (b) Choisir un seul modèle de lecture et l'écrire dans AD-3 et AD-5 de la même façon. Recommandé : les requêtes ne déclenchent jamais de commande. Un statut effectif est calculé par une projection partagée (`AnimalStatutEffectif` dans `Common/Domain` ou exposée par Reservations en lecture seule) : `Reserve` seulement s'il existe une réservation `EnAttente` avec `ExpiresAt > now`, sinon `Disponible`. Le filtre du catalogue utilise cette projection. Placer `ReservationRules.EstActive` à un emplacement nommé, accessible en lecture aux deux slices.

**H2 — Transitions de réservation hors `ReserverAnimal` sans contrôle de concurrence.**
- AD-6 ne protège que la création. Confirmer, Annuler (admin sur téléphone et sur PC en même temps, double toucher) et le `BackgroundService` d'expiration peuvent agir sur la même réservation en parallèle. Sans jeton de concurrence ni mise à jour conditionnelle, on peut obtenir `Confirmee` et `Expiree` en concurrence, ou l'animal `Adopte` puis remis `Disponible` par `Liberer()`.
- Aucun code d'erreur n'est prévu pour « réservation déjà traitée » (Confirmer sur une réservation déjà Annulée ou Confirmée), alors que la carte admin doit se recharger proprement.
- **Correction :** ajouter à AD-5/AD-6 (ou dans un nouvel AD) : toute transition de `Reservation` et d'`Animal.Statut` est protégée par un `rowversion` (`IsRowVersion()`) sur `Reservation` et `Animal`, ou par une mise à jour conditionnelle (`WHERE Statut = 'EnAttente' AND ExpiresAt > @now`). `DbUpdateConcurrencyException` ou zéro ligne modifiée donne 409 `reservation-deja-traitee`. Ajouter ce code à AD-10. Le `BackgroundService` ignore silencieusement les conflits.

**H3 — AD-11 « ISO 8601 avec `Z` » n'est pas garanti avec la persistance prévue.**
- Avec `DateTime` + EF Core SQL Server (`datetime2`), les valeurs relues en base ont `DateTimeKind.Unspecified` ; System.Text.Json les sérialise **sans `Z`**. Le navigateur les interprète en heure locale (UTC+1 en Tunisie) : `expiresAt` dans Mes réservations et Admin · Réservations est décalé d'une heure, et le compte à rebours tombe à 00:00 dès l'affichage. La réponse 201 de création (entité en mémoire, `Kind = Utc`) est juste, ce qui masque le bug au premier test. FitClub ne rencontre pas ce cas parce qu'il ne renvoie aucune date lue en base.
- **Correction :** fixer le mécanisme dans AD-11 : soit toutes les dates en `DateTimeOffset` (colonne `datetimeoffset`), soit un `ValueConverter` global dans `ConfigureConventions` (`DateTime` relu en `DateTime.SpecifyKind(v, DateTimeKind.Utc)`). Ajouter un test d'intégration qui vérifie le suffixe `Z` sur `GET /api/reservations` et sur la liste admin.

**H4 — AD-8 : le claim de rôle et la policy `Admin` ne sont pas spécifiés, et FitClub n'en fournit aucun modèle.**
- FitClub n'émet aucun claim de rôle et configure `MapInboundClaims = false` sans `RoleClaimType`. Un `RequireRole("Admin")` ne fonctionnera donc avec aucun claim `role` brut. Deux développeurs choisiront `role`, `ClaimTypes.Role` ou un claim maison, et la policy et le front divergeront.
- AD-9 dit que le rôle affiché vient de la réponse de connexion, mais `UserDto` de FitClub (`Id`, `Name`, `Email`) n'a pas de rôle, et la restauration après rechargement de page n'est pas décrite (voir M3).
- **Correction :** compléter AD-8 : claim `role` (valeurs `Membre` / `Admin`), `TokenValidationParameters.RoleClaimType = "role"`, policy `Admin` = `RequireClaim("role", "Admin")`, déclarée une seule fois dans la configuration de l'authentification. `UserDto` contient `role` et `telephone`. Retirer le tag `[ADOPTED]` des parties nouvelles (rôles, policy, seed admin, téléphone), ou écrire « [ADOPTED partiel : hachage, JWT, garde de clé] ».

### Medium

**M1 — Les tags `[ADOPTED]` prétendent plus que FitClub ne fournit (AD-2, AD-7, AD-8).** AD-2 rompt avec FitClub (endpoints → `AppDbContext` direct) sans le dire ; AD-7 introduit `IEntityTypeConfiguration`, `ApplyConfigurationsFromAssembly` et `HasConversion<string>` absents de FitClub. *Correction :* pour chaque AD, séparer « repris de FitClub » et « nouveau dans AAAS ». Écrire dans AD-2 : « s'écarte volontairement de FitClub, où les endpoints accèdent au DbContext ».

**M2 — 403 et 404 de route hors `ApiErrors`.** FitClub n'a pas d'`OnForbidden` (le 403 a un corps vide) et pas de fallback 404. AD-10 dit « toute erreur passe par ApiErrors ». *Correction :* ajouter `OnForbidden` (corps `{ error: "Cette page est réservée à l'association." }`), un `MapFallback` pour `/api/**` qui renvoie un 404 `ApiErrors`, et un gestionnaire d'exceptions qui renvoie un 500 `{ error }` sans détail.

**M3 — Restauration de la session front.** AD-9 interdit de décoder le jeton pour accorder un droit, mais ne dit pas comment `AuthService` retrouve l'utilisateur et le rôle après un rechargement (stocker `user` avec le jeton, ou appeler `GET /api/auth/me` au démarrage). *Correction :* au démarrage, si un jeton existe, `AuthService` appelle `/api/auth/me` (qui renvoie le rôle) avant d'activer les gardes ; un 401 déclenche la déconnexion locale.

**M4 — Échec SMTP après commit laissé en Open Question alors qu'il change le comportement de CAP-4.** Un développeur fait `await` sur l'envoi dans la requête : une exception donne un 500 alors que la réservation est créée, et Gmail ralentit la réponse de plusieurs secondes. Un autre envoie en fire-and-forget. *Correction :* trancher dans AD-13. Recommandé : envoi après commit dans un `try/catch` avec timeout court, échec journalisé, réponse toujours 201. Contact reste le seul cas qui renvoie une erreur (le statut à préciser, par exemple 503).

**M5 — Représentation de l'âge, de la taille et des ententes pour les filtres (CAP-2) non décidée.** EXPERIENCE parle de « listes » pour l'âge et la taille et de filtres d'ententes « Oui ou À voir ». Le contrat de requête n'est pas fixé (âge en mois ou en tranches ? taille en enum ? le filtre « Enfants » renvoie-t-il `Oui ∪ AVoir` ?). La slice Animaux (API) et la feature front divergeront. *Correction :* ajouter à la convention « Filtres du catalogue » : enums `TrancheAge` / `Taille` (valeurs listées) et les noms des paramètres de requête. Préciser la sémantique : le filtre d'entente correspond à `Oui` ou `AVoir`.

**M6 — Les Open Questions « forçage de statut avec réservation active » et « suppression d'un animal réservé ou en favori » bloquent CAP-7.** `ForcerStatut(Reserve)` sans réservation laisse l'animal Réservé pour toujours (le `BackgroundService` ne libère que via une réservation). La suppression dépend du comportement de cascade d'EF. *Correction :* proposer des défauts et les écrire dans AD-4 avant les stories CAP-7. Par exemple : `ForcerStatut` refuse `Reserve` ; forcer `Disponible` ou `Adopte` annule d'abord la réservation `EnAttente` dans la même transaction ; supprimer un animal supprime ses favoris en cascade et est refusé (409 `animal-a-des-reservations`) s'il a des réservations.

**M7 — Fichiers : service de `/fichiers/{clé}`, validation des uploads et contrat d'image d'actualité non fixés.** AD-12 dit que seul `IFileStorage` lit les fichiers, donc `/fichiers` doit être un endpoint qui passe par `IFileStorage` et non `UseStaticFiles`, mais ce n'est pas écrit. La clé doit être validée (pas de `..`, format GUID + extension). Formats et taille sont en Open Question, donc chaque endpoint validera à sa façon. L'upload de l'image d'actualité (multipart avec l'actualité ou endpoint séparé) n'est pas dit. *Correction :* écrire ces trois points dans AD-12 avec des valeurs par défaut (par exemple jpeg/png/webp ≤ 5 Mo, vérifiés par type MIME et signature, constantes dans `Common/`).

**M8 — Enveloppe dev/test incomplète.** Rien ne dit comment l'API démarre sans `Smtp:Password` (nouveau développeur, tests), si `ValidateOnStart` sur les options SMTP bloque le démarrage, si l'`ExpirationReservationsService` tourne pendant les tests `WebApplicationFactory` (risque de non-déterminisme), ni quel port ou schéma vise le proxy (FitClub fait `UseHttpsRedirection`, donc un proxy vers `http` est redirigé). *Correction :* ajouter à AD-17 ou aux conventions : `IEmailSender` « fichier/journal » en `Testing` (et en option en `Development`) ; `BackgroundService` désactivable par configuration et désactivé en `Testing` (les tests appellent la commande directement) ; cible du proxy `https://localhost:<port>` avec `secure: false`, port fixé dans `launchSettings.json`.

### Low

- **L1 — Stack non épinglée :** Angular « 22 » sans minor/patch, Node ouvert, librairie SMTP ouverte, package de faux temps (`Microsoft.Extensions.TimeProvider.Testing`) absent alors que les tests en dépendent. *Correction :* épingler à l'initialisation et clore les Open Questions de version.
- **L2 — « TimeProvider remplacé… (modèle FitClub) » :** FitClub ne remplace pas `TimeProvider` dans ses tests. *Correction :* reformuler (« nouveau : `FakeTimeProvider` injecté dans la factory »).
- **L3 — Secrets :** FitClub garde une clé `DEV-ONLY` dans `appsettings.Development.json` ; AAAS l'interdit. *Correction :* déclarer l'écart, exiger `UserSecretsId` dans le csproj et fournir les secrets aux tests par configuration en mémoire (comme `FitClubFactory`).
- **L4 — Champs d'inscription :** EXPERIENCE demande prénom + nom, le modèle FitClub n'a que `Name` ; le format du téléphone (+216…) n'est pas fixé. *Correction :* noter `Prenom`, `Nom`, `Telephone` (format E.164 ou libre validé) dans la convention ou AD-8.
- **L5 — 401 en cours de saisie :** AD-9 déconnecte et redirige, ce qui fait perdre la saisie d'un formulaire, alors qu'EXPERIENCE demande de conserver la saisie en cas d'erreur. *Correction :* préciser que le `returnUrl` ramène au formulaire et que les brouillons simples (questionnaire, contact) sont gardés en mémoire de service.
- **L6 — Nouvelles réservations invisibles pour l'admin tant qu'il ne recharge pas :** cohérent avec le Deferred « temps réel », mais à dire dans AD-11 pour que le front n'invente pas un sondage.
- **L7 — Pagination différée :** fixer dès maintenant la forme des listes (tableau nu ou enveloppe `{ items, total }`) pour que l'ajout futur de la pagination ne casse pas le contrat.
- **L8 — Réponse `limite-reservations` :** l'écran de refus liste les 2 réservations avec leur temps restant ; le 409 ne dit pas si elles sont incluses dans la réponse. *Correction :* soit les inclure avec `serverNow`, soit fixer que le front appelle `GET /api/reservations`.
- **L9 — Ordre des photos :** supprimer la photo principale ou réordonner les photos n'est pas décrit (renumérotation d'`Ordre`). *Correction :* une ligne dans AD-12.

## Compte

- Critical : 0
- High : 4 (H1–H4)
- Medium : 8 (M1–M8)
- Low : 9 (L1–L9)
