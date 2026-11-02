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
        using var transaction = connection.BeginTransaction();
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS Notification (
                Id TEXT PRIMARY KEY,
                UsuarioId TEXT NOT NULL,
                Tipo TEXT NOT NULL CHECK (Tipo IN ('Email', 'Push', 'SMS')),
                Mensaje TEXT NOT NULL CHECK (length(Mensaje) BETWEEN 1 AND 500),
                Estado TEXT NOT NULL CHECK (Estado IN ('Pendiente', 'Enviada', 'Fallida')),
                FechaEnvio TEXT NOT NULL
            );
            """, transaction: transaction);

        // Las bases originales guardaban FechaCreacion y no tenían estado. Se conserva
        // su fecha y no se inventa un envío real para esos registros históricos.
        var columns = connection.Query<TableColumn>("PRAGMA table_info(Notification);", transaction: transaction)
            .Select(column => column.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!columns.Contains("FechaEnvio") && columns.Contains("FechaCreacion"))
        {
            connection.Execute("ALTER TABLE Notification RENAME COLUMN FechaCreacion TO FechaEnvio;", transaction: transaction);
            columns.Add("FechaEnvio");
        }
        if (!columns.Contains("FechaEnvio"))
            throw new InvalidOperationException("El esquema de Notification no contiene una fecha compatible para migrar.");
        if (!columns.Contains("Estado"))
            connection.Execute("ALTER TABLE Notification ADD COLUMN Estado TEXT NOT NULL DEFAULT 'Pendiente';", transaction: transaction);

        connection.Execute("CREATE INDEX IF NOT EXISTS IX_Notification_UsuarioId_FechaEnvio ON Notification (UsuarioId, FechaEnvio DESC);", transaction: transaction);
        transaction.Commit();
        logger.LogInformation("Persistencia de Notifications.API inicializada y migraciones aplicadas.");
    }

    private sealed class TableColumn
    {
        public string Name { get; set; } = string.Empty;
    }
}
