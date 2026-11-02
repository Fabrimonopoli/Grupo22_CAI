namespace Products.API.DTOs;
/// <summary>Reserva atómica e idempotente utilizada por Orders.API.</summary>
public sealed class ReserveStockRequest
{
    public List<ReserveStockItem> Items { get; set; } = [];
}
public sealed record ReserveStockItem(Guid ProductoId, int Cantidad);
public sealed record ReservedStockItem(Guid ProductoId, string Nombre, int Cantidad, decimal PrecioUnitario);
public sealed record StockReservationResponse(Guid OrderId, string Estado, IReadOnlyList<ReservedStockItem> Items);
