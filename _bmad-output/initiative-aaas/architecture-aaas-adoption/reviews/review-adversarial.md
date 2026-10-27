---
review-of: ../architecture-aaas-adoption.md
lens: adversarial
date: '2026-10-14'
inputs:
  - ../architecture-aaas-adoption.md
  - ../../spec-aaas-adoption/state-machines.md
  - ../../spec-aaas-adoption/spec-aaas-adoption.md
  - ../../ux-aaas-adoption/EXPERIENCE.md
note: 'Le fichier architecture-aaas-adoption.memlog.md n''existe pas ; revue faite sans lui.'
---

# Revue adversariale — spine AAAS

## Verdict

La spine fixe bien la forme générale (slices, CQRS, JWT, erreurs, temps), mais elle laisse ouverts le contrat sur le fil (routes, enums, enveloppes, upload), la propriété des transitions d'`Animal` entre Animaux et Reservations, et la frontière de transaction de l'expiration. Deux stories peuvent respecter toutes les AD à la lettre et produire quand même un système incohérent. **3 critiques, 12 hautes, 10 moyennes, 8 basses.**

Méthode : pour chaque trou, deux unités développées indépendamment (slice API, feature Angular, BackgroundService), chacune conforme à toutes les AD, qui divergent malgré tout. Chaque trou propose une AD nouvelle ou resserrée (texte de Rule prêt à coller).

---

## CRITIQUES

### C1 — `ForcerStatut` contre une réservation active : deux propriétaires de l'invariant « une réservation active ⇔ animal Réservé »

- **Unité A :** story « Admin · Animaux » (CAP-7). Elle applique AD-4 : `ForcerStatut(statut)` est « réservée au CRUD admin ». Le formulaire expose le statut en choix segmenté (EXPERIENCE). Rien n'interdit de forcer `Disponible` ou `Adopte` quand une réservation `EnAttente` existe, ni de forcer `Reserve` sans réservation.
- **Unité B :** story « Réserver » + « Admin · Réservations » (CAP-4, CAP-6). Elle s'appuie sur l'index unique filtré (AD-6) et appelle `MarquerAdopte()` / `Liberer()` en supposant que l'animal est `Reserve`.
- **Divergence :**
  - Animal forcé `Disponible` avec une réservation `EnAttente` : le catalogue affiche « Je veux l'adopter », puis l'index unique renvoie `animal-indisponible` (« Trop tard ») sur un animal affiché Disponible.
  - L'admin confirme ensuite : `MarquerAdopte()` part de `Disponible`, transition absente de `state-machines.md`.
  - Animal forcé `Reserve` sans réservation : aucun chemin ne le libère, puisque le BackgroundService (AD-5) ne libère que via une réservation.
  - AD-1 dit « seule la slice propriétaire écrit ses entités » (Animal → Animaux), alors qu'AD-4 fait écrire `Animal.Statut` par Reservations. Un développeur qui lit AD-1 à la lettre peut refuser d'appeler `Animal.Reserver()` depuis Reservations et passer par un événement ou un second `SaveChanges`. On perd alors la transaction unique.
  - La question est listée en Open Questions, mais deux stories ne peuvent pas avancer en parallèle tant qu'elle n'est pas tranchée.
- **AD à resserrer — AD-4 (Rule remplacée) :**
  > `Animal.Statut` a un setter privé ; chaque méthode de transition vérifie l'état source et lève `TransitionInterditeException` sinon : `Reserver()` Disponible→Reserve, `Liberer()` Reserve→Disponible, `MarquerAdopte()` Reserve→Adopte. Exception explicite à AD-1 : les commandes de la slice Reservations appellent ces trois méthodes dans leur propre transaction ; c'est la seule écriture inter-slices autorisée. `ForcerStatut(statut)` n'accepte que `Disponible` ou `Adopte` (jamais `Reserve`) et elle est refusée (409, code `reservation-active`) tant qu'une réservation de l'animal est active au sens de `ReservationRules.EstActive(now)`. La commande admin vérifie ce point dans la même transaction, après `ExpirerReservationsEchues`. Pour libérer un animal réservé, l'admin annule la réservation.

### C2 — Dates sans `Z` sur le fil : EF relit les `DateTime` en `Kind=Unspecified`

- **Unité A :** slice Reservations. Elle respecte AD-11 : `ExpiresAt = timeProvider.GetUtcNow().UtcDateTime + 15 min`, stocké en `datetime2`.
- **Unité B :** composant partagé de compte à rebours (front). Il fait `new Date(expiresAt)` et `new Date(serverNow)`.
- **Divergence :**
  - `serverNow` vient directement de `TimeProvider` (`DateTimeOffset` → sérialisé avec `+00:00`, voire `Z`).
  - `expiresAt`, relu depuis la base par une requête, a `Kind=Unspecified`. System.Text.Json l'écrit alors sans suffixe (`2026-10-14T10:15:00`) et le navigateur l'interprète en heure **locale**.
  - Résultat : le compteur est faux de 1 à 2 h (Europe/Paris). Chaque story respecte AD-11 (« stockées en UTC, sérialisées avec Z ») dans l'intention, mais aucune règle ne dit **comment** le `Z` est garanti.
  - Même problème pour `PublieeLe` (tri et affichage des actualités) et `CreatedAt`.
- **AD à resserrer — AD-11 (ajout à la Rule) :**
  > Toutes les propriétés temporelles des entités et des DTO sont de type `DateTimeOffset` (jamais `DateTime`), stockées en `datetimeoffset` avec un offset nul. Une convention unique dans `AppDbContext.ConfigureConventions` l'impose. Un test d'intégration vérifie que chaque date renvoyée par l'API se termine par `Z` ou `+00:00`. Le front ne parse les dates qu'avec un utilitaire unique de `core/` (`parseUtc`), qui rejette une date sans fuseau.

### C3 — L'expiration paresseuse est inaccessible aux slices qui en ont besoin (contradiction AD-1 / AD-3 / AD-5)

- **Unité A :** slice Animaux, requête catalogue et fiche (CAP-2, CAP-3). AD-3 autorise une expiration paresseuse « par une commande appelée avant la lecture ». Or cette commande, `ExpirerReservationsEchues`, appartient à Reservations, et AD-1 interdit « une slice n'appelle jamais les handlers d'une autre slice ». La requête lit donc `Animal.Statut` tel qu'il est persisté.
- **Unité B :** slice Reservations, « Mes réservations » et la liste admin. Elle applique `ReservationRules.EstActive(now)` dans la projection et renvoie `Expiree` pour une réservation échue.
- **Divergence :**
  - Pendant ~30 s, le même animal est montré `Reserve` sur la fiche (Animaux) alors que sa réservation est `Expiree` dans Mes réservations et dans l'admin (Reservations).
  - Le bouton « Réserver à nouveau » (EXPERIENCE, temps écoulé) affiche la fiche « Déjà réservé ».
  - Le compteur de l'onglet admin « En attente (n) » peut compter des réservations échues si une autre story compte les lignes en base.
  - Enfin, le front peut déduire lui-même `Expiree` à partir de `expiresAt` (story A) ou attendre le serveur (story B).
- **AD à resserrer — AD-5 (ajout à la Rule) :**
  > Le statut **effectif** est calculé par deux prédicats uniques placés dans `Common/Disponibilite/` (sans dépendance de slice) : `ReservationRules.StatutEffectif(r, now)` et `AnimalRules.StatutEffectif(a, reservations, now)`. Ce dernier renvoie `Disponible` si l'animal est `Reserve` et n'a aucune réservation active. Toute requête qui expose un statut d'animal ou de réservation projette ce statut effectif. Aucune requête ne s'appuie sur le statut persisté seul, et aucun endpoint de lecture n'appelle de commande. Le fil ne transporte que le statut effectif. Le front ne déduit jamais un statut de `expiresAt` : à 00:00 il recharge (AD-11).

---

## HAUTES

### H1 — Le formulaire admin périmé écrase le statut (mise à jour perdue)

- **Unité A :** front `features/animaux/admin`. Le formulaire charge l'animal (`statut: Disponible`) puis renvoie tout l'objet par `PUT /api/admin/animaux/{id}`.
- **Unité B :** API Animaux `ModifierAnimal`. Conforme à AD-4, elle appelle `ForcerStatut(dto.Statut)` à chaque enregistrement.
- **Divergence :** un membre réserve entre le chargement et l'enregistrement. L'admin corrige une faute dans la description, et le `PUT` force `Disponible`, ce qui libère silencieusement un animal réservé. Aucune AD ne prévoit de jeton de concurrence.
- **Nouvelle AD-18 — Statut séparé de l'édition :**
  > `PUT /api/admin/animaux/{id}` ne porte jamais `statut` ; le DTO de modification n'a pas ce champ. Forcer un statut est une commande dédiée `POST /api/admin/animaux/{id}/statut` `{ statut, statutAttendu }`, refusée en 409 `statut-modifie` si le statut effectif diffère de `statutAttendu`. `Animal` porte un `RowVersion` (`IsRowVersion`). Une `DbUpdateConcurrencyException` est renvoyée en 409 `modifie-ailleurs`, et le front recharge le formulaire.

### H2 — Qui possède la transaction de `ExpirerReservationsEchues` ?

- **Unité A :** `ExpirationReservationsService` (BackgroundService). Il appelle le handler, qui fait `BeginTransaction` + `SaveChanges`, ce qui est cohérent s'il est appelé seul.
- **Unité B :** `ReserverAnimal` (AD-6). Il ouvre une transaction `Serializable`, puis appelle ce même handler « dans la même transaction » (AD-5).
- **Divergence :**
  - EF lève `InvalidOperationException` (« connection is already in a transaction »). Si le handler A n'ouvre pas de transaction, la story BG perd l'atomicité réservation/animal.
  - L'« essai borné » d'AD-6 copié de FitClub échoue avec `EnableRetryOnFailure` si la transaction n'est pas ouverte via `CreateExecutionStrategy().ExecuteAsync`.
  - La boucle de nouvel essai d'une story et la stratégie de l'autre se cumulent.
- **AD à resserrer — AD-5/AD-6 (ajout) :**
  > `ExpirerReservationsEchues` n'est pas un handler mais une opération interne `ReservationExpiration.ExpirerEchues(AppDbContext db, DateTimeOffset now)`. Elle ne fait ni `BeginTransaction` ni `SaveChanges` : l'appelant possède la transaction. Toute commande transactionnelle s'exécute via un utilitaire unique `Common/Data/TransactionRunner.RunAsync(isolation, work)`, qui encapsule `CreateExecutionStrategy` et le nouvel essai borné sur interblocage (3 essais). Aucun handler n'ouvre de transaction autrement.

### H3 — Canal d'erreur des handlers non défini (exceptions contre résultat)

- **Unité A :** Reservations. Le handler lève `RefusMetierException("limite-reservations")`, et l'endpoint fait un `try/catch` vers `ApiErrors`.
- **Unité B :** Animaux. Le handler renvoie `null` pour « introuvable » et `bool` pour « refusé », et l'endpoint fait `Results.NotFound()`, qui produit un ProblemDetails et non le format d'AD-10.
- **Divergence :** les deux respectent AD-2 (`ICommandHandler<TCommand,TResult>`) et AD-10 (« passe par ApiErrors »), mais les 404 et 409 sortent sous deux formes, et le front parse `error` sur l'un et `title` sur l'autre.
- **AD à resserrer — AD-2 + AD-10 :**
  > `TResult` est toujours `Result<T>` (ou `Result`) de `Common/` : `Ok(value)`, `NotFound()`, `Conflict(code)`, `Invalid(errors)`, `Forbidden()`. Un refus métier n'est jamais signalé par une exception. Les endpoints convertissent uniquement via `result.ToHttpResult()` (unique, dans `Common/`), qui produit le format d'AD-10, y compris pour 404 et 403. `Results.NotFound()`, `Results.Problem()` et `ProblemDetails` sont interdits. Un middleware unique convertit toute exception non gérée en 500 `{ error }`.

### H4 — Catalogue d'énumérations incomplet et filtres non spécifiés

- **Unité A :** formulaire admin Animal. Il crée `Espece` = `Chat|Chien`, `Sexe` = `Male|Femelle`, `Age` = `int` (années), `Taille` = `Petite|Moyenne|Grande`.
- **Unité B :** catalogue public. Il filtre avec `?espece=chat&sexe=F&age=jeune&taille=M&enfants=oui,avoir`.
- **Divergence :**
  - La table « Statuts » ne fixe que les statuts et les ententes. Espèce, sexe, catégorie d'âge, taille, logement (`string Logement` dans l'ERD), noms des paramètres de filtre, casse, multi-valeur et sémantique « Oui ou À voir » ne sont fixés nulle part.
  - Par défaut, ASP.NET sérialise les enums en **entiers** tant qu'un `JsonStringEnumConverter` n'est pas enregistré. Une story l'ajoute sur ses DTO via un attribut, l'autre non.
- **AD à resserrer — convention « Statuts » devenue AD-19 :**
  > Énumérations C#, sérialisées en chaîne par un `JsonStringEnumConverter` global (`ConfigureHttpJsonOptions`), noms exacts : `Espece {Chat, Chien}`, `Sexe {Male, Femelle}`, `TrancheAge {Bebe, Jeune, Adulte, Senior}` (stockée, jamais calculée), `Taille {Petite, Moyenne, Grande}`, `Entente {Oui, AVoir, Non}`, `Logement {Appartement, Maison}`, `StatutAnimal`, `StatutReservation`. `Jardin` est un `bool`. Paramètres de filtre du catalogue (`GET /api/animaux`) : `espece`, `sexe`, `age`, `taille` (une valeur chacun) et `enfants`, `chiens`, `chats` (valeur `Oui` = « Oui ou À voir »). Les valeurs sont celles des enums, avec la casse exacte, et une valeur inconnue renvoie un 400. Les libellés affichés vivent dans un seul fichier `shared/libelles.ts`.

### H5 — Forme des collections, emplacement de `serverNow`, payload du refus « limite »

- **Unité A :** `GET /api/reservations/mes`. Elle renvoie un tableau nu et met `serverNow` dans chaque élément, pour respecter AD-11 (« toute réponse qui contient une réservation EnAttente renvoie expiresAt et serverNow »).
- **Unité B :** liste admin. Elle renvoie `{ items, serverNow }`. Le composant de compte à rebours partagé attend `@Input() serverNow` unique.
- **Divergence :**
  - Deux formes de liste, et deux sources d'écart d'horloge pour un même écran.
  - Le refus `limite-reservations` doit afficher les 2 réservations avec leur temps restant (EXPERIENCE), mais AD-10 fige `{ error, code }`. Une story ajoute `reservations` dans l'erreur, l'autre refait un `GET`.
  - Ajouter plus tard une pagination (Deferred) cassera tous les tableaux nus.
- **AD à resserrer — AD-11 + convention JSON :**
  > Toute collection est renvoyée enveloppée : `{ "items": [...], "serverNow": "…Z" }` (et `total` quand une pagination sera ajoutée), jamais en tableau nu. Une ressource unique qui contient une réservation porte `serverNow` à la racine, jamais dans un sous-objet. Les erreurs restent strictement au format d'AD-10, sans payload métier : sur `limite-reservations`, le front appelle `GET /api/reservations` pour afficher la liste.

### H6 — Aucune table de routes ni de verbes pour les sous-ressources et les actions

- **Unité A :** Favoris API. `POST /api/favoris { animalId }` et `DELETE /api/favoris/{favoriId}`. Favori a une clé composite, donc pas d'`Id`.
- **Unité B :** front favoris et fiche. `PUT /api/favoris/{animalId}` et `DELETE /api/favoris/{animalId}`.
- **Autres paires :**
  - réserver : `POST /api/reservations { animalId }` ou `POST /api/animaux/{id}/reservations` ;
  - mes réservations : `GET /api/reservations` ou `/api/reservations/mes` ;
  - adoptés : `GET /api/animaux?statut=Adopte` (contredit « le catalogue exclut Adopte ») ou `/api/animaux/adoptes` ;
  - accueil « 4 derniers » : `?limite=4` ou `/api/animaux/recents` ;
  - auth : `/connexion` ou `/login` ;
  - actions : `POST …/confirmer` ou `PATCH { statut }` ;
  - codes de retour : 200 avec DTO ou 204.
- **Nouvelle AD-20 — Contrat de routes :**
  > Table normative (toute route absente est interdite sans mise à jour de la spine) :
  > - `POST /api/auth/inscription`, `POST /api/auth/connexion`, `GET /api/auth/moi`
  > - `GET /api/animaux` (catalogue filtré, hors Adopte, `?limite=n` trié `CreatedAt` desc), `GET /api/animaux/adoptes`, `GET /api/animaux/{id}` (tout statut)
  > - `POST /api/reservations { animalId, logement, jardin, experience }` → 201, `GET /api/reservations` (celles du membre)
  > - `GET /api/favoris`, `PUT /api/favoris/{animalId}` → 204 idempotent, `DELETE /api/favoris/{animalId}` → 204 idempotent
  > - `GET /api/actualites`, `POST /api/contact` → 204, `GET /api/site`
  > - admin : `GET|POST /api/admin/animaux`, `GET|PUT|DELETE /api/admin/animaux/{id}`, `POST /api/admin/animaux/{id}/statut`, `POST /api/admin/animaux/{id}/photos`, `DELETE /api/admin/animaux/{id}/photos/{photoId}`, `GET /api/admin/reservations?statut=`, `POST /api/admin/reservations/{id}/confirmer`, `POST /api/admin/reservations/{id}/annuler`, `GET|POST /api/admin/actualites`, `PUT|DELETE /api/admin/actualites/{id}`, `PUT /api/admin/actualites/{id}/image`
  >
  > Actions d'état = `POST` + verbe français à l'infinitif, réponse 200 avec la ressource mise à jour. Création = 201 avec la ressource. Suppression = 204.

### H7 — Routes front, lien de l'email et `returnUrl` non fixés

- **Unité A :** API Reservations (AD-13). Elle construit le lien de l'email `{Site:BaseUrl}/admin/reservations`.
- **Unité B :** feature `reservations/admin` (AD-15 : « sous la route /admin »). Elle se monte sur `/admin/reservations/en-attente`. La feature auth utilise `/login?redirect=` alors qu'AD-9 parle de `returnUrl`.
- **Divergence :** le lien de l'email tombe sur une 404 front, et la garde redirige vers une route que la feature auth n'écoute pas. Les deux respectent la lettre de leurs AD.
- **Nouvelle AD-21 — Routes front :**
  > Table normative des routes Angular : `/`, `/adopter`, `/animaux/:id`, `/animaux/:id/reserver`, `/reservations/:id/envoyee`, `/ils-ont-trouve-une-famille`, `/actualites`, `/nous-aider/dons|benevolat|famille-accueil`, `/contact`, `/connexion?returnUrl=`, `/inscription`, `/mes-reservations`, `/mes-favoris`, `/admin/reservations?statut=EnAttente`, `/admin/animaux`, `/admin/animaux/nouveau`, `/admin/animaux/:id`, `/admin/actualites`, `/acces-refuse`. Les liens construits par l'API (email) utilisent uniquement les constantes `Common/LiensFront.cs`, alignées sur cette table.

### H8 — Plusieurs projections d'`Animal` pour une seule « carte animal » partagée

- **Unité A :** Animaux catalogue. `AnimalCarteDto { id, nom, espece, statut, photoPrincipaleUrl }`.
- **Unité B :** Favoris liste (AD-1 : « une requête peut lire n'importe quelle entité »). Sa propre projection est `{ animalId, prenom, photoUrl, statutAnimal }`. Le composant `shared/carte-animal` (AD-15) ne peut pas accepter les deux.
- **Divergence :**
  - Le même concept a deux noms de champ et deux règles de photo principale (`Ordre` minimal, ou première insérée).
  - Le statut affiché est tantôt effectif (C3), tantôt persisté.
  - L'URL `/fichiers/{clé}` est construite par deux slices différentes.
- **AD à resserrer — AD-1 (ajout) :**
  > Une slice qui expose une entité à d'autres lectures publie, dans `Features/<Slice>/Contrats/`, le DTO et l'expression de projection (`Expression<Func<Animal, AnimalCarteDto>>`) qui la produit. Les autres slices les réutilisent tels quels : c'est la seule dépendance inter-slices autorisée en lecture. `AnimalCarteDto` (id, nom, espece, sexe, statut effectif, photoPrincipaleUrl = photo d'`Ordre` minimal ou `null`) est le seul format de carte. L'URL d'un fichier n'est construite que par `IFileStorage.UrlPublique(cle)`.

### H9 — Contrat d'upload non spécifié (champ, ordre, actualité, réponse)

- **Unité A :** API Animaux photos. Elle lit `IFormFile photo` et prend `Ordre` d'un champ de formulaire envoyé par le client.
- **Unité B :** front admin. Il envoie la sélection multiple en parallèle avec le champ `file`, sans `ordre`. L'actualité est envoyée en un seul `multipart` (champs + image), parce qu'AD-12 ne décrit le flux en deux temps que pour les animaux.
- **Divergence :**
  - 400 systématique à cause du nom de champ.
  - Doublons d'`Ordre` en parallèle, donc photo principale indéterminée.
  - L'API Actualites, elle, attend du JSON puis une image séparée.
  - La réponse du `POST` photo n'est pas définie, alors que le front doit afficher la vignette et permettre de supprimer ou renvoyer « chaque photo en échec ».
- **AD à resserrer — AD-12 (ajout) :**
  > Upload = `multipart/form-data` avec un unique champ `fichier`, une requête par fichier, aussi bien pour les photos d'animal que pour l'image d'une actualité (`PUT /api/admin/actualites/{id}/image`, après création en JSON). L'`Ordre` est attribué par le serveur (max + 1, dans la transaction). Le client n'envoie jamais d'ordre, et il envoie les photos **séquentiellement** dans l'ordre de sélection. La réponse est 201 `PhotoDto { id, url, ordre }`. Le contrôle de format et de taille est fait par une seule `Common/Fichiers/RegleFichier` (valeurs à fixer : Open Question), avec refus 400 `{ errors: { fichier } }`. Le fichier est écrit avant le commit, puis supprimé si le commit échoue. Une suppression de fichier se fait après le commit.

### H10 — Supprimer un animal qui a des réservations ou des favoris : comportement FK choisi par deux slices

- **Unité A :** `IEntityTypeConfiguration<Reservation>` (slice Reservations) : `OnDelete(Restrict)` pour garder l'historique.
- **Unité B :** `IEntityTypeConfiguration<Favori>` (slice Favoris) : `OnDelete(Cascade)`. La commande `SupprimerAnimal` (Animaux) ne peut pas supprimer les réservations elle-même (AD-1).
- **Divergence :** la suppression lève une exception FK → 500. Ou bien Reservations choisit `Cascade` et l'historique d'adoption disparaît, ainsi que « Ils ont trouvé une famille ». Chaque story règle la question dans son dossier, selon AD-7.
- **AD à resserrer — AD-7 (ajout) :**
  > Les comportements de suppression des clés étrangères vers `Animal` et `User` sont fixés ici, et non par chaque slice : `Favori` → `Cascade` ; `PhotoAnimal` → `Cascade` (fichiers supprimés par la commande via `IFileStorage`) ; `Reservation` → `Restrict`. `SupprimerAnimal` est refusé en 409 `animal-a-des-reservations` si au moins une réservation existe, quel que soit son statut. L'animal adopté reste en base pour CAP-11.

### H11 — Lecture de l'identité : `sub` remappé par le handler JWT

- **Unité A :** Favoris lit `User.FindFirstValue("sub")` (AD-8 à la lettre).
- **Unité B :** Reservations lit `ClaimTypes.NameIdentifier` (habitude FitClub). Par défaut, `JwtBearer` remappe `sub` vers `NameIdentifier`, donc A obtient `null`, puis un 500 ou un favori sans utilisateur. Même problème pour `role` contre `ClaimTypes.Role`, dont dépend la policy `Admin`.
- **AD à resserrer — AD-8 (ajout) :**
  > `MapInboundClaims = false` ; `TokenValidationParameters.NameClaimType = "sub"`, `RoleClaimType = "role"`. L'identifiant de l'utilisateur ne se lit que par `ClaimsPrincipalExtensions.UserId()` (`Common/`), qui lève une exception s'il est absent. Aucun autre code ne lit un claim.

### H12 — Noms FR/EN des champs de DTO non fixés

- **Unité A :** API. `nom`, `description`, `vaccine`, `sterilise`, `puce`, `ententeEnfants`, `createdAt`, `publieeLe`, `resume`, `contenu`.
- **Unité B :** front. Il suit le vocabulaire d'EXPERIENCE : `prenom`, `sante.vaccine`, `ententes.enfants`, `dateCreation`, `texte`.
- **Divergence :** la convention « noms métier en français sans accent, suffixes en anglais » ne couvre pas les **propriétés**. L'ERD mélange déjà `CreatedAt` et `PublieeLe`, et le choix entre champs plats et objets imbriqués n'est pas fait.
- **AD à resserrer — convention « Langue du code » + glossaire :**
  > Les propriétés des entités et des DTO sont en français sans accent, y compris les dates (`CreeLe`, `PublieeLe`, `ExpireLe`). Seuls `Id` et les suffixes techniques sont en anglais. Les DTO sont plats (pas d'objets imbriqués `sante` ou `ententes`). Le glossaire normatif (`Animal` : `nom, espece, sexe, age, taille, description, vaccine, sterilise, puce, ententeEnfants, ententeChiens, ententeChats, statut, creeLe, photos` ; `Reservation` : `animalId, logement, jardin, experience, statut, creeLe, expireLe` ; `Actualite` : `titre, resume, texte, imageUrl, publieeLe`) est la seule source. Les interfaces TypeScript de `core/api/` le reprennent à l'identique.

---

## MOYENNES (10)

| # | Unités en conflit | Divergence | Rule proposée |
| --- | --- | --- | --- |
| M1 | Auth API (identifiants faux → 401) / intercepteur `core` (AD-9 : 401 → déconnexion + redirection) | Un mauvais mot de passe renvoie vers Connexion et efface le message d'erreur. | « Des identifiants invalides renvoient 400 `code: identifiants-invalides` ; 401 est réservé à un jeton absent ou expiré. L'intercepteur ignore `/api/auth/*`. » |
| M2 | Slice qui remplit `errors` avec `nameof(Telephone)` / slice qui utilise `telephone` | Le front ne raccroche pas les erreurs aux champs. | « Les clés de `errors` sont les noms de propriété JSON camelCase du corps de la requête. » |
| M3 | Auth stocke `06 12 34 56 78` / Site:Telephone `+33612…` / lien `tel:` admin | Formats de téléphone hétérogènes, `tel:` invalide. | « Téléphone normalisé en E.164 à l'écriture (inscription, configuration) ; affichage formaté par un seul pipe `shared/telephone.pipe`. » |
| M4 | Confirmer une réservation déjà Confirmée/Annulée (double clic, deux onglets) | Une story renvoie 200 idempotent, l'autre un 409 sans code. | « Ajouter le code `reservation-deja-traitee` (409) pour toute action sur une réservation non EnAttente non échue ; `reservation-echue` reste pour l'échue. » |
| M5 | Deux stories qui ajoutent chacune une migration en parallèle | Conflit sur `AppDbContextModelSnapshot`, migrations à la même date. | « Une migration par story, nommée `<Slice>_<Objet>`, régénérée après rebase ; jamais deux migrations ouvertes sur la branche principale. » |
| M6 | Réponse 201 de réservation (« numéro de l'association », séquence) / AD-14 (`/api/site` seule source) | Deux sources du numéro. | « La réponse de réservation ne contient pas le numéro ; l'écran le lit dans `SiteService`. » |
| M7 | `features/animaux` (fiche, cœur) / `features/favoris` (service à signals, AD-15 « un par fonctionnalité ») | La fiche réimplémente l'appel favori, ou importe un service d'une autre feature : deux états du cœur. | « Un état partagé par plusieurs features (`FavorisService`, `AuthService`, `SiteService`) vit dans `core/` ; une feature n'importe jamais une autre feature. » |
| M8 | Story Actualités édition / AD-11 (« date fixée à la publication ») | La modification met à jour ou non `PublieeLe`, ce qui réordonne l'accueil. | « `PublieeLe` est fixé une fois à la création et jamais modifié ; `LOCALE_ID` = `fr-FR` et l'affichage des dates passe par un pipe partagé. » |
| M9 | Tri non spécifié : catalogue, adoptés, admin | Ordre différent d'un écran à l'autre ; aucun `AdopteLe` pour trier « Ils ont trouvé une famille ». | « Catalogue : `CreeLe` desc ; adoptés : `AdopteLe` desc (champ posé par `MarquerAdopte` / `ForcerStatut(Adopte)`) ; admin : `ExpireLe` asc pour En attente, `CreeLe` desc sinon. » |
| M10 | Pagination différée / listes admin Expirées qui grossissent sans fin | Une story pagine en `?page=`, une autre en `?skip=&take=`. | « Si une pagination est ajoutée : `?page=1&taille=20`, réponse `{ items, total, serverNow }` (voir H5). Liste admin Expirées/Annulées limitée aux 30 derniers jours par défaut. » |

## BASSES (8)

| # | Divergence | Rule proposée |
| --- | --- | --- |
| L1 | Ajout d'un favori déjà présent : 409 ou 204. | Idempotent, 204 (voir H6). |
| L2 | `GET /api/actualites` vide : `[]`, 204 ou 404. | Toujours 200 `{ items: [] }`. |
| L3 | Le seed admin (`Data/`) écrit `User`, propriété d'Auth (AD-1). | Le seed appelle une opération de la slice Auth (`Features/Auth/SeedAdmin`). |
| L4 | Contact « page d'origine » : chemin ou titre de page. | Le front envoie `pageOrigine` = titre affiché ; l'API le met dans le sujet. |
| L5 | URL de photo relative dans les DTO, mais absolue nécessaire dans l'email. | DTO : relative `/fichiers/{clé}` ; email : `Site:BaseUrl` + relative. |
| L6 | `experience` absente : `null` ou `""`. | Texte facultatif = `null`, chaînes trimées, vide → `null`. |
| L7 | Rôle après rechargement : lu du localStorage ou de `/api/auth/moi`. | `AuthService` appelle `GET /api/auth/moi` au démarrage si un jeton existe. |
| L8 | Réservation d'un autre membre demandée : 403 ou 404. | 404 (ne révèle pas l'existence). |
