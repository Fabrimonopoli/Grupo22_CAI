using Dapper;
using Microsoft.Data.Sqlite;

namespace Users.API.Data;

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
        var connectionString = _config.GetConnectionString("DefaultConnection") ?? "Data Source=users.db;Foreign Keys=True;";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        connection.Execute(@"
            CREATE TABLE IF NOT EXISTS User (
                Id                  TEXT PRIMARY KEY,
                Nombre              TEXT NOT NULL,
                Apellido            TEXT NOT NULL,
                Email               TEXT NOT NULL UNIQUE,
                PasswordHash        TEXT NOT NULL,
                FechaRegistro       TEXT NOT NULL,
                Activo              INTEGER NOT NULL,
                IntentosFallidos    INTEGER NOT NULL
            );");

        _logger.LogInformation("SQLite inicializado correctamente para Users.API → {db}", connectionString);
    }
}
