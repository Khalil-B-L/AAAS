---
name: AAAS Adoption
description: Site d'adoption de chiens et chats de l'AAAS (Cap Bon, Nabeul). Ludique, aéré, vert et orange du logo, mode clair uniquement.
status: final
updated: 2026-10-14
colors:
  primary: '#0F5A2A'
  on-primary: '#FFFFFF'
  accent: '#FF7A00'
  on-accent: '#111111'
  accent-text: '#B45100'
  background: '#FFFFFF'
  surface: '#FFFFFF'
  surface-soft: '#F1F3F2'
  text: '#0B0F0C'
  text-muted: '#4A524C'
  border: '#E2E5E3'
  border-control: '#848B86'
  success: '#127A3A'
  success-soft: '#E8F4EC'
  warning: '#B54708'
  warning-soft: '#FFF1E5'
  danger: '#C4231A'
  danger-soft: '#FCEBEA'
  info: '#475467'
  neutral-expired: '#646B66'
  paw: '#0F5A2A'
  photo-overlay: '#0B0F0CA6'
  photo-placeholder-start: '#FFC48A'
  photo-placeholder-end: '#A9D9B8'
  logo-green: '#3FA535'
  logo-orange: '#F08A1C'
typography:
  display:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 25px
    fontWeight: '800'
    lineHeight: '1.12'
  display-desktop:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 44px
    fontWeight: '800'
    lineHeight: '1.12'
  title-animal:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 32px
    fontWeight: '800'
    lineHeight: '1.15'
  title-message:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 27px
    fontWeight: '800'
    lineHeight: '1.15'
  section-title:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 17px
    fontWeight: '800'
    letterSpacing: 1px
  section-title-desktop:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 24px
    fontWeight: '800'
    letterSpacing: 1px
  card-title:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 19px
    fontWeight: '800'
  button-lg:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 19px
    fontWeight: '800'
  nav-link:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 17px
    fontWeight: '800'
  label:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 15px
    fontWeight: '800'
  body:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 15px
    fontWeight: '400'
    lineHeight: '1.55'
  body-sm:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.45'
  caption:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 12.5px
    fontWeight: '400'
    lineHeight: '1.35'
  badge:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 11px
    fontWeight: '800'
  countdown:
    fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, "Helvetica Neue", Arial, sans-serif'
    fontSize: 54px
    fontWeight: '900'
    lineHeight: '1'
    letterSpacing: 1px
rounded:
  sm: 14px
  md: 16px
  lg: 18px
  xl: 22px
  2xl: 24px
  full: 9999px
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '5': 20px
  '6': 24px
  '7': 28px
  '8': 44px
  gutter-mobile: 20px
  gutter-desktop: 56px
  grid-gap-mobile: 14px
  grid-gap-desktop: 26px
  tap-min: 48px
components:
  nav-bar:
    background: '{colors.background}'
    foreground: '{colors.primary}'
    border-bottom: '4px solid {colors.primary}'
    typography: '{typography.nav-link}'
  nav-link-active:
    underline: '3px solid {colors.accent-text}'
  button-miaou:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.full}'
    typography: '{typography.button-lg}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
  button-wouf:
    background: '{colors.primary}'
    foreground: '{colors.on-primary}'
    radius: '{rounded.full}'
    typography: '{typography.button-lg}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
  button-outline:
    background: '{colors.surface}'
    foreground: '{colors.primary}'
    border: '2px solid {colors.primary}'
    radius: '{rounded.full}'
  button-disabled:
    background: '{colors.surface-soft}'
    foreground: '{colors.text-muted}'
    radius: '{rounded.full}'
  button-connexion:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.full}'
  button-confirmer:
    background: '{colors.success}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  button-annuler:
    background: '{colors.surface}'
    foreground: '{colors.danger}'
    border: '2px solid {colors.danger}'
    radius: '{rounded.full}'
  button-danger:
    background: '{colors.danger}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  confirm-dialog:
    background: '{colors.surface}'
    radius: '{rounded.xl}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
    scrim: '{colors.photo-overlay}'
  section-title:
    foreground: '{colors.primary}'
    rule: '2px solid {colors.primary}'
    typography: '{typography.section-title}'
  animal-card:
    background: '{colors.surface}'
    radius: '{rounded.xl}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
    title: '{typography.card-title}'
    meta: '{typography.caption}'
  badge-disponible:
    background: '{colors.success}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
    typography: '{typography.badge}'
  badge-reserve:
    background: '{colors.warning}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  badge-adopte:
    background: '{colors.primary}'
    foreground: '{colors.on-primary}'
    radius: '{rounded.full}'
  badge-en-attente:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.full}'
  badge-confirmee:
    background: '{colors.success}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  badge-annulee:
    background: '{colors.danger}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  badge-expiree:
    background: '{colors.neutral-expired}'
    foreground: '#FFFFFF'
    radius: '{rounded.full}'
  photo-gallery:
    radius: '{rounded.2xl}'
    counter-background: '{colors.photo-overlay}'
    dot-active: '{colors.primary}'
    dot: '{colors.border-control}'
    thumb-radius: '{rounded.sm}'
    thumb-active-outline: '3px solid {colors.accent-text}'
  favorite-button:
    background: '#FFFFFF'
    foreground: '{colors.accent-text}'
    size: 46px
    radius: '{rounded.full}'
  fact-chip:
    background: '{colors.surface-soft}'
    radius: '{rounded.full}'
  health-tile:
    background: '{colors.success-soft}'
    foreground: '{colors.success}'
    off-background: '{colors.surface-soft}'
    off-foreground: '{colors.text-muted}'
    radius: '{rounded.md}'
  entente-row:
    border: '2px solid {colors.border}'
    radius: '{rounded.md}'
    oui: '{colors.success} on {colors.success-soft}'
    a-voir: '{colors.warning} on {colors.warning-soft}'
    non: '{colors.danger} on {colors.danger-soft}'
  input:
    background: '{colors.surface}'
    border: '2px solid {colors.border-control}'
    placeholder: '{colors.neutral-expired}'
    radius: '{rounded.md}'
    min-height: '{spacing.tap-min}'
  segmented-choice:
    border: '2px solid {colors.border-control}'
    radius: '{rounded.md}'
    selected-border: '{colors.primary}'
    selected-background: '{colors.success-soft}'
    selected-foreground: '{colors.primary}'
  checkbox-row:
    border: '2px solid {colors.border-control}'
    radius: '{rounded.md}'
    checked: '{colors.primary}'
  countdown:
    background: '{colors.warning-soft}'
    border: '3px solid {colors.accent}'
    radius: '{rounded.2xl}'
    digits: '{colors.warning}'
    typography: '{typography.countdown}'
    bar: '{colors.accent}'
  alert-warning:
    background: '{colors.warning-soft}'
    border: '2px solid {colors.warning}'
    title: '{colors.warning}'
    radius: '{rounded.lg}'
  filter-chip:
    border: '2px solid {colors.border-control}'
    radius: '{rounded.full}'
    active-background: '{colors.primary}'
    active-foreground: '{colors.on-primary}'
  reservation-card:
    background: '{colors.surface}'
    radius: '{rounded.xl}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
    urgent-border: '2px solid {colors.accent}'
    urgent-time: '{colors.warning}'
    time: '{colors.success}'
  photo-upload:
    background: '{colors.warning-soft}'
    border: '3px dashed {colors.accent}'
    foreground: '{colors.warning}'
    radius: '{rounded.lg}'
  news-card:
    background: '{colors.surface}'
    radius: '{rounded.lg}'
    shadow: '0 8px 20px rgba(15,90,42,.16)'
  admin-tag:
    background: '{colors.accent}'
    foreground: '{colors.on-accent}'
    radius: '{rounded.full}'
  footer:
    background: '{colors.primary}'
    foreground: '{colors.on-primary}'
---

# AAAS Adoption — Design Spine

Maquettes de référence : [`mockups/accueil.html`](mockups/accueil.html) (accueil), [`mockups/ecrans-cles.html`](mockups/ecrans-cles.html) (fiche, questionnaire, compte à rebours, refus, admin), [`mockups/palettes.html`](mockups/palettes.html) (variation 4 « Contrasté moderne », colonne claire). **Ce document et `EXPERIENCE.md` priment sur les maquettes**, qui peuvent contenir des données fictives, des cadres « mode sombre » caducs et les anciens hex de contraste.

## Brand & Style

L'AAAS (Association d'amitié et d'action sociale) fait adopter des chiens et chats sauvés au Cap Bon, près de Nabeul. Le site doit donner envie d'adopter en un coup d'œil sur un téléphone, à l'arrivée depuis un post Facebook ou Instagram. Le ton visuel est **ludique et aéré** : gros titres épais, beaucoup de blanc, pattes en filigrane, titres de section encadrés de filets, cartes photo arrondies et ombrées, boutons pilule qui « parlent » (Miaou ! / Wouf !).

Le style s'inspire de deux captures d'un autre site associatif ([`imports/inspiration-accueil-sections.png`](imports/inspiration-accueil-sections.png), [`imports/inspiration-hero.png`](imports/inspiration-hero.png)) : seuls les procédés sont repris (filigrane, filets, cartes, boutons ludiques). Le nom, le logo, le bleu et les contenus de cette association ne sont pas repris. Son hero photo plein écran est rejeté : les animaux doivent être visibles dès le premier écran.

**Logo officiel** ([`imports/logo-aaas.jpg`](imports/logo-aaas.jpg)) : deux mains, verte et orange, qui se rejoignent dans un ovale gris, texte orange « Association d'amitié et d'action sociale », sigle vert « A. A. A. S ». Utilisé tel quel, sur fond blanc, recadré en cercle ; jamais recoloré ni redessiné. Emplacements : en-tête (40 px téléphone, 60 px ordinateur, suivi du sigle « A.A.A.S »), bandeau d'accueil (70 px / 120 px, filet `{colors.border}`), pied de page (52 px / 70 px).

**Icônes** : emojis des maquettes (🐱 🐶 📞 🐾 📷), toujours accompagnés d'un libellé texte. Le cœur favori est un pictogramme (♡ / ♥) coloré en `{colors.accent-text}`.

## Colors

Palette dérivée des deux mains du logo (`{colors.logo-green}` `#3FA535`, `{colors.logo-orange}` `#F08A1C`), poussée vers plus de contraste : variation 4 « Contrasté moderne ». **Mode clair uniquement** ; aucun jeton sombre. Ratios WCAG 2.1 calculés sur `#FFFFFF` sauf mention.

- **Vert forêt `{colors.primary}` (`#0F5A2A`, 8,35:1)** — couleur de structure : liens et sigle de l'en-tête, filet sous la navigation, titres de section et leurs filets, bouton Wouf !, boutons d'envoi, pied de page, choix sélectionné, pastille Adopté.
- **Orange vif `{colors.accent}` (`#FF7A00`)** — couleur d'élan, **en fond ou en forme uniquement** : bouton Miaou !, bouton d'appel, « Je veux l'adopter », bouton Connexion, bouton Dons du pied de page, pastilles En attente et Admin, bordure et barre du compte à rebours, bordure des réservations urgentes, zone d'ajout photo. Le texte posé dessus est toujours `{colors.on-accent}` (`#111111`, **7,23:1**). Jamais en texte ni en icône sur blanc (2,61:1).
- **Orange foncé `{colors.accent-text}` (`#B45100`, 5,10:1 ; 4,61:1 sur `{colors.warning-soft}`)** — même teinte que `#FF7A00`, assombrie pour tout texte ou icône orange : fin de l'accroche « au Cap Bon ! », cœur favori, soulignement du lien actif, contour de la vignette photo active.
- **Blanc `{colors.background}` / `{colors.surface}`** — fond de page et des cartes ; la profondeur vient de l'ombre, pas d'un fond teinté.
- **`{colors.surface-soft}` (`#F1F3F2`)** — puces de caractéristiques, bloc « Comment ça marche ? », encart membre des cartes admin, bouton désactivé, indicateur de santé absent.
- **Texte `{colors.text}` (`#0B0F0C`) / `{colors.text-muted}` (`#4A524C`, 8,07:1)** — corps et métadonnées.
- **Bordures** — `{colors.border-control}` (`#848B86`, **3,49:1**) pour le contour des contrôles : champs, choix segmentés, cases à cocher, puces de filtre, points de galerie (WCAG 1.4.11). `{colors.border}` (`#E2E5E3`) reste réservé aux séparateurs décoratifs (filet du logo, lignes d'entente, carte expirée).
- **Statuts** — `{colors.success}` (`#127A3A`, 5,43:1) = Disponible, Confirmée, santé présente, Confirmer, entente Oui ; `{colors.warning}` (`#B54708`, 5,43:1) = Réservé, compte à rebours, alerte, entente À voir ; `{colors.danger}` (`#C4231A`, 5,83:1) = Annulée, Annuler, Supprimer, entente Non ; `{colors.neutral-expired}` (`#646B66`, **5,47:1**) = Expirée et texte d'exemple des champs. Chacun a sa version douce (`-soft`) pour les fonds ; couple texte / fond doux ≥ 4,8:1.
- **`{colors.paw}`** — pattes en filigrane, à 7 % d'opacité.
- **`{colors.photo-placeholder-start}` → `{colors.photo-placeholder-end}`** — dégradé de remplacement tant qu'une photo charge ou manque ; jamais décoratif.
- **`{colors.photo-overlay}`** — compteur de la galerie et voile derrière le dialogue de confirmation.
- **`{colors.info}`** — réservé, sans usage actuel.

## Typography

Pile système (`system-ui`, -apple-system, Segoe UI, Roboto…) : rapide sur téléphone, sans police à charger, pas de police de marque.

Le caractère vient du **poids** : titres, libellés, boutons et liens en 800, compte à rebours en 900. Le corps reste en 400.

| Rôle | Jeton | Usage |
|---|---|---|
| Accroche d'accueil | `{typography.display}` / `{typography.display-desktop}` | « Une truffe vous attend au Cap Bon ! », fin de phrase en `{colors.accent-text}` |
| Nom de l'animal (fiche) | `{typography.title-animal}` | Fiche animal |
| Message plein écran | `{typography.title-message}` | Réservation envoyée, temps écoulé, refus, invitation à se connecter |
| Titre de section | `{typography.section-title}` / `{typography.section-title-desktop}` | Majuscules, interlettrage 1 px, centré entre filets |
| Nom sur carte | `{typography.card-title}` | Cartes animal, carte réservation admin |
| Gros bouton | `{typography.button-lg}` | Miaou !, Wouf !, appel, envoi ; sous-libellé en 12 px 600 |
| Navigation | `{typography.nav-link}` | Liens de l'en-tête ordinateur |
| Libellé de champ | `{typography.label}` | Formulaires ; précision entre parenthèses en `{typography.caption}` `{colors.text-muted}` |
| Corps | `{typography.body}` / `{typography.body-sm}` | Description, aides, alertes |
| Méta | `{typography.caption}` | « Chatte · 2 ans », dates d'actualité |
| Pastille | `{typography.badge}` | Statuts |
| Compte à rebours | `{typography.countdown}` | Chiffres tabulaires (`font-variant-numeric: tabular-nums`) |

## Layout & Spacing

**Mobile-first**, maquettes de référence à 375 px ; ordinateur à 1240 px. Échelle `{spacing.1}`–`{spacing.8}` (4 → 44 px). Points de bascule : voir `EXPERIENCE.md` § Responsive & Platform.

- Marges latérales : `{spacing.gutter-mobile}` (20 px) sur téléphone, `{spacing.gutter-desktop}` (56 px) sur ordinateur.
- Grille d'animaux : 2 colonnes, écart `{spacing.grid-gap-mobile}` sur téléphone ; 3 colonnes sur tablette ; 4 colonnes, écart `{spacing.grid-gap-desktop}` sur ordinateur.
- Respiration entre sections : `{spacing.7}` au-dessus d'un titre de section sur téléphone, `{spacing.8}` sur ordinateur.
- Formulaires : une colonne, `{spacing.4}`–`{spacing.5}` entre champs ; deux champs courts (Âge / Taille) peuvent partager une ligne. Sur ordinateur, formulaires et écrans admin en colonne centrée de 720 px maximum.
- Fiche animal sur ordinateur : galerie à gauche, informations à droite ; le bouton d'action n'est plus collé en bas.
- Cible tactile minimale `{spacing.tap-min}` (48 px) pour champs et boutons.
- Bandeau d'accueil : logo + accroche sur une ligne puis les deux boutons côte à côte (téléphone) ; logo, accroche et boutons sur une seule ligne (ordinateur).

## Elevation & Depth

Une seule ombre, teintée de vert : `0 8px 20px rgba(15,90,42,.16)`. Elle porte les cartes animal, cartes d'actualité, cartes de réservation, la galerie, la vignette du questionnaire, le dialogue de confirmation et les gros boutons pilule. Le bouton favori a une ombre neutre plus courte (`0 4px 12px rgba(0,0,0,.15)`) pour se détacher de la photo.

Pas d'ombre sur : boutons contour, bouton désactivé, champs, puces, carte de réservation expirée. Le bouton d'action collé en bas de la fiche repose sur un fondu blanc, pas sur une barre.

Les pattes en filigrane (`{colors.paw}`, 7 % d'opacité, inclinées de −20°, ~52 px sur téléphone, ~100 px sur ordinateur) sont posées sous le contenu, jamais cliquables, jamais sur les écrans admin de travail.

## Shapes

Tout est arrondi ; plus l'objet est grand, plus l'arrondi est généreux.

| Jeton | Valeur | Objets |
|---|---|---|
| `{rounded.sm}` | 14 px | Vignettes photo, photos du formulaire admin, vignette animal d'une liste |
| `{rounded.md}` | 16 px | Champs, choix segmentés, cases santé, lignes d'entente, cases à cocher |
| `{rounded.lg}` | 18 px | Cartes d'actualité, vignette du questionnaire, zone d'ajout photo, alertes |
| `{rounded.xl}` | 22 px | Cartes animal, cartes de réservation, dialogue de confirmation |
| `{rounded.2xl}` | 24 px | Galerie de la fiche, compte à rebours |
| `{rounded.full}` | 9999 px | Tous les boutons, pastilles, puces, filtres, logo, bouton favori, icônes sociales |

Les photos suivent l'arrondi de leur conteneur.

## Components

- **Barre de navigation** — fond blanc, filet inférieur 4 px `{colors.primary}`. Téléphone : logo + « A.A.A.S » à gauche, menu burger vert à droite. Ordinateur : liens en `{typography.nav-link}` `{colors.primary}`, lien actif souligné 3 px `{colors.accent-text}`, bouton pilule « Connexion » `{colors.accent}` texte `{colors.on-accent}`. Espace admin : pastille « Admin » (`admin-tag`) après le sigle.
- **Boutons Miaou ! / Wouf !** — pilules pleine largeur partagée, emoji + mot en `{typography.button-lg}`, sous-libellé « Les chats » / « Les chiens ». Miaou ! : fond `{colors.accent}`, texte `{colors.on-accent}` (7,23:1). Wouf ! : fond `{colors.primary}`, texte blanc. Ombre standard.
- **Gros bouton d'action** — même gabarit que Miaou ! / Wouf !. Orange (texte `{colors.on-accent}`) pour « Je veux l'adopter », « Réserver à nouveau » et « Appeler l'association » (numéro en sous-libellé) ; vert pour « Envoyer ma réservation », « Connexion », « Enregistrer l'animal », « Mes réservations », « Envoyer le message ».
- **Bouton désactivé** (`button-disabled`) — « Déjà réservé » sur la fiche d'un animal Réservé : même gabarit, fond `{colors.surface-soft}`, texte `{colors.text-muted}`, sans ombre.
- **Bouton contour** (`button-outline`) — « Voir tous les animaux → », « Créer mon compte », « Retour à … ». Bordure 2 px `{colors.primary}`, sans ombre.
- **Dialogue de confirmation** — carte `{rounded.xl}` ombrée, centrée sur un voile `{colors.photo-overlay}` ; titre « Êtes-vous sûr ? » en 19 px 800, une phrase en `{typography.body}`, deux boutons pleine largeur empilés : action destructive pleine `{colors.danger}` texte blanc (`button-danger`), puis « Retour » en contour.
- **Titre de section à filets** — texte centré, majuscules, `{colors.primary}`, filets de 2 px de part et d'autre qui occupent l'espace restant (minimum 18 px).
- **Carte animal** — `{rounded.xl}`, ombre, photo (128 px de haut sur téléphone, 220 px sur ordinateur) avec pastille de statut en haut à gauche, puis nom en `{typography.card-title}` et méta « Espèce · âge · sexe » en `{typography.caption}` `{colors.text-muted}`. Animal Réservé : photo désaturée (saturation 65 %), pastille Réservé.
- **Pastilles de statut** — pilules `{typography.badge}`. Animal : Disponible (`{colors.success}`, blanc), Réservé (`{colors.warning}`, blanc), Adopté (`{colors.primary}`, blanc). Réservation : En attente (`{colors.accent}`, `{colors.on-accent}`), Confirmée (`{colors.success}`, blanc), Annulée (`{colors.danger}`, blanc), Expirée (`{colors.neutral-expired}`, blanc, 5,47:1). Libellé invariable, tel que dans `state-machines.md` (« Réservé » même pour une femelle).
- **Galerie photo (fiche)** — grande photo `{rounded.2xl}` (270 px de haut sur téléphone), pastille de statut, compteur « 1 / 4 » sur `{colors.photo-overlay}`, points de pagination `{colors.border-control}` (actif allongé 22 px `{colors.primary}`), rangée de vignettes 58 px dont l'active est cerclée 3 px `{colors.accent-text}`.
- **Bouton favori** — cercle blanc 46 px en haut à droite de la galerie, cœur `{colors.accent-text}` (vide ♡ / plein ♥, 5,10:1).
- **Mention « A trouvé une famille »** — fiche d'un animal Adopté, à la place du bouton d'action : bandeau `{colors.success-soft}` `{rounded.lg}`, texte 17 px 800 `{colors.primary}`, sans bouton.
- **Puces de caractéristiques** — « Chatte », « 2 ans », « Femelle », « Taille moyenne » en pilules `{colors.surface-soft}`, 13,5 px 700.
- **Cases santé** — trois tuiles côte à côte (Vaccinée, Stérilisée, Pucée, accordées au sexe de l'animal). Présent : `{colors.success-soft}`, coche ✓ 22 px et libellé `{colors.success}`. Absent : `{colors.surface-soft}`, tiret « — » et libellé négatif (« Non vaccinée ») en `{colors.text-muted}`.
- **Lignes d'entente** — trois lignes : icône + « Les enfants », « Les chiens », « Les chats », pastille à droite : Oui (`success`), À voir (`warning`), Non (`danger`), chacune sur son fond doux.
- **Champs** — `{rounded.md}`, bordure 2 px `{colors.border-control}`, hauteur ≥ `{spacing.tap-min}` ; zone de texte ≥ 96 px. Texte d'exemple en `{colors.neutral-expired}`. Focus : bordure `{colors.primary}`. Erreur : bordure et message `{colors.danger}` sous le champ.
- **Choix segmenté** — boutons égaux côte à côte, bordure 2 px `{colors.border-control}` (Appartement / Maison, Oui / Non, Chat / Chien, Mâle / Femelle, Oui / À voir / Non, Disponible / Réservé / Adopté). Sélection : bordure `{colors.primary}`, fond `{colors.success-soft}`, texte `{colors.primary}`.
- **Case à cocher en ligne** — ligne pleine largeur `{rounded.md}`, bordure 2 px `{colors.border-control}`, carré 24 px arrondi 7 px ; cochée : bordure de ligne et carré `{colors.primary}`, coche blanche.
- **Puces de filtre** — pilules à bordure 2 px `{colors.border-control}` ; active pleine `{colors.primary}` texte blanc ; compteur entre parenthèses pour les onglets admin. Sur Adopter, rangée défilante horizontalement sur téléphone, sur plusieurs lignes sur ordinateur.
- **Vignette animal du questionnaire** — photo 62 px `{rounded.sm}`, « Réserver Caramel » et « 3 petites questions, 1 minute chrono », dans une carte `{rounded.lg}` ombrée.
- **Compte à rebours** — bloc `{colors.warning-soft}`, bordure 3 px `{colors.accent}`, `{rounded.2xl}` ; minutes:secondes en `{typography.countdown}` `{colors.warning}`, « restantes pour appeler » en 16 px 800, barre de progression `{colors.accent}` sur fond blanc (10 px, pilule). À 00:00 : chiffres et barre vides remplacés par le message de temps écoulé (alerte).
- **Bloc « Comment ça marche ? »** — `{colors.surface-soft}`, `{rounded.lg}`, liste numérotée en `{typography.body-sm}`.
- **Alerte** — `{colors.warning-soft}`, bordure 2 px `{colors.warning}`, titre 17 px 800 `{colors.warning}`, corps `{typography.body}`. Variante erreur : `{colors.danger-soft}` et `{colors.danger}`.
- **Ligne de réservation membre** — carte ombrée `{rounded.lg}` : vignette 54 px, nom, pastille de statut, temps restant (« 9 min restantes ») en `{typography.caption}` pour une réservation En attente.
- **Carte de réservation (admin)** — `{rounded.xl}`, ombre ; en-tête vignette 64 px + nom + méta + temps restant aligné à droite (`{colors.success}`, 900) ; encart membre `{colors.surface-soft}` avec nom et numéro cliquable `{colors.primary}` ; questionnaire repliable (bordure 2 px `{colors.border-control}`, titre `{colors.primary}`) ; boutons Confirmer (plein `{colors.success}`, « ✓ ») et Annuler (contour `{colors.danger}`). **Urgente** : bordure 2 px `{colors.accent}`, temps en `{colors.warning}`. **Expirée** : vignette en niveaux de gris à 55 % d'opacité, texte à pleine opacité, bordure pointillée `{colors.border}`, sans ombre ni boutons, pastille Expirée.
- **Zone d'ajout photo (admin)** — bordure pointillée 3 px `{colors.accent}`, fond `{colors.warning-soft}`, « 📷 Prendre / choisir des photos » `{colors.warning}`. Photos ajoutées en grille de 3 (88 px), croix de retrait blanche en haut à droite (× `{colors.danger}`), pastille « Principale » `{colors.primary}` sur la première.
- **Carte d'actualité** — `{rounded.lg}`, ombre, image 88 px (140 px sur ordinateur) à gauche, titre 15 px 800, date et résumé en `{typography.caption}`. Empilées sur téléphone, côte à côte sur ordinateur.
- **Pied de page** — fond `{colors.primary}`, texte blanc. Logo, « AAAS · Cap Bon (Nabeul) », numéro de téléphone, icônes Facebook et Instagram (cercles blancs, glyphe `{colors.primary}`), puis pilules Dons (pleine `{colors.accent}`, texte `{colors.on-accent}`), Bénévolat, Famille d'accueil et Contact (contour blanc), mention « Association d'amitié et d'action sociale ». Centré sur téléphone, sur une ligne sur ordinateur.

Écrans sans maquette (Adopter, connexion / inscription, favoris, Ils ont trouvé une famille, pages Nous aider et Contact, listes admin, formulaire d'actualité) : assemblés avec les composants ci-dessus.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Ouvrir l'accueil sur un bandeau compact puis les animaux, visibles dès le premier écran du téléphone | Hero photo plein écran |
| Utiliser le logo officiel (mains verte et orange) tel quel | Recolorer, redessiner le logo ou reprendre celui, le nom, le bleu ou les textes de l'autre association |
| Mode clair uniquement | Ajouter un mode sombre ou des jetons `-dark` |
| Titres de section en majuscules entre filets verts | Titres de section sans filets ou en orange |
| Cartes et boutons arrondis, une seule ombre verte douce | Angles vifs, ombres noires dures, empilement d'ombres |
| `{colors.accent}` en fond avec texte `{colors.on-accent}` ; `{colors.accent-text}` pour tout texte ou icône orange | `#FF7A00` en texte ou icône sur blanc, texte blanc sur `#FF7A00` |
| Contours de contrôles en `{colors.border-control}` | Champs ou puces bordés de `{colors.border}` (1,27:1) |
| Pattes en filigrane discrètes sur les pages publiques | Pattes au premier plan, cliquables ou sur les écrans admin de travail |
| Statut toujours en pastille de couleur + mot | Statut signalé par la couleur seule |
| Pages Dons : appel et formulaire de contact | Toute mention de déduction fiscale (66 % ou autre) |
