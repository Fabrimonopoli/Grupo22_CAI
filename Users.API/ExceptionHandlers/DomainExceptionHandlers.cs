using Microsoft.AspNetCore.Diagnostics;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;

        var (status, type, title) = ex.ErrorCode switch
        {
            UserErrorCodes.Usr001 => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict"),
            UserErrorCodes.Usr003 => (StatusCodes.Status401Unauthorized, "https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
            UserErrorCodes.Usr004 => (StatusCodes.Status403Forbidden, "https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
            UserErrorCodes.Usr005 => (StatusCodes.Status403Forbidden, "https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
            _ => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request")
        };

        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogWarning("Regla de negocio no cumplida [{ErrorCode}] en {Path}: {Message}",
            ex.ErrorCode, context.Request.Path, ex.Message);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type,
            title,
            status,
            detail = ex.Message,
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message,
            correlationId
        }, cancellationToken);

        return true;
    }
}

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogError(exception, "Error no controlado procesando {Path} [CorrelationId: {CorrelationId}]",
            context.Request.Path, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error inesperado en el servidor.",
            instance = context.Request.Path.Value,
            errorCode = UserErrorCodes.Usr006,
            errorMessage = "Error interno al procesar el usuario.",
            correlationId
        }, cancellationToken);

        return true;
    }
}
