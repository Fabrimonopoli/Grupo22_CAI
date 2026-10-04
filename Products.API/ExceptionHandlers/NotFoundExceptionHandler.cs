using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MiniApi.Exceptions;

namespace MiniApi.ExceptionHandlers
{
    public class NotFoundExceptionHandler : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not NotFoundException ex) return false;

            context.Response.StatusCode = StatusCodes.Status404NotFound;

            var problemDetails = new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = "El recurso solicitado no fue encontrado.",
                Instance = context.Request.Path
            };

            problemDetails.Extensions.Add("errorCode", ex.ErrorCode);
            problemDetails.Extensions.Add("errorMessage", ex.Message);

            await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}
