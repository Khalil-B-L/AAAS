---
title: 'Animaux de démonstration (Development)'
type: 'feature'
ticket: '3'
created: '2026-10-27'
status: 'built'
baseline_revision: '42124e7ac135f444be79bb3368d859377525d980'
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

**Problem:** En local, la base est vide : l'accueil (« Nos derniers pensionnaires ») et les écrans à venir ne montrent rien sans saisie manuelle.

**Approach:** Au démarrage, après les migrations et le seed admin, créer en environnement **Development seulement** et **uniquement si aucun animal n'existe** un jeu de démonstration de 4 chats et 4 chiens aux âges, tailles, sexes et ententes variés, dont 2 passés Adoptés par `Animal.ForcerStatut` (aucun Réservé : reporté à l'epic 3 ; aucune photo : entrée 7). Jamais en `Testing`. Un test le prouve pour `Testing` et pour une base déjà peuplée ; un autre vérifie le jeu créé en Development sur une base vide.

</frozen-after-approval>

## Implementation Notes

Route oneshot : environ 100 lignes (un fichier de seed sur le modèle de `SeedAdmin`, un appel dans `Program.cs`, une classe de tests).

- `Features/Animaux/DemoAnimaux.cs` : 8 modèles (Caramel, Mimi, Pacha, Simba adopté ; Rex, Filou, Bella, Kira adoptée), âges, tailles, sexes, ententes et santé variés ; `CreeLe` échelonnés d'une heure via une horloge figée privée (`TempsFixe`) passée au constructeur d'`Animal` et à `ForcerStatut` ; garde `IsDevelopment()` puis « aucun animal » (`AnyAsync`).
- `Program.cs` : appel après `SeedAdmin`, avant le pipeline.
- `Aaas.Api.Tests/DemoAnimauxTests.cs` (collection `api`, table vidée avant et après) : Development sur table vide → 8 animaux (4 chats, 4 chiens, 2 adoptés avec `AdopteLe`, aucun Réservé, `CreeLe` distincts) ; Development avec un animal existant → rien créé ; Testing → rien créé. En Development, `Smtp:Password` vide force le mode Journal quelles que soient les user-secrets du poste.
- Vérification : `dotnet build` 0 avertissement ; `dotnet test` 157 réussis + 1 ignoré.

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` et `dotnet test aaas/api/Aaas.sln` -- expected: 0 avertissement ; tous verts, un ignoré.

**Manual checks (if no CLI):**
- `dotnet run --launch-profile https` (Development) puis `npm start` : l'accueil montre 4 pensionnaires de démonstration non adoptés.

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 0, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `DemoAnimaux.cs` : le commentaire dit « plus récents d'abord = ordre de la liste », le code fait l'inverse (accueil : Bella, Filou, Rex, Pacha, trois chiens) | low | patch | Décalage `AddHours(i - n)` : le premier de la liste était le plus ancien. Corrigé : `AddHours(-(i + 1))` et liste réordonnée ; l'accueil montre Caramel, Rex, Mimi, Filou (deux chats, deux chiens), vérifié par test. `dotnet test` 157 réussis + 1 ignoré. |
| 2 | AD-3 : seules les commandes écrivent ; `DemoAnimaux` écrit au démarrage hors commande (seul `SeedAdmin` est nommé) | low | defer | Le seed est voulu (décision 2026-10-15 de l'epic) ; c'est l'architecture qui doit nommer ce second écrivain de démarrage. Ajouté à `deferred-work.md` (réconciliation de spec). |
