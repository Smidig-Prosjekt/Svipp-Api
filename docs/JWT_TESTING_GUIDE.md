# JWT Autentisering - Testing Guide

## Hvordan teste JWT-autentisering

### Metode 1: Swagger UI (Anbefalt for utvikling)

1. **Start API-et:**
   ```bash
   cd src/Svipp.Api
   dotnet run
   ```

2. **Åpne Swagger UI:**
   - Gå til: `https://localhost:5087/swagger` (eller `http://localhost:5087/swagger`)

3. **Registrer eller logg inn:**
   - Finn `/api/auth/register` eller `/api/auth/login` endpoint
   - Klikk "Try it out"
   - Fyll inn data og klikk "Execute"
   - Kopier `token` fra responsen

4. **Autoriser i Swagger:**
   - Klikk på "Authorize" knappen øverst til høyre
   - Lim inn tokenet (uten "Bearer" prefix)
   - Klikk "Authorize"
   - Nå kan du teste alle beskyttede endepunkter!

### Metode 2: HTTP-filer (Visual Studio / Rider)

1. **Åpne `Svipp.Api.http` filen**

2. **Registrer bruker:**
   - Kjør `POST /api/auth/register` requesten
   - Kopier token fra responsen

3. **Sett token:**
   - Endre `@jwt_token = your-jwt-token-here` til `@jwt_token = <ditt-token>`
   - Eller legg til en ny variabel: `@token = <ditt-token>`

4. **Test beskyttede endepunkter:**
   - Alle requests med `Authorization: Bearer {{jwt_token}}` vil nå fungere

### Metode 3: cURL / Postman

**Registrer:**
```bash
curl -X POST "http://localhost:5087/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "fullName": "John Doe",
    "email": "john@example.com",
    "phoneNumber": "+47 123 45 678",
    "password": "SecurePassword123!"
  }'
```

**Logg inn:**
```bash
curl -X POST "http://localhost:5087/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

**Bruk token:**
```bash
curl -X GET "http://localhost:5087/api/users/me" \
  -H "Authorization: Bearer <ditt-token-her>"
```

### Metode 4: JavaScript / TypeScript (for testing)

```javascript
// 1. Logg inn
const loginResponse = await fetch('http://localhost:5087/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'john@example.com',
    password: 'SecurePassword123!'
  })
});

const { token } = await loginResponse.json();

// 2. Bruk token i neste request
const userResponse = await fetch('http://localhost:5087/api/users/me', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});

const user = await userResponse.json();
console.log(user);
```

## Token-detaljer

- **Levetid:** 24 timer
- **Format:** JWT (JSON Web Token)
- **Claims:** 
  - `sub` / `NameIdentifier`: User ID (GUID)
  - `email`: Brukerens e-post
  - `name`: Fullt navn
  - `userId`: User ID (ekstra claim for kompatibilitet)

## Feilsøking

### 401 Unauthorized
- Sjekk at tokenet er riktig kopiert (ingen ekstra mellomrom)
- Sjekk at tokenet ikke er utløpt (24 timer)
- Sjekk at `Authorization` header er formatert som: `Bearer <token>`

### 403 Forbidden
- Sjekk at endepunktet krever autentisering (`[Authorize]`)
- Sjekk at tokenet er gyldig

### Token utløpt
- Logg inn på nytt for å få et nytt token
- Tokens utløper etter 24 timer



