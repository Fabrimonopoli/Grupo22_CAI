using Microsoft.AspNetCore.Diagnostics;
using Notifications.API.Clients;
using Notifications.API.Exceptions;

namespace Notifications.API.ExceptionHandlers;

public sealed class NotificationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail, code) = exception switch
        {
            NotificationNotFoundException ex =>
                (StatusCodes.Status404NotFound, "Not Found", ex.Message, ex.ErrorCode),
            BusinessRuleException ex when ex.ErrorCode == NotificationErrorCodes.Ntf002 =>
                (StatusCodes.Status400BadRequest, "Bad Request", ex.Message, ex.ErrorCode),
            BusinessRuleException ex when ex.ErrorCode == NotificationErrorCodes.Ntf001 =>
                (StatusCodes.Status404NotFound, "Not Found", ex.Message, ex.ErrorCode),
            UsersApiUnavailableException ex =>
                (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", ex.Message, NotificationErrorCodes.Ntf003),
            BusinessRuleException ex when ex.ErrorCode == NotificationErrorCodes.Ntf003 =>
                (StatusCodes.Status503ServiceUnavailable, "Service Unavailable", ex.Message, ex.ErrorCode),
            _ => (0, string.Empty, string.Empty, string.Empty)
        };

        if (status == 0) return false;
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail,
            instance = context.Request.Path.Value,
            errorCode = code,
            errorMessage = exception.Message
        }, cancellationToken);
        return true;
    }
}

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Error no controlado procesando {Path}", context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.com/500",
            title = "Internal Server Error",
            status = 500,
            detail = "Ocurrió un error inesperado en el servidor.",
            instance = context.Request.Path.Value,
            errorCode = NotificationErrorCodes.Ntf004
        }, cancellationToken);
        return true;
    }
}
