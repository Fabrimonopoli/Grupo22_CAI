using CartModel = Cart.API.Models.Cart;
using CartItem = Cart.API.Models.CartItem;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

public class CartRepository
{
    private readonly IConfiguration _config;

    public CartRepository(IConfiguration config)
    {
        _config = config;
    }

    public async Task AddItemAsync(Guid userId, Guid productId, int cantidad)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            INSERT INTO CartItems (UserId, ProductId, Cantidad)
            VALUES (@UserId, @ProductId, @Cantidad)
            ON CONFLICT(UserId, ProductId) DO UPDATE SET
                Cantidad = CartItems.Cantidad + excluded.Cantidad;
            """,
            new
            {
                UserId = userId.ToString(),
                ProductId = productId.ToString(),
                Cantidad = cantidad
            });
    }

    public async Task UpdateItemQuantityAsync(Guid userId, Guid productId, int cantidad)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            UPDATE CartItems
            SET Cantidad = @Cantidad
            WHERE UserId = @UserId AND ProductId = @ProductId;
            """,
            new
            {
                UserId = userId.ToString(),
                ProductId = productId.ToString(),
                Cantidad = cantidad
            });
    }

    public async Task<bool> CartExistsAsync(Guid userId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM CartItems WHERE UserId = @UserId;",
            new { UserId = userId.ToString() });

        return count > 0;
    }

    public async Task<bool> ItemExistsAsync(Guid userId, Guid productId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var count = await connection.ExecuteScalarAsync<int>("""
            SELECT COUNT(1)
            FROM CartItems
            WHERE UserId = @UserId AND ProductId = @ProductId;
            """,
            new
            {
                UserId = userId.ToString(),
                ProductId = productId.ToString()
            });

        return count > 0;
    }

    public async Task<bool> RemoveItemAsync(Guid userId, Guid productId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var rows = await connection.ExecuteAsync("""
            DELETE FROM CartItems
            WHERE UserId = @UserId AND ProductId = @ProductId;
            """,
            new
            {
                UserId = userId.ToString(),
                ProductId = productId.ToString()
            });

        return rows > 0;
    }

    public async Task<bool> ClearCartAsync(Guid userId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var rows = await connection.ExecuteAsync(
            "DELETE FROM CartItems WHERE UserId = @UserId;",
            new { UserId = userId.ToString() });

        return rows > 0;
    }

    public async Task<CartModel> GetCartAsync(Guid userId)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync();

        var items = await connection.QueryAsync<CartItem>("""
            SELECT ProductId, Cantidad
            FROM CartItems
            WHERE UserId = @UserId;
            """,
            new { UserId = userId.ToString() });

        return new CartModel
        {
            UsuarioId = userId,
            FechaActualizacion = DateTime.UtcNow,
            Items = items.AsList()
        };
    }

    private SqliteConnection CreateConnection()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db;Foreign Keys=True;";

        return new SqliteConnection(connectionString);
    }
}
