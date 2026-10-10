using Microsoft.AspNetCore.Diagnostics;
using Users.API.Exceptions;

namespace Users.API.ExceptionHandlers;

public class BusinessRuleExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException ex) return false;

        var (status, type, title, detail) = ex.ErrorCode switch
        {
            UserErrorCodes.Usr001 => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict", "Ya existe un recurso con esos datos."),
            UserErrorCodes.Usr002 => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request", "Los datos del usuario son inválidos."),
            UserErrorCodes.Usr003 => (StatusCodes.Status401Unauthorized, "https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized", "Las credenciales no son válidas."),
            UserErrorCodes.Usr004 => (StatusCodes.Status403Forbidden, "https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden", "El acceso está prohibido."),
            UserErrorCodes.Usr005 => (StatusCodes.Status403Forbidden, "https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden", "El acceso está prohibido."),
            _ => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request", "Solicitud inválida.")
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
        await context.Response.WriteAsJsonAsync(new { type = "https://tools.ietf.org/html/rfc7231#section-6.6.1", title = "Internal Server Error", status = StatusCodes.Status500InternalServerError, detail = "Ocurrió un error inesperado en el servidor.", instance = context.Request.Path.Value, errorCode = UserErrorCodes.Usr006, errorMessage = "Error interno al procesar el usuario." }, cancellationToken);
        return true;
    }
}
