using ECommerce.Shared;
namespace Cart.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder) => builder.AddApiLogging("Cart.API");
}
