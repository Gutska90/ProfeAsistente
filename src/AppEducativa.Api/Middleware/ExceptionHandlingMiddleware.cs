using System.Net;
using System.Text.Json;
using AppEducativa.Api.Services.AI.ClassGeneration;
using AppEducativa.Api.Services.AI.Gemini;
using AppEducativa.Api.Services.Auth;
using AppEducativa.Shared.Responses;

namespace AppEducativa.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        var (status, error, message) = Map(ex);
        _logger.LogError(ex, "Error no controlado: {Message}", ex.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var body = new ApiErrorResponse
        {
            Status = (int)status,
            Error = error,
            Message = message,
            TraceId = context.TraceIdentifier
        };

        if (_env.IsDevelopment() && status == HttpStatusCode.InternalServerError)
            body.Message = ex.Message;

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static (HttpStatusCode Status, string Error, string Message) Map(Exception ex) =>
        ex switch
        {
            ClassGenerationException cg => ((HttpStatusCode)cg.StatusCode, cg.ErrorCode, cg.Message),
            GeminiConfigurationException gc => ((HttpStatusCode)(gc.StatusCode ?? 503), gc.ErrorCode, gc.Message),
            GeminiRateLimitException gr => ((HttpStatusCode)(gr.StatusCode ?? 429), gr.ErrorCode, gr.Message),
            GeminiApiException ga => ((HttpStatusCode)(ga.StatusCode ?? 502), ga.ErrorCode, ga.Message),
            AuthException => (HttpStatusCode.Unauthorized, "AuthError", ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Forbidden", ex.Message),
            ArgumentException => (HttpStatusCode.BadRequest, "ValidationError", ex.Message),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NotFound", ex.Message),
            InvalidOperationException => (HttpStatusCode.Conflict, "Conflict", ex.Message),
            _ => (HttpStatusCode.InternalServerError, "InternalServerError",
                "Ocurrió un error interno. Intente nuevamente.")
        };
}
