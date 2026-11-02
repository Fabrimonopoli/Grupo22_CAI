using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.Exceptions;
using Orders.API.Models;
namespace Orders.API.Data;

public sealed class CreationIntent
{
    public string OrderId { get; set; } = ""; public string Fingerprint { get; set; } = ""; public string RequestJson { get; set; } = "";
    public string Estado { get; set; } = ""; public string? ErrorCode
    {
        get; set;
    }
    public string? ErrorMessage
    {
        get; set;
    }
    public DateTime FechaCreacion
    {
        get; set;
    }
}
public sealed class StockAction
{
    public string OrderId { get; set; } = ""; public string Action { get; set; } = "";
}
public class OrderRepository(IConfiguration config)
{
    private const string Columns = "Id,UsuarioId,COALESCE(TotalExacto,CAST(Total AS TEXT)) AS Total,Estado,FechaCreacion,FechaActualizacion";
    private const string ItemColumns = "OrderId,ProductoId,Cantidad,COALESCE(PrecioUnitarioExacto,CAST(PrecioUnitario AS TEXT)) AS PrecioUnitario";
    private SqliteConnection CreateConnection() => new(config.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db;Foreign Keys=True;");
    public async Task<IEnumerable<Order>> GetAllAsync(Guid? usuarioId = null)
    {
        using var conn = CreateConnection();
        var orders = (await conn.QueryAsync<Order>($"SELECT {Columns} FROM \"Order\" WHERE (@userId IS NULL OR UsuarioId=@userId) ORDER BY FechaCreacion DESC", new
        {
            userId = usuarioId?.ToString()
        })).ToList();
        if (orders.Count > 0)
        {
            var ids = orders.Select(o => o.Id.ToString()).ToArray();
            var items = (await conn.QueryAsync<OrderItem>($"SELECT {ItemColumns} FROM OrderItem WHERE OrderId IN @ids", new
            {
                ids
            })).ToLookup(i => i.OrderId);
            foreach (var order in orders)
                order.Items = items[order.Id].ToList();
        }
        return orders;
    }
    public async Task<Order?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        var order = await conn.QuerySingleOrDefaultAsync<Order>($"SELECT {Columns} FROM \"Order\" WHERE Id=@id", new
        {
            id = id.ToString()
        });
        if (order is not null)
            order.Items = (await conn.QueryAsync<OrderItem>($"SELECT {ItemColumns} FROM OrderItem WHERE OrderId=@id", new
            {
                id = id.ToString()
            })).ToList();
        return order;
    }
    public async Task<CreationIntent> GetOrCreateIntentAsync(Guid id, string fingerprint, string requestJson)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        await conn.ExecuteAsync("""
            INSERT INTO OrderCreationIntent(OrderId,Fingerprint,RequestJson,Estado,FechaCreacion) VALUES(@id,@fingerprint,@requestJson,'Processing',@date)
            ON CONFLICT(OrderId) DO NOTHING
            """, new
        {
            id = id.ToString(),
            fingerprint,
            requestJson,
            date = DateTime.UtcNow.ToString("O")
        }, tx);
        var row = await conn.QuerySingleAsync<CreationIntent>("SELECT * FROM OrderCreationIntent WHERE OrderId=@id", new
        {
            id = id.ToString()
        }, tx);
        tx.Commit();
        return row;
    }
    public async Task SaveIntentOrderAsync(Order order)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var intent = await conn.QuerySingleAsync<CreationIntent>("SELECT * FROM OrderCreationIntent WHERE OrderId=@id", new
        {
            id = order.Id.ToString()
        }, tx);
        if (intent.Estado == "Completed")
        {
            tx.Commit();
            return;
        }
        if (intent.Estado == "Failed")
            throw new BusinessRuleException(intent.ErrorCode ?? OrderErrorCodes.Ord007, intent.ErrorMessage ?? "La creación fue cancelada.");
        await conn.ExecuteAsync("""
            INSERT INTO "Order"(Id,UsuarioId,Total,TotalExacto,Estado,FechaCreacion) VALUES(@id,@userId,@total,@total,@estado,@date)
            """, new
        {
            id = order.Id.ToString(),
            userId = order.UsuarioId.ToString(),
            total = Money(order.Total),
            estado = order.Estado,
            date = order.FechaCreacion.ToString("O")
        }, tx);
        foreach (var item in order.Items)
            await conn.ExecuteAsync("""
                INSERT INTO OrderItem(OrderId,ProductoId,Cantidad,PrecioUnitario,PrecioUnitarioExacto) VALUES(@orderId,@productId,@cantidad,@precio,@precio)
                """, new
            {
                orderId = order.Id.ToString(),
                productId = item.ProductoId.ToString(),
                cantidad = item.Cantidad,
                precio = Money(item.PrecioUnitario)
            }, tx);
        await conn.ExecuteAsync("UPDATE OrderCreationIntent SET Estado='Completed' WHERE OrderId=@id", new
        {
            id = order.Id.ToString()
        }, tx);
        await QueueAction(conn, tx, order.Id, "confirm");
        tx.Commit();
    }
    public async Task<bool> FailIfUncommittedAsync(Guid id, string code, string message)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var state = await conn.ExecuteScalarAsync<string>("SELECT Estado FROM OrderCreationIntent WHERE OrderId=@id", new
        {
            id = id.ToString()
        }, tx);
        if (state == "Failed")
        {
            tx.Commit();
            return true;
        }
        if (state == "Completed" || await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM \"Order\" WHERE Id=@id", new
        {
            id = id.ToString()
        }, tx) > 0)
        {
            tx.Commit();
            return false;
        }
        await conn.ExecuteAsync("UPDATE OrderCreationIntent SET Estado='Failed',ErrorCode=@code,ErrorMessage=@message WHERE OrderId=@id", new
        {
            id = id.ToString(),
            code,
            message
        }, tx);
        await QueueAction(conn, tx, id, "release");
        tx.Commit();
        return true;
    }
    public async Task<bool> UpdateStatusAsync(Guid id, string expected, string estado)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var rows = await conn.ExecuteAsync("UPDATE \"Order\" SET Estado=@estado,FechaActualizacion=@date WHERE Id=@id AND Estado=@expected", new
        {
            id = id.ToString(),
            expected,
            estado,
            date = DateTime.UtcNow.ToString("O")
        }, tx);
        if (rows > 0)
        {
            // Legacy orders predate reservations; do not invent a stock refund for them.
            if (await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM OrderCreationIntent WHERE OrderId=@id AND Estado='Completed'", new
            {
                id = id.ToString()
            }, tx) > 0)
                await QueueAction(conn, tx, id, estado == "Cancelada" ? "release" : estado is "Enviada" or "Entregada" ? "complete" : "confirm");
        }
        tx.Commit();
        return rows > 0;
    }
    public async Task<bool> HasActiveOrdersForProductAsync(Guid productId)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM "Order" o JOIN OrderItem i ON i.OrderId=o.Id WHERE i.ProductoId=@id AND o.Estado IN ('Pendiente','Confirmada')
            """, new
        {
            id = productId.ToString()
        }) > 0;
    }
    public async Task<IReadOnlyList<CreationIntent>> GetPendingIntentsAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<CreationIntent>("SELECT * FROM OrderCreationIntent WHERE Estado='Processing' ORDER BY FechaCreacion LIMIT 20")).ToList();
    }
    public async Task<IReadOnlyList<StockAction>> GetStockActionsAsync()
    {
        using var conn = CreateConnection();
        return (await conn.QueryAsync<StockAction>("SELECT OrderId,Action FROM OrderStockAction LIMIT 50")).ToList();
    }
    public async Task RemoveStockActionAsync(StockAction action)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync("DELETE FROM OrderStockAction WHERE OrderId=@id AND Action=@action", new
        {
            id = action.OrderId,
            action = action.Action
        });
    }
    private static Task QueueAction(SqliteConnection conn, SqliteTransaction tx, Guid id, string action) => conn.ExecuteAsync("""
        INSERT INTO OrderStockAction(OrderId,Action) VALUES(@id,@action) ON CONFLICT(OrderId) DO UPDATE SET Action=excluded.Action
        """, new { id = id.ToString(), action }, tx);
    private static string Money(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
}
