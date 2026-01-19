using System.Net;
using System.Text.Json;
using CrmSaas.Api.Common;

namespace CrmSaas.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, response) = exception switch
        {
            ApiException apiEx => (apiEx.StatusCode, new ErrorResponse
            {
                Success = false,
                Message = apiEx.Message,
                Errors = apiEx.Errors,
                TraceId = context.TraceIdentifier
            }),
            
            FluentValidation.ValidationException valEx => (422, new ErrorResponse
            {
                Success = false,
                Message = "Validation failed",
                Errors = valEx.Errors.Select(e => e.ErrorMessage).ToList(),
                TraceId = context.TraceIdentifier
            }),
            
            UnauthorizedAccessException => (401, new ErrorResponse
            {
                Success = false,
                Message = "Unauthorized",
                TraceId = context.TraceIdentifier
            }),
            
            KeyNotFoundException => (404, new ErrorResponse
            {
                Success = false,
                Message = exception.Message,
                TraceId = context.TraceIdentifier
            }),
            
            _ => (500, new ErrorResponse
            {
                Success = false,
                Message = _environment.IsDevelopment() ? exception.Message : "An error occurred",
                StackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                TraceId = context.TraceIdentifier
            })
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning("Request failed with status {StatusCode}: {Message}", statusCode, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

public class ErrorResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }
    public string? StackTrace { get; set; }
    public string? TraceId { get; set; }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
