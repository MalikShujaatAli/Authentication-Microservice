namespace AuthServiceAPI.Services;

using AuthServiceAPI.DTOs;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshRequest request);
    Task<AuthResponse> LogoutAsync(string refreshToken);
}
