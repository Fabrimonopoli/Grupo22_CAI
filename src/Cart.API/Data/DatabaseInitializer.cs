using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

/// <summary>Prepara el carrito y agrega fecha persistida a bases anteriores sin borrar ítems.</summary>
public sealed class DatabaseInitializer(IConfiguration config)
{
    public async Task InitializeAsync()
    {
        using var connection = new SqliteConnection(config.GetConnectionString("DefaultConnection")
            ?? "Data Source=cart.db;Foreign Keys=True;");
        await connection.OpenAsync();
        using var transaction = connection.BeginTransaction(deferred: false);
        await connection.ExecuteAsync("""
            CREATE TABLE IF NOT EXISTS CartState (
                UserId TEXT PRIMARY KEY NOT NULL,
                FechaActualizacion TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS CartItems (
                UserId TEXT NOT NULL,
                ProductId TEXT NOT NULL,
                Cantidad INTEGER NOT NULL CHECK (Cantidad > 0 AND Cantidad <= 2147483647),
                PRIMARY KEY (UserId, ProductId),
                FOREIGN KEY (UserId) REFERENCES CartState(UserId)
            );
            CREATE TRIGGER IF NOT EXISTS CartItems_ValidInsert BEFORE INSERT ON CartItems
            WHEN NEW.UserId IS NULL OR NEW.ProductId IS NULL OR NEW.Cantidad IS NULL
                OR typeof(NEW.Cantidad) <> 'integer' OR NEW.Cantidad <= 0 OR NEW.Cantidad > 2147483647
            BEGIN SELECT RAISE(ABORT, 'Datos de carrito inválidos'); END;
            CREATE TRIGGER IF NOT EXISTS CartItems_ValidUpdate BEFORE UPDATE ON CartItems
            WHEN NEW.UserId IS NULL OR NEW.ProductId IS NULL OR NEW.Cantidad IS NULL
                OR typeof(NEW.Cantidad) <> 'integer' OR NEW.Cantidad <= 0 OR NEW.Cantidad > 2147483647
            BEGIN SELECT RAISE(ABORT, 'Datos de carrito inválidos'); END;
            """, transaction: transaction);
        // La fecha histórica no estaba almacenada; se fija una sola vez al migrar, no en cada GET.
        await connection.ExecuteAsync("""
            INSERT OR IGNORE INTO CartState (UserId, FechaActualizacion)
            SELECT DISTINCT UserId, @now FROM CartItems WHERE UserId IS NOT NULL
            """, new
        {
            now = DateTime.UtcNow.ToString("O")
        }, transaction);
        transaction.Commit();
    }
}
