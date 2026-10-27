---
type: epic
title: "Catalogue, gestion des animaux et adoptés"
parent: initiative-aaas
covers: [CAP-2, CAP-3, CAP-7, CAP-11]
after: []
assignee: ""
risk: medium
---

# Catalogue, gestion des animaux et adoptés

## Description

L'admin ajoute, modifie, supprime les animaux avec leurs photos et force leur statut ; les visiteurs parcourent le catalogue filtré, consultent les fiches, voient les derniers pensionnaires sur l'accueil et la page « Ils ont trouvé une famille ».

## Outcome

Les visiteurs trouvent un animal à adopter ; signal : un animal créé par l'admin apparaît au catalogue et, passé Adopté, dans « Ils ont trouvé une famille ».

## Done when

1. Admin, sur téléphone et ordinateur : créer un animal avec photos (JPEG/PNG/WebP, 5 Mo, 10 max), le modifier, forcer son statut, le supprimer ; les refus 409 (`reservation-active`, `animal-a-des-reservations`, `modifie-ailleurs`) sont testés.
2. Le catalogue filtré (espèce, âge, taille, sexe, ententes) n'affiche que les correspondances et jamais les Adoptés ; la fiche montre photos, santé, ententes et statut effectif.
3. L'accueil montre les derniers pensionnaires ; « Ils ont trouvé une famille » liste les Adoptés.
4. L'entité Réservation, sa migration et `Common/Disponibilite` existent, avec des tests du statut effectif.

## Boundaries

Slice Animaux + fondations de Réservation (entité, réservation active, statut effectif). Ni réserver, ni expirer, ni emails (epic-reservations). Propriétaire unique de : IFileStorage, AnimalCarteDto, dialogue de confirmation, badge de statut. Possède aussi les méthodes de transition de l'entité Animal (Reserver / Liberer / MarquerAdopte / ForcerStatut, AD-4) ; epic-reservations les appelle. Remet à epic-reservations l'emplacement du bouton « Je veux l'adopter » sur la fiche et les animaux « Réservé » de démonstration ; à epic-contenus-engagement l'emplacement du cœur favori.

## References

- spec — _bmad-output/initiative-aaas/spec-aaas-adoption/spec-aaas-adoption.md, CAP-2, CAP-3, CAP-7, CAP-11 ; state-machines.md
- architecture — _bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md, AD-1, AD-4, AD-5, AD-7, AD-12, AD-18, AD-19 ; contrats-api.md §1-§7
- ux — _bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md (catalogue, fiche, admin animaux, adoptés) ; DESIGN.md (carte animal, pastilles, galerie, dialogue de confirmation, zone d'ajout photo) ; mockups/ecrans-cles.html (fiche, ajout d'animal)
- rétrospective epic 1 — _bmad-output/initiative-aaas/epic-socle-comptes/epic-socle-comptes-retrospective.md (A4 : outillage de tests partagé)

## Notes

- Decision: (2026-10-14) cet epic crée l'entité Réservation et `Common/Disponibilite` parce qu'il en a besoin le premier.
- Decision: (2026-10-14) CAP-11 rattachée à cet epic (requête adoptés dans la slice Animaux).
- Decision: (2026-10-15) animaux de démonstration créés au démarrage en environnement Development seulement (jamais en Testing), juste après l'entité Animal ; photos de démonstration = illustrations générées versionnées dans le dépôt, ajoutées quand le stockage des photos existe.
- Decision: (2026-10-15) les quatre méthodes de transition de l'entité Animal (AD-4) sont construites dans cet epic dès l'entrée qui crée l'entité ; epic-reservations les appelle (aligne l'epic sur le plan d'initiative « Animal et ses transitions »).
- Decision: (2026-10-15) animaux « Réservé » de démonstration reportés à epic-reservations (une réservation réelle expire en 15 min et demande un membre de démonstration).
- Decision: (2026-10-15) découpage fin (~15 entrées) plutôt que 8 à 12, suite à la rétrospective de l'epic 1 (plans trop gros) ; écrans admin en trois entrées.
- Decision: (2026-10-15) entrée d'ouverture « Outillage de tests partagé » (rétrospective epic 1, A4), qui ramène aussi les gardes de démarrage à un seul style (constat F23).
- Decision: (2026-10-15) les illustrations de démonstration sont générées par le build (script, pas d'images tierces, aucun droit d'auteur) ; la personne de l'étape hitl vérifie seulement leur rendu.
- Decision: (2026-10-15) tracer bullet = entrée 2 (entité Animal → GET /api/animaux → carte animal sur l'accueil) ; puis réservation et statut effectif, avant catalogue et fiche ; catalogue, fiche et adoptés en chaîne (mêmes endpoints publics et routes front).
