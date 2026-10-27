---
title: 'Habillage Angular'
type: 'feature'
ticket: '5'
created: '2026-10-14'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md'
  - '{project-root}/_bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md'
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Le front n'est qu'un pied de page brut : aucune page, aucune navigation, aucun style ; les epics suivants n'ont ni routes ni composants transverses où se brancher.

**Approach:** Poser le shell public de `aaas/web` conforme à DESIGN.md et `mockups/accueil.html` : jetons CSS, en-tête avec burger, pied de page alimenté par `SiteService`, routes de `contrats-api.md` §7 en pages vides chargées à la demande, pipe téléphone, bouton d'appel, `parseUtc` et pipe de date en `fr-FR`, et l'accueil avec ses emplacements de sections.

## Boundaries & Constraints

**Always:** jetons du frontmatter de DESIGN.md déclarés une seule fois en variables CSS dans `src/styles.css` (`--color-*`, `--rounded-*`, `--spacing-*`, `--tap-min`, typographie) et seuls utilisés par les composants ; mobile-first (base 375 px, `min-width` 768 et 1024 px) ; mode clair uniquement ; standalone, zoneless, `OnPush`, signals ; une feature n'importe jamais une autre feature ; composants transverses dans `shared/`, états partagés dans `core/` ; le numéro et les réseaux viennent uniquement de `SiteService` ; tout numéro affiché par le pipe téléphone et appelé par `tel:` + E.164 ; accessibilité de EXPERIENCE.md § Accessibility Floor (noms accessibles « Menu », « Facebook », « Instagram », « Appeler l'association », cibles ≥ 48 px, focus visible, emojis et pattes `aria-hidden`) ; `LOCALE_ID` `fr-FR`.

**Décision (2026-10-14) :** numéros tunisiens. Le pipe téléphone affiche un E.164 `+216` suivi de 8 chiffres en `+216 72 000 000` (groupes 2-3-3) ; tout autre E.164 est affiché tel quel ; une valeur vide donne une chaîne vide. La correction de la règle de saisie de la story 1.3 (8 chiffres nationaux → `+216`) et du placeholder `Site:Telephone` est un changement séparé, hors de cette story.

**Never:** Angular Material ou autre librairie de composants ; zone.js ; couleur, espacement ou rayon codé en dur dans un composant ; numéro ou URL de réseau social codé en dur ; hero plein écran, carrousel, mode sombre ; gardes, connexion, menu membre, pastille Admin (story 6) ; appels au catalogue ou aux actualités (epics 2 et 4).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Téléphone | 375 px | logo + « A.A.A.S » et burger ; liens cachés jusqu'à l'ouverture du burger | — |
| Ordinateur | ≥ 1024 px | liens Accueil · Adopter · Actualités · Nous aider · Contact · Connexion visibles, pas de burger, lien actif souligné | — |
| Burger | toucher, puis navigation ou Échap | `aria-expanded` bascule ; le menu se ferme après navigation ou Échap | — |
| Pied de page | `SiteDto` chargé | numéro cliquable (`tel:` E.164), Facebook et Instagram en nouvel onglet, liens Dons / Bénévolat / Famille d'accueil / Contact / Ils ont trouvé une famille | site non chargé : ni numéro ni icônes sociales, liens présents |
| Route connue | chaque route de §7 | page vide portant son titre, chargée à la demande | — |
| Route inconnue | `/nimporte-quoi` | redirection vers `/` | — |
| Pipe téléphone | `"+21672000000"` ; `"+33612345678"` | « +216 72 000 000 » ; « +33612345678 » | `null` / vide → « » |
| Pipe date | `"2026-10-12T08:00:00Z"` | « 12 octobre 2026 » | date sans fuseau : `parseUtc` lève |

</frozen-after-approval>

## Code Map

- `aaas/web/src/app/core/site.service.ts`, `core/api/site.dto.ts` -- signal `site()` chargé sans attente par `provideAppInitializer` (`app.config.ts`) ; à réutiliser tel quel.
- `aaas/web/src/app/app.ts`, `app.html`, `app.spec.ts` -- pied de page minimal de la story 1.1, à remplacer par le layout ; garder le test « boot via `appConfig` » (il prouve le chargement au démarrage) en l'adaptant au nouveau pied de page.
- `aaas/web/src/styles.css` (vide), `src/index.html` (`lang="en"`, titre « Web ») -- à remplir / corriger (`lang="fr"`, titre « AAAS · Adoption »).
- `_bmad-output/initiative-aaas/ux-aaas-adoption/mockups/accueil.html` -- référence visuelle (téléphone : `.nav`, `.band`, `.ctas`, `.tt`, `.grid`, `.more`, `.news`, `.foot`) ; DESIGN.md prime sur ses couleurs et données fictives (+216, Caramel…).
- `_bmad-output/initiative-aaas/ux-aaas-adoption/imports/logo-aaas.jpg` -- logo officiel, à copier dans `aaas/web/public/`, jamais retouché.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/web/src/styles.css` -- variables CSS de tous les jetons DESIGN.md, base typographique (pile système), focus visible, utilitaires (`.titre-section` à filets, boutons pilule, `.visually-hidden`).
- [x] `aaas/web/public/logo-aaas.jpg`, `src/index.html` -- logo copié ; `lang="fr"`, titre.
- [x] `aaas/web/src/app/app.config.ts` -- `LOCALE_ID` `fr-FR` (`registerLocaleData`), `withComponentInputBinding`, `withInMemoryScrolling`.
- [x] `aaas/web/src/app/core/parse-utc.ts` -- seul parseur de date (rejette une date sans `Z` ni décalage).
- [x] `aaas/web/src/app/shared/telephone.pipe.ts`, `shared/date-fr.pipe.ts` -- affichage du numéro (règle de la Décision) ; date « 12 octobre 2026 » via `parseUtc`.
- [x] `aaas/web/src/app/shared/bouton-appel/` -- gros bouton orange « Appeler l'association », numéro en sous-libellé, `href="tel:<E.164>"`, rien si le site n'est pas chargé.
- [x] `aaas/web/src/app/layout/en-tete/`, `layout/pied-de-page/` -- en-tête (logo, sigle, liens, bouton Connexion vers `/connexion`, burger) et pied de page conformes à DESIGN.md § Components.
- [x] `aaas/web/src/app/app.html`, `app.ts` -- en-tête, `<main>` avec pattes en filigrane, `router-outlet`, pied de page.
- [x] `aaas/web/src/app/shared/page-vide/` et `aaas/web/src/app/app.routes.ts` -- toutes les routes de §7 chargées à la demande par feature (`features/<feature>/<feature>.routes.ts`), chacune affichant son titre ; `**` → `/` ; `title` de page par route.
- [x] `aaas/web/src/app/features/accueil/` -- bandeau (logo, accroche « Une truffe vous attend au Cap Bon ! »), Miaou ! / Wouf ! vers `/adopter?espece=Chat|Chien`, emplacement « Nos derniers pensionnaires » + « Voir tous les animaux → », emplacement Actualités non rendu tant qu'il n'y a pas de données.
- [x] Tests Vitest à côté de chaque fichier -- pipes (cas de la matrice), `parseUtc`, bouton d'appel, en-tête (burger `aria-expanded`, fermeture à la navigation et à Échap), pied de page (avec et sans site), routes (chaque chemin de §7 résout, inconnue → `/`).

**Acceptance Criteria:**
- Given `npm start` et l'API lancée, when on ouvre `/` à 375 px puis à 1240 px, then l'en-tête, le bandeau, les boutons et le pied de page correspondent à `mockups/accueil.html` (vérification visuelle humaine).
- Given le code de `src/app`, when on cherche une couleur hex, `px` de rayon ou un numéro codé en dur dans un composant, then aucune occurrence hors `styles.css`.
- Given `npx ng test --watch=false` et `npx ng build`, then tout passe, budgets compris.

## Implementation Notes

- Jetons : `--color-*`, `--rounded-*`, `--spacing-*`, `--tap-min`, `--font-*`/`--line-height-*`/`--letter-spacing-*` par rôle typographique, plus des jetons dérivés déclarés eux aussi dans `styles.css` seulement : `--shadow`, `--opacity-paw`, tailles du logo et des pattes (`--size-*`), épaisseurs de filet (`--width-*`), `--rounded-glyph` (glyphe Instagram), `--width-band-buttons` (440 px des boutons du bandeau ordinateur) et alias responsives `--gutter`, `--grid-gap`, `--section-space` (basculent à 1024 px). Les CSS de composants ne contiennent du `px` que dans les media queries.
- Routes : une entrée `loadChildren` par préfixe de §7 dans `app.routes.ts`, chaque fichier `features/<feature>/<feature>.routes.ts` exporte un tableau par point de montage (`animaux`, `reservations`, `auth`, `actualites`, `nous-aider`, `contact`, `favoris`, `accueil`). Le questionnaire `/animaux/:id/reserver` vit dans `reservations`, `/acces-refuse` dans `auth`, les écrans admin dans leur feature. `routePageVide(path, titre)` (`shared/page-vide/`) donne le composant `PageVide` (titre lié par `withComponentInputBinding`) et le titre d'onglet « <titre> · AAAS ».
- En-tête : lien actif calculé sur le chemin (`/nous-aider/*` active « Nous aider ») et marqué `aria-current="page"` ; le menu se ferme sur `NavigationEnd`, au toucher d'un lien (même vers la page courante) et à Échap (focus rendu au burger).
- Pied de page : numéro dans `a[href=tel:<E.164>] .telephone` via le pipe ; réseaux affichés seulement si `site()` est chargé et que l'URL est non vide ; « Ils ont trouvé une famille » en lien texte sous les pilules.
- Accueil : grille des pensionnaires et cartes d'actualité laissées en commentaires d'emplacement ; `actualites` est un signal vide en attendant l'epic 4.

- 2026-10-14 : ajout de `RACINE_VERS_ACCUEIL` (`shared/page-vide/`) et d'un `**` dans `admin` : sans eux, `/admin`, `/animaux`, `/reservations` et `/nous-aider` (parents sans page à leur racine) restaient affichés à vide au lieu d'être redirigés vers `/` comme l'exige la ligne « Route inconnue » de la matrice.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick) — high 0, medium 1, low 3, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | Pattes en filigrane sur toutes les routes, admin comprises | medium | patch | `app.html` les pose autour de l'unique `router-outlet` ; les routes `/admin/**` sont montées dans ce layout ; DESIGN.md l'interdit (§ Elevation, Do's and Don'ts). Correctif : ne pas rendre les pattes quand le chemin commence par `/admin`. |
| 2 | `parseUtc` accepte des dates impossibles (30 février, 24 h) | low | rejected | Réel (V8 normalise), mais les dates viennent toutes du convertisseur de l'API qui n'écrit que des instants valides ; le contrôle demanderait une comparaison composant par composant. |
| 3 | `<ul class="reseaux">` vide si les deux URLs sont vides | low | patch | La liste dépend de `site()` seul, ses éléments de chaque URL. Correctif : rendre la liste seulement si au moins une URL est non vide. |
| 4 | Échap écouté sur tout le document | low | patch | `host: '(document:keydown.escape)'` : un futur dialogue fermé par Échap fermerait aussi le menu et volerait le focus. Correctif : écouter Échap sur l'en-tête seulement. |

## Verification

**Commands:**
- `npx ng test --watch=false` (dans `aaas/web`) -- expected: tous verts.
- `npx ng build` (dans `aaas/web`) -- expected: succès sans dépassement de budget.
- `grep -rnE "#[0-9A-Fa-f]{3,8}\b" aaas/web/src/app --include=*.css --include=*.html --include=*.ts` -- expected: aucune ligne.

**Manual checks (if no CLI):**
- `/` à 375 px et 1240 px face à `mockups/accueil.html` ; burger au clavier ; lien `tel:` du pied de page.
