using ECommerce.Shared;
namespace Notifications.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder) => builder.AddApiLogging("Notifications.API");
}
