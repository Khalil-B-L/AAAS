---
name: 'AAAS — contrats API et front'
type: architecture-companion
companion-of: architecture-aaas-adoption.md
status: final
created: '2026-10-14'
updated: '2026-10-14'
---

# Contrats API et front — AAAS

Tables normatives citées par la spine (AD-19, AD-20, AD-21, convention « Langue du code »). Toute route, valeur d'énumération ou propriété absente d'ici est interdite tant que ce fichier n'est pas mis à jour. Les tables décident ; le code possède le détail d'implémentation.

## 1. Énumérations (AD-19)

Noms C# exacts ; sérialisées en chaîne (même casse) par le convertisseur global `EnumEnChaineConverter` (`Common/Json/`) : nom exact, entier ou casse différente → 400 (décision 2026-10-14 : `JsonStringEnumConverter` ignore la casse en lecture) ; stockées en chaîne (`HasConversion<string>()`).

| Enum | Valeurs | Libellés affichés (`shared/libelles.ts`) |
| --- | --- | --- |
| `Espece` | `Chat`, `Chien` | Chat, Chien |
| `Sexe` | `Male`, `Femelle` | Mâle, Femelle |
| `TrancheAge` | `Bebe`, `Jeune`, `Adulte`, `Senior` | Bébé, Jeune, Adulte, Senior |
| `Taille` | `Petite`, `Moyenne`, `Grande` | Petite, Moyenne, Grande |
| `Entente` | `Oui`, `AVoir`, `Non` | Oui, À voir, Non |
| `Logement` | `Appartement`, `Maison` | Appartement, Maison |
| `StatutAnimal` | `Disponible`, `Reserve`, `Adopte` | Disponible, Réservé, Adopté |
| `StatutReservation` | `EnAttente`, `Confirmee`, `Annulee`, `Expiree` | En attente, Confirmée, Annulée, Expirée |
| `Role` | `Membre`, `Admin` | Membre, Admin |

`TrancheAge` est saisie par l'admin et stockée, jamais calculée. `Jardin` est un `bool`.

## 2. Filtres du catalogue (`GET /api/animaux`)

| Paramètre | Type | Sémantique |
| --- | --- | --- |
| `espece` | `Espece` | égalité |
| `sexe` | `Sexe` | égalité |
| `age` | `TrancheAge` | égalité |
| `taille` | `Taille` | égalité |
| `enfants`, `chiens`, `chats` | `Oui` uniquement | l'entente correspondante vaut `Oui` ou `AVoir` |
| `limite` | int 1..50 | les n plus récents (accueil : 4) |

Une valeur par paramètre ; casse exacte des enums ; valeur inconnue → 400 `ApiErrors` avec `errors.<param>`. Filtres combinés en ET. Le catalogue exclut toujours les animaux dont le statut effectif est `Adopte`.

## 3. Tri des listes

| Liste | Tri |
| --- | --- |
| Catalogue, admin animaux | `creeLe` décroissant |
| Ils ont trouvé une famille | `adopteLe` décroissant |
| Actualités | `publieeLe` décroissant |
| Admin réservations `EnAttente` | `expireLe` croissant |
| Admin réservations autres statuts, mes réservations | `creeLe` décroissant |
| Favoris | ajout le plus récent en premier |

## 4. Routes API (AD-20)

Notation `:id` = paramètre de route `int`. Actions d'état : `POST` + verbe à l'infinitif, 200 avec la ressource à jour. Création : 201 avec la ressource. Suppression : 204. Toute collection : 200 `{ items, serverNow }` (jamais un tableau nu, jamais 204/404 si vide).

| Méthode et route | Accès | Corps → réponse |
| --- | --- | --- |
| `POST /api/auth/inscription` | public | `InscriptionDto` → 201 `AuthReponseDto` |
| `POST /api/auth/connexion` | public | `ConnexionDto` → 200 `AuthReponseDto` ; échec → 400 `identifiants-invalides` |
| `GET /api/auth/moi` | membre | → 200 `UtilisateurDto` |
| `GET /api/site` | public | → 200 `SiteDto` |
| `GET /fichiers/:cle` | public | fichier via `IFileStorage` ; clé invalide ou absente → 404 |
| `GET /api/animaux` | public | filtres §2 → 200 `{ items: AnimalCarteDto[], serverNow }` |
| `GET /api/animaux/adoptes` | public | → 200 `{ items: AnimalCarteDto[], serverNow }` |
| `GET /api/animaux/:id` | public | → 200 `AnimalDetailDto` (tout statut) |
| `POST /api/reservations` | membre | `ReservationCreationDto` → 201 `ReservationDto` + `serverNow` à la racine |
| `GET /api/reservations` | membre | celles du membre → 200 `{ items: ReservationDto[], serverNow }` |
| `GET /api/favoris` | membre | → 200 `{ items: AnimalCarteDto[], serverNow }` |
| `PUT /api/favoris/:animalId` | membre | → 204, idempotent |
| `DELETE /api/favoris/:animalId` | membre | → 204, idempotent |
| `GET /api/actualites` | public | → 200 `{ items: ActualiteDto[], serverNow }` |
| `POST /api/contact` | public | `ContactDto` → 204 ; échec SMTP → 503 `envoi-impossible` |
| `GET /api/admin/animaux` | admin | → 200 `{ items: AnimalCarteDto[], serverNow }` (tous statuts) |
| `POST /api/admin/animaux` | admin | `AnimalEcritureDto` → 201 `AnimalDetailDto` (statut `Disponible`) |
| `GET /api/admin/animaux/:id` | admin | → 200 `AnimalDetailDto` |
| `PUT /api/admin/animaux/:id` | admin | `AnimalEcritureDto` (sans statut) → 200 `AnimalDetailDto` |
| `DELETE /api/admin/animaux/:id` | admin | → 204 ; 409 `animal-a-des-reservations` |
| `POST /api/admin/animaux/:id/statut` | admin | `StatutAnimalDto` → 200 `AnimalDetailDto` ; 409 `reservation-active`, `modifie-ailleurs` |
| `POST /api/admin/animaux/:id/photos` | admin | multipart `fichier` → 201 `PhotoDto` |
| `DELETE /api/admin/animaux/:id/photos/:photoId` | admin | → 204 |
| `GET /api/admin/reservations?statut=` | admin | statut effectif → 200 `{ items: ReservationAdminDto[], serverNow, enAttente }` |
| `POST /api/admin/reservations/:id/confirmer` | admin | → 200 `ReservationAdminDto` + `serverNow` ; 409 `reservation-echue`, `reservation-deja-traitee` |
| `POST /api/admin/reservations/:id/annuler` | admin | idem confirmer |
| `GET /api/admin/actualites` | admin | → 200 `{ items: ActualiteDto[], serverNow }` |
| `POST /api/admin/actualites` | admin | `ActualiteEcritureDto` (JSON) → 201 `ActualiteDto` |
| `PUT /api/admin/actualites/:id` | admin | `ActualiteEcritureDto` → 200 `ActualiteDto` |
| `DELETE /api/admin/actualites/:id` | admin | → 204 |
| `PUT /api/admin/actualites/:id/image` | admin | multipart `fichier` → 200 `ActualiteDto` (remplace l'image) |

Toute autre route sous `/api` → 404 `ApiErrors`. Une réservation d'un autre membre n'est jamais exposée (404).

## 5. Codes de refus métier (AD-10)

| Code | HTTP | Cas | Écran front |
| --- | --- | --- | --- |
| `identifiants-invalides` | 400 | email ou mot de passe faux | message sur le formulaire Connexion |
| `animal-indisponible` | 409 | animal non `Disponible` (effectif) ou index unique violé | « Trop tard » |
| `limite-reservations` | 409 | déjà 2 réservations actives | refus « limite » ; le front appelle `GET /api/reservations` pour la liste |
| `reservation-echue` | 409 | confirmer/annuler une réservation dont le statut effectif est `Expiree` | carte rechargée |
| `reservation-deja-traitee` | 409 | réservation déjà `Confirmee` ou `Annulee`, ou conflit `RowVersion` | carte rechargée |
| `reservation-active` | 409 | forcer le statut d'un animal qui a une réservation active | message dans le formulaire animal |
| `animal-a-des-reservations` | 409 | supprimer un animal ayant au moins une réservation | message dans la liste admin |
| `modifie-ailleurs` | 409 | conflit `RowVersion` sur `Animal` ou `statutAttendu` différent du statut effectif | formulaire rechargé |
| `envoi-impossible` | 503 | échec SMTP du formulaire de contact | saisie conservée, alerte d'erreur |

## 6. Glossaire des DTO (convention « Langue du code »)

Propriétés JSON en camelCase, français sans accent ; seuls `id`, les suffixes techniques et `serverNow` sont en anglais. DTO plats : aucune imbrication de regroupement (`sante`, `ententes`) ; seule imbrication permise, un DTO de contrat publié (`AnimalCarteDto`, `PhotoDto`). Texte facultatif : chaînes trimées, vide → `null`. Les interfaces TypeScript de `core/api/` reprennent ces noms à l'identique.

| DTO | Propriétés |
| --- | --- |
| `InscriptionDto` | `prenom`, `nom`, `email`, `telephone`, `motDePasse` |
| `ConnexionDto` | `email`, `motDePasse` |
| `AuthReponseDto` | `jeton`, `expireLe`, `utilisateur` (`UtilisateurDto`) |
| `UtilisateurDto` | `id`, `prenom`, `nom`, `email`, `telephone`, `role` |
| `SiteDto` | `telephone`, `facebook`, `instagram` |
| `AnimalCarteDto` | `id`, `nom`, `espece`, `sexe`, `age`, `taille`, `statut` (effectif), `photoPrincipaleUrl` (photo d'`ordre` minimal ou `null`) |
| `AnimalDetailDto` | carte + `description`, `vaccine`, `sterilise`, `puce`, `ententeEnfants`, `ententeChiens`, `ententeChats`, `creeLe`, `adopteLe` (`null` sauf adopté), `photos` (`PhotoDto[]` triées par `ordre`) |
| `AnimalEcritureDto` | `nom`, `espece`, `sexe`, `age`, `taille`, `description`, `vaccine`, `sterilise`, `puce`, `ententeEnfants`, `ententeChiens`, `ententeChats` |
| `StatutAnimalDto` | `statut` (`Disponible` ou `Adopte`), `statutAttendu` |
| `PhotoDto` | `id`, `url`, `ordre` |
| `ReservationCreationDto` | `animalId`, `logement`, `jardin`, `experience` (facultatif) |
| `ReservationDto` | `id`, `animal` (`AnimalCarteDto`), `statut` (effectif), `creeLe`, `expireLe` |
| `ReservationAdminDto` | `ReservationDto` + `logement`, `jardin`, `experience`, `membrePrenom`, `membreNom`, `membreTelephone`, `membreEmail` |
| `ActualiteEcritureDto` | `titre`, `resume`, `texte` |
| `ActualiteDto` | `id`, `titre`, `resume`, `texte`, `imageUrl` (`null` sans image), `publieeLe` |
| `ContactDto` | `nom`, `telephone`, `email` (facultatif), `message`, `pageOrigine` (titre de la page affichée) |

Entités : mêmes noms en PascalCase (`CreeLe`, `ExpireLe`, `PublieeLe`, `AdopteLe`, `Cle`, `Ordre`, `ImageCle`). Les URLs (`url`, `photoPrincipaleUrl`, `imageUrl`) sont relatives (`/fichiers/<clé>`) ; seul l'email les rend absolues avec `Site:BaseUrl`.

## 7. Routes Angular (AD-21)

| Route | Écran | Garde |
| --- | --- | --- |
| `/` | Accueil | — |
| `/adopter` | Catalogue + filtres (filtres en query params, mêmes noms qu'au §2) | — |
| `/animaux/:id` | Fiche animal | — |
| `/animaux/:id/reserver` | Questionnaire | membre |
| `/reservations/:id/envoyee` | Réservation envoyée (compte à rebours) | membre |
| `/ils-ont-trouve-une-famille` | Adoptés | — |
| `/actualites` | Actualités | — |
| `/nous-aider/dons`, `/nous-aider/benevolat`, `/nous-aider/famille-accueil` | Pages Nous aider | — |
| `/contact` | Contact | — |
| `/connexion?returnUrl=` | Connexion | — |
| `/inscription?returnUrl=` | Créer mon compte | — |
| `/mes-reservations` | Mes réservations | membre |
| `/mes-favoris` | Mes favoris | membre |
| `/admin/reservations?statut=EnAttente` | Admin · Réservations (onglet = query param) | admin |
| `/admin/animaux`, `/admin/animaux/nouveau`, `/admin/animaux/:id` | Admin · Animaux | admin |
| `/admin/actualites` | Admin · Actualités | admin |
| `/acces-refuse` | « Cette page est réservée à l'association. » | — |

Liens construits par l'API (email) : uniquement via les constantes de `Common/LiensFront.cs`, alignées sur cette table.
