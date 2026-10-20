using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

public class DatabaseInitializer
{
    private readonly IConfiguration _config;

    public DatabaseInitializer(IConfiguration config)
    {
        _config = config;
    }

    public async Task InitializeAsync()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db;Foreign Keys=True;";

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS CartItems (
                UserId TEXT,
                ProductId TEXT,
                Cantidad INTEGER,
                PRIMARY KEY (UserId, ProductId)
            );
            """);
    }
}
