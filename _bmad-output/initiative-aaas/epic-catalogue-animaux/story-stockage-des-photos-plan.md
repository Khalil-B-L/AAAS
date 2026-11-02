---
title: 'Stockage des photos'
type: 'feature'
ticket: '6'
created: '2026-11-02'
status: 'built'
baseline_revision: '90d30b7ff15a640d5b7d4eec9814cf1f9bc5f4eb'
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

**Problem:** Les animaux n'ont aucune photo : la carte affiche un dégradé, la fiche (entrée 8) et le formulaire admin (entrée 12) n'ont rien sur quoi s'appuyer, et rien ne sert ni ne protège des fichiers envoyés par l'admin (CAP-3, CAP-7).

**Approach:** Construire le stockage unique des photos (AD-12) : `IFileStorage` sur disque derrière `Storage:PhotosPath`, `GET /fichiers/:cle`, la règle de validation unique `RegleFichier`, l'entité `PhotoAnimal` et sa migration, l'envoi et la suppression d'une photo par l'admin, et la photo principale dans `AnimalCarteDto`.

## Boundaries & Constraints

**Always:** seul `IFileStorage` (`Common/Fichiers/`) lit, écrit et supprime des fichiers et construit l'URL (`UrlPublique(cle)` → `/fichiers/<clé>`, relative) ; la base ne stocke que la clé générée par le serveur (GUID + extension issue du type détecté) ; `GET /fichiers/:cle` valide le format de la clé puis lit via `IFileStorage` (pas d'`UseStaticFiles`), clé mal formée ou absente → 404 `{ error }` ; `RegleFichier` unique : JPEG, PNG ou WebP vérifiés par type MIME **et** signature, 5 Mo max, 10 photos max par animal, refus → 400 `errors.fichier` ; upload `multipart/form-data` avec un unique champ `fichier`, une requête par photo, `POST /api/admin/animaux/:id/photos` → 201 `PhotoDto` (`id`, `url`, `ordre`) et `DELETE /api/admin/animaux/:id/photos/:photoId` → 204, dans le groupe admin (`MapGroupeAdmin`) ; `Ordre` = max + 1 attribué dans la transaction (`TransactionRunner`), jamais renuméroté à la suppression ; photo principale = `Ordre` minimal ; fichier écrit avant le commit et supprimé si le commit échoue ; fichier d'une photo supprimée effacé après le commit ; `PhotoAnimal` → `Animal` en `Cascade` (AD-7) ; `Storage:PhotosPath` validé au démarrage (garde de démarrage de la slice), relatif au dossier de l'API s'il n'est pas absolu ; tests sur un dossier temporaire par exécution, supprimé à la fin.

**Never:** chemin absolu ou nom de fichier client stocké en base ; fichier servi hors de `IFileStorage` ; redimensionnement ou compression (Deferred) ; CRUD admin des animaux (entrée 9) ; fiche et galerie (entrée 8) ; photos de démonstration (entrée 7) ; écran admin (entrée 12).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Upload valide | admin, `fichier` JPEG 200 Ko | 201 `PhotoDto` `{ id, url: "/fichiers/<guid>.jpg", ordre: 1 }` ; `GET` de l'URL → 200 avec le bon `Content-Type` | — |
| Ordre | 2e puis 3e upload | `ordre` 2 puis 3 ; carte : `photoPrincipaleUrl` = photo d'ordre 1 | — |
| Type refusé | GIF, PDF, ou JPEG déclaré mais signature PNG/texte | 400 `errors.fichier` ; aucun fichier écrit | — |
| Trop gros | 5 Mo + 1 octet | 400 `errors.fichier` | — |
| 11e photo | animal à 10 photos | 400 `errors.fichier` | — |
| Champ absent / vide | aucun `fichier` ou fichier de 0 octet | 400 `errors.fichier` | — |
| Animal inconnu | `:id` inexistant | 404 `{ error }` | — |
| Non admin | Membre ou anonyme | 403 / 401 `{ error }` | — |
| Suppression | `DELETE …/photos/:photoId` | 204 ; fichier effacé ; ordres restants inchangés ; nouvelle principale = ordre minimal restant | photo d'un autre animal → 404 |
| Clé invalide | `/fichiers/../appsettings.json`, `/fichiers/abc` | 404 `{ error }` | — |
| Commit en échec | erreur base après écriture du fichier | fichier supprimé, erreur propagée | — |
| Démarrage | `Storage:PhotosPath` vide | l'API refuse de démarrer, message nommant la clé | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Common/` -- `Result` (`Invalid(errors)`, `NotFound`), `ApiErrors`, `GroupeAdmin.MapGroupeAdmin`, `GardesDemarrage.AddGardeDemarrage`, `Data/TransactionRunner.RunAsync` (travail + `SaveChanges` + commit, vidage du `ChangeTracker` en cas d'échec) : à réutiliser ; y ajouter `Common/Fichiers/` (aucune dépendance vers `Features`).
- `aaas/api/Aaas.Api/Features/Animaux/` -- `Animal.cs` (ajouter la collection de photos), `AnimalConfiguration.cs`, `Contrats/AnimalCarteDto.cs` (`photoPrincipaleUrl` vaut `null` ligne 29 : la projection doit prendre l'URL de la photo d'`Ordre` minimal via `IFileStorage`), `AnimauxEndpoints.cs` (y ajouter un groupe admin), `DemoAnimaux.cs` (inchangé ici).
- `aaas/api/Aaas.Api/Features/Site/SiteEndpoints.cs` -- modèle d'options validées + garde de démarrage.
- `aaas/api/Aaas.Api/Program.cs` -- enregistrement du stockage, `MapFichiersEndpoints()` avant `MapApiFallback()`.
- `aaas/api/Aaas.Api/appsettings.json` -- ajouter `Storage:PhotosPath` (défaut relatif, ex. `App_Data/photos`) ; ajouter ce dossier au `.gitignore` racine.
- `aaas/api/Aaas.Api.Tests/` -- `AaasFactory.cs` (config en mémoire : y fixer un dossier temporaire par exécution, supprimé dans `DisposeAsync`), `Outils/` (`Animaux`, `Comptes`, `Jetons`, `RequetesApi`, `AssertionsErreur`) ; jetons admin et membre via les helpers existants.
- `aaas/web/proxy.conf.json` -- `/fichiers` est déjà relayé vers l'API ; aucun changement front.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Common/Fichiers/` -- `IFileStorage` (écrire, ouvrir, supprimer, `UrlPublique`, format de clé), implémentation disque, `StorageOptions` validées, `RegleFichier` (types, signatures, 5 Mo, 10 par animal).
- [x] `aaas/api/Aaas.Api/Features/Animaux/PhotoAnimal.cs`, `PhotoAnimalConfiguration.cs`, `Data/Migrations/*_Animaux_PhotoAnimal.cs` -- entité (`AnimalId`, `Cle`, `Ordre`), `Cascade`, index `(AnimalId, Ordre)` ; migration générée.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Commands/AjouterPhoto.cs`, `SupprimerPhoto.cs`, `Contrats/PhotoDto.cs` -- handlers transactionnels (écriture avant commit, nettoyage sur échec, suppression du fichier après commit).
- [x] `aaas/api/Aaas.Api/Features/Animaux/AnimauxEndpoints.cs`, `Common/Fichiers/FichiersEndpoints.cs`, `Program.cs`, `appsettings.json`, `.gitignore` -- routes, `GET /fichiers/:cle`, enregistrement, garde de démarrage, dossier par défaut ignoré par git.
- [x] `aaas/api/Aaas.Api/Features/Animaux/Contrats/AnimalCarteDto.cs` -- `photoPrincipaleUrl` = URL de la photo d'`Ordre` minimal ou `null`.
- [x] `aaas/api/Aaas.Api.Tests/` -- dossier de stockage temporaire dans la factory ; `Outils/Fichiers.cs` (contenus JPEG, PNG, WebP minimaux valides, faux JPEG, fichier trop gros) ; `PhotosTests.cs` couvrant chaque ligne de la matrice.

**Acceptance Criteria:**
- Given `dotnet test` et `dotnet build`, then tout passe sans avertissement, y compris les tests existants.
- Given le code de l'API, when on cherche `File.`, `Directory.` ou `FileStream` hors de `Common/Fichiers/`, then aucune occurrence en code de production.
- Given l'API lancée en Development avec le `Storage:PhotosPath` par défaut, when un admin envoie une photo par `POST …/photos`, then elle est servie par `http://localhost:4200/fichiers/<clé>` via le proxy (vérification humaine, `hitl`).

## Implementation Notes

- Index `(AnimalId, Ordre)` créé **unique** : garantit en base que `max + 1` n'attribue jamais deux fois le même ordre ; l'ajout s'exécute en `Serializable` (un interblocage est réessayé par le runner, la clé de fichier étant générée une fois hors du travail pour qu'un nouvel essai réécrive le même fichier).
- Le formulaire est lu par `HttpRequest.ReadFormAsync` (et non lié en `IFormFile`) : corps non multipart ou champ absent → 400 `errors.fichier` ; plusieurs fichiers dans `fichier` → 400 `errors.fichier` (« une seule photo par requête »).
- La validation taille/type/signature précède la transaction (aucune base touchée) ; existence de l'animal (404) puis nombre de photos sont vérifiés dans la transaction.
- `photoPrincipaleUrl` : la clé est lue en SQL (sous-requête), l'URL construite côté client par `IFileStorage.UrlPubliqueOuNull` ; `IFileStorage` est singleton (instance capturée par la projection) et la projection doit rester la dernière opération (`Take` déplacé avant le `Select` dans `ListerAnimaux`).
- Échec de suppression d'un fichier (après commit, ou nettoyage après échec) : journalisé en `Error`, la réponse n'en dépend pas.
- `GET /fichiers/:cle` ajoute `X-Content-Type-Options: nosniff`.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 1, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Corps multipart mal formé (sans boundary, tronqué) → 500 au lieu de 400 `errors.fichier` | medium | patch | `ReadFormAsync` lève `InvalidDataException`, transformée en 500 par `UseApiErrors` ; contredit « refus → 400 `errors.fichier` ». Corriger la lecture du formulaire et tester. Le 413 de Kestrel au-delà de 30 Mo est accepté (au-dessus de la règle de 5 Mo, format `{ error }` conservé). |
| 2 | `Supprimer` sur un dossier de stockage pas encore créé lève `DirectoryNotFoundException` : faux `Error` journalisé après un 404 ou un refus « 10 photos » | low | patch | Contredit le contrat « sans effet s'il n'existe pas ». Corrigé : `Supprimer` ne fait rien si le dossier n'existe pas ; test ajouté. Après correctifs : `dotnet test` 257 réussis + 1 ignoré, 0 avertissement. |

## Design Notes

Ordre des opérations d'un upload, dans `TransactionRunner.RunAsync` : valider (`RegleFichier` : taille, type, signature, nombre de photos de l'animal) → générer la clé → écrire le fichier → ajouter `PhotoAnimal` avec `Ordre = max + 1` → `SaveChanges`/commit par le runner. Si le runner échoue (exception ou `Result` d'échec), supprimer le fichier écrit. Les endpoints minimaux qui lisent un `IFormFile` exigent l'antiforgery depuis .NET 8 : l'API est en JWT Bearer, donc désactiver l'antiforgery sur ces routes (`DisableAntiforgery()`), sans l'ajouter ailleurs.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 avertissement.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts, un ignoré.
- `grep -rnE "\bFile\.|\bDirectory\.|FileStream" aaas/api/Aaas.Api --include=*.cs | grep -v Common/Fichiers | grep -v Migrations` -- expected: aucune ligne.
