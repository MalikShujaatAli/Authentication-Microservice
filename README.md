# Authentication Microservice

A small **ASP.NET Core 8** service handling registration, login, token refresh and logout, with
role-based authorisation. Six endpoints, cleanly layered.

> **Scope, honestly:** this is a focused learning project — roughly 500 lines — built to get every
> authentication decision right rather than to be large. If you are evaluating it, the useful question is
> not "how big is it" but "can he explain each choice", and the section below is there so you can check.

---

## Stack

| | |
|---|---|
| Framework | ASP.NET Core 8 Web API |
| Data | Entity Framework Core 8, SQL Server |
| Auth | JWT bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Hashing | BCrypt.Net-Next |
| Docs | Swashbuckle / Swagger with a Bearer security definition |

## Endpoints

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/api/Auth/register` | — | Create an account; password hashed with BCrypt |
| `POST` | `/api/Auth/login` | — | Return an access token + refresh token |
| `POST` | `/api/Auth/refresh` | — | Exchange a valid refresh token for a new access token |
| `POST` | `/api/Auth/logout` | Bearer | Revoke the refresh token |
| `GET` | `/api/Auth/profile` | Bearer | Read claims from the validated token |
| `GET` | `/api/Auth/admin-only` | Bearer, `Admin` | Demonstrates role-gated authorisation |

---

## The decisions worth defending

**Two tokens, not one.** A short-lived access token (60 minutes, configurable) carries the claims; a
long-lived refresh token (7 days) lives in the database as a row with `ExpiresAt` and `IsRevoked`. Logout
flips `IsRevoked`. The point is that a stateless JWT cannot be revoked — so the thing that *can* be
revoked is kept in a place that has state.

**`ClockSkew = TimeSpan.Zero`.** The .NET default is five minutes of grace, which means a token you
believe expired keeps working for another five minutes. Most tutorials never touch this. Here it is set to
zero, so expiry means expiry.

**Issuer *and* audience are both validated.** Validating the signature alone proves the token was signed
by a key you trust — not that it was minted for this application. Both are checked.

**Refresh tokens come from a CSPRNG, not a GUID.** 64 bytes from `RandomNumberGenerator`, base64-encoded.
A `Guid.NewGuid()` is not designed to be unguessable and should not be used as a credential.

**BCrypt, with its own salt.** Not SHA-256, not MD5, and no hand-rolled salting. BCrypt is deliberately
slow and stores the salt and work factor in the hash string.

**Secrets are not in source.** The JWT key, issuer and audience come from configuration.
`appsettings.json` here holds placeholders.

---

## Layout

```
Controllers/AuthController.cs     six endpoints, no logic
Services/IAuthService.cs          registration, login, refresh, logout
Services/IJwtService.cs           token generation only
Data/AuthDbContext.cs             EF Core context
Models/                           User, RefreshToken
DTOs/                             request and response shapes
Migrations/                       InitialCreate
```

Controllers → Services → DbContext, everything behind an interface and wired through dependency
injection. The controller decides HTTP status codes and nothing else.

---

## Running it

Needs the .NET 8 SDK and SQL Server (LocalDB is fine).

```bash
# point at your database
#   appsettings.json → ConnectionStrings:DefaultConnection
# set a JWT key of at least 32 characters
#   appsettings.json → Jwt:Key, Jwt:Issuer, Jwt:Audience

dotnet restore
dotnet run
```

Migrations are applied at startup (`db.Database.Migrate()`), so the database is created on first run.
Swagger UI is at `/swagger` in development — use the **Authorize** button to paste a token and try the
protected routes.

---

## Known limitations

These are deliberate omissions for a project of this size, not oversights:

- **Refresh tokens are not rotated.** Refreshing returns a new access token but reuses the same refresh
  token. Rotation with reuse-detection would be the next step.
- **No rate limiting** on login, so nothing slows down a brute-force attempt.
- **No email verification or password reset.**
- **Refresh tokens are stored in plaintext.** Hashing them, as passwords are, would mean a leaked
  database table is not a set of live credentials.
- **No tests.** For a service about security this is the gap I would close first.

## Built by

Malik Shujaat Ali — [github.com/MalikShujaatAli](https://github.com/MalikShujaatAli) ·
[linkedin.com/in/malik-shujaat-ali](https://www.linkedin.com/in/malik-shujaat-ali)
