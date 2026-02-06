# Authentication Microservice - Quick Start Guide

## Project Structure

```
AuthServiceAPI/
??? Models/
?   ??? User.cs
?   ??? RefreshToken.cs
??? DTOs/
?   ??? RegisterRequest.cs
?   ??? LoginRequest.cs
?   ??? AuthResponse.cs
?   ??? RefreshRequest.cs
??? Data/
?   ??? AuthDbContext.cs
??? Services/
?   ??? IAuthService.cs
?   ??? AuthService.cs
?   ??? IJwtService.cs
?   ??? JwtService.cs
??? Controllers/
?   ??? AuthController.cs
??? appsettings.json
??? Program.cs
??? Authentication Microservice.csproj
```

## Installation & Setup

### Step 1: Install Dependencies
All required NuGet packages are already included:
- `BCrypt.Net-Next` - Password hashing
- `Microsoft.AspNetCore.Authentication.JwtBearer` - JWT authentication
- `Microsoft.EntityFrameworkCore.SqlServer` - SQL Server database access
- `Microsoft.EntityFrameworkCore.Tools` - Database migrations
- `Swashbuckle.AspNetCore` - Swagger/OpenAPI documentation

### Step 2: Configure Database Connection
Edit `appsettings.json` and update the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER;Database=AuthServiceDB;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;"
}
```

**Examples:**
- **Local SQL Server:** `Server=.;Database=AuthServiceDB;Trusted_Connection=true;Encrypt=false;`
- **Named instance:** `Server=.\SQLEXPRESS;Database=AuthServiceDB;Trusted_Connection=true;Encrypt=false;`
- **Remote server:** `Server=192.168.1.100;Database=AuthServiceDB;User Id=sa;Password=YourPassword;Encrypt=false;`

### Step 3: Create Database & Tables
Open **Package Manager Console** and run:

```powershell
Add-Migration InitialCreate
Update-Database
```

This will:
- Create the `AuthServiceDB` database
- Create `Users` table
- Create `RefreshTokens` table

### Step 4: Change JWT Secret (Production)
In `appsettings.json`, update the Jwt Key to a strong random string:

```json
"Jwt": {
  "Key": "USE_A_STRONG_RANDOM_32_CHARACTER_SECRET_KEY_HERE",
  "Issuer": "AuthServiceAPI",
  "Audience": "AuthServiceUsers",
  "ExpiresInMinutes": 60
}
```

### Step 5: Run the Application
```powershell
dotnet run
```

Application will start at: `https://localhost:7000`
Swagger UI: `https://localhost:7000/swagger`

---

## Database Setup (if running migrations fails)

### Manual Database Creation
```sql
CREATE DATABASE AuthServiceDB;
```

Then run migrations in Package Manager Console:
```powershell
Update-Database
```

### Connection String Troubleshooting

**For Windows Authentication (Recommended):**
```
Server=.;Database=AuthServiceDB;Trusted_Connection=true;Encrypt=false;TrustServerCertificate=true;
```

**For SQL Authentication:**
```
Server=.;Database=AuthServiceDB;User Id=sa;Password=YOUR_PASSWORD;Encrypt=false;TrustServerCertificate=true;
```

---

## API Testing

### Use Swagger UI
1. Navigate to `https://localhost:7000/swagger`
2. Click on each endpoint to test
3. For protected endpoints, click "Authorize" and paste JWT token

### Or Use Postman/cURL

**Register:**
```bash
curl -X POST https://localhost:7000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "John Doe",
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

**Login:**
```bash
curl -X POST https://localhost:7000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePassword123!"
  }'
```

**Refresh Token:**
```bash
curl -X POST https://localhost:7000/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{
    "refreshToken": "YOUR_REFRESH_TOKEN_HERE"
  }'
```

**Protected Endpoint (Profile):**
```bash
curl -X GET https://localhost:7000/api/auth/profile \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN_HERE"
```

---

## Creating Admin Users

To create an admin user, you need to:

1. Register a user normally via `/api/auth/register`
2. Open SQL Server Management Studio
3. Connect to `AuthServiceDB`
4. Run this query:

```sql
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@example.com'
```

Then login with that user - you'll have access to `/api/auth/admin-only` endpoint.

---

## Understanding the Code

See **CONTROL_FLOW.md** for detailed control flow documentation.

Key files:
- **Program.cs** - Dependency injection & middleware configuration
- **AuthService.cs** - Core business logic (register, login, refresh, logout)
- **JwtService.cs** - Token generation
- **AuthDbContext.cs** - Database schema definition
- **AuthController.cs** - API endpoints

---

## Common Issues

### Issue: "Cannot open database"
**Solution:** Check SQL Server is running, verify connection string, check server name

### Issue: "The type initializer for 'System.Security.Cryptography.AesGcm' threw an exception"
**Solution:** This is a known issue on some Windows versions. Update .NET 8 or try:
```powershell
dotnet new -i Microsoft.AspNetCore.App.Runtime.win-x64
```

### Issue: Migration fails
**Solution:** 
1. Delete the `Migrations` folder
2. Run: `Add-Migration InitialCreate`
3. Run: `Update-Database`

### Issue: 401 Unauthorized on protected endpoints
**Solution:** 
- Make sure you're sending the JWT token in Authorization header
- Format: `Authorization: Bearer <token>`
- Token may have expired - use refresh endpoint to get new one

---

## Project Features

? User Registration with password hashing  
? User Login with JWT tokens  
? Refresh tokens stored in database  
? Token refresh endpoint  
? Logout with token revocation  
? Role-based authorization (User/Admin)  
? Protected endpoints with [Authorize] attribute  
? Swagger documentation with JWT support  
? Clean architecture with services & DTOs  
? Entity Framework Core with SQL Server  
? Async/await throughout  

---

## Files Overview

| File | Purpose |
|------|---------|
| Models/* | Define User and RefreshToken entities |
| DTOs/* | Request/Response data transfer objects |
| Services/* | Business logic & JWT generation |
| Controllers/* | API endpoint handlers |
| Data/AuthDbContext.cs | Database configuration & schema |
| Program.cs | DI configuration & middleware setup |
| appsettings.json | Application configuration |

---

## Next Steps

1. ? Database is created and migrations are ready
2. ? Build the project: `dotnet build`
3. ? Run the project: `dotnet run`
4. ? Test endpoints in Swagger UI
5. ? Deploy to production (update JWT key, connection string, etc.)

---

## For More Details

Read **CONTROL_FLOW.md** for:
- Detailed endpoint flow diagrams
- Database schema explanation
- Security features overview
- Token lifecycle explanation
- Dependency injection flow

