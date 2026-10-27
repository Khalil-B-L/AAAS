---
epic: epic-socle-comptes
date: 2026-10-15
verdict: accepted-with-open-items
criteria: declared
headless: false
---

# Rétrospective — epic « Socle et comptes »

Verdict : **accepted-with-open-items**, par décision humaine du 2026-10-15 (verdict machine : rejected, deux critères « Done when » non démontrés : envoi Gmail réel, parcours navigateur).

## Epic summary

- **Epic** : `epic-socle-comptes` (`_bmad-output/initiative-aaas/epic-socle-comptes/`), couvre CAP-1 et E1.
- **Tickets** (ordre de build, tous `status: built`, `state: review` — aucun `pending_tickets`) :

| Ref | Titre | Plan | Risque | Revue |
| --- | --- | --- | --- | --- |
| 1.1 | Squelette API, web et tests | `story-squelette-api-web-et-tests-de-bout-en-bout-plan.md` | medium | quick, 3 patchs / 3 rejetés |
| 1.2 | Briques communes backend | `story-briques-communes-backend-plan.md` | medium (relevé de low) | quick, 2 patchs / 2 rejetés |
| 1.3 | Comptes côté API | `story-comptes-cote-api-plan.md` | medium | quick, 3 patchs / 2 rejetés |
| 1.4 | Envoi d'emails | `story-envoi-d-emails-plan.md` | medium | quick, 4 patchs |
| 1.5 | Habillage Angular | `story-habillage-angular-plan.md` | low | quick, 3 patchs / 1 rejeté |
| 1.6 | Comptes côté site | `story-comptes-cote-site-plan.md` | medium | quick, 1 patch |
| 1.7 | Refactor sweep | `story-refactor-sweep-plan.md` (oneshot) | low | quick, 2 patchs |

- **Tickets encore à `built`** : les sept (aucun n'est passé `done` ; la clôture relève de `bmad-ticket`).
- **Plages de diff** : aucune — le projet n'est pas un dépôt git (`baseline_revision: NO_VCS` dans les sept plans). Pas de commits, pas de `git_evidence.py`. Substitut : diffs par story produits pendant le build contre des instantanés (scratchpad de session, non conservés dans le dépôt) et un diff de l'epic entier (toute l'arborescence `aaas/` en ajout, 8 458 lignes) pour la revue. Les volumes de changement par fichier ne sont donc pas mesurés ; la vue « taille » se fonde sur la taille actuelle des fichiers.
- **Inventaire des preuves** :
  - disponibles : fichier d'epic, `initiative-aaas.md`, architecture + `contrats-api.md`, 7 plans (Implementation Notes, Review Triage Log), tout le code `aaas/`, `deferred-work.md` de l'initiative, transcript de la session de build (celle-ci) ;
  - absents : historique git, rétrospective précédente (premier epic de l'initiative), fichiers story (aucun ticket tiré en story).

## Findings

Chaque constat porte sa source et sa disposition (**fix now** / **defer** / **accept**). Les constats déjà triés dans les Review Triage Log des plans ne sont pas répétés. Lentilles : vues agrégées (sous-agent), adversarial, edge-case-hunter, verification-gap (via `bmad-review`), vérification de comportement (exécutée en session).

### Frontière auth API (1.3) ↔ site (1.6)

| # | Constat | Source | Disposition |
| --- | --- | --- | --- |
| F1 | Le rôle de la policy Admin vient du claim figé dans le JWT, celui du site vient de `/api/auth/moi` (base). Après une promotion ou rétrogradation par `SeedAdmin`, les deux divergent jusqu'à 60 min (admin rétrogradé garde ses droits API ; membre promu voit l'espace admin mais reçoit 403). | `Features/Auth/AuthSetup.cs:43-44`, `Jetons.cs:34`, `SeedAdmin.cs:37-42` ; `web/src/app/core/auth/auth.service.ts:26`, `gardes.ts:21` | **defer** — n'arrive qu'au redémarrage de l'API avec un autre `Admin:Email` ; à traiter avec la révocation de jeton (déjà en Deferred de l'architecture). |
| F2 | Le site ignore `expireLe` : après 60 min, l'en-tête et les gardes considèrent encore la session valide ; l'expiration n'est découverte qu'au prochain 401. | `auth.service.ts:97-103`, `gardes.ts:9-10,17-21` ; API `Jetons.cs:22` | **defer** — sans effet tant qu'aucune route membre n'existe ; à traiter dans l'epic 3 (formulaire de réservation : saisie perdue sur 401). |
| F3 | `/moi` en échec réseau ou délai dépassé : jeton gardé, utilisateur `null`, restauration jamais retentée ; l'intercepteur continue d'envoyer le jeton. | `auth.service.ts:76-97` | **defer** (bas). |
| F4 | Aucun écouteur `storage` : se déconnecter dans un onglet laisse les autres onglets sur l'ancienne identité. | `auth.service.ts:20,106-124` | **defer** (bas). |
| F5 | `returnUrl` pris sur `router.url` : pendant une navigation, c'est la page précédente ; sur `/connexion` même, retour à la page de connexion. | `auth.interceptor.ts:27`, `return-url.ts:5-11` | **defer** (bas). |
| F6 | Formulaires : toute erreur sans `code` ni `errors` (500, 503, 400 de corps invalide, 429) affiche « Vérifiez votre connexion et réessayez », jamais le message `error` de l'API ; aucun test de formulaire sur un 500. | `features/auth/erreurs-formulaire.ts:4-5,38` (vérifié) | **fix now** — afficher `api.error` quand le corps API est lisible, garder le message réseau pour l'absence de corps. |
| F7 | `/api/auth/moi` répond 404 pour un compte disparu (le site en dépend depuis 1.7) mais aucun test API ne fixe ce statut ; §4 ne liste que 200. | `Queries/ObtenirMoi.cs:21` ; `Aaas.Api.Tests/AuthTests.cs` (aucun cas) ; `contrats-api.md` §4 | **fix now** (test) + **réconciliation de spec** (§4). |
| F8 | Aucun test ne passe par la vraie chaîne de démarrage auth (`appConfig` : intercepteur → `restaurer()` → `Bearer` sur `/moi` → `withComponentInputBinding` pour `returnUrl`) ; chaque spec monte ses propres providers. | `web/src/app/app.spec.ts:74-94` (seul usage d'`appConfig`, ne teste que `/api/site`) | **fix now** (test). |
| F9 | Le 403 « Membre sur route admin » n'est prouvé que sur la route de test `/tests/admin` ; en réel, un Membre sur `/api/admin/...` reçoit 404 (aucune route admin n'existe encore). | `Aaas.Api.Tests/EndpointsDeTest.cs:49` ; vérification de comportement (étape 8) | **accept** — attendu tant que les epics 2-4 n'ont pas ajouté de route admin ; le 403 sera vérifiable dès la première. |

### Chaîne de démarrage, configuration, emails

| # | Constat | Source | Disposition |
| --- | --- | --- | --- |
| F10 | `Site:Telephone` n'est contrôlé que par `[Required]` ; son format n'est vérifié que dans `SeedAdmin`, après `Migrate()` ; une valeur nationale (`72 000 000`) est acceptée et `/api/site` la renvoie telle quelle → lien `tel:72 000 000` sans indicatif. | `Features/Site/SiteOptions.cs:11`, `Queries/ObtenirSite.cs:14` (vérifié : valeur brute), `SeedAdmin.cs:33-35`, `Program.cs:46-50` | **fix now** — valider (ou normaliser) `Site:Telephone` en E.164 dans les options du site, avant `Migrate()`. |
| F11 | Le seed ne met pas à jour le téléphone (ni le prénom/nom lors d'une promotion) d'un compte admin existant : le compte admin garde `+33600000000` alors que le site est passé en `+21672000000`. | `SeedAdmin.cs:45-66` ; vérification de comportement (étape 9) | **fix now** (bas, correctif direct). |
| F12 | Le choix du mode email lit `IOptions<SmtpOptions>.Value`, ce qui exécute le validateur Smtp même quand le mode effectif est Journal : une section `Smtp` invalide empêche de démarrer en Journal, contrairement au commentaire. | `Common/Email/EmailSetup.cs:28-31` (vérifié) | **fix now** (bas). |
| F13 | Le repli Smtp → Journal sans mot de passe s'applique dans tout environnement autre que `Testing`, pas seulement `Development` (AD-17). | `EmailSetup.cs:44-55` ; architecture AD-17 | **accept** pour l'exécution locale ; à revoir à la mise en ligne (voir A9). |
| F14 | Le mode Journal écrit le corps complet des emails (données personnelles) en `Information`. | `Common/Email/JournalEmailSender.cs:11` | **defer** — exécution locale uniquement ; à traiter avec l'hébergement. |
| F15 | La règle email partagée accepte des adresses que MimeKit refuse (`a,b@x.fr`) : inscription ou config acceptées, envoi en échec plus tard. | `Common/AdresseEmail.cs:11`, `SmtpEmailSender.cs` | **defer** (bas). |
| F16 | `Jwt:ExpiresMinutes` n'a pas de borne haute alors qu'AD-8 fixe 60 min. | `Features/Auth/JwtOptions.cs:38` | **fix now** (bas, correctif d'une ligne). |
| F17 | Pas de limitation de débit sur `POST /api/auth/connexion` ; un seul compte admin dont l'email est dans `appsettings.json`. | `AuthEndpoints.cs`, `Program.cs` ; architecture Deferred (seul le formulaire de contact y figure) | **defer** + **réconciliation de spec** (ajouter la connexion au Deferred « limitation de débit »). |
| F18 | L'inscription révèle qu'un email a un compte (message de champ, réponse rapide), ce qui rend la protection anti-énumération de la connexion partielle. | `Commands/Inscrire.cs:55-56,84` ; `Connecter.cs` (hachage factice) | **accept** — choix produit (message sous le champ, décision 2026-10-14) ; à consigner dans l'architecture. |
| F19 | En Development, un corps JSON mal formé est journalisé en `fail` (erreur serveur) alors qu'il s'agit d'une erreur client. | journal de la vérification de comportement (étape 11) ; `Common/ErreursHttp.cs` | **defer** (bas, bruit de log). |

### Vues agrégées

| # | Constat | Source | Disposition |
| --- | --- | --- | --- |
| F20 | Sens des dépendances API respecté (aucun `Features` dans `Common/`/`Data/` après correction en 1.4) ; seule arête Features→Features : `SeedAdmin` lit `SiteOptions` (options seulement, prévu par le plan 1.3). | `SeedAdmin.cs:3,28` ; `story-envoi-d-emails-plan.md` triage #4 | **accept**. |
| F21 | Front : aucun import feature→feature ni core→features ; mais un dossier `layout/` hors Structural Seed et `shared/` qui importe `core/`. | `web/src/app/layout/`, `shared/date-fr.pipe.ts:3`, `shared/bouton-appel/bouton-appel.ts:2` | **réconciliation de spec** (Structural Seed : ajouter `layout/` et la règle `shared → core`). |
| F22 | Doublons de tests : `TrouverValidation` ×3, redémarrage d'hôte ×2, `PostJsonAsync` ×2, `AssertErreurAsync` ×2 (signatures différentes) ; front : fixture « Sonia » ×7, « connecter comme » ×3. | `SocleTests.cs:70`, `AuthTests.cs:619`, `EmailTests.cs:419`, … ; specs `core/auth/*`, `features/auth/*`, `en-tete.spec.ts` | **defer** — extraire des helpers de test partagés au début de l'epic 2 (ils vont encore servir). |
| F23 | Quatre styles de garde de démarrage et trois styles de validation d'options (DataAnnotations, `IValidateOptions`, validation conditionnelle) ; `AddSiteOptions` vit dans le fichier d'endpoints. | `Program.cs:37`, `AuthSetup.cs:52`, `EmailSetup.cs:62`, `SeedAdmin.cs:33`, `SiteOptions.cs`, `SiteEndpoints.cs:8` | **defer** — à converger en traitant F10. |
| F24 | AD-10 : `ResultHttpExtensions` construit lui-même `ErrorResponse` pour le cas `Errors` au lieu de passer par `ApiErrors` ; `ApiErrors.Validation`, `BadRequest`, `Refus`, `Interne` sans appelant. | `Common/ResultHttpExtensions.cs:18`, `Common/ApiErrors.cs:14,51-52` | **defer** (bas). |
| F25 | AD-6 : aucune commande n'utilise encore `TransactionRunner` (écritures simples dans `Inscrire`, `Connecter`). | `Inscrire.cs:69-78`, `Connecter.cs:54` | **accept** — écriture unique, pas de transaction multi-étapes ; préciser AD-6 (« toute commande à plusieurs écritures »). |
| F26 | Les listes de routes §7 sont recopiées en dur dans deux tests (API et front) sans lien avec la table ni entre elles. | `CommonTests.cs:262-284`, `web/src/app/app.routes.spec.ts:49-70` | **accept** — garde-fou suffisant à cette échelle. |
| F27 | Tailles saines : aucun fichier C# > 101 lignes ; plus gros fichiers : `styles.css` (395), `en-tete.css` (207), `AuthTests.cs` (626 lignes, mélange intégration/unitaire), `EmailTests.cs` (465, embarque un faux serveur SMTP TLS). | comptage des lignes, sous-agent vues agrégées | **defer** — scinder `AuthTests` et sortir le faux SMTP en fichier d'outillage (F22). |

### Processus (focus demandé)

| # | Constat | Source | Disposition |
| --- | --- | --- | --- |
| P1 | Pays des numéros découvert à la story 1.5 : la règle de saisie de 1.3 (France, `06…` → `+33…`) avait été proposée et validée alors que l'association est en Tunisie (maquettes en +216, parcours à Hammamet) ; corrigé en 1.7. La spec et l'architecture ne nomment ni pays ni indicatif. | `story-comptes-cote-api-plan.md` (Décisions), `story-habillage-angular-plan.md` (Décision), `deferred-work.md`, `story-refactor-sweep-plan.md` | **réconciliation de spec** + leçon : vérifier les hypothèses de localisation contre les maquettes et les parcours avant de proposer un choix. |
| P2 | Stack non installable telle qu'écrite (SDK 10.0.402 / runtime 10.0.13 absents du poste) : découvert en 1.1, décision utilisateur, table Stack corrigée. | `story-squelette-…-plan.md` (Décisions), architecture § Stack | **accept** — leçon : vérifier la Stack contre le poste à l'écriture de l'architecture. |
| P3 | Six plans sur sept au-dessus de la cible de 1 600 tokens (~1 700 à 2 500) ; l'utilisateur a choisi de les garder entiers à chaque fois. Aucune dérive d'implémentation attribuable à la taille n'apparaît dans les Review Triage Log. | gates de taille des plans 1.1–1.6 | **accept** — leçon : découper les stories de l'epic 2 un peu plus fin, ou relever la cible pour les stories de socle. |
| P4 | Les implémenteurs ont écrit deux fois dans le Plan Change Log (réservé aux reprises après revue) ; corrigé à la main en 1.2 et 1.5. | `story-briques-communes-backend-plan.md`, `story-habillage-angular-plan.md` (Implementation Notes) | **defer** — leçon de processus : rappeler la règle dans la consigne d'implémentation ou dans le template. |
| P5 | Chaque revue « quick » a trouvé au moins un défaut réel (18 patchs sur l'epic, dont 4 moyens : blocage au démarrage front, faux échec SMTP, seed sans validation E.164, ancien admin resté Admin). | Review Triage Log des 7 plans | **accept** (réussite) — garder une revue par story. |
| P6 | Aucune vérification humaine de bout en bout n'a été faite pendant l'epic : parcours navigateur, envoi Gmail réel, comparaison visuelle aux maquettes ; elles se sont accumulées. | AC manuels des plans 1.1, 1.3, 1.4, 1.5, 1.6 | **fix now** (voir A1) — leçon : planifier une session de vérification manuelle par epic. |
| P7 | Pas de dépôt git : ni commits ni diffs entre plans ; la rétrospective n'a pas pu mesurer les volumes de changement par story. | `baseline_revision: NO_VCS` ×7 | **fix now** (voir A2). |

## Behavior verification

Exercé en session le 2026-10-15, API lancée en `Development` (`dotnet run --launch-profile https`) avec `Jwt:Key` et `Admin:Password` passés en variables d'environnement temporaires :

1. `GET /api/site` → 200, numéro `+21672000000`.
2. Inscription sans téléphone → 400 `errors.telephone`.
3. Inscription avec `72 123 456` → 201, téléphone stocké `+21672123456`, rôle `Membre`, `expireLe` en UTC `Z`.
4. Même email en majuscules avec espaces → 400 `errors.email` « Un compte existe déjà avec cet email. ».
5. Mauvais mot de passe → 400 `code: identifiants-invalides`.
6. `/api/auth/moi` sans jeton → 401 `{ error }` ; 7. avec jeton → 200 `UtilisateurDto`.
8. Membre sur `/api/admin/reservations` → 404 (aucune route admin encore, voir F9).
9. Connexion de l'admin seedé (`admin@aaas-placeholder.fr`) → 200, rôle `Admin` ; téléphone resté `+33600000000` (F11).
10. Route inconnue → 404 `{ error }` ; 11. JSON mal formé → 400 `{ error }`, journalisé en `fail` (F19).
- Au démarrage : avertissement de repli email (Smtp sans mot de passe → Journal), attendu.

**Non exercé** : le front dans un navigateur (pas d'outil de navigation utilisé : formulaires, session après rechargement, menu, gardes), l'envoi Gmail réel, la comparaison visuelle aux maquettes. Ces points restent « jamais vérifiés », pas « vérifiés et propres ».

## Previous-retro follow-through

Rien à suivre : aucune rétrospective précédente n'existe (premier epic de l'initiative `initiative-aaas`).

## Action items

| # | Action | Propriétaire | Nature |
| --- | --- | --- | --- |
| A1 | Faire la vérification manuelle de l'epic : parcours navigateur (inscription avec `72 000 000`, rechargement, déconnexion, Membre sur `/admin`, admin sur `/admin`), comparaison à `mockups/accueil.html` à 375 et 1240 px, envoi Gmail réel (`AAAS_EMAIL_REEL=1`). | Khalil (utilisateur) | vérification — conditionne le verdict |
| A2 | Initialiser un dépôt git à la racine du projet et faire un premier commit de l'état actuel, avant l'epic 2. | Khalil | processus (P7) |
| A3 | Story de correctifs « socle » (une story ou un oneshot avant 2.1) : F10 (Site:Telephone validé/normalisé avant `Migrate()`), F11 (seed met à jour téléphone et identité), F12 (mode Journal sans validation Smtp), F16 (borne 60 min), F6 (message `error` de l'API dans les formulaires), F7 et F8 (tests `/moi` 404 et chaîne `appConfig`). | `bmad-build` (proposé) | remédiation proposée |
| A4 | Au début de l'epic 2 : helpers de test partagés (F22, F27) et convergence des gardes de démarrage (F23). | `bmad-build` (proposé) | remédiation proposée |
| A5 | Réconcilier `contrats-api.md` §4 : `GET /api/auth/moi` → 200 `UtilisateurDto` ; 401 sans jeton ; 404 compte introuvable. | Khalil / `bmad-architecture` | réconciliation de spec proposée (F7) |
| A6 | Réconcilier l'architecture : Structural Seed (`layout/`, `shared → core` autorisé) ; AD-6 limité aux commandes à plusieurs écritures ; inscription qui révèle les comptes existants consignée comme choix assumé ; pays Tunisie / indicatif +216 dans les conventions. | Khalil / `bmad-architecture` | réconciliation de spec proposée (F18, F21, F25, P1) |
| A7 | Ajouter au Deferred de l'architecture : limitation de débit de la connexion (F17), révocation/rafraîchissement du rôle (F1), journal email sans données personnelles hors Development (F14), repli Smtp limité à Development (F13). | Khalil / `bmad-architecture` | réconciliation de spec proposée |
| A8 | Epic 3 : gérer `expireLe` côté site avant le formulaire de réservation (F2) et la reprise de restauration après panne réseau (F3). | `bmad-ticket` (refinement epic 3) | report tracé |
| A9 | Processus : rappeler aux implémenteurs que le Plan Change Log est réservé aux reprises après revue (P4) ; découper les stories de l'epic 2 un peu plus fin (P3). | Khalil / `bmad-customize` | leçon de processus |

Reportés sans action immédiate (contexte suffisant ci-dessus) : F4, F5, F15, F19, F24.

## Acceptance verdict

**Machine : rejected (non accepté)** — critères déclarés (`epic-socle-comptes.md`, Done when). Toutes les stories sont terminées (aucun `pending_tickets`) ; le verdict tient aux preuves manquantes, pas à un échec observé :

| Done when | Preuve | Statut |
| --- | --- | --- |
| 1. `dotnet run` + `ng serve` affichent le shell sur téléphone et ordinateur | API + proxy + pied de page testés (`app.spec.ts`, `pied-de-page.spec.ts`) ; affichage ouvert par l'utilisateur le 2026-10-14 (sans comparaison formelle) | partiel — comparaison visuelle non faite |
| 2. Inscription sans téléphone refusée ; session conservée ; Membre refusé (403, `/acces-refuse`) | API vérifiée en exécution (étapes 2-7) ; front par tests Vitest ; 403 seulement sur route de test (F9) | partiel — parcours navigateur non fait |
| 3. Admin créé au premier démarrage, accède à l'espace admin | Connexion admin réelle en Development (étape 9) ; espace admin par tests | démontré (API) ; navigateur non fait |
| 4. `dotnet test` et Vitest passent ; suffixe Z et énums en texte prouvés | 122 réussis + 1 ignoré, 139 Vitest, exécutés en session ; `CommonTests` | **démontré** |
| 5. Journal écrit l'email ; Smtp envoie par Gmail | Journal testé ; envoi Gmail réel jamais exécuté | **non démontré** |

Constats ouverts non bloquants : F6, F7, F8, F10, F11, F12, F16 (A3). Aucun constat bloquant (sécurité ou perte de données en exécution locale) n'a été trouvé.

**Décision humaine (2026-10-15, Khalil)** : **accepted-with-open-items**. L'epic est accepté avec réserves ; A1 (vérifications manuelles) et A3–A9 restent ouverts et suivis.

## Open questions

1. (Tranché le 2026-10-15 : accepté avec réserves, A1 reste à faire.)
2. Les correctifs A3 doivent-ils passer avant l'epic 2 (recommandé : F10 touche le bouton d'appel de toutes les pages publiques), ou au fil de l'epic 2 ?
3. Admin unique : confirmer que l'association n'aura jamais plusieurs comptes admin (conditionne F1 et la rétrogradation automatique du seed).
