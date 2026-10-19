using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cart.API.Extensions
{
    public class CartOpenApiFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var relativePath = context.ApiDescription.RelativePath?.ToLower();
            if (relativePath is null || !relativePath.Contains("api/cart")) return;

            // Documentación para los endpoints que programaste vos (POST y PUT)
            if (context.ApiDescription.HttpMethod == "POST" || context.ApiDescription.HttpMethod == "PUT")
            {
                operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Cantidad inválida (CRT-004)" });
                operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Not Found - Producto o Carrito no encontrado (CRT-001 / CRT-002)" });
                operation.Responses.TryAdd("422", new OpenApiResponse { Description = "Unprocessable Entity - Stock insuficiente (CRT-003)" });
            }

            // Nota para tu compañero: Él tendrá que agregar acá las respuestas de GET y DELETE.

            operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error - Error inesperado en el carrito (CRT-005)" });
        }
    }
}