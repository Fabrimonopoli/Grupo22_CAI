using Dapper;
using Microsoft.Data.Sqlite;

namespace Products.API.Data;

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
        var connectionString = _config.GetConnectionString("DefaultConnection") ?? "Data Source=products.db;Foreign Keys=True;";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS Product (
                Id              TEXT PRIMARY KEY,
                Nombre          TEXT NOT NULL,
                Descripcion     TEXT NULL,
                Precio          REAL NOT NULL,
                Stock           INTEGER NOT NULL,
                Categoria       TEXT NOT NULL,
                FechaCreacion   TEXT NOT NULL
            );");

        _logger.LogInformation("SQLite inicializado correctamente para Products.API → {db}", connectionString);
    }
}
