using Notifications.API.Extensions.Endpoints;

namespace Notifications.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app) => app.MapNotificationEndpoints();
}
