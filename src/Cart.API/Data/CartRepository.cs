using System.Globalization;
using Cart.API.Exceptions;
using CartItem = Cart.API.Models.CartItem;
using CartModel = Cart.API.Models.Cart;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

/// <summary>Modifica cantidades y fecha en una transacción local, sin alterar el carrito al leer.</summary>
public sealed class CartRepository(IConfiguration config)
{
    private SqliteConnection CreateConnection() => new(config.GetConnectionString("DefaultConnection")
        ?? "Data Source=cart.db;Foreign Keys=True;");

    public async Task<CartModel> GetCartAsync(Guid userId)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        // Una única vista coherente de los ítems y de su fecha persistida.
        using var transaction = connection.BeginTransaction(deferred: true);
        return await ReadAsync(connection, transaction, userId);
    }

    public async Task<CartModel> AddItemAsync(Guid userId, Guid productId, int quantity, int availableStock)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(deferred: false);
        var current = await connection.ExecuteScalarAsync<int?>("""
            SELECT Cantidad FROM CartItems WHERE UserId=@userId AND ProductId=@productId
            """, new
        {
            userId = userId.ToString(),
            productId = productId.ToString()
        }, transaction) ?? 0;
        var total = (long)current + quantity;
        if (total > availableStock || total > int.MaxValue)
            throw InsufficientStock(availableStock, total);
        await TouchAsync(connection, transaction, userId);
        await connection.ExecuteAsync("""
            INSERT INTO CartItems (UserId, ProductId, Cantidad) VALUES (@userId, @productId, @quantity)
            ON CONFLICT(UserId, ProductId) DO UPDATE SET Cantidad=excluded.Cantidad
            """, new
        {
            userId = userId.ToString(),
            productId = productId.ToString(),
            quantity = (int)total
        }, transaction);
        var cart = await ReadAsync(connection, transaction, userId);
        transaction.Commit();
        return cart;
    }

    public async Task<CartModel> UpdateItemQuantityAsync(Guid userId, Guid productId, int quantity, int availableStock)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(deferred: false);
        if (quantity > availableStock)
            throw InsufficientStock(availableStock, quantity);
        var rows = await connection.ExecuteAsync("""
            UPDATE CartItems SET Cantidad=@quantity WHERE UserId=@userId AND ProductId=@productId
            """, new
        {
            userId = userId.ToString(),
            productId = productId.ToString(),
            quantity
        }, transaction);
        if (rows == 0)
            throw new NotFoundException("CRT-001", "Carrito o producto del carrito no encontrado.");
        await TouchAsync(connection, transaction, userId);
        var cart = await ReadAsync(connection, transaction, userId);
        transaction.Commit();
        return cart;
    }

    public async Task RemoveItemAsync(Guid userId, Guid productId)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(deferred: false);
        var rows = await connection.ExecuteAsync("DELETE FROM CartItems WHERE UserId=@userId AND ProductId=@productId",
            new
            {
                userId = userId.ToString(),
                productId = productId.ToString()
            }, transaction);
        if (rows == 0)
            throw new NotFoundException("CRT-001", "Carrito o producto del carrito no encontrado.");
        await TouchAsync(connection, transaction, userId);
        transaction.Commit();
    }

    public async Task ClearCartAsync(Guid userId)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(deferred: false);
        var rows = await connection.ExecuteAsync("DELETE FROM CartItems WHERE UserId=@userId",
            new
            {
                userId = userId.ToString()
            }, transaction);
        if (rows == 0)
            throw new NotFoundException("CRT-001", "Carrito no encontrado.");
        await TouchAsync(connection, transaction, userId);
        transaction.Commit();
    }

    private static async Task<CartModel> ReadAsync(SqliteConnection connection, SqliteTransaction transaction, Guid userId)
    {
        var items = (await connection.QueryAsync<CartItem>("""
            SELECT ProductId, Cantidad FROM CartItems WHERE UserId=@userId ORDER BY ProductId
            """, new
        {
            userId = userId.ToString()
        }, transaction)).AsList();
        if (items.Count == 0)
            throw new NotFoundException("CRT-001", "Carrito no encontrado.");
        var updated = await connection.QuerySingleAsync<string>(
            "SELECT FechaActualizacion FROM CartState WHERE UserId=@userId", new
            {
                userId = userId.ToString()
            }, transaction);
        return new CartModel
        {
            UsuarioId = userId,
            Items = items,
            FechaActualizacion = DateTime.Parse(updated, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }

    private static Task TouchAsync(SqliteConnection connection, SqliteTransaction transaction, Guid userId) =>
        connection.ExecuteAsync("""
            INSERT INTO CartState (UserId, FechaActualizacion) VALUES (@userId, @updated)
            ON CONFLICT(UserId) DO UPDATE SET FechaActualizacion=excluded.FechaActualizacion
            """, new
        {
            userId = userId.ToString(),
            updated = DateTime.UtcNow.ToString("O")
        }, transaction);

    private static BusinessRuleException InsufficientStock(int available, long requested) =>
        new("CRT-003", $"Stock insuficiente. Disponible: {available}, solicitado en total: {requested}.");
}
