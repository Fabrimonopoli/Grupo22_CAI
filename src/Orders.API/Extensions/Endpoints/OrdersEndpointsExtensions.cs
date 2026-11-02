using Orders.API.Data;
using Microsoft.AspNetCore.Routing;
namespace Orders.API.Extensions.Endpoints;

/// <summary>Consulta interna usada por Products; las órdenes públicas están en OrdersController.</summary>
public static class OrdersEndpointsExtensions
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/orders/active-for-product/{productId:guid}", async (Guid productId, OrderRepository repo)
            => Results.Ok(new
            {
                hasActiveOrders = await repo.HasActiveOrdersForProductAsync(productId)
            }))
            .WithTags("Internal orders").ExcludeFromDescription();
    }
}
