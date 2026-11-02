---
title: 'Photos de démonstration'
type: 'feature'
ticket: '7'
created: '2026-11-02'
status: 'built'
baseline_revision: 'c05aa8839323023fa9ae9431f367db216802a256'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Les 8 animaux de démonstration (story 2.3) n'ont aucune photo : en local, les cartes montrent un dégradé et la fiche (entrée 8) n'aura rien à afficher.

**Approach:** Générer par un script versionné des illustrations simples sans droits d'auteur (silhouettes de chats et de chiens, fonds aux couleurs du site), les versionner dans le dépôt, et les attacher au démarrage, en Development seulement, aux animaux de démonstration qui n'ont aucune photo, en passant par le même chemin qu'un envoi admin (`RegleFichier` et `IFileStorage`).

## Boundaries & Constraints

**Always:** illustrations produites par un script du dépôt (aucune image tierce), au format PNG ou JPEG, chacune sous 5 Mo et acceptée par `RegleFichier` ; une illustration par animal de démonstration (8), silhouette chat pour les chats, chien pour les chiens, fonds variés pris dans la palette de DESIGN.md ; rattachement au démarrage après `DemoAnimaux`, en `Development` seulement, uniquement aux animaux de démonstration (par leur nom) **qui n'ont aucune photo** — un animal ayant déjà une photo n'en reçoit pas, un animal supprimé n'est pas recréé ; écriture par le handler `AjouterPhoto` existant (validation, clé, `Ordre`, transaction, nettoyage), jamais par un accès disque direct hors `IFileStorage` ; jamais en `Testing` ; illustrations copiées avec la sortie de build de l'API (pas lues depuis le dossier source au moment de l'exécution).

**Never:** photo réelle ou téléchargée ; nouvelle route ; modification de `RegleFichier` ou d'`IFileStorage` ; animaux « Réservé » de démonstration (epic réservations) ; fiche (entrée 8).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Premier démarrage Development | 8 animaux de démo sans photo | chacun reçoit 1 photo (`Ordre` 1) ; carte : `photoPrincipaleUrl` non nul, image servie par `/fichiers/<clé>` | — |
| Redémarrage | animaux de démo ayant déjà leur photo | aucune photo ajoutée (toujours 1 par animal) | — |
| Animal de démo avec photo de l'admin | Caramel a déjà une photo envoyée | Caramel ne reçoit rien ; les autres reçoivent la leur | — |
| Animal hors démo | animal créé par l'admin, sans photo | ne reçoit rien | — |
| Testing | environnement de test | aucune photo de démonstration | — |
| Illustration invalide | (garde-fou) une illustration refusée par `RegleFichier` | l'animal est sauté, avertissement journalisé, le démarrage continue | — |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Animaux/DemoAnimaux.cs` -- 8 modèles nommés, garde `IsDevelopment()`, création seulement si aucun animal ; à étendre (ou compléter par un `DemoPhotos`) sans changer la règle de création des animaux.
- `aaas/api/Aaas.Api/Features/Animaux/Commands/AjouterPhoto.cs` -- `AjouterPhotoCommand(int AnimalId, IReadOnlyList<IFormFile> Fichiers)` : réutiliser ce handler (un `FormFile` construit sur les octets de l'illustration, type MIME correct) plutôt que recopier sa logique.
- `aaas/api/Aaas.Api/Common/Fichiers/RegleFichier.cs` -- `ValiderAsync`, types acceptés (signatures JPEG/PNG/WebP), `TailleMax`.
- `aaas/api/Aaas.Api/Program.cs:50-53` -- `SeedAdmin` puis `DemoAnimaux` ; y ajouter l'appel des photos de démonstration après.
- `aaas/api/Aaas.Api/Aaas.Api.csproj` -- y déclarer les illustrations comme contenu copié en sortie.
- `aaas/api/Aaas.Api.Tests/DemoAnimauxTests.cs` (démarrage Development sur la base partagée, `Smtp:Password` vide), `Outils/Fichiers.cs`, `Outils/Animaux.cs` -- harnais à réutiliser.
- Node 24 est installé sur le poste (aucun Python) : le script de génération peut être un `.mjs` sans dépendance (encodeur PNG avec `zlib` intégré).

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Demo/generer-illustrations.mjs` -- script sans dépendance qui dessine 4 silhouettes de chat et 4 de chien sur des fonds de la palette et écrit des PNG (≈ 400 × 300) ; commande de régénération en commentaire d'en-tête.
- [x] `aaas/api/Aaas.Api/Demo/Photos/*.png`, `Aaas.Api.csproj` -- les 8 illustrations générées, nommées d'après les animaux, copiées en sortie.
- [x] `aaas/api/Aaas.Api/Features/Animaux/DemoPhotos.cs`, `Program.cs` -- rattachement au démarrage (Development, animaux de démo sans photo) via `AjouterPhotoHandler`.
- [x] `aaas/api/Aaas.Api.Tests/DemoPhotosTests.cs` -- chaque illustration passe `RegleFichier` ; une couverture par ligne de la matrice.

**Acceptance Criteria:**
- Given `dotnet build` et `dotnet test`, then aucun avertissement et tout passe.
- Given l'API en Development et `npm start`, when on ouvre l'accueil puis `/adopter`, then chaque carte montre son illustration (vérification humaine, `hitl`).

## Implementation Notes

- `DemoPhotos.ExecuterAsync` (garde `IsDevelopment()`) appelle `internal AttacherAsync(services, dossier, ct)` ; le test « illustration invalide » appelle `AttacherAsync` sur un dossier temporaire (faux PNG pour Caramel). Illustration absente : même traitement (sautée, avertissement).
- Animaux de démo reconnus par `DemoAnimaux.Noms` (nouvelle propriété publique) ; fichier `Demo/Photos/<nom en minuscules>.png`, lu depuis `AppContext.BaseDirectory` (copié aussi dans la sortie des tests par référence de projet).
- Un scope DI par animal pour `ICommandHandler<AjouterPhotoCommand, PhotoDto>` ; `FormFile` construit sur les octets, `ContentType = image/png`.
- Silhouettes paramétriques (chat assis / chien assis, 4 variantes chacune : queue, oreilles, museau, échelle, miroir), suréchantillonnage 4×4, fonds en dégradé de deux couleurs de la palette ; PNG ≈ 5–6,5 Ko. Régénération vérifiée identique (md5) sur ce poste ; l'identité octet à octet dépend de la version de zlib de Node.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Fonds des illustrations tirés du dégradé `photo-placeholder-*` (Bella : exactement le dégradé « photo manquante ») | medium | patch | DESIGN.md : ce dégradé est « de remplacement tant qu'une photo charge ou manque ; jamais décoratif » ; une photo chargée ressemblerait à l'état « photo manquante ». Corriger la palette du script (autres couleurs de DESIGN.md) et régénérer les 8 PNG. |
| 2 | Reconnaissance par le nom seul : un animal admin homonyme (même d'une autre espèce) reçoit l'illustration | low | patch | `DemoPhotos.cs` ne vérifie pas `Espece`. Exiger nom **et** espèce du modèle de démo ; test d'un « Rex » chat sans photo qui ne reçoit rien. |
| 3 | Photo de démo supprimée par l'admin recréée au redémarrage | low | rejected | Development seulement, données de démonstration ; l'empêcher demanderait de mémoriser les suppressions (complexité sans usage réel). |
| 4 | Pas de test « illustration absente » | low | patch | `DemoPhotos.cs` saute l'animal et journalise ; ajouter le test sur un dossier sans le fichier d'un animal. |

## Verification

**Commands:**
- `node aaas/api/Aaas.Api/Demo/generer-illustrations.mjs` -- expected: 8 PNG régénérés, identiques à ceux versionnés.
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.
