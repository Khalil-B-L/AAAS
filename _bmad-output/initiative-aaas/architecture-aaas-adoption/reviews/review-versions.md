# Revue « versions » — Architecture Spine AAAS (site d'adoption)

- Relecteur : revue indépendante, lentille « versions / reality-check »
- Date : 2026-10-14
- Objet : `architecture-aaas-adoption.md` + `.memlog.md` (non modifiés)
- Références locales : `server/FitClub.Api/FitClub.Api.csproj`, `server/FitClub.Api.Tests/FitClub.Api.Tests.csproj`, poste du développeur

## Verdict

La spine est globalement ancrée dans la réalité (versions .NET alignées sur FitClub vérifié, Angular 22 et Gmail vérifiés sur le web au memlog). Mais elle a un patch de retard : .NET 10.0.13, une version de sécurité, est sortie le 2026-10-13. Elle épingle aussi xUnit v2, aujourd'hui en mode maintenance, pour un projet greenfield. Elle n'épingle ni la version mineure d'Angular, ni le runner de tests front, ni la version de Node, alors que ces informations sont désormais connues. Aucun point bloquant (critical).

## Constat local (vérifié le 2026-10-14)

| Élément | Valeur locale | Commande |
| --- | --- | --- |
| SDK .NET | 10.0.401 (aussi 10.0.201 installé) | `dotnet --version`, `dotnet --list-sdks` |
| Node.js | v24.15.0 | `node --version` |
| npm | 11.12.1 | `npm --version` |
| LocalDB | `MSSQLLocalDB`, Microsoft SQL Server 2025 (17.0.4025.3), en cours d'exécution | `sqllocaldb info MSSQLLocalDB` |
| Angular CLI publié (npm) | 22.2.2, engines node `^22.22.3 \|\| ^24.15.0 \|\| >=26.0.0` | `npm view @angular/cli version` / `engines` |
| FitClub.Api | net10.0, EF Core SqlServer/Design 10.0.12, JwtBearer 10.0.12 | csproj |
| FitClub.Api.Tests | xunit 2.9.3, xunit.runner.visualstudio 3.1.4, Microsoft.AspNetCore.Mvc.Testing 10.0.12, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4 | csproj |

## Findings

### HIGH-1 — Les paquets .NET 10 ont un patch de retard ; 10.0.13 est une version de sécurité

- **Spine** : Stack, lignes EF Core SqlServer/Design 10.0.12, JwtBearer 10.0.12, Mvc.Testing 10.0.12 (« alignés sur FitClub »).
- **Réalité** : .NET 10.0.13 est sortie le 2026-10-13 avec `security: true`. Le SDK correspondant est 10.0.402, alors que le poste a 10.0.401. NuGet publie 10.0.13 pour `Microsoft.EntityFrameworkCore.SqlServer` et `Microsoft.AspNetCore.Mvc.Testing`, tous deux datés du 2026-10-13.
- **Correction proposée** : épingler `10.0.13` pour EF Core SqlServer/Design, JwtBearer et Mvc.Testing ; installer le SDK 10.0.402 (runtime 10.0.13). Écrire « 10.0.x, dernier patch au moment du scaffold (10.0.13 au 2026-10-14) » plutôt qu'une valeur figée copiée de FitClub. Aligner FitClub reste optionnel.
- **Sources** :
  - https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json (latest-release 10.0.13, 2026-10-13, latest-sdk 10.0.402, security true)
  - https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer (10.0.13, 2026-10-13)
  - https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing (10.0.13, 2026-10-13)

### HIGH-2 — xUnit 2.9.3 (v2) épinglé pour un projet neuf alors que v2 est en mode maintenance

- **Spine** : Stack, `xUnit | 2.9.3` (copié de FitClub, jamais confronté au web : aucune entrée `(version)` xUnit dans le memlog).
- **Réalité** : xunit.net indique que v2 (2.9.3) est « in maintenance mode » : seules les corrections critiques sont publiées, toutes les nouveautés vont dans v3, et la migration est recommandée « at their earliest convenience ». Le template `dotnet new xunit3` génère v3. La dernière version stable est `xunit.v3` 4.0.2 (2026-10-09). Mvc.Testing 10.0.13 ne dépend pas du framework de test et fonctionne aussi avec v3.
- **Correction proposée** : `xunit.v3` 4.0.2 (paquet `xunit.v3`, pas `xunit`) avec `xunit.runner.visualstudio` 3.x. Autre option : garder 2.9.3 en l'écrivant explicitement comme choix d'alignement sur FitClub, en sachant que v2 ne reçoit plus de nouveautés.
- **Sources** :
  - https://xunit.net/ (v2 en maintenance, v3 recommandé)
  - https://www.nuget.org/packages/xunit.v3 (4.0.2, 2026-10-09)

### MEDIUM-1 — La version de Node pour Angular 22 est connue : la question ouverte peut être fermée

- **Spine** : Open Questions, « Version de Node.js à installer pour Angular 22 : à vérifier ».
- **Réalité** : la table officielle donne, pour Angular 22.x : Node `^22.22.3 || ^24.15.0 || ^26.0.0`, TypeScript `>=6.0.0 <6.1.0`, RxJS `^6.5.3 || ^7.4.0`. Le champ `engines` de `@angular/cli@22.2.2` publié sur npm dit la même chose. Le poste a Node **v24.15.0**, soit exactement le plancher de la branche 24 : c'est compatible. Attention, Netlify et releases.sh annoncent encore « Node 22.12 ». Ce chiffre est le plancher d'Angular 21 et il est faux pour Angular 22.
- **Correction proposée** : ajouter à la Stack la ligne `Node.js | 24.x LTS (>= 24.15.0), déjà installé (v24.15.0)` et la ligne `TypeScript | 6.0.x (imposé par Angular 22)`, puis retirer la question ouverte.
- **Sources** : https://angular.dev/reference/versions ; `npm view @angular/cli@22 engines` (local) ; source contradictoire et obsolète : https://www.netlify.com/changelog/2026-06-01-support-for-angular-v22/

### MEDIUM-2 — « Angular 22 » sans version mineure, et défauts du starter `ng new` non reflétés

- **Spine** : Stack `Angular | 22` ; AD-15 « standalone + signals » ; convention Tests limitée à xUnit.
- **Réalité** : le schéma `@schematics/angular@22.2.2` (application/schema.json, inspecté localement) donne les défauts suivants :
  - `standalone: true` et `zoneless: true` (pas de zone.js) ;
  - `testRunner: "vitest"` (Karma seulement en option) ;
  - `style: "css"`, `routing: true`, `ssr: false`, `strict: true` ;
  - builder `@angular/build:application`, dev server `@angular/build:dev-server`, tests `@angular/build:unit-test` ;
  - composants générés en `ChangeDetectionStrategy.OnPush` par défaut (l'ancien comportement s'appelle désormais `Eager`).

  Ces défauts vont dans le sens d'AD-15 (signals) et d'AD-16 (CSS maison, `style: css`). En revanche, la spine ne dit rien des tests front : Vitest, `await fixture.whenStable()` en zoneless. Un développeur pourrait aussi ajouter zone.js ou `Eager` par habitude.
- **Correction proposée** : Stack `Angular | 22.2.x (CLI 22.2.2)`. Ajouter une ligne Tests front : `Vitest (défaut ng new 22), builder @angular/build:unit-test`. Préciser dans AD-15 : « application zoneless et OnPush (défauts de ng new 22) ; pas de zone.js ». Ajouter `ng new --ssr=false --style=css` dans le seed si l'on veut figer ces choix.
- **Sources** : `npm pack @schematics/angular@22.2.2` (schema.json, workspace-models.js, component/schema.json, inspectés localement) ; https://angular.dev/reference/versions ; annonce : https://blog.angular.dev/announcing-angular-v22-c52bb83a4664 (403 au fetch, défauts confirmés par le schematic).

### MEDIUM-3 — Librairie SMTP : la réponse est connue, il faut épingler MailKit

- **Spine** : Open Questions, « MailKit ou System.Net.Mail.SmtpClient ; à vérifier ».
- **Réalité** : la page Microsoft Learn de `SmtpClient` pour net-10.0 dit : « The SmtpClient type is not recommended for new development… Use MailKit or other libraries instead ». La classe n'est pas marquée `[Obsolete]` dans le code, mais elle est déconseillée (DE0005). MailKit 4.18.1 (2026-09-27) est compatible net10.0 et n'a pas d'avertissement de vulnérabilité. Les versions 4.5.0 à 4.15.1 sont signalées vulnérables (modérée), donc ne pas descendre en dessous de 4.16.
- **Correction proposée** : Stack `MailKit | 4.18.1` (MimeKit 4.18.1 en dépendance). Dans `IEmailSender` : `SecureSocketOptions.StartTls` sur le port 587. Fermer la question ouverte.
- **Sources** : https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-10.0 ; https://github.com/dotnet/platform-compat/blob/master/docs/DE0005.md ; https://www.nuget.org/packages/MailKit

### LOW-1 — Gmail : mot de passe d'application confirmé, avec des restrictions de compte à noter

- **Spine** : AD-13 et Stack, Gmail 587 STARTTLS avec mot de passe d'application et validation en 2 étapes. Le memlog l'a vérifié sur le web en 2026-10 : **confirmé**.
- **Compléments** : Google indique que les mots de passe d'application sont « not recommended » mais toujours disponibles. Ils exigent la validation en 2 étapes. L'option peut être absente pour les comptes professionnels ou scolaires, les comptes en Protection avancée, et les comptes dont la 2FA n'utilise que des clés de sécurité. Changer le mot de passe Google révoque tous les mots de passe d'application.
- **Correction proposée** : préciser « compte Gmail personnel (pas Workspace ni Protection avancée) ». Noter dans Deferred qu'un changement de mot de passe du compte casse l'envoi.
- **Source** : https://support.google.com/accounts/answer/185833

### LOW-2 — LocalDB : version non écrite, mais confirmée localement

- **Spine** : « instance `(localdb)\MSSQLLocalDB` déjà utilisée par FitClub ».
- **Réalité** : instance `MSSQLLocalDB` en SQL Server 2025 (17.0.4025.3), en cours d'exécution. Depuis SQL Server 2025, LocalDB est inclus dans l'édition Express.
- **Correction proposée** : Stack `SQL Server LocalDB | 2025 (17.0), instance MSSQLLocalDB`.
- **Sources** : `sqllocaldb info MSSQLLocalDB` (local) ; https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

### LOW-3 — Paquets de test et horloge de test non épinglés

- Les versions de `Microsoft.NET.Test.Sdk` (17.14.1 dans FitClub), `xunit.runner.visualstudio` (3.1.4) et `coverlet.collector` (6.0.4) n'apparaissent pas dans la spine et n'ont pas été vérifiées sur le web. Si v3 est retenu (HIGH-2), il faut les réaligner au moment du scaffold.
- La convention Tests parle d'une « horloge de test (modèle FitClub) ». FitClub.Api.Tests ne contient aucune référence à `TimeProvider` ou `FakeTimeProvider`, donc ce modèle n'existe pas. Il faut le préciser : `FakeTimeProvider` du paquet `Microsoft.Extensions.TimeProvider.Testing` (version 10.x à vérifier au scaffold), ou une horloge maison.

## Décisions déjà vérifiées (aucune action)

| Décision | Statut | Preuve |
| --- | --- | --- |
| .NET 10 / net10.0 | Confirmé, version supportée (LTS) ; SDK 10.0.401 installé | csproj FitClub, `dotnet --list-sdks`, releases.json |
| Angular 22 (sortie 2026-06-03) | Confirmé, dernière version 22.2.2 | memlog (web), npm |
| MediatR sous licence commerciale | Vérifié sur le web au memlog ; la spine l'évite (AD-2) | memlog |
| `PasswordHasher<User>` sans Identity complet | Inclus dans le framework partagé ASP.NET Core, aucun paquet en plus | FitClub |
| Gmail 587 STARTTLS + mot de passe d'application | Confirmé | support.google.com |

## Sources

- https://angular.dev/reference/versions
- https://blog.angular.dev/announcing-angular-v22-c52bb83a4664
- https://www.netlify.com/changelog/2026-06-01-support-for-angular-v22/ (contient le plancher Node 22.12, obsolète)
- https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json
- https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer
- https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing
- https://xunit.net/
- https://www.nuget.org/packages/xunit.v3
- https://learn.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-10.0
- https://github.com/dotnet/platform-compat/blob/master/docs/DE0005.md
- https://www.nuget.org/packages/MailKit
- https://support.google.com/accounts/answer/185833
- https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb
