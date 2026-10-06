using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Orders.API.Extensions;

public class OrderOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLower();
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpper();

        if (relativePath is null || !relativePath.Contains("api/orders")) return;

        if (httpMethod == "POST")
        {
            operation.Responses.Remove("200");
            operation.Responses.TryAdd("201", new OpenApiResponse { Description = "Created - Orden creada exitosamente" });
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos de la orden inválidos (ORD-002)" });
            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Usuario o producto no encontrado (ORD-003 / ORD-004)" });
            operation.Responses.TryAdd("422", new OpenApiResponse { Description = "Unprocessable Entity - Stock insuficiente para uno o más productos (ORD-005)" });
        }
        else if (httpMethod == "GET")
        {
            if (relativePath.Contains("{id}"))
            {
                operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Orden no encontrada (ORD-001)" });
            }
        }
        else if (httpMethod == "PUT")
        {
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos de estado inválidos (ORD-002)" });
            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Orden no encontrada (ORD-001)" });
            operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict - El estado de la orden no puede ser modificado (ORD-006)" });
        }

        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error - Error inesperado al procesar la orden (ORD-007)" });
    }
}
