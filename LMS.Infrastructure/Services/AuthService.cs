using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LMS.Application.DTOs.Auth;
using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Data;
using LMS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly LmsDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly ILogger<AuthService> _logger;
    private static readonly Dictionary<string, int> _failedLoginAttempts = new(); // In-memory tracking (use Redis in production)

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        LmsDbContext context,
        IConfiguration configuration,
        IEmailNotificationService emailNotificationService,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
        _emailNotificationService = emailNotificationService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        // Parse role from string to enum
        UserRole userRole = UserRole.Student; // Default
        if (!string.IsNullOrEmpty(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
        {
            userRole = parsedRole;
        }

        // Create new user
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Role = userRole,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        // Ensure role exists (using enum name as string)
        var roleName = user.Role.ToString();
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new ApplicationRole(roleName));
        }

        // Assign role to user
        await _userManager.AddToRoleAsync(user, roleName);

        // Generate email verification token
        var emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        // Send email verification email
        try
        {
            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var verificationUrl = $"{baseUrl}/api/auth/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailVerificationToken)}";

            await _emailNotificationService.SendEmailVerificationAsync(
                user.Id,
                user.Email!,
                user.FullName,
                verificationUrl,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email verification for {Email}", user.Email);
        }

        // Generate tokens
        var token = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        // Send welcome email for new users (after verification email)
        if (user.Role == UserRole.Student)
        {
            try
            {
                var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
                var loginUrl = $"{baseUrl}/login";

                await _emailNotificationService.SendWelcomeEmailAsync(
                    user.Id,
                    user.Email!,
                    user.FullName,
                    loginUrl,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing welcome email for {Email}", user.Email);
            }
        }

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Email = user.Email!,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            EmailConfirmed = user.EmailConfirmed,
            EmailVerificationWarning = !user.EmailConfirmed 
                ? "Lütfen email adresinizi doğrulayın. Email adresinize doğrulama linki gönderildi." 
                : null
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        var emailKey = request.Email.ToLowerInvariant();
        if (!isValidPassword)
        {
            // Track failed login attempts
            if (!_failedLoginAttempts.ContainsKey(emailKey))
            {
                _failedLoginAttempts[emailKey] = 0;
            }
            _failedLoginAttempts[emailKey]++;

            // Send security alert email after 3 failed attempts
            if (_failedLoginAttempts[emailKey] >= 3)
            {
                try
                {
                    var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
                    var resetPasswordUrl = $"{baseUrl}/reset-password";

                    await _emailNotificationService.SendSecurityAlertEmailAsync(
                        user.Id,
                        user.Email!,
                        user.FullName,
                        _failedLoginAttempts[emailKey],
                        resetPasswordUrl,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing security alert for {Email}", user.Email);
                }
            }

            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        // Reset failed login attempts on successful login
        if (_failedLoginAttempts.ContainsKey(emailKey))
        {
            _failedLoginAttempts.Remove(emailKey);
        }

        // Get user role from entity
        var role = user.Role.ToString();

        // Generate tokens
        var token = GenerateJwtToken(user);
        var refreshToken = await GenerateRefreshTokenAsync(user.Id);

        // Check if email is confirmed and provide warning
        var emailVerificationWarning = !user.EmailConfirmed
            ? "Email adresiniz henüz doğrulanmamış. Lütfen email adresinizi doğrulayın. Doğrulama linki için 'Email Doğrulama' butonunu kullanabilirsiniz."
            : null;

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Email = user.Email!,
            FullName = user.FullName,
            Role = role,
            EmailConfirmed = user.EmailConfirmed,
            EmailVerificationWarning = emailVerificationWarning
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(token.UserId.ToString());
        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found.");
        }

        // Revoke old token
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
        token.ReasonRevoked = "Replaced by new token";

        // Generate new tokens
        var newToken = GenerateJwtToken(user);
        var newRefreshToken = await GenerateRefreshTokenAsync(user.Id);

        await _context.SaveChangesAsync();

        // Get user role from entity
        var role = user.Role.ToString();

        return new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            Email = user.Email!,
            FullName = user.FullName,
            Role = role
        };
    }

    public async Task<bool> VerifyEmailAsync(int userId, string token)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return true; // Already confirmed
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Email verification failed for user {UserId}. Errors: {Errors}", 
                userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }

        _logger.LogInformation("Email verified successfully for user {UserId}", userId);
        return true;
    }

    public async Task<bool> ResendVerificationEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (user.EmailConfirmed)
        {
            return true; // Already confirmed, no need to resend
        }

        try
        {
            // Generate email verification token
            var emailVerificationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            
            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var verificationUrl = $"{baseUrl}/api/auth/verify-email?userId={user.Id}&token={Uri.EscapeDataString(emailVerificationToken)}";

            await _emailNotificationService.SendEmailVerificationAsync(
                user.Id,
                user.Email!,
                user.FullName,
                verificationUrl,
                CancellationToken.None);

            _logger.LogInformation("Email verification email resent to {Email}", email);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending email verification for {Email}", email);
            return false;
        }
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var token = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (token != null && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.ReasonRevoked = "Revoked by user";
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("Role", user.Role.ToString()), // Add UserRole enum as claim
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // Add role to claims (for [Authorize(Roles="...")] to work)
        claims.Add(new Claim(ClaimTypes.Role, user.Role.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(GetJwtExpirationMinutes()),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId)
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var token = Convert.ToBase64String(randomNumber);

        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // Refresh token expires in 7 days
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken;
    }

    private int GetJwtExpirationMinutes()
    {
        var expirationMinutes = _configuration["Jwt:ExpirationMinutes"];
        return int.TryParse(expirationMinutes, out var minutes) ? minutes : 60;
    }
}

