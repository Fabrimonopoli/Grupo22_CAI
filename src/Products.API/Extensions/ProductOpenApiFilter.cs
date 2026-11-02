using ECommerce.Shared;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Products.API.Models;
using Products.API.DTOs;

namespace Products.API.Extensions;

/// <summary>Esquemas y ejemplos de todos los resultados públicos del servicio.</summary>
public sealed class ProductOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? "";
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (!path.Contains("api/products"))
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

        var errors = new Dictionary<int, string[]> { [500] = ["PRD-005"] };
        if (method == "GET")
        {
            if (single)
                errors[404] = ["PRD-001"];
            ApiDocumentation.Complete(operation, context, 200, single ? typeof(Product) : typeof(Product[]), single ? product : new[] { product }, errors);
        }
        else if (method == "DELETE")
        {
            errors[404] = ["PRD-001"];
            errors[409] = ["PRD-004"];
            ApiDocumentation.Complete(operation, context, 204, null, null, errors);
        }
        else
        {
            errors[400] = ["PRD-002"];
            errors[409] = ["PRD-003"];
            if (method == "PUT")
                errors[404] = ["PRD-001"];
            ApiDocumentation.Complete(operation, context, method == "POST" ? 201 : 200, typeof(Product), product, errors, productRequest);
        }
    }
}
