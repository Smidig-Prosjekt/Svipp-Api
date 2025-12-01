## Svipp API

Svipp er en tjeneste som gjør det mulig for sjåfører med el‑sparkesykkel å få oppdrag om å hente og kjøre biler hjem for personer som har drukket eller av andre grunner ikke bør kjøre selv.  
Sjåføren kommer på el‑sparkesykkel, kjører deg hjem i din egen bil, og drar videre til neste oppdrag på sparkesykkelen – litt som Uber, men for deg **og** din bil.

Dette repositoriet inneholder **Svipp API**, som eksponerer funksjonalitet for:

- **Kunder**: bestille henting, se status på oppdrag, historikk m.m.
- **Sjåfører**: motta og akseptere oppdrag, se rute, fullføre turer.
- **Administrasjon**: administrere brukere, roller, soner, priser, rapportering m.m. (på sikt).

---

## Mål og hovedfunksjoner

- **Trygg hjemtransport** for personer som ikke bør kjøre egen bil.
- **Oppdragsformidling** mellom kunder og sjåfører med el‑sparkesykkel.
- **Sanntidsstatus** på oppdrag (bestilt, tildelt sjåfør, på vei, pågår, fullført).
- **Grunnlag for fakturering/betaling** (selve betalingsløsningen kan ligge i egen tjeneste/app, men API-et gir nødvendig data).

---

## Teknologi (planlagt)

- **Plattform**: .NET (ASP.NET Core Web API)
- **Arkitektur**: lagdelt/Clean-inspirert
  - `Svipp.Api` – HTTP‑lag (kontrollere/endpoints, request/response‑modeller)
  - `Svipp.Application` – use cases, forretningsregler, tjenester
  - `Svipp.Domain` – domenemodeller (bruker, sjåfør, oppdrag, rute, kjøretøy osv.)
  - `Svipp.Infrastructure` – database, repositories, integrasjoner (f.eks. kart/tredjeparts‑API)
- **Tester**:
  - `Svipp.UnitTests` – enhetstester for domenelogikk og applikasjonslag
  - `Svipp.IntegrationTests` – integrasjonstester mot API og infrastruktur

---

## Mappestruktur

Per nå er prosjektet strukturert slik:

- `src/`
  - `Svipp.Api/`
  - `Svipp.Application/`
  - `Svipp.Domain/`
  - `Svipp.Infrastructure/`
- `tests/`
  - `Svipp.UnitTests/`
  - `Svipp.IntegrationTests/`

Prosjektene (`.csproj`) og selve koden vil bygges videre inne i disse mappene.

---

## Videre arbeid

Planlagte neste steg:

- Opprette .NET‑prosjekter (`dotnet new`) for Api, Application, Domain, Infrastructure og tester.
- Definere grunnleggende domenemodeller (kunde, sjåfør, oppdrag).
- Designe første versjon av API‑endpoints (f.eks. registrere kunde, registrere sjåfør, opprette oppdrag, hente status).
- Koble til database og legge til enkel persistering av oppdrag og brukere.

---

## Første funksjon: brukerregistrering

Første prioritet i Svipp API er **brukerregistrering** (kunde først, sjåfør kan komme som neste steg).

- **Mål**:
  - Kunder skal kunne registrere seg via API-et.
  - Data skal lagres i database (minst: navn, e‑post, telefonnummer, passord-hash).
  - Grunnlag for innlogging/autentisering settes (f.eks. JWT senere).

- **Enkel domenemodell (første versjon)**:
  - `User` / `Customer` med felter som:
    - Id (GUID)
    - FullName
    - Email
    - PhoneNumber
    - PasswordHash
    - CreatedAt
    - UpdatedAt

- **Typiske endpoints (første versjon)**:
  - `POST /api/users/register` – registrere ny kunde.
  - `POST /api/auth/login` – innlogging (kan implementeres etter registrering).
  - `GET /api/users/me` – hente info om innlogget bruker (krever auth, kan komme senere).

---

## Forslag til konkrete steg (for utvikling)

1. **Oppsett av løsning og prosjekter**
   - Opprette en løsning:
     - `dotnet new sln -n Svipp`
   - Opprette prosjekter:
     - `dotnet new webapi -n Svipp.Api -o src/Svipp.Api`
     - `dotnet new classlib -n Svipp.Application -o src/Svipp.Application`
     - `dotnet new classlib -n Svipp.Domain -o src/Svipp.Domain`
     - `dotnet new classlib -n Svipp.Infrastructure -o src/Svipp.Infrastructure`
   - Legge prosjektene inn i løsningen:
     - `dotnet sln Svipp.sln add src/Svipp.Api/Svipp.Api.csproj src/Svipp.Application/Svipp.Application.csproj src/Svipp.Domain/Svipp.Domain.csproj src/Svipp.Infrastructure/Svipp.Infrastructure.csproj`
   - Legge til prosjektreferanser:
     - `Svipp.Application` refererer `Svipp.Domain`
     - `Svipp.Infrastructure` refererer `Svipp.Domain` og `Svipp.Application`
     - `Svipp.Api` refererer `Svipp.Application` og `Svipp.Infrastructure`

2. **Domenemodell for bruker**
   - Lage en enkel `User`/`Customer`‑entitet i `Svipp.Domain`.
   - Evt. skille mellom kunde og sjåfør senere (`Customer` og `Driver`).

3. **Application‑lag for registrering**
   - Lage en kommando / service for brukerregistrering i `Svipp.Application`.
   - Legge på enkel validering (e‑postformat, passordlengde osv.).

4. **Infrastruktur for lagring**
   - Velge database (f.eks. SQL Server eller SQLite lokalt).
   - Konfigurere Entity Framework Core (DbContext, migrasjoner).
   - Lage repository / DbSet for brukere.

5. **API‑endpoint**
   - Legge til en `UsersController` (eller tilsvarende) i `Svipp.Api`.
   - Implementere `POST /api/users/register` som tar inn en `RegisterUserRequest`‑modell og kaller application‑laget.

Når disse stegene er gjennomført, har Svipp API første versjon av **brukerregistrering** klar, og kan utvides med innlogging, sjåførregistrering og oppdragslogikk.




