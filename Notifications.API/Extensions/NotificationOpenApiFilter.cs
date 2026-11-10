using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Notifications.API.Extensions;

public class NotificationOpenApiFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath?.ToLowerInvariant() ?? string.Empty;
        var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

        if (!path.Contains("api/notifications")) return;

        // 500 común a los endpoints
        if (operation.Responses.TryGetValue("500", out var r500))
        {
            r500.Description = "Internal Server Error (NTF-004)";
        }

        if (method == "POST")
        {
            if (operation.Responses.TryGetValue("400", out var r400))
                r400.Description = "Bad Request (NTF-002)";
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (NTF-001)";
        }
        else if (method == "GET")
        {
            if (operation.Responses.TryGetValue("404", out var r404))
                r404.Description = "Not Found (NTF-003)";
        }
    }
}