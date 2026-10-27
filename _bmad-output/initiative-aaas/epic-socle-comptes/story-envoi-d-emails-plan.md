---
title: "Envoi d'emails"
type: 'feature'
ticket: '4'
created: '2026-10-14'
status: 'built'
baseline_revision: 'NO_VCS'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-aaas/architecture-aaas-adoption/architecture-aaas-adoption.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** L'admin doit être prévenu par email des réservations (CAP-5) et des messages de contact (CAP-12), mais l'API ne sait envoyer aucun email ; sans mécanisme unique, chaque slice configurerait son propre SMTP (AD-13).

**Approach:** Un seul `IEmailSender` dans `Common/`, qui envoie à `Admin:NotificationEmail`, avec deux implémentations choisies par `Email:Mode` : `Smtp` (MailKit 4.18.1 vers `smtp.gmail.com:587` en StartTls, mot de passe d'application) et `Journal` (l'email est écrit dans le log). `Testing` est forcé en `Journal` ; `Smtp` sans `Smtp:Password` retombe sur `Journal` avec un avertissement, pour que l'API démarre toujours.

## Boundaries & Constraints

**Always:** destinataire unique `Admin:NotificationEmail`, jamais fourni par l'appelant ; expéditeur `Smtp:User` ; `Smtp:Password` uniquement en user-secrets ; délai court (`Smtp:TimeoutSecondes`, 10 par défaut) ; un échec d'envoi lève une exception, la politique (journaliser ou 503) appartient à l'appelant ; échecs SMTP journalisés en `Error`, jamais le mot de passe ; options validées au démarrage ; MailKit et MimeKit jamais en dessous de 4.16.

**Never:** `System.Net.Mail.SmtpClient` ; file de nouvel essai ; envoi depuis une requête (aucune route n'est ajoutée) ; email de réservation ou de contact (epics 3 et 4).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Journal | `Email:Mode = Journal`, message (sujet, texte, réponse-à facultatif) | log `Information` avec destinataire, sujet et corps ; aucune connexion SMTP | — |
| Testing | environnement `Testing`, `Email:Mode = Smtp` | implémentation `Journal` résolue | — |
| Smtp sans mot de passe | `Development`, `Email:Mode = Smtp`, `Smtp:Password` absent | implémentation `Journal`, avertissement au démarrage | l'API démarre |
| Smtp OK | mode `Smtp`, compte Gmail valide | email reçu dans la boîte `Admin:NotificationEmail` | — |
| Smtp en échec | serveur injoignable ou authentification refusée | exception levée au plus tard après le délai | `Error` journalisé sans mot de passe |
| Config invalide | `Admin:NotificationEmail` absent ou mal formé, `Email:Mode` inconnu | l'API refuse de démarrer | message nommant la clé |

</frozen-after-approval>

## Code Map

- `aaas/api/Aaas.Api/Features/Auth/AdminOptions.cs`, `AuthSetup.VerifierConfigurationAuth` -- modèle d'options validées au démarrage avec validateur `IValidateOptions` et garde avant la base ; `AdresseEmail.EstValide` / `Normaliser` à réutiliser pour `Admin:NotificationEmail`.
- `aaas/api/Aaas.Api/Program.cs` -- garde de configuration avant `Migrate()` ; y ajouter l'enregistrement et la garde email.
- `aaas/api/Aaas.Api/appsettings.json` -- sections existantes `Jwt`, `Admin` (placeholder marqué), `Site` ; ajouter `Email`, `Smtp`, `Admin:NotificationEmail`.
- `aaas/api/Aaas.Api.Tests/AaasFactory.cs` (`Journal` : `JournalDeTest` capture le log), `AuthTests.cs` (`Demarrage_refuse…`, `TrouverValidation`) -- harnais à réutiliser.
- `aaas/README.md` -- section « Secrets » à compléter avec `Smtp:Password`.

## Tasks & Acceptance

**Execution:**
- [x] `aaas/api/Aaas.Api/Aaas.Api.csproj` -- `MailKit` 4.18.1.
- [x] `aaas/api/Aaas.Api/Common/Email/IEmailSender.cs` -- `MessageEmail(Sujet, Texte, RepondreA?)` et `Task EnvoyerAsync(MessageEmail, CancellationToken)`.
- [x] `aaas/api/Aaas.Api/Common/Email/EmailOptions.cs` -- `Email:Mode` (`Smtp`/`Journal`), `Smtp:Host` (`smtp.gmail.com`), `Smtp:Port` (587), `Smtp:User`, `Smtp:Password`, `Smtp:TimeoutSecondes` ; validateur.
- [x] `aaas/api/Aaas.Api/Common/Email/SmtpEmailSender.cs`, `JournalEmailSender.cs` -- MailKit `SecureSocketOptions.StartTls`, `Timeout`, texte brut UTF-8, `Reply-To` si fourni ; journal en `Information`.
- [x] `aaas/api/Aaas.Api/Common/Email/EmailSetup.cs` -- choix de l'implémentation (`Testing` → Journal ; Smtp sans mot de passe → Journal + avertissement), `Admin:NotificationEmail` validé, garde de démarrage.
- [x] `aaas/api/Aaas.Api/Program.cs`, `appsettings.json`, `aaas/README.md` -- enregistrement + garde ; `Email:Mode = Smtp`, `Smtp:User` et `Admin:NotificationEmail` en placeholders marqués ; README : `dotnet user-secrets set "Smtp:Password"` (mot de passe d'application Gmail, validation en 2 étapes).
- [x] `aaas/api/Aaas.Api.Tests/EmailTests.cs` -- une couverture par ligne de la matrice, sauf « Smtp OK » ; l'échec SMTP est testé contre un port local fermé (délai respecté, exception, log `Error` sans mot de passe) ; un test d'envoi réel ignoré (`Assert.SkipUnless`) sauf si la variable `AAAS_EMAIL_REEL=1` est définie, qui lit les user-secrets de `Aaas.Api` et envoie un email de test.

**Acceptance Criteria:**
- Given `Smtp:Password` (mot de passe d'application) et `Smtp:User` / `Admin:NotificationEmail` réels, when on lance le test réel avec `AAAS_EMAIL_REEL=1`, then l'email de test arrive dans la boîte `Admin:NotificationEmail` (vérification humaine).
- Given `dotnet test` sans `AAAS_EMAIL_REEL`, then tous les tests passent, le test réel est signalé ignoré.

## Implementation Notes

- `Email:Mode` est lu en chaîne et validé exactement (`Smtp` / `Journal`, casse comprise) pour que l'erreur nomme la clé ; absent → refus.
- `Admin:NotificationEmail` vit dans `NotificationOptions` (section `Admin`, validateur propre) pour ne pas mêler la garde email à `AdminOptions`.
- La section `Smtp` (`Host`, `Port`, `User` email valide, `TimeoutSecondes` 1–60) n'est validée que si le mode effectif est `Smtp` (résolution de `SmtpEmailSender` dans la garde de démarrage).
- `SmtpEmailSender` borne l'envoi complet par `CancelAfter(TimeoutSecondes)` en plus de `SmtpClient.Timeout` ; dépassement → `TimeoutException` ; annulation par l'appelant relancée sans log `Error`.
- `System.Security.Cryptography.Pkcs` épinglé en 10.0.13 : MimeKit 4.18.1 tire la 10.0.0 (NU1902/NU1903), ce qui cassait « 0 avertissement ».
- `JournalDeTest` capture désormais aussi `Information` pour les catégories `Aaas.Api.*` (nécessaire pour vérifier le mode Journal).
- Après revue : `AdresseEmail` déplacée dans `Common/` (`AdresseEmail.Max`, `User.EmailMax` en dérive) ; `RepondreA` vérifié par `MessageEmail.Verifier` dans les deux modes (`ArgumentException`, avant connexion, sans log d'échec SMTP) ; échec de `DisconnectAsync` après `SendAsync` → `Warning`, pas d'exception ; `SmtpEmailSender.CreerClient` interne (InternalsVisibleTo) pour le faux serveur SMTP TLS des tests.

## Plan Change Log

## Review Triage Log

Pass 1 (lens quick + constat de l'orchestrateur) — high 0, medium 2, low 2, false 0, maybe-false 0.

| # | Finding | Verdict | Route | Evidence / action |
|---|---------|---------|-------|-------------------|
| 1 | `SmtpEmailSender` : un échec de `DisconnectAsync` après un `SendAsync` réussi est signalé comme échec d'envoi | medium | patch | Même `try` et même délai ; pour Contact (AD-13) le visiteur verrait 503 alors que l'admin a reçu l'email, et renverrait un doublon. Correctif : une fois `SendAsync` terminé, un échec de déconnexion est journalisé en `Warning` et n'est pas relancé. |
| 2 | `RepondreA` mal formé : exception MimeKit journalisée comme échec SMTP en mode Smtp, ignoré en mode Journal | medium | patch | `MailboxAddress.Parse` dans le `try` ; `Testing` résout toujours Journal, donc les tests de Contact ne verraient rien. Correctif : même validation dans les deux modes, avant toute connexion, `ArgumentException` hors du chemin d'échec SMTP. |
| 3 | `Journal_demande_en_Development…` ne vérifie pas l'absence d'avertissement | low | patch | N'assert que le type. Correctif : compter les avertissements de repli avant/après, comme le test voisin. |
| 4 | `Common/Email` dépend de `Features/Auth/AdresseEmail` (constat de l'orchestrateur) | low | patch | Les règles de dépendance de l'architecture vont de `Features` vers `Common`, jamais l'inverse. Correctif : déplacer `AdresseEmail` dans `Common/`, mettre à jour ses usages dans Auth. |

## Verification

**Commands:**
- `dotnet build aaas/api/Aaas.sln` -- expected: 0 erreur, 0 avertissement.
- `dotnet test aaas/api/Aaas.sln` -- expected: tous verts, un ignoré.

**Manual checks (if no CLI):**
- Avec les vrais secrets : `AAAS_EMAIL_REEL=1 dotnet test aaas/api/Aaas.sln --filter "FullyQualifiedName~EmailReel"` puis vérifier la boîte de réception.
