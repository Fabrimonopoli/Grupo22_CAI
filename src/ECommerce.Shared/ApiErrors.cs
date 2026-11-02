using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Shared;

public sealed record ApiProfile(string Service, string ValidationCode, string InternalCode);

/// <summary>Contrato común para toda respuesta HTTP de error.</summary>
public sealed record ApiError(string Type, string Title, int Status, string Detail,
    string Instance, string ErrorCode, string ErrorMessage, string CorrelationId);

public static class ErrorCatalog
{
    public static readonly IReadOnlyDictionary<string, (int Status, string Message)> Entries =
        new Dictionary<string, (int, string)>
        {
            ["PRD-001"] = (404, "Producto no encontrado."),
            ["PRD-002"] = (400, "Los datos del producto son inválidos."),
            ["PRD-003"] = (409, "Ya existe un producto con ese nombre en la categoría."),
            ["PRD-004"] = (409, "El producto tiene órdenes activas y no puede eliminarse."),
            ["PRD-005"] = (500, "Error interno al procesar el producto."),
            ["PRD-006"] = (422, "Stock insuficiente para reservar los productos."),
            ["PRD-007"] = (409, "La reserva de stock no admite esta operación."),
            ["USR-001"] = (409, "El email ya está registrado."),
            ["USR-002"] = (400, "Los datos del usuario son inválidos."),
            ["USR-003"] = (401, "Credenciales incorrectas."),
            ["USR-004"] = (403, "Usuario bloqueado por demasiados intentos fallidos."),
            ["USR-005"] = (403, "Usuario bloqueado por detección de fraude."),
            ["USR-006"] = (500, "Error interno al procesar el usuario."),
            ["USR-007"] = (404, "Usuario no encontrado."),
            ["ORD-001"] = (404, "Orden no encontrada."),
            ["ORD-002"] = (400, "Los datos de la orden son inválidos."),
            ["ORD-003"] = (404, "Usuario no encontrado al crear la orden."),
            ["ORD-004"] = (404, "Producto no encontrado al crear la orden."),
            ["ORD-005"] = (422, "Stock insuficiente para uno o más productos."),
            ["ORD-006"] = (409, "El estado de la orden no puede ser modificado."),
            ["ORD-007"] = (500, "Error interno al procesar la orden."),
            ["ORD-008"] = (409, "La clave de idempotencia ya se usó para otra solicitud."),
            ["CRT-001"] = (404, "Carrito no encontrado."),
            ["CRT-002"] = (404, "Producto no encontrado."),
            ["CRT-003"] = (422, "Stock insuficiente para agregar al carrito."),
            ["CRT-004"] = (400, "Cantidad o identificador inválido."),
            ["CRT-005"] = (500, "Error interno al procesar el carrito."),
            ["NTF-001"] = (404, "Usuario no encontrado."),
            ["NTF-002"] = (400, "Los datos de la notificación son inválidos."),
            ["NTF-003"] = (404, "No se encontraron notificaciones para el usuario."),
            ["NTF-004"] = (500, "Error interno al procesar la notificación.")
        };
}

public static class ApiErrors
{
    public static async Task WriteAsync(HttpContext context, int status, string code, string message,
        Exception? exception = null)
    {
        context.Items["ErrorCode"] = code;
        var logger = context.RequestServices.GetRequiredService<ILogger<ApiExceptionHandler>>();
        if (status >= 500)
            logger.LogError(exception, "Error {ErrorCode} en {Endpoint}", code, context.Request.Path.Value);
        else
            logger.LogWarning("Error de solicitud {ErrorCode} en {Endpoint}: {ErrorMessage}", code, context.Request.Path.Value, message);
        var correlationId = CorrelationIdMiddleware.GetId(context);
        context.Response.StatusCode = status;
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        var body = new ApiError("https://www.rfc-editor.org/rfc/rfc9110",
            ReasonPhrases.GetReasonPhrase(status), status, message, context.Request.Path.Value ?? "/",
            code, message, correlationId);
        await context.Response.WriteAsJsonAsync(body, options: (JsonSerializerOptions?)null,
            contentType: "application/problem+json", cancellationToken: context.RequestAborted);
    }
}

/// <summary>Manejo global con IExceptionHandler, incluido el binding de requests.</summary>
public sealed class ApiExceptionHandler(ApiProfile profile) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        // Domain assemblies remain independent; each exception exposes its catalog code.
        var code = exception.GetType().GetProperty("ErrorCode")?.GetValue(exception) as string
            ?? exception.GetType().GetProperty("Code")?.GetValue(exception) as string;
        if (exception.GetType().Name == "UsersApiUnavailableException")
            code = "NTF-004";
        if (code is not null && ErrorCatalog.Entries.TryGetValue(code, out var domain))
        {
            await ApiErrors.WriteAsync(context, domain.Status, code,
                domain.Status >= 500 ? domain.Message : exception.Message, exception);
            return true;
        }
        if (exception is ValidationException or JsonException || exception is BadHttpRequestException)
        {
            var status = exception is BadHttpRequestException bad ? bad.StatusCode : 400;
            var invalidCode = status == 400 ? profile.ValidationCode : $"HTTP-{status}";
            var message = exception is ValidationException ? exception.Message : ErrorCatalog.Entries.GetValueOrDefault(invalidCode).Message
                ?? "El formato de la solicitud es inválido.";
            await ApiErrors.WriteAsync(context, status, invalidCode, message, exception);
            return true;
        }
        await ApiErrors.WriteAsync(context, 500, profile.InternalCode,
            ErrorCatalog.Entries[profile.InternalCode].Message, exception);
        return true;
    }
}

/// <summary>Ejecuta Data Annotations también en Minimal APIs.</summary>
public sealed class RequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var value in context.Arguments)
        {
            if (value is null || value.GetType().Namespace?.Contains(".DTOs", StringComparison.Ordinal) != true)
                continue;
            var results = new List<ValidationResult>();
            if (!Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true))
                throw new ValidationException(string.Join("; ", results.Select(r => r.ErrorMessage)));
        }
        return await next(context);
    }
}
