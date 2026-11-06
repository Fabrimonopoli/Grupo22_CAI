using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

public sealed class NotificationExceptionHandler(ILogger<NotificationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, type, code) = exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound,
                 "Not Found",
                 "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                 ex.ErrorCode),

            BusinessRuleException ex =>
                (StatusCodes.Status400BadRequest,
                 "Bad Request",
                 "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                 ex.ErrorCode),

            _ => (0, string.Empty, string.Empty, string.Empty)
        };

        if (status == 0) return false;

        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogWarning("Error de negocio [{ErrorCode}] en {Path}: {Message}",
            code, context.Request.Path, exception.Message);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type,
            title,
            status,
            detail = exception.Message,
            instance = context.Request.Path.Value,
            errorCode = code,
            errorMessage = exception.Message,
            correlationId
        }, cancellationToken);

        return true;
    }
}

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
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
            detail = "Ocurrió un error inesperado al procesar la solicitud.",
            instance = context.Request.Path.Value,
            errorCode = NotificationErrorCodes.Ntf004,
            errorMessage = "Error interno al procesar la notificación.",
            correlationId
        }, cancellationToken);

        return true;
    }
}