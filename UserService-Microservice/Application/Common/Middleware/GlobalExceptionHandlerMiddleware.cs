using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using UserService.Application.Common.Exceptions;
using UserService.Application.Common.Models;

namespace UserService.Application.Common.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ValidationException validationException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = validationException.Errors.Any()
                    ? (validationException.Errors.Count() == 1 ? validationException.Errors.First().ErrorMessage : "Validation failed")
                    : validationException.Message;
                response.Details = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                break;

            case UserNotFoundException:
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.Message = exception.Message;
                break;

            case InvalidCredentialsException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = exception.Message;
                break;

            case UserAlreadyExistsException:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                break;

            case DuplicateApiKeyException:
                response.StatusCode = (int)HttpStatusCode.Conflict;
                response.Message = exception.Message;
                break;

            case EmailNotConfirmedException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                break;

            case InvalidTokenException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                break;

            case PasswordResetTokenExpiredException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                break;

            case UserProfileUpdateException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = exception.Message;
                break;

            case UserSuspendedException:
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Message = exception.Message;
                break;

            case QuotaExceededException:
                response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                response.Message = exception.Message;
                break;

            case InvalidApiKeyException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = exception.Message;
                break;

            case SubscriptionRequiredException:
                response.StatusCode = (int)HttpStatusCode.PaymentRequired;
                response.Message = exception.Message;
                break;

            case UnauthorizedAccessException:
                response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response.Message = "Unauthorized access";
                break;

            case ArgumentNullException:
            case ArgumentException:
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.Message = "Invalid request parameters";
                response.Details = new List<string> { exception.Message };
                break;

            default:
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Message = "An internal server error occurred";
                break;
        }

        context.Response.StatusCode = response.StatusCode;

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

public static class GlobalExceptionHandlerExtensions
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}