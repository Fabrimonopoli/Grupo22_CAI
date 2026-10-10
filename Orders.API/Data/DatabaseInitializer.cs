using Dapper;
using Microsoft.Data.Sqlite;

namespace Orders.API.Data;

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
        var connectionString = _config.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db;Foreign Keys=True;";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS ""Order"" (
                Id                  TEXT PRIMARY KEY,
                UsuarioId           TEXT NOT NULL,
                Total               REAL NOT NULL,
                Estado              TEXT NOT NULL,
                FechaCreacion       TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS OrderItem (
                OrderId         TEXT NOT NULL,
                ProductoId      TEXT NOT NULL,
                Cantidad        INTEGER NOT NULL,
                PrecioUnitario  REAL NOT NULL,
                PRIMARY KEY (OrderId, ProductoId),
                FOREIGN KEY (OrderId) REFERENCES ""Order""(Id) ON DELETE CASCADE
            );");

        _logger.LogInformation("SQLite inicializado correctamente para Orders.API → {db}", connectionString);
    }
}
