using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

public sealed class NotificationExceptionHandler(ILogger<NotificationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, type, code, genericDetail) = exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound,
                 "Not Found",
                 "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                 ex.ErrorCode,
                 "El recurso solicitado no fue encontrado."),

            BusinessRuleException ex =>
                (StatusCodes.Status400BadRequest,
                 "Bad Request",
                 "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                 ex.ErrorCode,
                 "Los datos de la notificación son inválidos o no cumplen las reglas de negocio."),

            _ => (0, string.Empty, string.Empty, string.Empty, string.Empty)
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
            detail = genericDetail,
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

        // Errores de binding o JSON malformado (400)
        if (exception is BadHttpRequestException or JsonException)
        {
            logger.LogWarning("Cuerpo de solicitud HTTP inválido en {Path}: {Message}", context.Request.Path, exception.Message);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = "Bad Request",
                status = StatusCodes.Status400BadRequest,
                detail = "Los datos del cuerpo de la solicitud son inválidos o tienen formato incorrecto.",
                instance = context.Request.Path.Value,
                errorCode = NotificationErrorCodes.Ntf002,
                errorMessage = "Los datos de la notificación son inválidos.",
                correlationId
            }, cancellationToken);

            return true;
        }

        // Errores internos no controlados (500)
        logger.LogError(exception, "Error no controlado procesando {Path} [CorrelationId: {CorrelationId}]",
            context.Request.Path, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error inesperado al procesar la notificación.",
            instance = context.Request.Path.Value,
            errorCode = NotificationErrorCodes.Ntf004,
            errorMessage = "Error interno al procesar la notificación.",
            correlationId
        }, cancellationToken);

        return true;
    }
}