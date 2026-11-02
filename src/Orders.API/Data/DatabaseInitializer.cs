using Dapper;
using Microsoft.Data.Sqlite;
namespace Orders.API.Data;

public class DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
{
    public void Initialize()
    {
        using var conn = new SqliteConnection(config.GetConnectionString("DefaultConnection") ?? "Data Source=orders.db;Foreign Keys=True;");
        conn.Open();
        using var tx = conn.BeginTransaction(deferred: false);
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS "Order" (Id TEXT PRIMARY KEY,UsuarioId TEXT NOT NULL,Total TEXT NOT NULL,Estado TEXT NOT NULL,FechaCreacion TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS OrderItem (OrderId TEXT NOT NULL,ProductoId TEXT NOT NULL,Cantidad INTEGER NOT NULL CHECK(Cantidad>0),PrecioUnitario TEXT NOT NULL,
                PRIMARY KEY(OrderId,ProductoId),FOREIGN KEY(OrderId) REFERENCES "Order"(Id) ON DELETE CASCADE);
            CREATE TABLE IF NOT EXISTS OrderCreationIntent (OrderId TEXT PRIMARY KEY,Fingerprint TEXT NOT NULL,RequestJson TEXT NOT NULL,
                Estado TEXT NOT NULL,ErrorCode TEXT NULL,ErrorMessage TEXT NULL,FechaCreacion TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS OrderStockAction (OrderId TEXT PRIMARY KEY,Action TEXT NOT NULL);
            """, transaction: tx);
        AddColumn(conn, tx, "Order", "TotalExacto");
        AddColumn(conn, tx, "Order", "FechaActualizacion");
        AddColumn(conn, tx, "OrderItem", "PrecioUnitarioExacto");
        conn.Execute("""
            UPDATE "Order" SET TotalExacto=CAST(Total AS TEXT) WHERE TotalExacto IS NULL;
            UPDATE OrderItem SET PrecioUnitarioExacto=CAST(PrecioUnitario AS TEXT) WHERE PrecioUnitarioExacto IS NULL;
            CREATE TRIGGER IF NOT EXISTS OrderItem_Insert_Validation BEFORE INSERT ON OrderItem
            WHEN NEW.Cantidad<=0 OR NEW.Cantidad>2147483647 BEGIN SELECT RAISE(ABORT,'Cantidad de orden inválida'); END;
            CREATE TRIGGER IF NOT EXISTS OrderItem_Update_Validation BEFORE UPDATE ON OrderItem
            WHEN NEW.Cantidad<=0 OR NEW.Cantidad>2147483647 BEGIN SELECT RAISE(ABORT,'Cantidad de orden inválida'); END;
            CREATE INDEX IF NOT EXISTS IX_Order_User ON "Order"(UsuarioId);
            CREATE INDEX IF NOT EXISTS IX_OrderItem_Product ON OrderItem(ProductoId);
            """, transaction: tx);
        tx.Commit();
        logger.LogInformation("Persistencia de Orders inicializada y migrada.");
    }
    private static void AddColumn(SqliteConnection conn, SqliteTransaction tx, string table, string column)
    {
        if (!conn.Query($"PRAGMA table_info(\"{table}\")", transaction: tx).Any(r => (string)r.name == column))
            conn.Execute($"ALTER TABLE \"{table}\" ADD COLUMN {column} TEXT NULL", transaction: tx);
    }
}
