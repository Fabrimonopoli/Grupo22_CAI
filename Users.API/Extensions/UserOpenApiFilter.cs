using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Users.API.Extensions;

public class UserOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath?.ToLower();

        if (relativePath is null || !relativePath.Contains("api/users")) return;

        if (relativePath.Contains("register"))
        {
            operation.Responses.Remove("200");
            operation.Responses.TryAdd("201", new OpenApiResponse { Description = "Created - Usuario registrado exitosamente" });
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos del usuario inválidos (USR-002)" });
            operation.Responses.TryAdd("409", new OpenApiResponse { Description = "Conflict - El email ya se encuentra registrado (USR-001)" });
        }
        else if (relativePath.Contains("login"))
        {
            operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Bad Request - Datos del usuario inválidos (USR-002)" });
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized - Credenciales incorrectas (USR-003)" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden - Usuario bloqueado o suspendido (USR-004 / USR-005)" });
        }

        operation.Responses.TryAdd("500", new OpenApiResponse { Description = "Internal Server Error - Error inesperado al procesar el usuario (USR-006)" });
    }
}
