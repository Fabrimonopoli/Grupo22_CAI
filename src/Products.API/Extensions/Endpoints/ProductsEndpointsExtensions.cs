using Products.API.DTOs;
using Products.API.Services;
using Microsoft.AspNetCore.Routing;
namespace Products.API.Extensions.Endpoints;

/// <summary>Operaciones internas idempotentes de reserva. El catálogo público está en ProductsController.</summary>
public static class ProductsEndpointsExtensions
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var stock = app.MapGroup("/internal/stock/reservations").WithTags("Internal stock").ExcludeFromDescription();
        stock.MapPost("/{orderId:guid}", async (Guid orderId, ReserveStockRequest request, ProductService service)
            => Results.Ok(await service.ReserveAsync(orderId, request)));
        stock.MapPut("/{orderId:guid}/confirm", async (Guid orderId, ProductService service) =>
        {
            await service.SetReservationStateAsync(orderId, "Committed");
            return Results.NoContent();
        });
        stock.MapPut("/{orderId:guid}/complete", async (Guid orderId, ProductService service) =>
        {
            await service.SetReservationStateAsync(orderId, "Completed");
            return Results.NoContent();
        });
        stock.MapDelete("/{orderId:guid}", async (Guid orderId, ProductService service) =>
        {
            await service.SetReservationStateAsync(orderId, "Released");
            return Results.NoContent();
        });
    }
}
