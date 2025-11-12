using LMS.API.Models;
using System.Net;
using System.Text.Json;
using FluentValidation;

namespace LMS.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        ApiResponse<object> apiResponse;

        switch (exception)
        {
            case ValidationException validationException:
                // Handle FluentValidation errors
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                var validationErrors = validationException.Errors
                    .Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToList();
                apiResponse = ApiResponse<object>.ErrorResponse(
                    "Validation failed",
                    validationErrors
                );
                break;

            case UnauthorizedAccessException:
                // Handle unauthorized access
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                apiResponse = ApiResponse<object>.ErrorResponse(
                    "Unauthorized access",
                    exception.Message
                );
                break;

            case KeyNotFoundException:
            case FileNotFoundException:
                // Handle not found errors
                response.StatusCode = (int)HttpStatusCode.NotFound;
                apiResponse = ApiResponse<object>.ErrorResponse(
                    "Resource not found",
                    exception.Message
                );
                break;

            case InvalidOperationException:
                // Handle invalid operation errors (e.g., business logic violations)
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                apiResponse = ApiResponse<object>.ErrorResponse(
                    "Invalid operation",
                    exception.Message
                );
                break;

            default:
                // Handle unexpected errors
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                apiResponse = ApiResponse<object>.ErrorResponse(
                    "An error occurred while processing your request",
                    "Internal server error"
                );
                break;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(apiResponse, options);
        await response.WriteAsync(json);
    }
}

