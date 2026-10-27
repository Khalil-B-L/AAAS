- source_plan: `_bmad-output/initiative-aaas/epic-socle-comptes/story-habillage-angular-plan.md`
  summary: Corriger la règle de saisie du téléphone de la story 1.3 pour la Tunisie (8 chiffres nationaux → `+216…`, à la place de `0X XX XX XX XX` → `+33…`) et passer le placeholder `Site:Telephone` en +216.
  evidence: Décision utilisateur du 2026-10-14 (numéros tunisiens : association au Cap Bon, Nabeul ; maquettes en +216) ; `Features/Auth/Telephone.cs` refuse aujourd'hui un numéro tunisien saisi sans `+216`.
- source_plan: `_bmad-output/initiative-aaas/epic-catalogue-animaux/story-animaux-de-demonstration-development-plan.md`
  summary: Réconcilier AD-1/AD-3 de l'architecture avec les écrivains de démarrage réels : `SeedAdmin` et `DemoAnimaux` (Development seulement) écrivent hors commande.
  evidence: AD-3 « Seules les commandes et ReservationExpiration écrivent » ; `Features/Animaux/DemoAnimaux.cs` ajoute des animaux au démarrage (décision 2026-10-15 de l'epic catalogue).
