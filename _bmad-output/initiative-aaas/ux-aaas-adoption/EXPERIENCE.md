---
name: AAAS Adoption
status: final
updated: 2026-10-14
sources:
  - ../spec-aaas-adoption/spec-aaas-adoption.md
  - ../spec-aaas-adoption/state-machines.md
  - ../spec-aaas-adoption/stack.md
---

# AAAS Adoption — Experience Spine

## Foundation

Site web responsive, **mobile-first** : la majorité des visiteurs arrivent sur téléphone depuis Facebook ou Instagram. Front-end Angular (`stack.md`) ; aucune bibliothèque de composants : les composants sont décrits ici (comportement) et dans `DESIGN.md` (visuel). Pas d'application native. Interface en français, mode clair uniquement.

Trois publics : **visiteur** (non connecté), **Membre** et **Admin** (CAP-1). L'admin gère le site **depuis son téléphone et depuis un ordinateur** : chaque écran admin est responsive et utilisable au doigt.

Maquettes : [`mockups/accueil.html`](mockups/accueil.html), [`mockups/ecrans-cles.html`](mockups/ecrans-cles.html). **`DESIGN.md` et ce document priment sur les maquettes**, qui peuvent contenir des données fictives (Caramel, Rex, +216…, « 14 animaux vaccinés ») et les anciens hex de contraste.

**Valeurs de configuration** (définies une seule fois, réutilisées partout) : numéro de téléphone de l'association, adresse de la page Facebook, adresse du compte Instagram.

## Information Architecture

| Surface | Public | Accès | CAP | Parcours | Maquette |
|---|---|---|---|---|---|
| Accueil | Tous | Logo, lien Accueil, arrivée depuis réseaux sociaux | CAP-8 (section Actualités) | 1, 3 | `accueil.html` |
| Adopter (catalogue + filtres) | Tous | Lien Adopter, Miaou ! / Wouf !, « Voir tous les animaux → » | CAP-2 | 1 | — |
| Fiche animal | Tous | Carte animal | CAP-3, CAP-10 (cœur) | 1, 4 | `ecrans-cles.html` §1 |
| Actualités | Tous | Lien Actualités, section de l'accueil | CAP-8 | 3 | `accueil.html` (section) |
| Ils ont trouvé une famille | Tous | Lien en bas de la page Adopter, pied de page | CAP-11 | 2 | — |
| Dons · Bénévolat · Famille d'accueil | Tous | « Nous aider », pied de page | CAP-12 | 5 | — |
| Contact | Tous | Lien Contact (en-tête, menu burger), pied de page | CAP-12 | 6 | — |
| Connexion · Créer mon compte | Visiteur | Bouton Connexion, invitation à se connecter | CAP-1 | 1 | `ecrans-cles.html` §2b (invitation) |
| Questionnaire de réservation | Membre | « Je veux l'adopter » (fiche Disponible) | CAP-4 | 1 | `ecrans-cles.html` §2 |
| Réservation envoyée (compte à rebours) | Membre | Envoi du questionnaire | CAP-4 | 1 | `ecrans-cles.html` §3 |
| Réservation refusée (limite atteinte) | Membre | Envoi d'une 3e réservation | CAP-4 | 1 (échec) | `ecrans-cles.html` §4 |
| Mes réservations | Membre | Menu membre, liens après réservation et refus | CAP-4 | 1 (échec) | — |
| Mes favoris | Membre | Menu membre | CAP-10 | 4 | — |
| Admin · Réservations | Admin | Menu admin, lien de l'email | CAP-6 | 2 | `ecrans-cles.html` §5 |
| Admin · Animaux (liste) | Admin | Menu admin | CAP-7 | 3 | — |
| Admin · Ajouter / modifier un animal | Admin | Liste Animaux | CAP-7 | 3 | `ecrans-cles.html` §6 (ajout) |
| Admin · Actualités (liste + formulaire) | Admin | Menu admin | CAP-9 | 3 | — |
| Email « nouvelle réservation » | Admin | Boîte mail | CAP-5 | 2 | — |
| Email « message reçu » | Admin | Boîte mail | CAP-12 | 5, 6 | — |

**Navigation.** Téléphone : menu burger dans l'en-tête. Ordinateur : Accueil · Adopter · Actualités · Nous aider · Contact · Connexion. Le lien Actualités reste présent même sans actualité. Le pied de page répète Dons, Bénévolat, Famille d'accueil, Contact, Ils ont trouvé une famille, le numéro de l'association et les liens Facebook et Instagram. Une fois connecté, le bouton Connexion devient un menu membre : Mes réservations, Mes favoris, Déconnexion ; l'admin y trouve en plus Réservations, Animaux, Actualités. L'espace admin garde le même en-tête avec la pastille « Admin ».

**Accès.** Page membre sans connexion → écran Connexion, puis retour à la page demandée. Page admin ouverte par un Membre → écran « Cette page est réservée à l'association. » avec retour à l'accueil (CAP-1).

## Voice and Tone

Microcopie ; la posture de marque vit dans `DESIGN.md` § Brand & Style. **Vouvoiement** partout. Ludique sur les pages publiques, factuel dans l'admin. Le prénom de l'animal remplace « l'animal » partout où il est connu ; phrases et santé s'accordent au sexe de l'animal (« Vaccinée »), les pastilles de statut restent invariables.

| Do | Don't |
|---|---|
| « Une truffe vous attend au Cap Bon ! » | Accroche générique sans lieu (« Bienvenue sur notre site ») |
| « 🐱 Miaou ! · Les chats » / « 🐶 Wouf ! · Les chiens » | « Filtrer par espèce : Chat » |
| « Miaou ! Je veux l'adopter » (chat) / « Wouf ! Je veux l'adopter » (chien) | « Soumettre une demande » |
| « 3 petites questions, 1 minute chrono » | « Formulaire de pré-adoption obligatoire » |
| « Caramel vous attend déjà ! » · « Dernière étape : un petit coup de fil à l'association. » | « Réservation enregistrée avec succès. » |
| « Sans appel à temps, Caramel redevient disponible pour d'autres familles. » | Menace ou culpabilisation (« Vous perdrez l'animal ») |
| « Le temps est écoulé, Caramel est de nouveau disponible. » | « Votre réservation a échoué. » |
| « Oups, vos deux paniers sont déjà pris » + explication sans reproche | « Erreur : quota dépassé » |
| « Connectez-vous pour réserver Caramel » · « Un compte membre suffit, c'est gratuit et rapide. » | « Accès refusé » |
| « Votre numéro permet à l'association de vous rappeler. » (inscription) | Téléphone demandé sans raison |
| Admin : « Confirmer », « Annuler », « Êtes-vous sûr ? », « ⏱ 3 min restantes », « aucun appel en 15 min · Filou est redevenu Disponible. » | Jeux de mots ou emojis décoratifs dans les actions admin |
| Statuts verbatim de `state-machines.md` : Disponible, Réservé, Adopté ; En attente, Confirmée, Annulée, Expirée | Synonymes (« Bloqué », « Pris », « Validé ») |
| Ententes : Oui / À voir / Non | « Peut-être », « Inconnu » |
| Pages Dons : appeler ou écrire | Déduction fiscale, « 66 % », vocabulaire de paiement en ligne |

## Component Patterns

Comportement uniquement ; le visuel vit dans `DESIGN.md` § Components.

| Composant | Où | Règles de comportement |
|---|---|---|
| Barre de navigation | Partout | Burger sur téléphone, liens sur ordinateur ; lien de la page courante marqué (`aria-current`). |
| Boutons Miaou ! / Wouf ! | Accueil | Ouvrent Adopter pré-filtré sur l'espèce ; la fiche propose alors le retour « ← Les chats » / « ← Les chiens ». |
| Carte animal | Accueil, Adopter, Mes favoris, Ils ont trouvé une famille | Toute la carte ouvre la fiche. Accueil : les 4 derniers animaux ajoutés (Disponibles ou Réservés), puis « Voir tous les animaux → ». Les animaux Adoptés n'apparaissent qu'à « Ils ont trouvé une famille » (CAP-2, CAP-11). |
| Filtres du catalogue | Adopter | Puces : espèce, âge, taille, sexe, ententes (Enfants / Chiens / Chats : Oui ou À voir) ; combinables, appliqués immédiatement, compteur de résultats à jour, « Effacer les filtres » dès qu'un filtre est actif (CAP-2). |
| Galerie photo | Fiche animal | Toutes les photos saisies (CAP-3). Toucher une vignette ou un point, ou balayer sur téléphone, change la photo ; compteur « n / total » à jour. |
| Bouton favori | Fiche animal | Bascule ajouté / retiré (CAP-10). Non connecté → invitation à se connecter, puis retour sur la fiche. |
| Bouton d'action « Je veux l'adopter » | Fiche animal | Collé en bas de l'écran sur téléphone pendant le défilement. Disponible : actif. Réservé : remplacé par « Déjà réservé », désactivé. Adopté : remplacé par la mention « A trouvé une famille », sans bouton. Non connecté → invitation à se connecter, puis retour sur la fiche. |
| Inscription | Créer mon compte | Prénom, nom, email, **téléphone (obligatoire)**, mot de passe. Inscription refusée sans téléphone, message sous le champ (CAP-1). Le numéro est montré à l'admin sur chaque réservation. |
| Connexion | Connexion | Email, mot de passe ; lien « Créer mon compte ». Après connexion, retour à la page demandée. |
| Questionnaire | Réservation | Logement (Appartement / Maison) et jardin (Oui / Non) en choix segmentés ; expérience en texte libre facultatif. L'aide annonce les 15 minutes **avant** l'envoi. Bouton « Envoyer ma réservation ». |
| Compte à rebours | Réservation envoyée, Mes réservations, Admin · Réservations | Voir § Réservation à durée limitée. |
| Bouton d'appel | Réservation envoyée, pages Nous aider et Contact | Lien `tel:` vers le numéro configuré, numéro affiché en clair sous le libellé. |
| Alerte de refus | Réservation refusée | Titre + explication + liste des réservations en cours + « Mes réservations » et « Retour à [prénom] ». |
| Filtres par statut | Admin · Réservations | Onglets En attente (n) · Confirmées · Annulées · Expirées ; « En attente » par défaut, compteur sur celui-ci. |
| Carte de réservation | Admin · Réservations | Animal, membre, numéro du membre (lien `tel:`), temps restant, questionnaire repliable. Confirmer → réservation Confirmée, animal Adopté. Annuler → dialogue de confirmation, puis réservation Annulée, animal Disponible (CAP-6). La liste se met à jour sans rechargement manuel. |
| Dialogue de confirmation | Admin | Avant Annuler une réservation et avant toute suppression (animal, actualité) : « Êtes-vous sûr ? », phrase nommant l'objet (« Annuler la réservation de Rex ? Rex redevient Disponible. »), bouton d'action destructive + « Retour ». « Retour » et la touche Échap ferment sans rien changer. Confirmer une réservation n'en demande pas. |
| Formulaire animal | Admin | Une colonne. Photos via appareil photo ou galerie, sélection multiple ; la 1re est la principale ; chaque photo retirable (×). Espèce, sexe, ententes Enfants / Chiens / Chats (Oui / À voir / Non) et statut en choix segmentés ; âge et taille en listes ; santé en cases à cocher ; texte alternatif des photos généré (« Photo n de [prénom] »). Statut Disponible par défaut, modifiable pour forcer un statut (`state-machines.md`). « Enregistrer l'animal ». Suppression depuis la liste ou le formulaire, après confirmation. |
| Formulaire d'actualité | Admin | Image (une), titre, résumé, texte ; date = date de publication, automatique. « Publier ». Suppression après confirmation ; effet immédiat côté public (CAP-9). |
| Formulaire de message | Dons, Bénévolat, Famille d'accueil, Contact | Nom, téléphone, email (facultatif), message ; le sujet est la page d'origine. Envoi → email à l'admin (CAP-12), puis « Merci, l'association vous répondra rapidement. » Le numéro et le bouton d'appel restent affichés à côté. |
| Section Actualités | Accueil | Absente (titre compris) s'il n'y a aucune actualité ; sinon les plus récentes en premier (CAP-8). |
| Pied de page | Partout (public) | Numéro cliquable, Facebook et Instagram en nouvel onglet, liens Dons / Bénévolat / Famille d'accueil / Contact / Ils ont trouvé une famille. |

## State Patterns

| État | Surface | Traitement |
|---|---|---|
| Animal Disponible | Carte, fiche | Pastille Disponible ; « Je veux l'adopter » actif. |
| Animal Réservé | Carte, fiche | Pastille Réservé, photo désaturée sur la carte ; reste au catalogue. Fiche : bouton « Déjà réservé » désactivé ; favori toujours possible. |
| Animal Adopté | Catalogue, Ils ont trouvé une famille, fiche | Retiré du catalogue et de l'accueil ; apparaît dans « Ils ont trouvé une famille » sans action de l'admin (CAP-11). Fiche accessible depuis cette page : pastille Adopté, mention « A trouvé une famille », sans bouton. |
| Réservation En attente | Mes réservations, Admin | Temps restant affiché ; admin : Confirmer / Annuler. |
| Réservation urgente | Admin · Réservations | 5 minutes restantes ou moins : carte mise en avant, temps écrit en minutes. |
| Réservation Confirmée / Annulée | Admin, Mes réservations | Admin : onglet correspondant, sans actions. Membre : ligne avec la pastille du statut, sans temps restant. |
| Réservation Expirée | Admin, Mes réservations | Admin : carte atténuée « aucun appel en 15 min · [prénom] est redevenu Disponible. », sans actions. Membre : pastille Expirée. |
| Temps écoulé (00:00) | Réservation envoyée | Le compteur s'arrête ; message « Le temps est écoulé, [prénom] est de nouveau disponible. » et bouton « Réserver à nouveau » (relance le questionnaire si l'animal est toujours Disponible ; sinon la fiche s'affiche avec son statut). Le bouton d'appel disparaît. |
| Limite de 2 réservations atteinte | Réservation refusée | Écran dédié, ton sans reproche, liste des 2 réservations avec temps restant. |
| Animal réservé par un autre membre pendant le questionnaire | Questionnaire | Envoi refusé : « Trop tard, [prénom] vient d'être réservé par une autre famille. » + « Voir les autres animaux » (une seule réservation active par animal). |
| Non connecté | Fiche → réservation, favori | Écran « Connectez-vous pour réserver [prénom] » : Connexion + Créer mon compte ; retour sur la fiche après connexion. |
| Membre sur une page admin | Admin | « Cette page est réservée à l'association. » + retour à l'accueil (CAP-1). |
| Aucune actualité | Accueil | Section absente : ni titre, ni message ; la page passe des animaux au pied de page. |
| Aucune actualité | Page Actualités | « Pas encore d'actualité. Suivez-nous sur Facebook et Instagram. » avec les deux liens. |
| Aucun résultat | Adopter | Phrase courte rappelant les filtres actifs + « Effacer les filtres ». |
| Catalogue vide | Adopter, Accueil | « Tous nos protégés ont trouvé une famille pour l'instant ! » + lien vers Ils ont trouvé une famille. |
| Aucun favori | Mes favoris | Phrase courte + lien vers Adopter. |
| Aucune réservation | Mes réservations, onglet admin | Phrase courte, sans illustration. |
| Photo en chargement ou absente | Cartes, fiche | Dégradé de remplacement (`DESIGN.md`). |
| Erreur d'envoi ou réseau | Tous formulaires | Saisie conservée ; alerte d'erreur au-dessus du bouton (« L'envoi n'a pas abouti. Vérifiez votre connexion et réessayez. ») ; le bouton reste actif pour réessayer. Photos : chaque photo en échec est signalée et peut être renvoyée seule. |
| Envoi en cours | Tous formulaires | Bouton désactivé avec libellé « Envoi… » pour éviter le double envoi. |

## Interaction Primitives

- Toucher pour agir ; toute la carte est la cible.
- Liens `tel:` pour tout numéro (association côté public, membre côté admin) : appel en un toucher.
- Bouton d'action principal collé en bas de la fiche sur téléphone.
- Questionnaire repliable (`details`) dans les cartes admin.
- Photos admin : appareil photo ou galerie du téléphone, sélection multiple.
- Choix segmentés plutôt que listes déroulantes jusqu'à 3 options.
- Toute action admin destructive (Annuler, Supprimer) passe par le dialogue de confirmation.
- **Proscrit** : hero plein écran à l'ouverture, carrousel ou compteur d'adoptions sur l'accueil, recherche ou filtres sur l'accueil (ils vivent sur Adopter), mode sombre, paiement ou don en ligne.

## Accessibility Floor

Comportement ; les contrastes (texte ≥ 4,5:1, contours de contrôle ≥ 3:1) sont fixés dans `DESIGN.md` § Colors.

- Cible WCAG 2.1 AA.
- Cibles tactiles ≥ 48 px, y compris dans l'admin.
- Burger, icônes sociales, bouton favori et bouton d'appel ont un nom accessible (« Menu », « Facebook », « Instagram », « Ajouter aux favoris » / « Retirer des favoris », « Appeler l'association ») ; les emojis décoratifs sont masqués aux technologies d'assistance.
- Statut toujours écrit (pastille texte), jamais porté par la couleur seule ; idem pour l'urgence admin (temps restant écrit).
- Compte à rebours en `role="timer"` ; annonce aux lecteurs d'écran limitée aux paliers de minutes et au passage à 00:00.
- Choix segmentés, ententes et cases santé exposés comme groupes de boutons radio / cases à cocher avec libellé de groupe.
- Dialogue de confirmation : focus placé sur « Retour » à l'ouverture, piégé dans le dialogue, rendu à l'élément d'origine à la fermeture.
- Erreurs de champ reliées au champ (`aria-describedby`) et annoncées.
- Galerie : chaque photo a un texte alternatif « Photo n de [prénom] ».
- Pattes en filigrane masquées aux technologies d'assistance.
- Ordre de tabulation = ordre de lecture ; focus visible sur tous les contrôles.

## Key Flows

Parcours rédigés à partir des décisions de l'utilisateur et des critères de succès de la spec.

### Parcours 1 — Réserver Caramel (Sonia, 34 ans, Hammamet, le soir sur son téléphone) · CAP-1, CAP-2, CAP-3, CAP-4

1. Sonia voit un post Instagram de l'AAAS et touche le lien.
2. L'Accueil s'ouvre : bandeau compact, puis les derniers animaux dès le premier écran.
3. Elle touche **Miaou !** ; Adopter s'ouvre filtré sur les chats.
4. Elle touche la carte de Caramel (Disponible) ; la fiche montre photos, santé, ententes.
5. Elle touche « Miaou ! Je veux l'adopter » ; non connectée, elle voit « Connectez-vous pour réserver Caramel ».
6. Elle touche « Créer mon compte », s'inscrit avec son téléphone, revient sur la fiche de Caramel et relance la réservation.
7. Elle répond au questionnaire (appartement, pas de jardin, une phrase d'expérience) et touche « Envoyer ma réservation ».
8. **Climax :** l'écran « Caramel vous attend déjà ! » affiche le compte à rebours et le gros bouton d'appel ; elle appelle en un toucher, l'admin confirme pendant l'appel.

Échec — limite : Sonia a déjà 2 réservations En attente → « Oups, vos deux paniers sont déjà pris », ses 2 réservations avec temps restant, « Mes réservations ».
Échec — délai : elle n'appelle pas dans les 15 minutes → « Le temps est écoulé, Caramel est de nouveau disponible. » et « Réserver à nouveau ».

### Parcours 2 — Confirmer l'adoption de Rex (l'admin, au marché, sur son téléphone) · CAP-5, CAP-6, CAP-11

1. L'admin reçoit l'email de nouvelle réservation (animal, membre, téléphone, réponses au questionnaire).
2. Il ouvre Admin · Réservations, onglet En attente.
3. La carte de Rex est mise en avant : « ⏱ 3 min restantes ».
4. Le membre appelle ; l'admin déplie le questionnaire pendant la conversation.
5. **Climax :** il touche **Confirmer** ; Rex passe Adopté, sort du catalogue et rejoint « Ils ont trouvé une famille ».

Échec : l'appel n'aboutit pas → l'admin rappelle le membre en touchant son numéro, ou touche Annuler et confirme « Êtes-vous sûr ? » (Rex redevient Disponible), ou laisse expirer.

### Parcours 3 — Ajouter Luna et publier une actualité (l'admin, sur ordinateur, le dimanche) · CAP-7, CAP-8, CAP-9

1. L'admin ouvre Admin · Animaux puis « Ajouter un animal ».
2. Il ajoute plusieurs photos (la première devient principale), remplit espèce, nom, âge, taille, sexe, santé, ententes, description ; le statut reste Disponible.
3. Il touche « Enregistrer l'animal » ; Luna apparaît au catalogue et parmi les derniers animaux de l'accueil.
4. Il ouvre Admin · Actualités et publie une actualité.
5. **Climax :** la section Actualités, jusque-là absente, réapparaît sur l'accueil avec cette actualité en tête.

Échec : envoi interrompu → la saisie est conservée, les photos en échec sont signalées et renvoyées.

### Parcours 4 — Garder Mimi en favori (Sonia, quelques jours plus tard) · CAP-10

1. Sur la fiche de Mimi, Sonia touche le cœur ; il se remplit.
2. Elle se déconnecte, revient le lendemain et se reconnecte.
3. **Climax :** dans Mes favoris, Mimi est toujours là ; un toucher sur le cœur l'en retire.

### Parcours 5 — Proposer d'être famille d'accueil (Sonia, après l'adoption) · CAP-12

1. Depuis le pied de page, elle touche « Famille d'accueil ».
2. La page affiche le numéro de l'association, le bouton d'appel et le formulaire.
3. **Climax :** elle envoie le formulaire ; l'admin reçoit le message par email.

Variante : Dons et Bénévolat suivent le même parcours ; aucun paiement en ligne.

### Parcours 6 — Poser une question (Karim, voisin, sur son téléphone) · CAP-12

1. Karim a trouvé un chiot errant ; il ouvre le menu et touche « Contact ».
2. La page affiche le numéro (appel en un toucher) et le formulaire de message.
3. **Climax :** il décrit la situation, envoie ; « Merci, l'association vous répondra rapidement. » et l'admin reçoit l'email.

## Inspiration & Anti-patterns

- **Repris de l'inspiration** ([`imports/inspiration-accueil-sections.png`](imports/inspiration-accueil-sections.png)) : pattes en filigrane, titres encadrés de filets, cartes photo arrondies et ombrées, boutons ludiques Miaou ! / Wouf !.
- **Rejeté — hero photo plein écran** ([`imports/inspiration-hero.png`](imports/inspiration-hero.png)) : cache les animaux au premier écran.
- **Rejeté — identité de l'autre association** : ni nom, ni logo, ni bleu, ni contenus.
- **Rejeté — directions d'accueil B, C, D** ([`.working/directions-accueil-1.html`](.working/directions-accueil-1.html)) : tuiles d'entrée, recherche et filtres sur l'accueil, compteur et carrousel d'adoptés. Retenu : structure A (bandeau compact puis animaux) habillée du style B.
- **Rejeté — mode sombre** : mode clair uniquement pour cette version.
- **Rejeté — mention de déduction fiscale 66 %** : règle française, association tunisienne.

## Responsive & Platform

| Format | Largeur | Comportement |
|---|---|---|
| Téléphone (référence 375 px) | < 768 px | Burger ; bandeau logo + accroche puis Miaou ! / Wouf ! côte à côte ; grille de 2 cartes ; actualités empilées ; pied de page centré ; bouton d'action collé en bas de la fiche. |
| Tablette | 768–1023 px | Mise en page téléphone, burger conservé ; grille de 3 cartes. |
| Ordinateur (référence 1240 px) | ≥ 1024 px | Liens de navigation visibles ; bandeau logo, accroche et boutons sur une ligne ; grille de 4 cartes ; actualités côte à côte ; fiche en deux colonnes (galerie / informations), bouton d'action dans la colonne ; pied de page sur une ligne. |
| Admin | Toutes | Mêmes écrans sur téléphone et ordinateur, utilisables au doigt ; sur ordinateur, colonne centrée. |

## Réservation à durée limitée

Règles métier : CAP-4 et `state-machines.md`. Comportement d'interface :

- Le délai de 15 minutes court **depuis la création** de la réservation ; tous les affichages (membre, Mes réservations, admin) se calent sur l'heure du serveur, pas sur l'horloge locale seule.
- Réservation envoyée : minutes:secondes, barre qui se vide, bouton d'appel immédiatement sous le compteur, explication en deux points (appeler dans les 15 minutes ; sinon l'animal redevient disponible), lien « Voir mes réservations → ».
- Mes réservations et écran de refus : temps restant en minutes (« 9 min restantes »).
- Admin : temps restant en minutes sur chaque carte En attente ; mise en avant à 5 minutes ou moins.
- L'admin confirme **pendant l'appel** ; le membre n'a rien à faire sur le site après l'envoi.
- À l'expiration : réservation Expirée, animal Disponible, carte admin atténuée, écran membre « Le temps est écoulé » (§ State Patterns).

## Open Questions

- **Valeurs de configuration à fournir** : numéro de téléphone de l'association, adresse de la page Facebook, adresse du compte Instagram (placeholders dans les maquettes).
