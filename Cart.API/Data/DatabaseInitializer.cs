using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

public class DatabaseInitializer
{
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
    {
        _config = config;
        _logger = logger;
    }

    public void Initialize()
    {
        var connectionString = _config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db;Foreign Keys=True;";

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        connection.Execute("""
            CREATE TABLE IF NOT EXISTS CartItems (
                UserId TEXT,
                ProductId TEXT,
                Cantidad INTEGER,
                PRIMARY KEY (UserId, ProductId)
            );
            """);

        _logger.LogInformation("SQLite inicializado correctamente para Cart.API → {db}", connectionString);
    }
}