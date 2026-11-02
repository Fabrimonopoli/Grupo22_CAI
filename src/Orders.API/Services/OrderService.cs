using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;
namespace Orders.API.Services;

public sealed class OrderService(OrderRepository repo, UsersApiClient users, ProductsApiClient products, ILogger<OrderService> logger)
{
    public Task<IEnumerable<Order>> GetAllAsync(Guid? usuarioId) => repo.GetAllAsync(usuarioId);
    public async Task<Order> GetAsync(Guid id) => await repo.GetByIdAsync(id) ?? throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
    public async Task<Order> CreateAsync(CreateOrderRequest request, Guid orderId)
    {
        var normalized = Normalize(request);
        var json = JsonSerializer.Serialize(normalized);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
        var intent = await repo.GetOrCreateIntentAsync(orderId, fingerprint, json);
        if (intent.Fingerprint != fingerprint)
            throw new BusinessRuleException(OrderErrorCodes.Ord008, "Idempotency-Key ya fue utilizado con otros datos.");
        return await ResumeAsync(intent);
    }
    public async Task<Order> ResumeAsync(CreationIntent intent)
    {
        var id = Guid.Parse(intent.OrderId);
        if (intent.Estado == "Completed")
            return await GetAsync(id);
        if (intent.Estado == "Failed")
            ThrowStored(intent);
        try
        {
            var request = JsonSerializer.Deserialize<CreateOrderRequest>(intent.RequestJson) ?? throw new InvalidDataException("Intento de orden sin datos.");
            var user = await users.GetByIdAsync(request.UsuarioId);
            if (user is null || !user.Activo)
                throw new NotFoundException(OrderErrorCodes.Ord003, "Usuario no encontrado o inactivo al crear la orden.");
            var reservation = await products.ReserveAsync(id, request.Items.Select(i => new StockRequestItem(i.ProductoId, i.Cantidad)).ToList());
            var order = new Order
            {
                Id = id,
                UsuarioId = request.UsuarioId,
                Estado = "Pendiente",
                FechaCreacion = intent.FechaCreacion.ToUniversalTime(),
                Items = reservation.Items.Select(i => new OrderItem { OrderId = id, ProductoId = i.ProductoId, Cantidad = i.Cantidad, PrecioUnitario = i.PrecioUnitario }).ToList()
            };
            try
            {
                order.Total = checked(order.Items.Sum(i => checked(i.Cantidad * i.PrecioUnitario)));
            }
            catch (OverflowException) { throw new BusinessRuleException(OrderErrorCodes.Ord002, "El total de la orden supera el máximo permitido."); }
            await repo.SaveIntentOrderAsync(order);
            await FlushStockActionsAsync();
            return await GetAsync(id);
        }
        catch (Exception ex)
        {
            var (code, message) = ex switch
            {
                NotFoundException e => (e.ErrorCode, e.Message),
                BusinessRuleException e => (e.ErrorCode, e.Message),
                _ => (OrderErrorCodes.Ord007, "No se pudo completar la orden; su compensación se procesa de forma durable.")
            };
            // Decide under the same DB write lock as SaveIntentOrderAsync. Never refund a committed order after an ambiguous commit.
            if (!await repo.FailIfUncommittedAsync(id, code, message))
                return await GetAsync(id);
            await FlushStockActionsAsync();
            throw;
        }
    }
    public async Task<UpdateOrderStatusResponse> UpdateStatusAsync(Guid id, UpdateOrderStatusRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Estado))
            throw new BusinessRuleException(OrderErrorCodes.Ord002, "El estado es requerido.");
        var target = request.Estado.Trim();
        var valid = new[] { "Pendiente", "Confirmada", "Enviada", "Entregada", "Cancelada" };
        if (!valid.Contains(target))
            throw new BusinessRuleException(OrderErrorCodes.Ord002, "El estado indicado no es válido.");
        var order = await GetAsync(id);
        if (order.Estado == target)
            return new()
            {
                Id = id,
                Estado = target,
                FechaActualizacion = order.FechaActualizacion ?? order.FechaCreacion
            };
        var allowed = order.Estado switch
        {
            "Pendiente" => new[] { "Confirmada", "Cancelada" },
            "Confirmada" => new[] { "Enviada", "Cancelada" },
            "Enviada" => new[] { "Entregada" },
            _ => Array.Empty<string>()
        };
        if (!allowed.Contains(target))
            throw new BusinessRuleException(OrderErrorCodes.Ord006, $"Una orden en estado '{order.Estado}' no puede cambiar a '{target}'.");
        if (!await repo.UpdateStatusAsync(id, order.Estado, target))
            throw new BusinessRuleException(OrderErrorCodes.Ord006, "La orden cambió mientras se procesaba la solicitud. Volvé a consultarla.");
        await FlushStockActionsAsync();
        var updated = await GetAsync(id);
        return new()
        {
            Id = id,
            Estado = updated.Estado,
            FechaActualizacion = updated.FechaActualizacion ?? DateTime.UtcNow
        };
    }
    public async Task FlushStockActionsAsync()
    {
        foreach (var action in await repo.GetStockActionsAsync())
        {
            try
            {
                await products.ApplyActionAsync(Guid.Parse(action.OrderId), action.Action);
                await repo.RemoveStockActionAsync(action);
            }
            catch (Exception ex) { logger.LogWarning(ex, "Acción de inventario pendiente {OrderId} {Action} {ErrorCode}", action.OrderId, action.Action, OrderErrorCodes.Ord007); }
        }
    }
    public static Guid ParseId(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
    public static Guid ParseIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Guid.NewGuid();
        return Guid.TryParse(key, out var id) && id != Guid.Empty ? id : throw new BusinessRuleException(OrderErrorCodes.Ord002, "Idempotency-Key debe ser un GUID no vacío.");
    }
    private static CreateOrderRequest Normalize(CreateOrderRequest request)
    {
        if (request is null || request.UsuarioId == Guid.Empty || request.Items is null || request.Items.Count == 0 || request.Items.Any(i => i is null || i.ProductoId == Guid.Empty || i.Cantidad <= 0))
            throw new BusinessRuleException(OrderErrorCodes.Ord002, "La orden debe incluir UsuarioId, productos y cantidades positivas válidas.");
        try
        {
            return new()
            {
                UsuarioId = request.UsuarioId,
                Items = request.Items.GroupBy(i => i.ProductoId).OrderBy(g => g.Key).Select(g => new CreateOrderItemRequest { ProductoId = g.Key, Cantidad = checked(g.Sum(i => i.Cantidad)) }).ToList()
            };
        }
        catch (OverflowException) { throw new BusinessRuleException(OrderErrorCodes.Ord002, "La cantidad acumulada supera el máximo permitido."); }
    }
    private static void ThrowStored(CreationIntent intent)
    {
        var code = intent.ErrorCode ?? OrderErrorCodes.Ord007;
        var message = intent.ErrorMessage ?? "La creación de esta orden falló.";
        if (code is OrderErrorCodes.Ord003 or OrderErrorCodes.Ord004)
            throw new NotFoundException(code, message);
        throw new BusinessRuleException(code, message);
    }
}
