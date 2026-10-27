# AAAS — site d'adoption

Deux projets lancés séparément sur le poste du développeur (AD-17) :

- `api/` — API .NET 10 (`Aaas.Api`) + tests xUnit v3 (`Aaas.Api.Tests`), base SQL Server LocalDB `Aaas`.
- `web/` — application Angular 22 ; `ng serve` relaie `/api` et `/fichiers` vers l'API (`proxy.conf.json`).

## Prérequis

- SDK .NET 10 (poste : 10.0.401, runtime 10.0.12) ;
- SQL Server LocalDB, instance `(localdb)\MSSQLLocalDB` ;
- Node.js 24.15.0 (ou ^22.22.3), puis `npm install` dans `web/` ;
- certificat de développement HTTPS : `dotnet dev-certs https --trust` (une fois).

## Secrets (une fois par poste)

L'API refuse de démarrer sans clé JWT d'au moins 32 octets ni mot de passe admin d'au moins 8 caractères.
Ils vivent uniquement dans `dotnet user-secrets`, jamais dans `appsettings*.json` :

```bash
cd aaas/api/Aaas.Api
dotnet user-secrets set "Jwt:Key" "<au moins 32 caractères aléatoires>"
dotnet user-secrets set "Admin:Password" "<mot de passe du compte admin, 8 caractères minimum>"
```

Au démarrage, le compte admin `Admin:Email` (placeholder dans `appsettings.json`, à remplacer par l'email de
l'association) est créé, ou remis `Admin` avec ce mot de passe s'il existe déjà. C'est la seule façon de créer
un Admin : l'inscription publique crée toujours un `Membre`.

### Emails (Gmail)

Les notifications partent de `Smtp:User` vers `Admin:NotificationEmail` (placeholders dans `appsettings.json`,
à remplacer par les vraies adresses) via `smtp.gmail.com:587` en StartTls. Il faut un **mot de passe
d'application** Gmail (compte avec validation en 2 étapes activée : Compte Google > Sécurité > Mots de passe
des applications), jamais le mot de passe du compte :

```bash
cd aaas/api/Aaas.Api
dotnet user-secrets set "Smtp:Password" "<mot de passe d'application Gmail, 16 caractères>"
```

Sans `Smtp:Password`, l'API démarre quand même en mode `Journal` (un avertissement au démarrage) : chaque
email est écrit dans le log au lieu d'être envoyé. `Email:Mode = Journal` force ce mode ; les tests
(environnement `Testing`) l'utilisent toujours. Pour vérifier l'envoi réel avec ces secrets :

```bash
AAAS_EMAIL_REEL=1 dotnet test aaas/api/Aaas.sln --filter "FullyQualifiedName~EmailReel"
```

## Démarrer

```bash
# 1. API — https://localhost:7180 (crée la base Aaas et applique les migrations au démarrage)
cd aaas/api/Aaas.Api
dotnet run --launch-profile https

# 2. Front — http://localhost:4200 (dans un second terminal)
cd aaas/web
npm start
```

Les valeurs du site (`Site:Telephone`, `Site:Facebook`, `Site:Instagram`) sont des placeholders dans
`api/Aaas.Api/appsettings.json`. Les secrets (`Jwt:Key`, `Admin:Password`, `Smtp:Password`)
passent uniquement par `dotnet user-secrets` (voir ci-dessus).

## Tests

```bash
# API : base LocalDB jetable AaasTests_<guid>, supprimée en fin de run
dotnet test aaas/api/Aaas.sln

# Front : Vitest
cd aaas/web
npx ng test --watch=false
```

## Migrations

```bash
cd aaas/api
dotnet tool restore
dotnet ef migrations add <Slice>_<Objet> --project Aaas.Api --output-dir Data/Migrations
```
