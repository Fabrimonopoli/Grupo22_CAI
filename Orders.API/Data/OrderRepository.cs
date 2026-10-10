using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.Models;

namespace Orders.API.Data;

public class OrderRepository
{
    private readonly IConfiguration _config;
    public OrderRepository(IConfiguration config) => _config = config;

    private SqliteConnection CreateConnection() => new(_config.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db;Foreign Keys=True;");

    public async Task<IEnumerable<Order>> GetAllAsync(Guid? usuarioId = null)
    {
        using var conn = CreateConnection();
        List<Order> orderList;

        if (usuarioId.HasValue)
        {
            var sqlWithFilter = @"SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM ""Order"" WHERE UsuarioId = @usuarioId ORDER BY FechaCreacion DESC";
            var result = await conn.QueryAsync<Order>(sqlWithFilter, new { usuarioId = usuarioId.Value.ToString() });
            orderList = result.ToList();
        }
        else
        {
            var sqlAll = @"SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM ""Order"" ORDER BY FechaCreacion DESC";
            var result = await conn.QueryAsync<Order>(sqlAll);
            orderList = result.ToList();
        }

        if (orderList.Any())
        {
            var orderIds = orderList.Select(o => o.Id.ToString()).ToList();
            var itemsSql = @"SELECT OrderId, ProductoId, Cantidad, PrecioUnitario FROM OrderItem WHERE OrderId IN @orderIds";
            var items = (await conn.QueryAsync<OrderItem>(itemsSql, new { orderIds })).ToList();

            foreach (var order in orderList)
            {
                order.Items = items.Where(i => i.OrderId == order.Id).ToList();
            }
        }

        return orderList;
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        var order = await conn.QuerySingleOrDefaultAsync<Order>(@"SELECT Id, UsuarioId, Total, Estado, FechaCreacion FROM ""Order"" WHERE Id = @id", new { id = id.ToString() });

        if (order != null)
        {
            var items = await conn.QueryAsync<OrderItem>(@"SELECT OrderId, ProductoId, Cantidad, PrecioUnitario FROM OrderItem WHERE OrderId = @id", new { id = id.ToString() });
            order.Items = items.ToList();
        }

        return order;
    }

    public async Task CreateAsync(Order order)
    {
        using var conn = CreateConnection();
        await conn.ExecuteAsync(@"INSERT INTO ""Order"" (Id, UsuarioId, Total, Estado, FechaCreacion) VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion)", new
        {
            Id = order.Id.ToString(),
            UsuarioId = order.UsuarioId.ToString(),
            order.Total,
            order.Estado,
            FechaCreacion = order.FechaCreacion.ToString("O")
        });

        if (order.Items.Any())
        {
            var itemsToInsert = order.Items.Select(i => new
            {
                OrderId = order.Id.ToString(),
                ProductoId = i.ProductoId.ToString(),
                i.Cantidad,
                i.PrecioUnitario
            });

            await conn.ExecuteAsync(@"INSERT INTO OrderItem (OrderId, ProductoId, Cantidad, PrecioUnitario) VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario)", itemsToInsert);
        }
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string estado)
    {
        using var conn = CreateConnection();
        var rows = await conn.ExecuteAsync(@"UPDATE ""Order"" SET Estado = @estado WHERE Id = @id", new { estado, id = id.ToString() });
        return rows > 0;
    }
    public async Task<bool> HasActiveOrdersForProductAsync(Guid productId)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT COUNT(1) 
            FROM ""Order"" o
            INNER JOIN OrderItem oi ON o.Id = oi.OrderId
            WHERE oi.ProductoId = @productId 
              AND o.Estado IN ('Pendiente', 'Confirmada')
        ";
        var count = await conn.ExecuteScalarAsync<int>(sql, new { productId = productId.ToString() });
        return count > 0;
    }
}
