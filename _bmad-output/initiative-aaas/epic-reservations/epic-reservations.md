---
type: epic
title: "Réservation à durée limitée"
parent: initiative-aaas
covers: [CAP-4, CAP-5, CAP-6]
after: []
assignee: ""
risk: high
---

# Réservation à durée limitée

## Description

Un membre réserve un animal Disponible avec un questionnaire et a 15 minutes pour appeler l'admin ; l'admin reçoit un email, voit les réservations avec le temps restant et confirme ou annule ; une réservation non confirmée expire seule.

## Outcome

Une réservation mène à un appel puis à une adoption ; signal : le tracer de l'initiative passe de bout en bout.

## Done when

1. Réserver passe l'animal Réservé, affiche le compte à rebours calé sur le serveur et le bouton d'appel ; une 2e réservation du même animal et une 3e réservation active du même membre sont refusées, concurrence incluse (tests).
2. Sans confirmation sous 15 minutes, la réservation est Expirée et l'animal Disponible, partout à la seconde (tests avec FakeTimeProvider).
3. Confirmer passe l'animal Adopté ; annuler le remet Disponible ; une action sur une réservation déjà traitée renvoie 409 `reservation-deja-traitee`.
4. Chaque réservation envoie un email à l'admin après commit ; un échec d'envoi est journalisé et la réservation reste valide.
5. Mes réservations et l'écran admin Réservations fonctionnent sur téléphone, avec l'alerte à 5 minutes ou moins.

## Boundaries

Slice Réservations. Ni l'entité Réservation ni le statut effectif de base (epic-catalogue-animaux). Propriétaire unique du compte à rebours.

## References

- spec — _bmad-output/initiative-aaas/spec-aaas-adoption/spec-aaas-adoption.md, CAP-4, CAP-5, CAP-6 ; state-machines.md
- architecture — _bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md, AD-4, AD-5, AD-6, AD-11, AD-13, AD-18 ; contrats-api.md §4, §5
- ux — _bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md (Réservation à durée limitée, parcours 1 et 2) ; mockups/ecrans-cles.html (questionnaire, confirmation, limite, admin réservations)

## Notes

- Decision: (2026-10-15, découpage de epic-catalogue-animaux) cet epic appelle les transitions de l'entité Animal (Reserver / Liberer / MarquerAdopte) construites par epic-catalogue-animaux ; il reçoit l'emplacement vide du bouton « Je veux l'adopter » sur la fiche ; il ajoute les animaux « Réservé » de démonstration (Development seulement, réservation réelle de 15 min et membre de démonstration).
