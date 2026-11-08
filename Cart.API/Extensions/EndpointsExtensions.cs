using Cart.API.Extensions.Endpoints;

namespace Cart.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapCartEndpoints();
    }
}