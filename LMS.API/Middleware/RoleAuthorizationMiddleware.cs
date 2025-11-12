using System.Security.Claims;
using LMS.Domain.Enums;

namespace LMS.API.Middleware;

/// <summary>
/// Custom middleware to check user's role dynamically
/// This middleware can be used for dynamic role-based authorization checks
/// </summary>
public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleAuthorizationMiddleware> _logger;

    public RoleAuthorizationMiddleware(RequestDelegate next, ILogger<RoleAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // Get user role from claims
            var roleClaim = context.User.FindFirst("Role")?.Value 
                ?? context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (!string.IsNullOrEmpty(roleClaim))
            {
                // Add role to context items for easy access in controllers
                context.Items["UserRole"] = roleClaim;

                // Parse enum if possible
                if (Enum.TryParse<UserRole>(roleClaim, out var userRole))
                {
                    context.Items["UserRoleEnum"] = userRole;
                }
            }
        }

        // Continue to next middleware
        await _next(context);
    }
}

/// <summary>
/// Extension method to register RoleAuthorizationMiddleware
/// </summary>
public static class RoleAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRoleAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoleAuthorizationMiddleware>();
    }
}

