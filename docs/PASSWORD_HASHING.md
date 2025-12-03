# Passordhashing - BCrypt + Pepper

## Hvordan passord hashes

Svipp API bruker **BCrypt** (via `BCrypt.Net-Next` pakken) kombinert med **Pepper** for ekstra sikkerhet.

### Implementasjon

```csharp
// Hashing (ved registrering eller passordendring)
// PasswordHasher legger automatisk til pepper før BCrypt hashing
PasswordHash = _passwordHasher.HashPassword(password);

// Verifisering (ved login)
// PasswordHasher legger automatisk til pepper før BCrypt verifisering
_passwordHasher.VerifyPassword(password, storedHash)
```

### Hva er Pepper?

**Pepper** er en ekstra hemmelig verdi som legges til passordet før hashing:
- Lagres **ikke** i databasen (kun i konfigurasjon)
- Samme pepper for alle passord (i motsetning til salt som er unikt per passord)
- Ekstra beskyttelse: Selv om databasen blir kompromittert, kan ikke passordene dekodes uten pepper-verdien

**Forskel mellom Salt og Pepper:**
- **Salt:** Unikt per passord, lagres sammen med hash i databasen
- **Pepper:** Samme for alle passord, lagres kun i konfigurasjon/appsettings

### Hvor det skjer

1. **Registrering:** `AuthController.Register()` - linje 109
2. **Passordendring:** `UsersController.ChangePassword()` - linje 302
3. **Login:** `AuthController.Login()` - linje 197

## Hvor lang tid tar det?

### Standard konfigurasjon

BCrypt bruker **work factor** (cost factor) som bestemmer kompleksiteten:
- **Standard work factor:** 10 (2^10 = 1024 iterasjoner)
- **Typisk tid:** 50-100ms på moderne hardware
- **Sikkerhet:** God balanse mellom sikkerhet og ytelse

### Tidsestimater (avhengig av hardware)

| Work Factor | Iterasjoner | Tid (ca.) |
|-------------|-------------|-----------|
| 8           | 256         | ~10-20ms  |
| 10 (default)| 1024        | ~50-100ms |
| 12          | 4096        | ~200-400ms|
| 14          | 16384       | ~1-2s     |

### Hvorfor er det langsomt?

BCrypt er **bevisst langsom** for å gjøre brute-force angrep vanskeligere:
- Angripere må bruke mye tid på hvert passord
- Selv med kraftig hardware tar det lang tid å teste mange passord
- Beskytter mot rainbow tables og brute-force angrep

## Sikkerhetsegenskaper

✅ **Salt automatisk:** BCrypt genererer unik salt for hvert passord  
✅ **Pepper:** Ekstra hemmelig verdi lagt til før hashing (ikke i database)  
✅ **Adaptive:** Work factor kan økes over tid når hardware blir bedre  
✅ **Kollisjonsresistent:** Svært usannsynlig at to passord gir samme hash  
✅ **One-way:** Umulig å dekode hash tilbake til passord  
✅ **Defense in depth:** Flere lag med sikkerhet (BCrypt + Salt + Pepper)  

## Eksempel på hash

```
$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy
 │  │  │
 │  │  └─ Salt + Hash (31 tegn)
 │  └───── Work factor (10)
 └──────── BCrypt variant (2a)
```

## Konfigurere work factor (valgfritt)

Hvis du vil endre work factor (ikke anbefalt med mindre du har spesifikke krav):

```csharp
// I HashPassword metoden
return BCryptNet.HashPassword(password, workFactor: 12);
```

**Anbefaling:**
- **Development:** Work factor 10 (default) er fint
- **Production:** Work factor 10-12 er anbefalt
- **Høy sikkerhet:** Work factor 12-14 (men saktere for brukere)

## Ytelseshensyn

- **Registrering:** 50-100ms ekstra (akseptabelt)
- **Login:** 50-100ms ekstra (akseptabelt)
- **Passordendring:** 50-100ms ekstra (akseptabelt)

Dette er **ikke** merkbar for brukere, men gir betydelig sikkerhet.

## Sammenligning med andre metoder

| Metode | Hastighet | Sikkerhet | Anbefaling |
|--------|-----------|-----------|------------|
| MD5/SHA256 | Rask (~1ms) | ❌ Dårlig | Ikke bruk |
| PBKDF2 | Medium (~50ms) | ✅ God | OK alternativ |
| **BCrypt** | Medium (~100ms) | ✅✅ Beste | ✅ **Anbefalt** |
| Argon2 | Langsom (~200ms) | ✅✅ Beste | Beste, men saktere |

## Best practices

1. ✅ **Ikke lagre passord i plaintext** - Alltid hash
2. ✅ **Bruk BCrypt eller Argon2** - Ikke MD5/SHA256
3. ✅ **Bruk Pepper** - Ekstra sikkerhetslag (ikke i database)
4. ✅ **Work factor 10-12** - God balanse
5. ✅ **Ikke logg passord** - Heller ikke hashet
6. ✅ **Valider passordstyrke** - Før hashing
7. ✅ **Lagre pepper separat** - I appsettings/environment variables, ikke i database

## Feilsøking

### Hashing tar for lang tid?
- Sjekk work factor (standard 10 er fint)
- Sjekk server-ytelse

### Verifisering feiler?
- Sjekk at hash er lagret korrekt i database
- Sjekk at samme BCrypt-versjon brukes

### Hash ser rar ut?
- BCrypt-hash starter alltid med `$2a$`, `$2b$`, eller `$2y$`
- Hash er ~60 tegn lang

