---
type: epic
title: "Actualités, favoris et contact"
parent: initiative-aaas
covers: [CAP-8, CAP-9, CAP-10, CAP-12]
after: []
assignee: ""
risk: low
---

# Actualités, favoris et contact

## Description

L'admin publie des actualités ; les visiteurs les lisent et la section de l'accueil disparaît quand il n'y en a pas ; les membres gèrent leurs favoris ; les pages Dons, Bénévolat, Famille d'accueil et Contact permettent d'appeler ou d'écrire à l'association.

## Outcome

L'association garde le lien avec ses visiteurs ; signal : une actualité publiée apparaît sur l'accueil et un message de contact arrive par email à l'admin.

## Done when

1. Admin : créer, modifier, supprimer une actualité avec image ; visible immédiatement, triée de la plus récente à la plus ancienne ; section absente de l'accueil sans actualité.
2. Un membre ajoute et retire des favoris ; la liste persiste après reconnexion ; le cœur sans connexion mène à la connexion.
3. Les quatre pages sont accessibles depuis la navigation, affichent le numéro, et leur formulaire délivre un email à l'admin (503 `envoi-impossible` en cas d'échec).
4. Tests d'intégration et Vitest couvrent ces parcours.

## Boundaries

Slices Actualites, Favoris, Contact. Consomme AnimalCarteDto, IFileStorage et IEmailSender sans les modifier.

## References

- spec — _bmad-output/initiative-aaas/spec-aaas-adoption/spec-aaas-adoption.md, CAP-8, CAP-9, CAP-10, CAP-12
- architecture — _bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md, AD-12, AD-13, AD-14 ; contrats-api.md §4
- ux — _bmad-output/initiative-aaas/ux-aaas-adoption/EXPERIENCE.md (actualités, favoris, pages de contact, parcours 3 à 6) ; mockups/accueil.html
