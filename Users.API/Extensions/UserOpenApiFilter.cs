using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Users.API.Extensions;

public class UserOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;

        if (!path.Contains("api/users")) return;

        // 500 común a los endpoints
        if (operation.Responses.TryGetValue("500", out var r500))
        {
            r500.Description = "Internal Server Error (USR-006)";
        }

        if (path.Contains("register"))
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (USR-002)";
            if (operation.Responses.TryGetValue("409", out var r409))
                r409.Description = "Conflict (USR-001)";
        }
        else if (path.Contains("login"))
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (USR-002)";
            if (operation.Responses.TryGetValue("401", out var r401))
                r401.Description = "Unauthorized (USR-003)";
            if (operation.Responses.TryGetValue("403", out var r403))
                r403.Description = "Forbidden (USR-004 / USR-005)";
        }
    }
}