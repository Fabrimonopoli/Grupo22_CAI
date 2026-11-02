using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
namespace Products.API.Data;

public sealed class StockReservationRepository(ProductRepository products)
{
    public async Task<StockReservationResponse> ReserveAsync(Guid orderId, IReadOnlyList<ReserveStockItem> items)
    {
        var fingerprint = string.Join(';', items.OrderBy(i => i.ProductoId).Select(i => $"{i.ProductoId:D}:{i.Cantidad}"));
        using var conn = products.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var existing = await conn.QuerySingleOrDefaultAsync<ReservationRow>("SELECT Estado,Fingerprint FROM StockReservation WHERE OrderId=@id", new
        {
            id = orderId.ToString()
        }, tx);
        if (existing is not null)
        {
            if (existing.Fingerprint != fingerprint || existing.Estado == "Released")
                throw new BusinessRuleException(ProductErrorCodes.Prd007, "La reserva ya fue liberada o tiene otros productos.");
            var snapshot = await ReadItems(conn, tx, orderId);
            tx.Commit();
            return new(orderId, existing.Estado, snapshot);
        }
        await conn.ExecuteAsync("INSERT INTO StockReservation(OrderId,Fingerprint,Estado,FechaCreacion) VALUES(@id,@fingerprint,'Reserved',@date)", new
        {
            id = orderId.ToString(),
            fingerprint,
            date = DateTime.UtcNow.ToString("O")
        }, tx);
        var reserved = new List<ReservedStockItem>();
        foreach (var item in items.OrderBy(i => i.ProductoId))
        {
            var product = await conn.QuerySingleOrDefaultAsync<Product>($"SELECT {ProductRepository.Columns} FROM Product WHERE Id=@id", new
            {
                id = item.ProductoId.ToString()
            }, tx)
                ?? throw new NotFoundException(ProductErrorCodes.Prd001, $"Producto '{item.ProductoId}' no encontrado.");
            var affected = await conn.ExecuteAsync("UPDATE Product SET Stock=Stock-@cantidad WHERE Id=@id AND Stock>=@cantidad", new
            {
                id = item.ProductoId.ToString(),
                cantidad = item.Cantidad
            }, tx);
            if (affected == 0)
                throw new BusinessRuleException(ProductErrorCodes.Prd006, $"Stock insuficiente para '{product.Nombre}'. Disponible: {product.Stock}, solicitado: {item.Cantidad}.");
            reserved.Add(new(product.Id, product.Nombre, item.Cantidad, product.Precio));
            await conn.ExecuteAsync("INSERT INTO StockReservationItem(OrderId,ProductoId,Nombre,Cantidad,PrecioUnitario) VALUES(@orderId,@productId,@nombre,@cantidad,@precio)",
                new
                {
                    orderId = orderId.ToString(),
                    productId = product.Id.ToString(),
                    nombre = product.Nombre,
                    cantidad = item.Cantidad,
                    precio = ProductRepository.Money(product.Precio)
                }, tx);
        }
        tx.Commit();
        return new(orderId, "Reserved", reserved);
    }
    public async Task SetStateAsync(Guid orderId, string action)
    {
        using var conn = products.CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var current = await conn.QuerySingleOrDefaultAsync<ReservationRow>("SELECT Estado,Fingerprint FROM StockReservation WHERE OrderId=@id", new
        {
            id = orderId.ToString()
        }, tx);
        if (action == "Released")
        {
            if (current?.Estado == "Completed")
                throw new BusinessRuleException(ProductErrorCodes.Prd007, "Una reserva despachada no se puede liberar.");
            if (current is null)
                // Tombstone prevents a delayed reserve from spending stock after cancellation.
                await conn.ExecuteAsync("INSERT INTO StockReservation(OrderId,Fingerprint,Estado,FechaCreacion) VALUES(@id,'','Released',@date)", new
                {
                    id = orderId.ToString(),
                    date = DateTime.UtcNow.ToString("O")
                }, tx);
            else if (current.Estado != "Released")
            {
                foreach (var item in await ReadItems(conn, tx, orderId))
                    await conn.ExecuteAsync("UPDATE Product SET Stock=Stock+@cantidad WHERE Id=@id", new
                    {
                        id = item.ProductoId.ToString(),
                        cantidad = item.Cantidad
                    }, tx);
                await conn.ExecuteAsync("UPDATE StockReservation SET Estado='Released' WHERE OrderId=@id", new
                {
                    id = orderId.ToString()
                }, tx);
            }
        }
        else
        {
            if (current is null)
                throw new NotFoundException(ProductErrorCodes.Prd001, "Reserva no encontrada.");
            if (current.Estado == "Released")
                throw new BusinessRuleException(ProductErrorCodes.Prd007, "La reserva fue liberada.");
            // A late confirmation cannot reopen a completed reservation.
            if (current.Estado != "Completed")
                await conn.ExecuteAsync("UPDATE StockReservation SET Estado=@action WHERE OrderId=@id", new
                {
                    id = orderId.ToString(),
                    action
                }, tx);
        }
        tx.Commit();
    }
    private static async Task<IReadOnlyList<ReservedStockItem>> ReadItems(SqliteConnection conn, SqliteTransaction tx, Guid id)
    {
        var rows = await conn.QueryAsync("SELECT ProductoId,Nombre,Cantidad,PrecioUnitario FROM StockReservationItem WHERE OrderId=@id ORDER BY ProductoId", new
        {
            id = id.ToString()
        }, tx);
        return rows.Select(r => new ReservedStockItem(Guid.Parse((string)r.ProductoId), (string)r.Nombre, Convert.ToInt32(r.Cantidad), decimal.Parse((string)r.PrecioUnitario, CultureInfo.InvariantCulture))).ToArray();
    }
    private sealed class ReservationRow
    {
        public string Estado { get; set; } = ""; public string Fingerprint { get; set; } = "";
    }
}
