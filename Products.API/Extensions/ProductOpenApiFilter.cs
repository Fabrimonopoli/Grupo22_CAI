using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Products.API.Extensions;

public class ProductOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

        if (!path.Contains("api/products")) return;

        // 500 común a todos los endpoints
        if (operation.Responses.TryGetValue("500", out var r500))
        {
            r500.Description = "Internal Server Error (PRD-005)";
        }

        if (method == "GET")
        {
            if (path.Contains("{id}") && operation.Responses.TryGetValue("404", out var r404))
            {
                r404.Description = "Not Found (PRD-001)";
            }
        }
        else if (method == "POST")
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (PRD-002)";
            if (operation.Responses.TryGetValue("409", out var r409))
                r409.Description = "Conflict (PRD-003)";
        }
        else if (method == "PUT")
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (PRD-002)";
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (PRD-001)";
            if (operation.Responses.TryGetValue("409", out var r409))
                r409.Description = "Conflict (PRD-003)";
        }
        else if (method == "DELETE")
        {
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (PRD-001)";
            if (operation.Responses.TryGetValue("409", out var r409))
                r409.Description = "Conflict (PRD-004)";
        }
    }
}