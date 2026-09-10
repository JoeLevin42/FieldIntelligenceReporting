using System.Text.Json;
using ReportApi.Exceptions;

namespace ReportApi.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
        catch (BadRequestException ex)
        {
            _logger.LogWarning(ex, "Bad request");

            await WriteErrorResponse(
                context,
                StatusCodes.Status400BadRequest,
                ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found");

            await WriteErrorResponse(
                context,
                StatusCodes.Status404NotFound,
                ex.Message);
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogError(ex, "External service failure");

            await WriteErrorResponse(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Elasticsearch is currently unavailable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled system error");

            await WriteErrorResponse(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static async Task WriteErrorResponse(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            statusCode
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response));
    }
}