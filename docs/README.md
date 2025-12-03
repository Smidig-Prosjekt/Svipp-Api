# Svipp API - Dokumentasjon

## Innhold

- **[JWT Testing Guide](./JWT_TESTING_GUIDE.md)** - Hvordan teste JWT-autentisering
- **[Next.js Integration](./NEXTJS_INTEGRATION.md)** - Komplett guide for å integrere Next.js frontend

## Quick Start

### Test JWT i Swagger

1. Start API: `dotnet run` i `src/Svipp.Api`
2. Gå til: `https://localhost:5087/swagger`
3. Test `/api/auth/register` eller `/api/auth/login`
4. Kopier token fra responsen
5. Klikk "Authorize" og lim inn tokenet
6. Test beskyttede endepunkter!

### Test med HTTP-filer

1. Åpne `src/Svipp.Api/Svipp.Api.http`
2. Kjør `POST /api/auth/register` eller `POST /api/auth/login`
3. Kopier token og sett i `@jwt_token` variabelen
4. Test alle andre endepunkter

### Koble til Next.js

Se [Next.js Integration Guide](./NEXTJS_INTEGRATION.md) for komplett implementasjon.

Kortversjon:
1. Opprett API client med token-håndtering
2. Bruk React Context for auth state
3. Beskytt ruter med middleware
4. Konfigurer CORS på API-siden



