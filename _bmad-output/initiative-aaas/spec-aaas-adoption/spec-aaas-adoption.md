---
id: SPEC-aaas-adoption
companions:
  - stack.md
  - state-machines.md
  - ../ux-aaas-adoption/DESIGN.md
  - ../ux-aaas-adoption/EXPERIENCE.md
  - ../architecture-aaas-adoption/architecture-aaas-adoption.md
  - ../architecture-aaas-adoption/contrats-api.md
sources: []
---

> **Canonical contract.** This SPEC and the files in `companions:` are the complete, preservation-validated contract for what to build, test, and validate. Source documents listed in frontmatter are for traceability — consult them only if you need narrative rationale or prose color this contract intentionally omits.

# AAAS — site d'adoption de chiens et chats

## Why

Vision et besoin : l'association AAAS (Association d'amitié et d'action sociale) veut un site pour faire adopter ses chiens et chats. Les visiteurs doivent trouver un animal, le réserver, puis appeler l'association pour finaliser. L'administrateur doit gérer seul les animaux, les actualités et les réservations, sans intermédiaire technique.

## Capabilities

- **CAP-1**
  - **intent:** Un visiteur peut créer un compte (téléphone obligatoire) et se connecter ; les comptes sont Membre ou Admin.
  - **success:** Inscription sans téléphone refusée ; inscription puis connexion réussies ; une page réservée aux membres est refusée sans connexion ; les écrans admin sont refusés à un Membre.
- **CAP-2**
  - **intent:** Tout visiteur peut parcourir les animaux à adopter et les filtrer par espèce, âge, taille, sexe et ententes (enfants, chiens, chats).
  - **success:** Chaque filtre, seul ou combiné, ne renvoie que les animaux correspondants ; les animaux Adoptés n'apparaissent pas dans le catalogue.
- **CAP-3**
  - **intent:** Tout visiteur peut consulter la fiche d'un animal : plusieurs photos, description, santé (vacciné, stérilisé, pucé), ententes (enfants, chiens, chats : Oui / À voir / Non) et statut.
  - **success:** La fiche affiche toutes les photos et les trois indicateurs de santé saisis par l'admin.
- **CAP-4**
  - **intent:** Un membre connecté peut réserver un animal Disponible en remplissant un questionnaire de pré-adoption (logement, jardin, expérience), puis voit le numéro de l'administrateur à appeler dans les 15 minutes.
  - **success:** Après envoi, l'animal passe Réservé, le numéro et le délai de 15 minutes s'affichent ; une seconde réservation du même animal est refusée ; une troisième réservation active du même membre est refusée ; sans confirmation de l'admin sous 15 minutes, la réservation expire et l'animal redevient Disponible.
- **CAP-5**
  - **intent:** L'administrateur est prévenu par email de chaque nouvelle réservation.
  - **success:** Chaque réservation déclenche un email à l'admin contenant l'animal, le membre et les réponses au questionnaire.
- **CAP-6**
  - **intent:** L'admin peut voir les réservations avec leur questionnaire, puis les confirmer ou les annuler.
  - **success:** Confirmer passe l'animal Adopté ; annuler le remet Disponible ; la liste reflète l'état après chaque action.
- **CAP-7**
  - **intent:** L'admin peut ajouter, modifier et supprimer un animal, photos et statut compris.
  - **success:** Un animal créé apparaît au catalogue ; modifié, la fiche reflète le changement ; supprimé, il disparaît du site.
- **CAP-8**
  - **intent:** Tout visiteur peut lire les actualités de l'association ; la section n'apparaît que s'il en existe.
  - **success:** Sans actualité, aucune section vide n'est affichée ; avec actualités, elles sont listées de la plus récente à la plus ancienne.
- **CAP-9**
  - **intent:** L'admin peut ajouter, modifier et supprimer une actualité.
  - **success:** Chaque opération est visible immédiatement côté public.
- **CAP-10**
  - **intent:** Un membre peut mettre des animaux en favoris et retrouver sa liste.
  - **success:** Un favori ajouté apparaît dans la liste du membre, persiste après reconnexion et peut être retiré.
- **CAP-11**
  - **intent:** Tout visiteur peut voir la page « Ils ont trouvé une famille » listant les animaux adoptés.
  - **success:** Un animal passé Adopté y apparaît sans autre action de l'admin.
- **CAP-12**
  - **intent:** Tout visiteur trouve des pages Dons, Bénévolat, Famille d'accueil et Contact où il peut appeler l'association ou lui écrire via un formulaire.
  - **success:** Les quatre pages sont accessibles depuis la navigation, affichent le numéro, et l'envoi du formulaire délivre un email à l'admin.

## Constraints

- Statuts et transitions fixés dans `state-machines.md` : une seule réservation active par animal, au plus 2 réservations actives par membre, expiration automatique après 15 minutes sans confirmation.
- L'adoption se finalise par téléphone avec l'admin ; le site ne conclut jamais l'adoption seul.
- Stack imposée : voir `stack.md` (C# .NET, Entity Framework, Angular, JWT, CQRS) ; invariants techniques AD-1..AD-22 dans `architecture-aaas-adoption.md`.
- Interface et contenus en français ; mode clair uniquement ; conception mobile-first, admin utilisable sur téléphone et ordinateur.
- Toute annulation ou suppression côté admin demande confirmation.

## Non-goals

- RGPD et mentions légales : mise en ligne sans, décision assumée pour cette version.
- Paiement ou don en ligne.
- Contrat ou adoption finalisés en ligne.
- Suivi des familles d'accueil et des bénévoles (seulement des pages de contact).
- Application mobile native.

## Success signal

- Démonstration de bout en bout : un visiteur s'inscrit, filtre, réserve un chat Disponible et voit le numéro ; l'admin reçoit l'email, confirme la réservation ; le chat disparaît du catalogue et apparaît dans « Ils ont trouvé une famille ».

## Assumptions

- Confirmer une réservation vaut adoption actée (animal → Adopté) ; annuler remet l'animal Disponible.
- Le numéro de l'admin est une valeur de configuration unique.
