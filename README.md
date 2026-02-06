# Authentication Microservice API

A production-ready ASP.NET Core 8 Authentication Microservice with JWT tokens, refresh tokens, role-based authorization, and BCrypt password hashing.

## ? Features

- **User Registration** - Create new user accounts with email and password
- **User Login** - Authenticate users and issue JWT access tokens
- **JWT Tokens** - Short-lived access tokens (60 minutes) with role claims
- **Refresh Tokens** - Long-lived tokens (7 days) stored in database for token renewal
- **Token Refresh** - Issue new access tokens without re-authentication
- **Logout** - Revoke refresh tokens to prevent further use
- **Role-Based Authorization** - Support for User and Admin roles
- **Password Hashing** - Secure BCrypt algorithm (never store plain passwords)
- **Protected Endpoints** - API endpoints with [Authorize] attribute
- **Swagger Documentation** - Built-in OpenAPI documentation with JWT support
- **Clean Architecture** - Organized layers (Controllers, Services, Data)
- **Entity Framework Core** - ORM with SQL Server database
- **Async/Await** - Asynchronous operations throughout

## ??? Architecture

```
HTTP Request
    ?
AuthController (Entry point, validation)
    ?
IAuthService (Business logic)
    ?? AuthService (Implementation)
    ?? IJwtService (Token generation)
         ?? JwtService (JWT & Refresh token creation)
    ?
AuthDbContext (Data access)
    ?? Users Table
    ?? RefreshTokens Table
    ?
SQL Server Database
```

## ?? Project Structure

```
Authentication Microservice/
?
?? Models/
?  ?? User.cs                 # User entity
?  ?? RefreshToken.cs         # Refresh token entity
?
?? DTOs/
?  ?? RegisterRequest.cs      # Register endpoint request
?  ?? LoginRequest.cs         # Login endpoint request
?  ?? AuthResponse.cs         # Common response format
?  ?? RefreshRequest.cs       # Token refresh request
?
?? Services/
?  ?? IAuthService.cs         # Authentication interface
?  ?? AuthService.cs          # Register, Login, Refresh, Logout logic
?  ?? IJwtService.cs          # JWT generation interface
?  ?? JwtService.cs           # Token generation implementation
?
?? Controllers/
?  ?? AuthController.cs       # /api/auth/* endpoints
?
?? Data/
?  ?? AuthDbContext.cs        # EF Core DbContext configuration
?
?? Program.cs                 # Dependency injection & middleware
?? appsettings.json           # Configuration
?
?? CONTROL_FLOW.md            # Detailed control flow documentation
?? SETUP_GUIDE.md             # Installation and setup guide
?? README.md                  # This file
```

## ?? Quick Start

### Prerequisites
- .NET 8 SDK
- SQL Server 2019+
- Visual Studio 2022 or VS Code

### 1. Configure Database
Edit `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=AuthServiceDB;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;"
}
```

### 2. Create Database
Open Package Manager Console:
```powershell
Add-Migration InitialCreate
Update-Database
```

### 3. Run Application
```bash
dotnet run
```

Access Swagger: `https://localhost:7000/swagger`

## ?? API Endpoints

### Public Endpoints

**POST /api/auth/register**
- Create new user account
- Request: `{ name, email, password }`
- Response: User details + success message

**POST /api/auth/login**
- Authenticate user
- Request: `{ email, password }`
- Response: AccessToken, RefreshToken, User details

**POST /api/auth/refresh**
- Get new access token using refresh token
- Request: `{ refreshToken }`
- Response: New AccessToken

### Protected Endpoints (Requires JWT)

**POST /api/auth/logout**
- Revoke refresh token
- Request: `{ refreshToken }`
- Headers: `Authorization: Bearer {accessToken}`
- Response: Success message

**GET /api/auth/profile**
- Get current user profile
- Headers: `Authorization: Bearer {accessToken}`
- Response: User ID, Email, Role

**GET /api/auth/admin-only**
- Admin-only endpoint (requires Role = "Admin")
- Headers: `Authorization: Bearer {accessToken}`
- Response: Success message

## ?? Security

### Password Security
- Passwords hashed with BCrypt algorithm
- Salts automatically generated per password
- Plain passwords never stored in database

### JWT Tokens
- Signed with HMAC-SHA256
- Contains: User ID, Email, Role
- Includes expiration time
- Validated on every protected request

### Token Expiration
- **AccessToken**: 60 minutes (short-lived for API calls)
- **RefreshToken**: 7 days (long-lived for token renewal)
- Can be revoked via logout endpoint

### Database Storage
- RefreshTokens stored in database
- Each token linked to user
- Can be revoked (IsRevoked flag)
- Automatic cascade delete when user is deleted

## ?? Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY,
    Name VARCHAR(255) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(MAX) NOT NULL,
    Role VARCHAR(50) NOT NULL DEFAULT 'User',
    CreatedAt DATETIME NOT NULL
);
```

### RefreshTokens Table
```sql
CREATE TABLE RefreshTokens (
    Id INT PRIMARY KEY IDENTITY,
    Token VARCHAR(MAX) UNIQUE NOT NULL,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    ExpiresAt DATETIME NOT NULL,
    IsRevoked BIT NOT NULL DEFAULT 0
);
```

## ?? Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "YOUR_CONNECTION_STRING"
  },
  "Jwt": {
    "Key": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "AuthServiceAPI",
    "Audience": "AuthServiceUsers",
    "ExpiresInMinutes": 60
  }
}
```

**Important Production Settings:**
- Use strong random Key (min 32 characters)
- Use HTTPS only
- Store secrets in Azure Key Vault or similar
- Implement rate limiting
- Enable CORS carefully

## ?? Dependency Injection

All services automatically injected via constructor:

```csharp
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
}
```

**Registered Services:**
- `AuthDbContext` - Database context
- `IAuthService` - Authentication service
- `IJwtService` - JWT token service
- Authentication middleware
- Authorization middleware

## ?? Testing Endpoints

### Register User
```bash
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"name":"John","email":"john@test.com","password":"Pass123!"}'
```

### Login
```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john@test.com","password":"Pass123!"}'
```

Response:
```json
{
  "success": true,
  "message": "Login successful",
  "accessToken": "eyJhbGc...",
  "refreshToken": "AbCdEf...",
  "user": {
    "id": 1,
    "name": "John",
    "email": "john@test.com",
    "role": "User"
  }
}
```

### Access Protected Endpoint
```bash
curl -X GET https://localhost:7000/api/auth/profile \
  -H "Authorization: Bearer eyJhbGc..."
```

## ??? Development

### Project Built With
- **.NET 8** - Latest .NET framework
- **ASP.NET Core** - Web API framework
- **Entity Framework Core** - ORM
- **SQL Server** - Database
- **JWT Bearer** - Token authentication
- **BCrypt.Net-Next** - Password hashing
- **Swagger/OpenAPI** - API documentation

### Code Style
- Clean architecture principles
- Dependency injection pattern
- Async/await throughout
- DTOs for API contracts
- Service layer abstraction
- Entity Framework best practices

## ?? Documentation

See detailed documentation in:
- **CONTROL_FLOW.md** - Request/response flow through system
- **SETUP_GUIDE.md** - Installation and configuration

## ?? Error Handling

All endpoints return consistent response format:

**Success:**
```json
{
  "success": true,
  "message": "Operation successful",
  "accessToken": "...",
  "user": {...}
}
```

**Error:**
```json
{
  "success": false,
  "message": "Error description"
}
```

## ?? Token Refresh Flow

```
1. User logs in ? Receive AccessToken (60 min) + RefreshToken (7 days)
2. Use AccessToken for API calls
3. After 60 minutes, AccessToken expires
4. Send RefreshToken to /api/auth/refresh
5. Receive new AccessToken (valid for 60 min)
6. Continue making API calls with new token
7. Repeat steps 3-6 as needed
8. Call /api/auth/logout to revoke RefreshToken
```

## ?? Admin Setup

Create admin user:

1. Register user normally
2. Open SQL Server Management Studio
3. Run SQL query:
```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@example.com'
```
4. User now has access to `/api/auth/admin-only`

## ?? Troubleshooting

**Issue:** Database connection fails
- Verify SQL Server is running
- Check connection string
- Verify server name is correct
- Check authentication (Windows/SQL)

**Issue:** 401 Unauthorized errors
- Verify JWT token is valid
- Check token expiration
- Ensure Authorization header format: `Bearer {token}`
- Get new token using refresh endpoint

**Issue:** 403 Forbidden on admin endpoint
- User must have Role = "Admin"
- Update user role in database

**Issue:** Build errors with BCrypt
- Clean and rebuild project
- Ensure BCrypt.Net-Next package is installed
- Check using statement: `using BC = BCrypt.Net.BCrypt;`

## ?? Best Practices

1. ? Never store plain passwords
2. ? Always use HTTPS in production
3. ? Implement rate limiting
4. ? Use strong JWT secret (min 32 chars)
5. ? Store secrets in secure vault (Azure Key Vault)
6. ? Validate all input
7. ? Use async/await
8. ? Handle exceptions gracefully
9. ? Keep RefreshTokens in database
10. ? Rotate JWT secrets periodically

## ?? License

This project is provided as-is for educational and production use.

## ?? Support

For issues or questions:
1. Check CONTROL_FLOW.md for detailed flow documentation
2. Review SETUP_GUIDE.md for configuration help
3. Examine code comments for implementation details
4. Ensure database is properly created and migrated

---

**Ready to use!** Follow SETUP_GUIDE.md to get started.
