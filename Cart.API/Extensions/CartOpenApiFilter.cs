using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Cart.API.Extensions;

public class CartOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

        if (!path.Contains("api/cart")) return;

        // 500 común a todos los endpoints
        if (operation.Responses.TryGetValue("500", out var r500))
        {
            r500.Description = "Internal Server Error (CRT-005)";
        }

        if (method == "GET")
        {
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (CRT-001)";
        }
        else if (method == "POST")
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (CRT-004)";
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (CRT-002)";
            if (operation.Responses.TryGetValue("422", out var r422))
                r422.Description = "Unprocessable Entity (CRT-003)";
        }
        else if (method == "PUT")
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (CRT-004)";
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (CRT-001 / CRT-002)";
            if (operation.Responses.TryGetValue("422", out var r422))
                r422.Description = "Unprocessable Entity (CRT-003)";
        }
        else if (method == "DELETE")
        {
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (CRT-001)";
        }
    }
}