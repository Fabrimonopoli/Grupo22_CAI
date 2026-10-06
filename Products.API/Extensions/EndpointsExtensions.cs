using Products.API.Extensions.Endpoints;

namespace Products.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapProductEndpoints();
    }
}
