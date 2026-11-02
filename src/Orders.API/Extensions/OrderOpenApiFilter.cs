using ECommerce.Shared;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Orders.API.Models;
using Orders.API.DTOs;

namespace Orders.API.Extensions;

/// <summary>Esquemas y ejemplos de todos los resultados públicos del servicio.</summary>
public sealed class OrderOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? "";
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (!path.Contains("api/orders"))
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

        var errors = new Dictionary<int, string[]> { [500] = ["ORD-007"] };
        if (path.Contains("active-for-product"))
            return;
        if (method == "GET")
        {
            if (!single) errors[400] = ["ORD-002"];
            if (single)
                errors[404] = ["ORD-001"];
            ApiDocumentation.Complete(operation, context, 200, single ? typeof(Order) : typeof(Order[]), single ? order : new[] { order }, errors);
        }
        else if (method == "POST")
        {
            errors[400] = ["ORD-002"];
            errors[404] = ["ORD-003", "ORD-004"];
            errors[409] = ["ORD-008"];
            errors[422] = ["ORD-005"];
            ApiDocumentation.Complete(operation, context, 201, typeof(Order), order, errors,
                new
                {
                    usuarioId = uid,
                    items = new[] { new { productoId = pid, cantidad = 2 } }
                });
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Idempotency-Key",
                In = ParameterLocation.Header,
                Required = false,
                Description = "GUID para reintentar una creación sin duplicarla. Conservarlo ante un fallo de conexión.",
                Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
            });
        }
        else
        {
            errors[400] = ["ORD-002"];
            errors[404] = ["ORD-001"];
            errors[409] = ["ORD-006"];
            ApiDocumentation.Complete(operation, context, 200, typeof(UpdateOrderStatusResponse),
                new
                {
                    id = oid,
                    estado = "Confirmada",
                    fechaActualizacion = date
                }, errors, new
                {
                    estado = "Confirmada"
                });
        }
    }
}
