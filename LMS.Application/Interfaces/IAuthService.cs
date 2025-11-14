using LMS.Application.DTOs.Auth;

namespace LMS.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task<bool> VerifyEmailAsync(int userId, string token);
    Task<bool> ResendVerificationEmailAsync(string email);
}

