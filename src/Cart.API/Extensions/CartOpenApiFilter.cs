using ECommerce.Shared;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Cart.API.Models;
using Cart.API.DTOs;

namespace Cart.API.Extensions;

/// <summary>Esquemas y ejemplos de todos los resultados públicos del servicio.</summary>
public sealed class CartOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? "";
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (!path.Contains("api/cart"))
            return;
        var single = path.Contains("{");

        var pid = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        var uid = "a1b2c3d4-0000-0000-0000-111122223333";
        var oid = "f1e2d3c4-0000-0000-0000-aabbccddeeff";
        var date = "2026-11-01T12:00:00Z";
        var product = new
        {
            id = pid,
            nombre = "Notebook Dell XPS 15",
            descripcion = "Laptop 15 pulgadas",
            precio = 1500m,
            stock = 10,
            categoria = "Electrónica",
            fechaCreacion = date
        };
        var productRequest = new
        {
            nombre = "Notebook Dell XPS 15",
            descripcion = "Laptop 15 pulgadas",
            precio = 1500m,
            stock = 10,
            categoria = "Electrónica"
        };
        var order = new
        {
            id = oid,
            usuarioId = uid,
            items = new[] { new { productoId = pid, cantidad = 2, precioUnitario = 1500m } },
            total = 3000m,
            estado = "Pendiente",
            fechaCreacion = date
        };
        var cart = new
        {
            usuarioId = uid,
            items = new[] { new { productoId = pid, cantidad = 2 } },
            fechaActualizacion = date
        };
        var notification = new
        {
            id = oid,
            usuarioId = uid,
            mensaje = "Su orden fue confirmada.",
            tipo = "Email",
            estado = "Enviada",
            fechaEnvio = date
        };

        var errors = new Dictionary<int, string[]> { [400] = ["CRT-004"], [404] = ["CRT-001"], [500] = ["CRT-005"] };
        if (method is "POST" or "PUT")
        {
            errors[400] = ["CRT-004"];
            errors[404] = ["CRT-001", "CRT-002"];
            errors[422] = ["CRT-003"];
        }
        if (method == "DELETE")
            ApiDocumentation.Complete(operation, context, 204, null, null, errors);
        else
            ApiDocumentation.Complete(operation, context, 200, typeof(Cart.API.Models.Cart), cart, errors,
            method == "POST" ? new
            {
                productoId = pid,
                cantidad = 2
            } : method == "PUT" ? new
            {
                cantidad = 4
            } : null);
    }
}
