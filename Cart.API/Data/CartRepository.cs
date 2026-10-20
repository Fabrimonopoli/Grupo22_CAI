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
