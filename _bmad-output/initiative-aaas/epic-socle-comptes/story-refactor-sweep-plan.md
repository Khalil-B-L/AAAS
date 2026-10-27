---
title: 'Refactor sweep'
type: 'refactor'
ticket: '7'
created: '2026-10-15'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'oneshot'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** L'epic 1 laisse trois écarts retenus : la saisie du téléphone suit encore la règle française alors que l'association est en Tunisie (décision du 2026-10-14, `deferred-work.md`) ; un jeton valide dont le compte n'existe plus fait répondre 404 à `/api/auth/moi`, que le front traite comme une panne réseau et garde donc le jeton indéfiniment ; le nettoyage de la base de test saute l'arrêt de l'hôte si la suppression échoue.

**Approach:** API : `Telephone.Normaliser` accepte un numéro tunisien de 8 chiffres (premier chiffre non nul, séparateurs espace/point/tiret tolérés) et le convertit en `+216…`, garde la forme internationale `+` 8 à 15 chiffres, et ne reconnaît plus la forme française `0X XX XX XX XX` ; le placeholder `Site:Telephone` passe à `+21672000000` ; messages et exemples en conséquence (`72 000 000`). Front : `AuthService.restaurer` efface le jeton sur 404 comme sur 401 ; le champ téléphone de l'inscription montre l'exemple `72 000 000`. Tests : `AaasFactory.DisposeAsync` appelle `base.DisposeAsync()` dans un `finally`. Les autres constats écartés pendant les revues de l'epic restent écartés.

</frozen-after-approval>

## Implementation Notes

Route oneshot : changement d'environ 100 lignes sur des fichiers déjà connus (règle de normalisation, ses tests, un placeholder, une condition côté front, un `finally`).

- `Features/Auth/Telephone.cs` : forme nationale tunisienne (8 chiffres, premier non nul) → `+216` ; la forme française `0X…` n'est plus reconnue (cas de test ajouté) ; la forme internationale `+` reste acceptée (un `+33…` passe toujours). Constante `IndicatifTunisie`.
- `Commands/Inscrire.cs` : exemple du message d'erreur → « 72 000 000 ou +33 6 12 34 56 78 ».
- `appsettings.json` : placeholder `Site:Telephone` → `+21672000000` (toujours marqué PLACEHOLDER).
- `AuthTests.cs` : cas acceptés / refusés réécrits pour la règle tunisienne ; `AaasFactory.SiteTelephone` → `+21672123456`.
- `AaasFactory.DisposeAsync` : `base.DisposeAsync()` dans un `finally`.
- Front : `AuthService.chargerSession` efface le jeton sur 404 comme sur 401 (test ajouté) ; placeholder du champ téléphone → `72 000 000` (test mis à jour).
- Vérification : `dotnet test` 122 réussis + 1 ignoré ; `ng test` 139 réussis ; `ng build` sans avertissement.
- L'entrée de `deferred-work.md` sur le téléphone tunisien est traitée par ce changement (le fichier n'est pas modifié : entrées existantes intouchables).

## Verification

**Commands:**
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts, un ignoré (envoi réel).
- `npx ng test --watch=false` et `npx ng build` (dans `aaas/web`) -- expected: tous verts, build sans dépassement de budget.

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `SeedAdmin.cs:35` : le message d'erreur de `Site:Telephone` décrit encore la règle française | low | patch | Un opérateur qui suit le message (« 10 chiffres commençant par 0 ») échouerait de nouveau. Corrigé : « ex. +21672000000, ou 8 chiffres tunisiens, ex. 72 000 000 ». |
| 2 | `auth.service.ts:57` : la doc de `restaurer()` ne mentionne que le 401 | low | patch | Le code efface aussi sur 404. Corrigé : « 401 ou 404 → jeton effacé ». |

Après correctifs : `dotnet test` 122 réussis + 1 ignoré ; `ng test` 139 réussis ; `ng build` sans avertissement.
