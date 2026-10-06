using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.API.Extensions;

public class ProductOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLower();
        var httpMethod = context.ApiDescription.HttpMethod?.ToUpper();

        if (relativePath is null || !relativePath.Contains("api/products")) return;

        if (httpMethod == "POST")
        {
            operation.Responses.Remove("200");
            operation.Responses.TryAdd("201", new OpenApiResponse { Description = "Created - Producto creado exitosamente" });

            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos del producto inválidos (PRD-002)" });
            operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict - Ya existe un producto con ese nombre en la categoría (PRD-003)" });
        }
        else if (httpMethod == "DELETE")
        {
            operation.Responses.Remove("200");
            operation.Responses.TryAdd("204", new OpenApiResponse { Description = "No Content - Producto eliminado exitosamente" });

            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Producto no encontrado (PRD-001)" });
            operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict - El producto tiene órdenes activas y no puede eliminarse (PRD-004)" });
        }
        else if (httpMethod == "GET")
        {
            if (relativePath.Contains("{id}"))
            {
                operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Producto no encontrado (PRD-001)" });
            }
        }
        else if (httpMethod == "PUT")
        {
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos del producto inválidos (PRD-002)" });
            operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Producto no encontrado (PRD-001)" });
        }

        // Error interno global
        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error - Error inesperado al procesar el producto (PRD-005)" });
    }
}