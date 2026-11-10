using Cart.API.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using System.Text.Json;

namespace Cart.API.ExceptionHandlers;

public class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex) return false;

        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogWarning("Recurso no encontrado [{ErrorCode}] en {Path}: {Message}", ex.Code, context.Request.Path, ex.Message);

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            title = "Not Found",
            status = StatusCodes.Status404NotFound,
            detail = "El recurso solicitado no fue encontrado.",
            instance = context.Request.Path.Value,
            errorCode = ex.Code,
            errorMessage = ex.Message,
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

        var (status, type, title, genericDetail) = ex.Code switch
        {
            CartErrorCodes.Crt004 => (
                StatusCodes.Status400BadRequest,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "Los datos de la solicitud son inválidos o tienen formato incorrecto."
            ),
            CartErrorCodes.Crt003 => (
                StatusCodes.Status422UnprocessableEntity,
                "https://tools.ietf.org/html/rfc4918#section-11.2",
                "Unprocessable Entity",
                "No se puede procesar la solicitud."
            ),
            _ => (
                StatusCodes.Status409Conflict,
                "https://tools.ietf.org/html/rfc7231#section-6.5.9",
                "Conflict",
                "Regla de negocio no cumplida."
            )
        };

        var correlationId = context.Response.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? string.Empty;

        logger.LogWarning("Regla de negocio no cumplida [{ErrorCode}] en {Path}: {Message}", ex.Code, context.Request.Path, ex.Message);

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type,
            title,
            status,
            detail = genericDetail,
            instance = context.Request.Path.Value,
            errorCode = ex.Code,
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
                errorCode = CartErrorCodes.Crt004,
                errorMessage = "Cantidad inválida.",
                correlationId
            }, cancellationToken);

            return true;
        }

        // Errores internos de servidor (500)
        logger.LogError(exception, "Error no controlado procesando {Path} [CorrelationId: {CorrelationId}]", context.Request.Path, correlationId);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            title = "Internal Server Error",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error inesperado al procesar el carrito.",
            instance = context.Request.Path.Value,
            errorCode = CartErrorCodes.Crt005,
            errorMessage = "Error interno al procesar el carrito.",
            correlationId
        }, cancellationToken);

        return true;
    }
}