using Orders.API.Extensions.Endpoints;

namespace Orders.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapOrderEndpoints();
    }
}
