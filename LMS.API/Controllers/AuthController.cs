using LMS.Application.DTOs.Auth;
using LMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IStringLocalizer<AuthController> _localizer;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, IStringLocalizer<AuthController> localizer)
    {
        _authService = authService;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var response = await _authService.RegisterAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["RegistrationError"]);
            return StatusCode(500, new { message = _localizer["RegistrationError"] });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var response = await _authService.LoginAsync(request);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["LoginError"]);
            return StatusCode(500, new { message = _localizer["LoginError"] });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var response = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Refresh token failed");
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["TokenRefreshError"]);
            return StatusCode(500, new { message = _localizer["TokenRefreshError"] });
        }
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<ActionResult> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken);
            return Ok(new { message = _localizer["TokenRevokedSuccessfully"] });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["TokenRevocationError"]);
            return StatusCode(500, new { message = _localizer["TokenRevocationError"] });
        }
    }

    /// <summary>
    /// Verify email address with verification token
    /// </summary>
    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<ActionResult> VerifyEmail([FromQuery] int userId, [FromQuery] string token)
    {
        try
        {
            var result = await _authService.VerifyEmailAsync(userId, token);
            if (result)
            {
                return Ok(new { message = "Email adresiniz başarıyla doğrulandı.", success = true });
            }
            else
            {
                return BadRequest(new { message = "Email doğrulama başarısız. Lütfen doğrulama linkini kontrol edin veya yeni bir link talep edin.", success = false });
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Email verification failed for user {UserId}", userId);
            return BadRequest(new { message = ex.Message, success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying email for user {UserId}", userId);
            return StatusCode(500, new { message = "Email doğrulama sırasında bir hata oluştu.", success = false });
        }
    }

    /// <summary>
    /// Resend email verification email
    /// </summary>
    [HttpPost("resend-verification-email")]
    [AllowAnonymous]
    public async Task<ActionResult> ResendVerificationEmail([FromBody] ResendVerificationEmailRequest request)
    {
        try
        {
            var result = await _authService.ResendVerificationEmailAsync(request.Email);
            if (result)
            {
                return Ok(new { message = "Doğrulama email'i başarıyla gönderildi. Lütfen email kutunuzu kontrol edin.", success = true });
            }
            else
            {
                return BadRequest(new { message = "Email gönderme başarısız. Lütfen daha sonra tekrar deneyin.", success = false });
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Resend verification email failed for {Email}", request.Email);
            return BadRequest(new { message = ex.Message, success = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending verification email for {Email}", request.Email);
            return StatusCode(500, new { message = "Email gönderme sırasında bir hata oluştu.", success = false });
        }
    }
}

