using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers;

public class NotFoundExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not NotFoundException ex) return false;

        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsJsonAsync(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", title = "Not Found", status = StatusCodes.Status404NotFound, detail = "El recurso solicitado no fue encontrado.", instance = context.Request.Path.Value, errorCode = ex.ErrorCode, errorMessage = ex.Message }, cancellationToken);
        return true;
    }
}

public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;

        var (status, type, title, detail) = ex.ErrorCode switch
        {
            OrderErrorCodes.Ord002 => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request", "Los datos de la orden son inválidos."),
            OrderErrorCodes.Ord005 => (StatusCodes.Status422UnprocessableEntity, "https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity", "No se puede procesar la solicitud."),
            OrderErrorCodes.Ord006 => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict", "No se puede modificar el estado."),
            _ => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict", "La solicitud no cumple con las reglas de negocio.")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { type, title, status, detail, instance = context.Request.Path.Value, errorCode = ex.ErrorCode, errorMessage = ex.Message }, cancellationToken);
        return true;
    }
}

public class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { type = "https://tools.ietf.org/html/rfc7231#section-6.6.1", title = "Internal Server Error", status = StatusCodes.Status500InternalServerError, detail = "Ocurrió un error inesperado en el servidor.", instance = context.Request.Path.Value, errorCode = OrderErrorCodes.Ord007, errorMessage = "Error interno al procesar la orden." }, cancellationToken);
        return true;
    }
}
