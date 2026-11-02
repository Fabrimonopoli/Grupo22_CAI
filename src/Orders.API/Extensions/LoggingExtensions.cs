using ECommerce.Shared;
namespace Orders.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder) => builder.AddApiLogging("Orders.API");
}
