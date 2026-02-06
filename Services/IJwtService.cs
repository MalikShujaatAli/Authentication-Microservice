namespace AuthServiceAPI.Services;

using AuthServiceAPI.DTOs;

public interface IJwtService
{
    string GenerateAccessToken(int userId, string email, string role);
    string GenerateRefreshToken();
}
