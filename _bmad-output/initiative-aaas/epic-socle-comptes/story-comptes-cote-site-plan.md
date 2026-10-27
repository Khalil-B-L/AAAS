---
title: 'Comptes côté site'
type: 'feature'
ticket: '6'
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
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** L'API sait inscrire, connecter et protéger les routes admin (story 1.3), mais le site n'a ni formulaires, ni session, ni gardes : un visiteur ne peut pas créer de compte et aucune page membre ou admin n'est protégée (CAP-1).

**Approach:** Dans `aaas/web`, un `AuthService` unique à signals (jeton en `localStorage`, session restaurée par `GET /api/auth/moi`), un intercepteur unique, des gardes membre et admin avec `returnUrl`, les pages Connexion, Inscription et Accès refusé, le menu membre/admin dans l'en-tête et un espace admin vide (AD-9, AD-21).

## Boundaries & Constraints

**Always:** un seul `AuthService` (`core/`) possède le jeton (`localStorage`) et l'utilisateur (signal) ; le rôle vient de `UtilisateurDto`, jamais d'un décodage du jeton ; un seul intercepteur ajoute `Authorization: Bearer` sur les requêtes vers `/api` ; au démarrage, si un jeton existe, `/api/auth/moi` est appelé et les gardes attendent cette restauration (délai max 10 s, le démarrage de l'app n'attend pas) ; 401 hors `/api/auth/*` → déconnexion locale puis `/connexion?returnUrl=<page>` ; 403 → `/acces-refuse` ; `returnUrl` n'accepte qu'un chemin interne (commence par `/`, pas `//`), sinon `/` ; interfaces `core/api/` identiques au glossaire §6 ; erreurs affichées selon `code` / `errors`, jamais selon le texte ; erreurs de champ reliées par `aria-describedby` ; cibles ≥ 48 px ; composants et jetons de la story 1.5.

**Never:** décodage du JWT côté client ; jeton ailleurs que dans `AuthService` ; déconnexion sur un mauvais mot de passe (400 de `/api/auth/connexion`) ; NgRx ; écrans admin réels (epics 2 à 4) ; changement de l'API.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Inscription OK | formulaire complet | jeton stocké, utilisateur connecté, retour à `returnUrl` ou `/` | — |
| Inscription refusée | 400 `errors` (téléphone manquant, email pris…) | message sous chaque champ concerné, saisie conservée | — |
| Connexion OK | identifiants justes | connecté, retour à `returnUrl` ou `/` | — |
| Identifiants faux | 400 `code: identifiants-invalides` | message sur le formulaire, aucun effet sur la session | — |
| Rechargement | jeton en `localStorage` | `/moi` restaure l'utilisateur ; le menu membre s'affiche | 401 → jeton effacé, visiteur |
| Page membre sans session | `/mes-reservations` | `/connexion?returnUrl=%2Fmes-reservations` | — |
| Page admin par un Membre | `/admin/animaux` | `/acces-refuse` (« Cette page est réservée à l'association. », retour à l'accueil) | — |
| Admin | `/admin` | `/admin/reservations` (page vide), pastille « Admin » dans l'en-tête | — |
| 401 en cours de route | requête membre hors `/api/auth/*` répond 401 | déconnexion locale, `/connexion?returnUrl=<page courante>` | — |
| `returnUrl` externe | `?returnUrl=https://evil.fr` ou `//evil.fr` | retour à `/` | — |
| Déconnexion | menu → Déconnexion | jeton effacé, menu redevient « Connexion », accueil | — |

</frozen-after-approval>

## Code Map

- `aaas/web/src/app/core/site.service.ts`, `app.config.ts` -- modèle de service à signal chargé par `provideAppInitializer` sans attente ; ajouter `provideHttpClient(withInterceptors([...]))`.
- `aaas/web/src/app/app.routes.ts` -- routes de §7 : `/mes-reservations`, `/mes-favoris`, `/animaux/:id/reserver`, `/reservations/:id/envoyee` (garde membre), groupe `admin` (garde admin ; son `**` actuel redirige vers `/`, à remplacer par `''` → `reservations`). Garder `RACINE_VERS_ACCUEIL` et les tests de `app.routes.spec.ts`.
- `aaas/web/src/app/features/auth/auth.routes.ts` -- pages vides `connexion`, `inscription`, `acces-refuse` à remplacer par les vrais composants.
- `aaas/web/src/app/layout/en-tete/` -- bouton « Connexion » (ligne 40 du HTML), burger, lien actif, Échap sur l'en-tête : à étendre (menu membre, liens admin, pastille) sans casser ses tests.
- `aaas/web/src/styles.css` -- jetons et utilitaires (`bouton-pilule`, `bouton-primaire`, `bouton-contour`, `titre-section`) ; ajouter les styles de champ et d'alerte de DESIGN.md § Components s'ils manquent.
- API (lecture seule) : `aaas/api/Aaas.Api/Features/Auth/AuthEndpoints.cs`, `Contrats/*.cs` -- routes et formes réelles ; `ApiErrors` : `{ error, errors?, code? }`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/web/src/app/core/api/auth.dto.ts` -- `InscriptionDto`, `ConnexionDto`, `AuthReponseDto`, `UtilisateurDto`, `ErreurApi` (§6, AD-10).
- [x] `aaas/web/src/app/core/auth/auth.service.ts` -- signals `utilisateur`, `estConnecte`, `estAdmin` ; `inscrire`, `connecter`, `deconnecter`, `restaurer` (promesse attendue par les gardes, 10 s max) ; jeton en `localStorage`.
- [x] `aaas/web/src/app/core/auth/auth.interceptor.ts` -- Bearer sur `/api` ; 401 hors `/api/auth/*` → déconnexion + connexion avec `returnUrl` ; 403 → `/acces-refuse`.
- [x] `aaas/web/src/app/core/auth/gardes.ts`, `core/auth/return-url.ts` -- `membreGuard`, `adminGuard` (fonctionnels) ; validation du `returnUrl`.
- [x] `aaas/web/src/app/features/auth/connexion/`, `inscription/`, `acces-refuse/`, `auth.routes.ts` -- formulaires réactifs (Connexion : email, mot de passe, lien « Créer mon compte » qui garde `returnUrl` ; Inscription : prénom, nom, email, téléphone obligatoire avec l'aide « Votre numéro permet à l'association de vous rappeler. » et l'exemple `+216 72 000 000`, mot de passe) ; page Accès refusé.
- [x] `aaas/web/src/app/app.routes.ts`, `app.config.ts` -- gardes sur les routes membre et admin, `/admin` → `/admin/reservations`, intercepteur, restauration au démarrage.
- [x] `aaas/web/src/app/layout/en-tete/` -- non connecté : bouton Connexion ; connecté : bouton menu (prénom) ouvrant Mes réservations, Mes favoris, Déconnexion ; Admin : en plus Réservations, Animaux, Actualités ; pastille « Admin » après le sigle sur les routes `/admin/**`.
- [x] Tests Vitest à côté de chaque fichier -- une couverture par ligne de la matrice (service, intercepteur, gardes, `returnUrl`, formulaires avec erreurs serveur, en-tête connecté/admin).

**Acceptance Criteria:**
- Given l'API lancée avec ses secrets et `npm start`, when on parcourt inscription → rechargement → déconnexion → membre sur `/admin` → admin seedé sur `/admin`, then chaque étape se comporte comme la matrice (vérification humaine).
- Given le code de `src/app`, when on cherche `localStorage` ou un décodage de jeton (`atob`, `jwt`), then seulement dans `core/auth/auth.service.ts`.
- Given `npx ng test --watch=false` et `npx ng build`, then tout passe, budgets compris.

## Implementation Notes

- `AuthService.restaurer()` est aussi tranchée par une connexion, une inscription ou une déconnexion explicite (les gardes n'attendent pas `/moi` après une connexion) ; sur échec réseau ou délai de 10 s, l'utilisateur reste visiteur mais le jeton est gardé (seul un 401 l'efface).
- Ajouts hors liste : `core/api/erreur-api.ts` (`lireErreurApi`) et `features/auth/erreurs-formulaire.ts` (choix de l'affichage selon `code` puis `errors`, partagé par Connexion et Inscription).
- Pas de validation côté client : l'API valide et renvoie `errors` ; le focus va au premier champ en erreur, l'alerte de formulaire est en `role="alert"`.
- Admin : liens du menu regroupés sous le libellé « Association ». `/admin/inconnue` retombe sur le `**` racine (→ `/`).
- Tests de `app.routes.spec.ts` gardés, joués avec une session Admin ; `/admin` retiré de la liste « redirige vers / » (il mène désormais à `/admin/reservations`).

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 1, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | L'intercepteur déconnecte sur un 401 d'une requête partie avec un ancien jeton | low | patch | `auth.interceptor.ts` appelle `deconnecter()` sans comparer le jeton envoyé au jeton courant ; une requête lente de la session A peut effacer la session B ouverte entre-temps. `chargerSession` fait déjà ce contrôle. Correctif : ne déconnecter que si le jeton envoyé est encore le jeton courant (ou si aucun jeton n'était envoyé). |

## Verification

**Commands:**
- `npx ng test --watch=false` (dans `aaas/web`) -- expected: tous verts.
- `npx ng build` (dans `aaas/web`) -- expected: succès sans dépassement de budget.
- `grep -rnE "localStorage|atob\(" aaas/web/src/app --include=*.ts | grep -v spec` -- expected: seulement `core/auth/auth.service.ts`.
