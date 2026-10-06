using Users.API.Extensions.Endpoints;

namespace Users.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapUserEndpoints();
    }
}

