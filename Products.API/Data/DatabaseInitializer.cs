using Dapper;
using Microsoft.Data.Sqlite;

namespace MiniApi.Data
{
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
                ?? "Data Source=app.db";

            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            // ALUMNO: en caso de tener que agregar otra tabla es duplicar este comando
            connection.Execute("""
            CREATE TABLE IF NOT EXISTS items (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                name        TEXT    NOT NULL,
                description TEXT,
                price       REAL    NOT NULL DEFAULT 0,
                stock       INTEGER NOT NULL DEFAULT 0,
                created_at  TEXT    NOT NULL DEFAULT (datetime('now')),
                updated_at  TEXT
            );
        """);

            // Creación de la tabla Producto
            connection.Execute("""
            CREATE TABLE IF NOT EXISTS Products (
            Id TEXT PRIMARY KEY,
            Nombre TEXT NOT NULL,
            Descripcion TEXT,
            Precio REAL NOT NULL,
            Stock INTEGER NOT NULL,
            Categoria TEXT NOT NULL,
            FechaCreacion TEXT NOT NULL
            );
        """);



            _logger.LogInformation("SQLite inicializado correctamente → {db}", connectionString);
        }
    }
}