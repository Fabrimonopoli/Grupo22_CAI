using ECommerce.Shared;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Users.API.Models;
using Users.API.DTOs;

namespace Users.API.Extensions;

/// <summary>Esquemas y ejemplos de todos los resultados públicos del servicio.</summary>
public sealed class UserOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? "";
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant();
        if (!path.Contains("api/users"))
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

        var errors = new Dictionary<int, string[]> { [400] = ["USR-002"], [500] = ["USR-006"] };
        if (path.Contains("register"))
        {
            errors[409] = ["USR-001"];
            ApiDocumentation.Complete(operation, context, 201, typeof(RegisterUserResponse),
                new
                {
                    id = uid,
                    nombre = "María",
                    apellido = "González",
                    email = "maria@email.com",
                    fechaRegistro = date,
                    activo = true
                }, errors,
                new
                {
                    nombre = "María",
                    apellido = "González",
                    email = "maria@email.com",
                    password = "MiPassword123!"
                });
        }
        else if (path.Contains("login"))
        {
            errors[401] = ["USR-003"];
            errors[403] = ["USR-004", "USR-005"];
            ApiDocumentation.Complete(operation, context, 200, typeof(LoginUserResponse),
                new
                {
                    id = uid,
                    nombre = "María",
                    apellido = "González",
                    email = "maria@email.com"
                }, errors,
                new
                {
                    email = "maria@email.com",
                    password = "MiPassword123!"
                });
        }
        else if (method == "GET")
        {
            errors.Remove(400);
            errors[404] = ["USR-007"];
            ApiDocumentation.Complete(operation, context, 200, typeof(UserLookupResponse),
                new
                {
                    id = uid,
                    nombre = "María",
                    apellido = "González",
                    email = "maria@email.com",
                    activo = true
                }, errors);
        }
    }
}
