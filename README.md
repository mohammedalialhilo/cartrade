# CarTrade ASP.NET Web App

Detta projekt implementerar ett samlat Remarketing Management-system i **ASP.NET Core MVC** med:

- Gemensam hantering av fordon i ett system
- Filimport (semikolon-separerad) + manuell inmatning
- Besiktningsflode (start/slut/skador/reparationskostnad)
- Finance-flode (vardering, offert, leverantorsbeslut)
- Sales-flode (publicera/styra auktioner)
- Responsiv auktionssida med budgivning
- Rollbaserad behorighet och inloggning (Identity)

## Roller

- `Admin`
- `Inspection`
- `Finance`
- `Sales`
- `Dealer`

Seedade demo-konton skapas automatiskt vid start:

- `admin@cartrade.local`
- `inspection@cartrade.local`
- `finance@cartrade.local`
- `sales@cartrade.local`
- `dealer@cartrade.local`

Losenord: `CarTrade!2026`

## Konto-hantering

- Sjavregistrering ar avstangd for vanliga anvandare.
- Endast `Admin` kan skapa nya konton.
- Admin skapar konton via `Users`-sidan i webbappen och valjer roll direkt vid skapande.

## Koera lokalt

```bash
dotnet restore
dotnet build
dotnet run
```

## Databas

Standard: SQLite (`DatabaseProvider: sqlite`) i `appsettings.json`.

For MySQL, satt:

- `DatabaseProvider` till `mysql`
- `ConnectionStrings:MySqlConnection` med korrekt server/databas/anvandare/losenord

Och koer migration:

```bash
dotnet ef database update
```

## CSV-import

Importerad fil ska vara semikolon-separerad. Vanliga rubriker som stods:

- `RegistrationNumber`, `Make`, `Model` (kravs)
- `ExternalReference`, `Vin`, `ModelYear`, `OdometerKm`, `Color`, `FuelType`, `PartnerName`

## Sakerhet

- Identity-baserad autentisering
- Rollstyrd auktorisering per avdelning
- Secure cookie
- Lockout vid misslyckade inloggningar
- Rate limiting (429 vid overbelastning)

## Deploy: Render + Netlify

Foljande ar redan tillagt i projektet:

- `Dockerfile` for container deploy pa Render
- `.dockerignore`
- `render.yaml` (Render Blueprint)
- `netlify.toml` (proxy config)
- `netlify-public/index.html` (required static publish dir for Netlify)

### Delar jag inte kan gora at dig (konto/dashboard) - steg for steg

1. Pusha projektet till GitHub.
2. Ga till Render och valj `New +` -> `Blueprint`.
3. Valj ditt repo och deploya `render.yaml`.
4. Nar Render ar klar, kopiera URL:en, t.ex. `https://cartrade-web.onrender.com`.
5. Oppna `netlify.toml` och byt:
   - `https://YOUR-RENDER-URL.onrender.com/:splat`
   - till din riktiga Render URL.
6. Commit + push den andringen till GitHub.
7. Ga till Netlify -> `Add new site` -> `Import an existing project`.
8. Valj samma repo.
9. Build settings:
   - Build command: tom (blank)
   - Publish directory: `netlify-public`
10. Deploya pa Netlify.
11. Testa din Netlify URL (login och alla routes).

### Viktigt for drift

- Med `DatabaseProvider=sqlite` pa Render utan persistent disk blir data ej permanent.
- For riktig drift: anvand MySQL och satt env vars i Render:
  - `DatabaseProvider=mysql`
  - `ConnectionStrings__MySqlConnection=...`
