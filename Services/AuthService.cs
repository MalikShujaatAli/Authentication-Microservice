namespace AuthServiceAPI.Services;

using AuthServiceAPI.Data;
using AuthServiceAPI.DTOs;
using AuthServiceAPI.Models;
using BC = BCrypt.Net.BCrypt;
using Microsoft.EntityFrameworkCore;

public class AuthService : IAuthService
{
    private readonly AuthDbContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(AuthDbContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
        {
            return new AuthResponse { Success = false, Message = "All fields are required" };
        }

        // Check if user already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return new AuthResponse { Success = false, Message = "User already exists" };
        }

        // Hash password
        var passwordHash = BC.HashPassword(request.Password);

        // Create new user
        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "User registered successfully",
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResponse { Success = false, Message = "Email and password are required" };
        }

        // Find user by email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password" };
        }

        // Verify password
        if (!BC.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid email or password" };
        }

        // Generate tokens
        var accessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Save refresh token to database
        var refreshTokenEntity = new RefreshToken
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Login successful",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role
            }
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshRequest request)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new AuthResponse { Success = false, Message = "Refresh token is required" };
        }

        // Find refresh token in database
        var refreshTokenEntity = await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (refreshTokenEntity == null || refreshTokenEntity.IsRevoked || refreshTokenEntity.ExpiresAt < DateTime.UtcNow)
        {
            return new AuthResponse { Success = false, Message = "Invalid or expired refresh token" };
        }

        var user = refreshTokenEntity.User!;

        // Generate new access token
        var newAccessToken = _jwtService.GenerateAccessToken(user.Id, user.Email, user.Role);

        return new AuthResponse
        {
            Success = true,
            Message = "Token refreshed successfully",
            AccessToken = newAccessToken,
            RefreshToken = request.RefreshToken
        };
    }

    public async Task<AuthResponse> LogoutAsync(string refreshToken)
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new AuthResponse { Success = false, Message = "Refresh token is required" };
        }

        // Find and revoke refresh token
        var refreshTokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (refreshTokenEntity == null)
        {
            return new AuthResponse { Success = false, Message = "Refresh token not found" };
        }

        refreshTokenEntity.IsRevoked = true;
        await _context.SaveChangesAsync();

        return new AuthResponse
        {
            Success = true,
            Message = "Logout successful"
        };
    }
}
