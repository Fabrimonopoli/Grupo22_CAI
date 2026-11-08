using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;

        var (status, type, title, genericDetail) = ex.ErrorCode switch
        {
            UserErrorCodes.Usr001 => (
                StatusCodes.Status409Conflict,
                "https://tools.ietf.org/html/rfc7231#section-6.5.9",
                "Conflict",
                "Conflicto con el estado actual del recurso."
            ),
            UserErrorCodes.Usr002 => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "Los datos de la solicitud son inválidos o tienen formato incorrecto."
            ),
            UserErrorCodes.Usr003 => (
                StatusCodes.Status401Unauthorized,
                "https://tools.ietf.org/html/rfc7235#section-3.1",
                "Unauthorized",
                "Credenciales de autenticación inválidas."
            ),
            UserErrorCodes.Usr004 => (
                StatusCodes.Status403Forbidden,
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                "Forbidden",
                "Acceso restringido por motivos de seguridad."
            ),
            UserErrorCodes.Usr005 => (
                StatusCodes.Status403Forbidden,
                "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                "Forbidden",
                "Acceso restringido por motivos de seguridad."
            ),
            _ => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "No se puede procesar la solicitud."
            )
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
            detail = genericDetail,
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
                errorCode = UserErrorCodes.Usr002,
                errorMessage = "Los datos del usuario son inválidos.",
                correlationId
            }, cancellationToken);

            return true;
        }

        // Errores internos de servidor (500)
        logger.LogError(exception, "Error no controlado procesando {Path} [CorrelationId: {CorrelationId}]",
            context.Request.Path, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error inesperado al procesar el usuario.",
            instance = context.Request.Path.Value,
            errorCode = UserErrorCodes.Usr006,
            errorMessage = "Error interno al procesar el usuario.",
            correlationId
        }, cancellationToken);

        return true;
    }
}