using Microsoft.AspNetCore.Diagnostics;
using Products.API.Exceptions;

namespace Products.API.ExceptionHandlers;

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
            detail = ex.Message,
            instance = context.Request.Path.Value,
            errorCode = ex.ErrorCode,
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

        var (status, type, title) = ex.ErrorCode switch
        {
            ProductErrorCodes.Prd002 => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            ProductErrorCodes.Prd003 => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict"),
            ProductErrorCodes.Prd004 => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict"),
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
            detail = "Ocurrió un error inesperado al procesar el producto.",
            instance = context.Request.Path.Value,
            errorCode = ProductErrorCodes.Prd005,
            errorMessage = "Error interno al procesar el producto.",
            correlationId
        }, cancellationToken);

        return true;
    }
}