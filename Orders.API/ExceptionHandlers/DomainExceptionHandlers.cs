using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;
using System.Text.Json;

namespace Orders.API.ExceptionHandlers;

public class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex) return false;

        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogWarning("Recurso no encontrado [{ErrorCode}] en {Path}: {Message}",
            ex.ErrorCode, context.Request.Path, ex.Message);

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = "El recurso solicitado no fue encontrado.", // Frase general
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message, // Mensaje dinámico puntual de la excepción
            correlationId
        }, cancellationToken);

        return true;
    }
}

public class BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;

        var (status, type, title, genericDetail) = ex.ErrorCode switch
        {
            OrderErrorCodes.Ord002 => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "Los datos de la orden son inválidos."
            ),
            OrderErrorCodes.Ord005 => (
                StatusCodes.Status422UnprocessableEntity,
                "https://tools.ietf.org/html/rfc4918#section-11.2",
                "Unprocessable Entity",
                "No se puede procesar la solicitud." // Exactamente el detail del ejemplo de la consigna
            ),
            OrderErrorCodes.Ord006 => (
                StatusCodes.Status409Conflict,
                "https://tools.ietf.org/html/rfc7231#section-6.5.9",
                "Conflict",
                "El estado actual de la orden no permite realizar la operación solicitada."
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
            detail = genericDetail, // Frase general del ejemplo
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
            errorMessage = ex.Message, // Mensaje específico dinámico de la regla
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

        // Errores de binding o JSON malformado
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
                errorCode = OrderErrorCodes.Ord002,
                errorMessage = "Los datos de la orden son inválidos.",
                correlationId
            }, cancellationToken);

            return true;
        }

        // Error interno no controlado (500)
        logger.LogError(exception, "Error no controlado procesando {Path} [CorrelationId: {CorrelationId}]",
            context.Request.Path, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error inesperado al procesar la orden.",
            instance = context.Request.Path.Value,
            errorCode = OrderErrorCodes.Ord007,
            errorMessage = "Error interno al procesar la orden.",
            correlationId
        }, cancellationToken);

        return true;
    }
}