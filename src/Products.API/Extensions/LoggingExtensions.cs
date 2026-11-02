using ECommerce.Shared;
namespace Products.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder) => builder.AddApiLogging("Products.API");
}
