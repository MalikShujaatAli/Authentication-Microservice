# Authentication Microservice - Control Flow Documentation

## Project Overview

This is a production-ready ASP.NET Core 8 Authentication Microservice that handles user registration, login, token management, and role-based access control.

---

## Architecture Overview

```
Request ? Controller ? Service ? Database
   ?         ?           ?         ?
 HTTP      Validate    Business   EF Core
 Layer     & Route     Logic      & SQL Server
```

---

## Data Flow for Each Endpoint

### 1. REGISTER ENDPOINT
**Path:** `POST /api/auth/register`

**Step-by-Step Flow:**

```
User sends RegisterRequest (Name, Email, Password)
    ?
AuthController.Register() receives request
    ?
AuthService.RegisterAsync() is called
    ?
Check if email already exists in database
    ?? If exists ? Return error "User already exists"
    ?? If not exists ? Continue
    ?
Hash password using BCrypt.HashPassword()
    ?
Create new User object with:
    - Name
    - Email
    - PasswordHash (hashed password, NOT plain text)
    - Role = "User" (default)
    - CreatedAt = current time
    ?
Save User to database via AuthDbContext
    ?
Return success response with User details
```

**Database Tables Involved:**
- `Users` table ? INSERT new user

---

### 2. LOGIN ENDPOINT
**Path:** `POST /api/auth/login`

**Step-by-Step Flow:**

```
User sends LoginRequest (Email, Password)
    ?
AuthController.Login() receives request
    ?
AuthService.LoginAsync() is called
    ?
Query database to find User by Email
    ?? If not found ? Return error "Invalid email or password"
    ?? If found ? Continue
    ?
Verify password using BCrypt.Verify()
    (Compare plain text password with stored hash)
    ?? If password doesn't match ? Return error "Invalid email or password"
    ?? If password matches ? Continue
    ?
Generate AccessToken using JwtService.GenerateAccessToken()
    - Contains: UserId, Email, Role
    - Expires in: 60 minutes
    ?
Generate RefreshToken using JwtService.GenerateRefreshToken()
    - Random 64-byte string encoded in Base64
    ?
Save RefreshToken to database with:
    - Token value
    - UserId (foreign key)
    - ExpiresAt = 7 days from now
    - IsRevoked = false
    ?
Return response with:
    - AccessToken (JWT)
    - RefreshToken (Database token)
    - User details
```

**Database Tables Involved:**
- `Users` table ? SELECT by email
- `RefreshTokens` table ? INSERT new refresh token

**Token Details:**

AccessToken (JWT):
```json
{
  "nameid": "1",           // User ID
  "email": "user@email.com",
  "role": "User",
  "exp": 1234567890,       // Expires after 60 minutes
  "iss": "AuthServiceAPI",
  "aud": "AuthServiceUsers"
}
```

---

### 3. REFRESH TOKEN ENDPOINT
**Path:** `POST /api/auth/refresh`

**Step-by-Step Flow:**

```
Client sends RefreshRequest (old RefreshToken)
    ?
AuthController.RefreshToken() receives request
    ?
AuthService.RefreshTokenAsync() is called
    ?
Query database to find RefreshToken by token value
    ?? If not found ? Return error "Invalid or expired refresh token"
    ?? If found ? Continue
    ?
Check if RefreshToken is revoked (IsRevoked = true)
    ?? If revoked ? Return error "Invalid or expired refresh token"
    ?? If not revoked ? Continue
    ?
Check if RefreshToken has expired (ExpiresAt < current time)
    ?? If expired ? Return error "Invalid or expired refresh token"
    ?? If valid ? Continue
    ?
Get associated User from database
    ?
Generate new AccessToken using JwtService.GenerateAccessToken()
    - New JWT with updated expiry time (60 minutes from now)
    ?
Return response with:
    - New AccessToken (JWT)
    - Same RefreshToken (can be reused until it expires)
```

**Database Tables Involved:**
- `RefreshTokens` table ? SELECT by token
- `Users` table ? SELECT to get user details

**Key Point:** The RefreshToken itself is NOT changed. You can use the same RefreshToken multiple times until it expires or is revoked.

---

### 4. LOGOUT ENDPOINT
**Path:** `POST /api/auth/logout`
**Requires:** Valid JWT in Authorization header

**Step-by-Step Flow:**

```
Client sends LogoutRequest (RefreshToken) with JWT in header
    ?
AuthController.Logout() receives request
    ?
[Authorize] middleware validates JWT
    ?? If JWT is invalid/expired ? Return 401 Unauthorized
    ?? If JWT is valid ? Continue
    ?
AuthService.LogoutAsync() is called
    ?
Query database to find RefreshToken by token value
    ?? If not found ? Return error "Refresh token not found"
    ?? If found ? Continue
    ?
Set IsRevoked = true for the RefreshToken
    ?
Save changes to database
    ?
Return success response "Logout successful"
```

**Database Tables Involved:**
- `RefreshTokens` table ? SELECT by token, then UPDATE IsRevoked

**Effect:** After logout, the RefreshToken cannot be used to generate new AccessTokens.

---

### 5. PROTECTED ENDPOINT - GET PROFILE
**Path:** `GET /api/auth/profile`
**Requires:** Valid JWT in Authorization header

**Step-by-Step Flow:**

```
Client sends request with JWT in Authorization header
    ?
[Authorize] middleware validates JWT
    ?? Validate JWT signature
    ?? Validate issuer and audience
    ?? Validate expiration time
    ?? If validation fails ? Return 401 Unauthorized
    ?? If validation succeeds ? Extract claims and continue
    ?
AuthController.GetProfile() is called
    ?
Extract claims from JWT:
    - UserId (from NameIdentifier claim)
    - Email (from Email claim)
    - Role (from Role claim)
    ?
Return user's profile information
```

**Database Tables Involved:** None (all data comes from JWT claims)

---

### 6. ADMIN-ONLY ENDPOINT
**Path:** `GET /api/admin-only`
**Requires:** Valid JWT with role "Admin"

**Step-by-Step Flow:**

```
Client sends request with JWT in Authorization header
    ?
[Authorize] middleware validates JWT (same as profile endpoint)
    ?
[Authorize(Roles = "Admin")] checks role claim
    ?? If role is NOT "Admin" ? Return 403 Forbidden
    ?? If role is "Admin" ? Continue
    ?
AuthController.AdminOnly() is called
    ?
Return admin-only information
```

**Note:** To test this endpoint, you need a user with Role = "Admin". You must manually update the user's role in the database to "Admin".

---

## Service Layer Responsibilities

### AuthService (IAuthService)
- **RegisterAsync()**: Register new users, hash passwords, save to DB
- **LoginAsync()**: Authenticate users, generate tokens
- **RefreshTokenAsync()**: Issue new access tokens using refresh tokens
- **LogoutAsync()**: Revoke refresh tokens

### JwtService (IJwtService)
- **GenerateAccessToken()**: Create JWT tokens with claims
- **GenerateRefreshToken()**: Generate random refresh tokens

---

## Database Schema

### Users Table
```
Id (int) - Primary Key
Name (string)
Email (string) - Unique
PasswordHash (string) - BCrypt hash
Role (string) - "User" or "Admin"
CreatedAt (DateTime)
```

### RefreshTokens Table
```
Id (int) - Primary Key
Token (string) - Unique, Base64 encoded random value
UserId (int) - Foreign Key to Users
ExpiresAt (DateTime) - Token expiration time
IsRevoked (bool) - True if logout was called
```

---

## Security Features

1. **Password Hashing**: BCrypt algorithm (never store plain passwords)
2. **JWT Tokens**: Signed with HMAC-SHA256
3. **Token Expiration**: 
   - AccessToken: 60 minutes (short-lived)
   - RefreshToken: 7 days (long-lived)
4. **Role-Based Access**: Endpoints can check user roles
5. **Token Revocation**: Refresh tokens can be revoked (logout)

---

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=AuthServiceDB;..."
  },
  "Jwt": {
    "Key": "SECRET_KEY_MIN_32_CHARS",
    "Issuer": "AuthServiceAPI",
    "Audience": "AuthServiceUsers",
    "ExpiresInMinutes": 60
  }
}
```

**Important:** Change the "Key" to a strong random string in production!

---

## Setting Up the Project

### 1. Prerequisites
- Visual Studio 2022 or higher
- SQL Server 2019 or higher
- .NET 8 SDK

### 2. Configure Database Connection
Update `appsettings.json`:
```json
"DefaultConnection": "Server=YOUR_SERVER;Database=AuthServiceDB;Trusted_Connection=true;..."
```

### 3. Create and Migrate Database
Run in Package Manager Console:
```powershell
Add-Migration InitialCreate
Update-Database
```

This creates:
- `AuthServiceDB` database
- `Users` table
- `RefreshTokens` table

### 4. Run the Application
```
dotnet run
```

Swagger UI will be available at: `https://localhost:7000/swagger`

---

## Testing the Endpoints

### 1. Register a User
**POST** `/api/auth/register`
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

### 2. Login
**POST** `/api/auth/login`
```json
{
  "email": "john@example.com",
  "password": "SecurePassword123!"
}
```

Response:
```json
{
  "success": true,
  "message": "Login successful",
  "accessToken": "eyJhbGc...",
  "refreshToken": "AbCdEfG...",
  "user": {
    "id": 1,
    "name": "John Doe",
    "email": "john@example.com",
    "role": "User"
  }
}
```

### 3. Access Protected Endpoint
**GET** `/api/auth/profile`
- Header: `Authorization: Bearer <accessToken>`

### 4. Refresh Token
**POST** `/api/auth/refresh`
```json
{
  "refreshToken": "AbCdEfG..."
}
```

### 5. Logout
**POST** `/api/auth/logout`
- Header: `Authorization: Bearer <accessToken>`
```json
{
  "refreshToken": "AbCdEfG..."
}
```

---

## Dependency Injection Flow

In `Program.cs`:

```
1. DbContext registered
   ?
2. Authentication service registered (JWT validation)
   ?
3. AuthService registered (business logic)
   ?
4. JwtService registered (token generation)
   ?
5. Controllers receive these services through constructor injection
```

**Example:**
```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    // IAuthService automatically injected by DI container
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
}
```

---

## Error Handling

All endpoints return structured responses:

**Success Response:**
```json
{
  "success": true,
  "message": "Operation successful",
  "accessToken": "...",
  "refreshToken": "...",
  "user": {...}
}
```

**Error Response:**
```json
{
  "success": false,
  "message": "Error description"
}
```

---

## Token Lifecycle Diagram

```
LOGIN
  ?
Generate AccessToken (expires in 60 min)
Generate RefreshToken (expires in 7 days) ? Save to DB
  ?
Client uses AccessToken for API calls
  ?
AccessToken expires after 60 minutes
  ?
REFRESH
  ?
Client sends RefreshToken
Server validates it in database
  ?
Generate new AccessToken (expires in 60 min)
Same RefreshToken is reusable
  ?
Repeat...
  ?
LOGOUT
  ?
Mark RefreshToken as IsRevoked = true
RefreshToken cannot be used anymore
```

---

## Key Points for Developers

1. **Always check database for RefreshTokens**: Never trust only the token format
2. **Store passwords as hashes**: Use BCrypt, never plain text
3. **AccessTokens are short-lived**: Users must use RefreshToken for new AccessTokens
4. **RefreshTokens are long-lived**: But can be revoked via logout
5. **Role-based access**: Check roles in database or JWT claims
6. **Error messages**: Be vague about login failures (don't reveal if email exists)

---

## Common Issues & Solutions

### Issue: "Invalid or expired refresh token"
**Cause:** 
- RefreshToken doesn't exist in database
- RefreshToken is revoked (logout called)
- RefreshToken has expired (7 days passed)

**Solution:** User must login again

### Issue: 401 Unauthorized on protected endpoint
**Cause:**
- Missing Authorization header
- AccessToken is expired
- AccessToken is invalid/tampered

**Solution:** Get new AccessToken using RefreshToken or login again

### Issue: 403 Forbidden on admin endpoint
**Cause:** User's role is not "Admin"

**Solution:** Database admin must update user role to "Admin"

