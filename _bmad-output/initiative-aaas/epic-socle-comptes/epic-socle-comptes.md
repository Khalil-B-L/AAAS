---
type: epic
title: "Socle et comptes"
parent: initiative-aaas
covers: [CAP-1]
after: []
assignee: ""
risk: medium
---

# Socle et comptes

## Description

Le socle plateforme du site AAAS (projets `aaas/api` et `aaas/web`, base, briques communes, harnais de tests, envoi d'emails, shell Angular) et les comptes : inscription avec téléphone obligatoire, connexion, session restaurée, rôles Membre/Admin, compte admin créé au démarrage.

## Outcome

Les epics suivants construisent sur un projet qui démarre, teste et authentifie ; signal : un visiteur crée un compte et se connecte, l'admin créé au démarrage accède à `/admin`.

## Requirements

- CAP-1 : la spec, Capabilities.
- E1 (socle) : scaffold, persistance, Common, harnais de tests, emails, `/api/site`, shell Angular (architecture, Structural Seed ; AD-1..AD-3, AD-6, AD-7, AD-10, AD-11, AD-13..AD-17, AD-19..AD-22).

## Done when

1. `dotnet run` + `ng serve` affichent le shell (en-tête, pied de page avec numéro et réseaux via `/api/site`, jetons CSS de DESIGN.md) sur téléphone et ordinateur.
2. Inscription sans téléphone refusée ; inscription, connexion et rechargement de page gardent la session ; un Membre est refusé sur l'espace admin (403 au format ApiErrors, page `/acces-refuse`).
3. Le compte admin est créé au premier démarrage depuis la configuration et accède à l'espace admin.
4. `dotnet test` (xunit.v3, base LocalDB jetable, environnement Testing, FakeTimeProvider) et les tests Vitest passent ; un test prouve le suffixe Z des dates et les énumérations en texte.
5. En mode Journal, un email de test est écrit dans le journal ; en mode Smtp, il part par Gmail.

## Boundaries

Socle plateforme + capacité comptes. Pas de déploiement ni de CI (local uniquement). Propriétaire unique de : harnais de tests, Common, IEmailSender (MailKit, modes Smtp/Journal), `/api/site`, pipe téléphone et bouton d'appel, LiensFront et routes Angular (contrats-api §7), JsonStringEnumConverter, menus membre/admin que les epics suivants remplissent, page d'accueil avec emplacements de sections.

## References

- spec — _bmad-output/initiative-aaas/spec-aaas-adoption/spec-aaas-adoption.md, CAP-1
- architecture — _bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md, AD-1..AD-3, AD-6..AD-11, AD-13..AD-17, AD-19..AD-22, Stack, Structural Seed
- contrat — _bmad-output/initiative-aaas/architecture-aaas-adoption/contrats-api.md, §4 (auth, site), §5, §6, §7
- ux — _bmad-output/initiative-aaas/ux-aaas-adoption/DESIGN.md ; EXPERIENCE.md (Foundation, Information Architecture, connexion et inscription) ; mockups/accueil.html

## Notes

- Decision: (2026-10-14) tracer bullet = entrée 1 (numéro de /api/site affiché par Angular via le proxy, base créée, tests qui passent).
- Decision: (2026-10-14) emails (4) après les comptes (3) : mêmes fichiers de configuration ; l'habillage Angular (5) avance en parallèle des entrées 2 à 4.
- Decision: (2026-10-14) Refactor sweep en clôture ; pas de suite end-to-end dédiée.
- Assumption: placeholders de configuration du site (numéro, réseaux, email admin) jusqu'à fourniture des vraies valeurs.
- Open question: valeurs de configuration réelles (numéro, Facebook, Instagram, email admin) ; placeholders en attendant.
