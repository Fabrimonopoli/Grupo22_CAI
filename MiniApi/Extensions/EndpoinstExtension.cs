using MiniApi.Endpoints;
using MiniApi.Extensions.Endpoints;
using MiniApi.Models;

namespace MiniApi.Extensions;

// ALUMNOS: exposicion de todos los endpoints
public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        // ALUMNOS: se agregan  
        app.MapItemEndpoints();
        app.MapProductsEndpoints();
    }
}
