using ECommerce.Shared;
namespace Cart.API.Extensions;

public static class MiddlewareExtensions
{
    public static void UseAppMiddleware(this WebApplication app)
    {
        app.UseApiInfrastructure();
        app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");
    }
}
