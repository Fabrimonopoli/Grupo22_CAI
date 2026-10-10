using Dapper;
using Microsoft.Data.Sqlite;

namespace Notifications.API.Data;

public sealed class DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
{
    public void Initialize()
    {
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? "Data Source=notifications.db;Foreign Keys=True;";
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Notification (
                Id TEXT PRIMARY KEY,
                UsuarioId TEXT NOT NULL,
                Tipo TEXT NOT NULL,
                Mensaje TEXT NOT NULL,
                FechaCreacion TEXT NOT NULL
            );
            """);
        logger.LogInformation("SQLite inicializado correctamente para Notifications.API → {db}", connectionString);
    }
}
