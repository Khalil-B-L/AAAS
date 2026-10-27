---
type: initiative
title: "AAAS — site d'adoption de chiens et chats"
parent: none
covers: [CAP-1, CAP-2, CAP-3, CAP-4, CAP-5, CAP-6, CAP-7, CAP-8, CAP-9, CAP-10, CAP-11, CAP-12]
after: []
assignee: ""
risk: medium
---

# AAAS — site d'adoption de chiens et chats

## Description

Un site où les visiteurs trouvent un chien ou un chat de l'association AAAS (Cap Bon, Nabeul), le réservent pour 15 minutes et appellent l'admin pour finaliser ; l'admin gère seul animaux, réservations et actualités. La spec porte les capacités, contraintes et non-goals ; cette initiative les livre en local.

## Outcome

Pour les visiteurs et l'admin de l'AAAS : le parcours d'adoption complet fonctionne en local ; signal = le success signal de la spec (démonstration de bout en bout).

## Done when

1. La démonstration du success signal de la spec passe en local (`dotnet run` + `ng serve`).
2. CAP-1 à CAP-12 sont utilisables sur téléphone et sur ordinateur, conformes à DESIGN.md et EXPERIENCE.md.
3. Les règles 15 minutes, 2 réservations actives par membre et 1 réservation active par animal sont prouvées par des tests d'intégration, concurrence incluse.
4. Le contrat `contrats-api.md` est respecté : dates en Z, énumérations en texte, format d'erreur unique.

## Boundaries

Découpage par capacité de la spec, un seul développeur, exécution locale uniquement. Hors périmètre : les non-goals de la spec et la section Deferred de l'architecture (hébergement, CI/CD, cookie httpOnly, pagination, stockage cloud, sauvegardes). Tracer : l'admin crée Caramel → visible au catalogue → un membre la réserve → l'admin confirme → Caramel apparaît dans « Ils ont trouvé une famille » (complet à la fin de l'epic 3).

- Touch point : Gmail SMTP — mot de passe d'application en user-secrets ; owner : epic-socle-comptes
- Touch point : SQL Server LocalDB et user-secrets (`Jwt:Key`, `Admin:Password`) ; owner : epic-socle-comptes
- Touch point : dossier des photos `Storage:PhotosPath` ; owner : epic-catalogue-animaux

## References

- spec — _bmad-output/initiative-aaas/spec-aaas-adoption/spec-aaas-adoption.md, section Capabilities
- architecture — _bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md (AD-1..AD-22, Capability → Architecture Map) et contrats-api.md
- ux — _bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md et EXPERIENCE.md, maquettes dans mockups/

## Notes

- Decision: (2026-10-14) socle plateforme sans CI, déploiement ni exploitation ; un epic est terminé quand il tourne en local.
- Decision: (2026-10-14) 4 epics dans l'ordre socle → catalogue → réservations → contenus ; pas de spike préalable sur l'expiration.
- Decision: (2026-10-14) l'epic catalogue crée l'entité Réservation, sa migration et `Common/Disponibilite` (statut effectif, réservation active) ; l'epic réservations ajoute le reste.
- Decision: (2026-10-14) CAP-11 (« Ils ont trouvé une famille ») rattachée à l'epic catalogue.
- Decision: (2026-10-14) propriétaires uniques des composants partagés : epic socle (pipe téléphone, bouton d'appel, LiensFront et routes Angular, JsonStringEnumConverter, IEmailSender, /api/site, harnais de tests) ; epic catalogue (dialogue de confirmation, badge de statut, IFileStorage, AnimalCarteDto) ; epic réservations (compte à rebours).
- Open question: valeurs de configuration à fournir — numéro de l'association, Facebook, Instagram, email de notification admin.
