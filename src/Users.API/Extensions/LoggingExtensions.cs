using ECommerce.Shared;
namespace Users.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder) => builder.AddApiLogging("Users.API");
}
