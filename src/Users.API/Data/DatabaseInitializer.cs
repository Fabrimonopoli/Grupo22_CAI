using Dapper;
using Microsoft.Data.Sqlite;

namespace Users.API.Data;

/// <summary>Crea el esquema y migra emails normalizados sin eliminar cuentas existentes.</summary>
public class DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
{
    public void Initialize()
    {
        using var connection = new SqliteConnection(config.GetConnectionString("DefaultConnection")
            ?? "Data Source=users.db;Foreign Keys=True;");
        connection.Open();
        using var transaction = connection.BeginTransaction(deferred: false);
        connection.Execute("""
            CREATE TABLE IF NOT EXISTS User (
                Id TEXT PRIMARY KEY, Nombre TEXT NOT NULL, Apellido TEXT NOT NULL,
                Email TEXT NOT NULL UNIQUE, NormalizedEmail TEXT NOT NULL, PasswordHash TEXT NOT NULL,
                FechaRegistro TEXT NOT NULL, Activo INTEGER NOT NULL CHECK (Activo IN (0,1)),
                IntentosFallidos INTEGER NOT NULL CHECK (IntentosFallidos >= 0)
            );
            """, transaction: transaction);
        var columns = connection.Query<ColumnInfo>("PRAGMA table_info(User);", transaction: transaction);
        if (!columns.Any(c => c.Name == "NormalizedEmail"))
            connection.Execute("ALTER TABLE User ADD COLUMN NormalizedEmail TEXT;", transaction: transaction);
        foreach (var row in connection.Query<EmailRow>("SELECT Id, Email, NormalizedEmail FROM User", transaction: transaction))
        {
            var normalized = UserRepository.NormalizeEmail(row.Email);
            if (normalized != row.NormalizedEmail)
                connection.Execute("UPDATE User SET NormalizedEmail=@normalized WHERE Id=@Id",
                    new
                    {
                        row.Id,
                        normalized
                    }, transaction);
        }
        var duplicates = connection.ExecuteScalar<long>("""
            SELECT COUNT(*) FROM (SELECT NormalizedEmail FROM User GROUP BY NormalizedEmail HAVING COUNT(*) > 1)
            """, transaction: transaction);
        if (duplicates > 0)
            throw new InvalidOperationException("La base contiene emails duplicados al normalizar. Deben resolverse sin eliminar cuentas automáticamente; la migración fue revertida.");
        connection.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_User_NormalizedEmail ON User(NormalizedEmail);", transaction: transaction);
        transaction.Commit();
        logger.LogInformation("Esquema de Users.API preparado sin eliminar usuarios existentes.");
    }

    private sealed class ColumnInfo
    {
        public string Name { get; set; } = "";
    }
    private sealed class EmailRow
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string? NormalizedEmail
        {
            get; set;
        }
    }
}
