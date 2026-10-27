---
title: 'Application web simple pour une salle de sport'
type: 'feature'
ticket: ''
created: '2026-10-12'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
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

**Problem:** L'utilisateur veut une application web simple pour une salle de sport ; le projet est vide.

**Approach:** Une page unique en français pour la salle fictive « FitClub » : vitrine (accueil, cours, tarifs, horaires, contact) et planning hebdomadaire où l'on réserve ou annule une place dans un cours à places limitées. HTML/CSS/JavaScript pur, sans framework ni build ; les réservations sont gardées dans le navigateur (localStorage).

## Boundaries & Constraints

**Always:** Interface entièrement en français. S'ouvre en double-cliquant sur `index.html` (protocole `file://`), sans serveur ni `npm install`. Mise en page responsive (mobile → desktop). Logique de réservation séparée du DOM et testée.

**Never:** Pas de framework, bundler ni dépendance npm. Pas de backend, pas d'envoi réel du formulaire de contact. Pas de comptes / mot de passe. Ne pas toucher `.claude/`, `.agents/`, `_bmad/`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Réservation OK | Nom « Léa », séance avec places libres | Places restantes −1, séance marquée « Réservé » pour Léa | No error expected |
| Séance complète | Places restantes = 0 | Réservation refusée | Message « Ce cours est complet » |
| Double réservation | « léa » déjà inscrite (nom comparé sans casse ni espaces autour) | Refusée | Message « Vous avez déjà réservé ce cours » |
| Nom vide | Nom vide ou espaces | Refusée | Message « Indiquez votre nom pour réserver » |
| Annulation | Léa inscrite à la séance | Place libérée, bouton redevient « Réserver » | No error expected |
| Rechargement | Réservations faites puis F5 | Réservations toujours présentes | No error expected |
| Stockage corrompu ou indisponible | JSON invalide dans localStorage, ou accès qui lève une exception | Planning affiché avec l'état initial | Aucune erreur bloquante, pas de crash |
| Contact invalide | Email sans « @ » ou champ requis vide | Pas de confirmation | Message d'erreur sous le champ concerné |

</frozen-after-approval>

## Code Map

- Projet vierge : seuls `.claude/`, `.agents/` (skills) et `_bmad/` existent — à laisser intacts. Pas de dépôt git.
- Node v24.15.0 disponible : tests avec `node --test`, sans dépendance.
- Tout le code va dans un nouveau dossier `gym-app/` à la racine du projet.

## Tasks & Acceptance

**Execution:**
- [x] `gym-app/booking.js` -- logique pure : planning initial (≈ 10–12 séances sur la semaine : jour, heure, cours, coach, capacité, inscrits de départ), `loadState(storage)`, `saveState(storage, state)`, `book(state, sessionId, name)`, `cancel(state, sessionId, name)`, `remainingSpots(session)` ; retour `{ ok, error, state }` ; aucune référence au DOM -- testable sous Node et chargeable dans le navigateur
- [x] `gym-app/app.js` -- rendu du planning (groupé par jour, places restantes, boutons Réserver/Annuler selon le nom saisi), champ « Votre nom » mémorisé, messages d'erreur/succès, validation du formulaire de contact avec confirmation à l'écran -- relie la logique au DOM
- [x] `gym-app/index.html` -- sections : en-tête/navigation, accueil, cours (4–5 disciplines), tarifs (3 formules), horaires d'ouverture, planning & réservation, contact, pied de page ; charge `booking.js` puis `app.js` en scripts classiques -- page unique
- [x] `gym-app/styles.css` -- styles responsive, navigation ancrée, cartes, états des boutons (complet, réservé), focus visible -- apparence propre
- [x] `gym-app/package.json` -- `{"name":"fitclub","private":true,"scripts":{"test":"node --test tests/"}}` sans dépendance -- lance les tests
- [x] `gym-app/tests/booking.test.js` -- un test par ligne de la matrice concernant la logique (réservation OK, complet, doublon insensible à la casse/espaces, nom vide, annulation, persistance via faux storage, JSON corrompu, storage qui lève) -- couvre les cas limites
- [x] `gym-app/README.md` -- comment ouvrir l'app et lancer les tests -- prise en main

**Acceptance Criteria:**
- Given `gym-app/index.html` ouvert directement depuis l'explorateur de fichiers, when la page se charge, then toutes les sections s'affichent et la console ne montre aucune erreur.
- Given une fenêtre de 375 px de large, when on parcourt la page, then rien ne déborde horizontalement et le planning reste lisible.
- Given un nom saisi et une séance réservée, when on change de nom, then les boutons reflètent les réservations du nouveau nom.
- Given le dossier `gym-app/`, when on lance `npm test`, then tous les tests passent.

## Implementation Notes

- Fichiers créés dans `gym-app/` : `booking.js`, `contact.js`, `app.js`, `index.html`, `styles.css`, `package.json`, `README.md`, `tests/booking.test.js`, `tests/contact.test.js`.
- Script de test modifié en `node --test "tests/**/*.test.js"` : sous Node 24, `node --test tests/` charge le dossier comme un module (MODULE_NOT_FOUND). README : Node 22+.
- Le planning initial contient une séance complète (Spinning mercredi 18:00) pour rendre le cas « complet » visible.
- Audit de la matrice : la ligne « Contact invalide » n'était couverte que par un script navigateur jetable ; validation extraite dans `contact.js` (script classique, même patron que `booking.js`) et couverte par `tests/contact.test.js`.
- Vérifié : `npm test` 16/16, `node --check` OK, parcours Chrome headless en `file://` (réservation, complet, changement de nom, rechargement, annulation, contact, JSON corrompu, 375 px sans débordement, console sans erreur).
- Correctif revue (deux onglets) : `app.js` recharge l'état depuis le storage avant chaque réservation/annulation et écoute l'événement `storage`. Vérifié dans Chrome headless : une réservation écrite par « un autre onglet » après le chargement est conservée quand cette page réserve à son tour ; `npm test` 16/16 ; parcours complet relancé sans erreur.

## Plan Change Log

## Review Triage Log

**Passe 1 (quick)** — high 0 · medium 1 · low 0 · false 1 · maybe-false 0

| Constat | Verdict | Route | Preuve |
|---|---|---|---|
| Deux onglets ouverts : le snapshot complet écrit par `saveState` depuis un `state` chargé une seule fois (`app.js`) écrase les réservations de l'autre onglet | medium | patch | Confirmé à la lecture : `state = B.loadState(storage)` uniquement au démarrage, aucun écouteur `storage`, `saveState` écrit toutes les séances. Correctif : recharger avant chaque action + écouteur `storage`. |
| En-tête sticky trop haut à 375 px : les titres de section passent sous l'en-tête lors d'un clic sur un lien d'ancre | false | rejeté | Mesuré dans Chrome headless à 375 px : en-tête 79,5 px ; après navigation vers `#planning`, le `<h2>` est à 119,5 px du haut (padding de section 48 px), donc entièrement visible. |

## Design Notes

Les modules ES (`type="module"`) sont bloqués par le navigateur en `file://` : `booking.js` est un script classique qui expose un global et s'exporte aussi pour Node :

```js
(function (root) {
  const FitBooking = { loadState, saveState, book, cancel, remainingSpots };
  if (typeof module !== 'undefined' && module.exports) module.exports = FitBooking;
  else root.FitBooking = FitBooking;
})(this);
```

`loadState` fusionne les réservations stockées (`{ [sessionId]: [noms] }`) avec le planning initial ; seules les réservations sont persistées, pas le planning. Les noms sont stockés tels que saisis (trim) et comparés en minuscules.

## Verification

**Commands:**
- `cd gym-app && npm test` -- expected: tous les tests passent, 0 échec
- `node --check gym-app/booking.js && node --check gym-app/app.js` -- expected: aucune erreur de syntaxe

**Manual checks (if no CLI):**
- Ouvrir `gym-app/index.html` : réserver, recharger (réservation conservée), annuler, tenter une séance complète et un doublon ; soumettre le contact avec un email invalide puis valide.
