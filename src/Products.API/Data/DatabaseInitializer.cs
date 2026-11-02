using Dapper;
using Microsoft.Data.Sqlite;
namespace Products.API.Data;

public class DatabaseInitializer(IConfiguration config, ILogger<DatabaseInitializer> logger)
{
    public void Initialize()
    {
        using var conn = new SqliteConnection(config.GetConnectionString("DefaultConnection") ?? "Data Source=products.db;Foreign Keys=True;");
        conn.Open();
        using var tx = conn.BeginTransaction(deferred: false);
        conn.Execute("""
            CREATE TABLE IF NOT EXISTS Product (Id TEXT PRIMARY KEY, Nombre TEXT NOT NULL, Descripcion TEXT NULL, Precio TEXT NOT NULL,
                Stock INTEGER NOT NULL CHECK(Stock BETWEEN 0 AND 2147483647), Categoria TEXT NOT NULL, FechaCreacion TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS StockReservation (OrderId TEXT PRIMARY KEY, Fingerprint TEXT NOT NULL, Estado TEXT NOT NULL, FechaCreacion TEXT NOT NULL);
            CREATE TABLE IF NOT EXISTS StockReservationItem (OrderId TEXT NOT NULL, ProductoId TEXT NOT NULL, Nombre TEXT NOT NULL,
                Cantidad INTEGER NOT NULL CHECK(Cantidad>0), PrecioUnitario TEXT NOT NULL, PRIMARY KEY(OrderId,ProductoId),
                FOREIGN KEY(OrderId) REFERENCES StockReservation(OrderId));
            """, transaction: tx);
        var columns = conn.Query("PRAGMA table_info(Product)", transaction: tx).Select(x => (string)x.name).ToHashSet();
        foreach (var column in new[] { "PrecioExacto", "NombreNormalizado", "CategoriaNormalizada" })
            if (!columns.Contains(column))
                conn.Execute($"ALTER TABLE Product ADD COLUMN {column} TEXT NULL", transaction: tx);
        var products = conn.Query("SELECT Id,Nombre,Categoria FROM Product WHERE NombreNormalizado IS NULL OR CategoriaNormalizada IS NULL", transaction: tx);
        foreach (var p in products)
            conn.Execute("UPDATE Product SET NombreNormalizado=@nombre,CategoriaNormalizada=@categoria WHERE Id=@id",
            new
            {
                id = (string)p.Id,
                nombre = ProductRepository.Normalize((string)p.Nombre),
                categoria = ProductRepository.Normalize((string)p.Categoria)
            }, tx);
        conn.Execute("""
            UPDATE Product SET PrecioExacto=CAST(Precio AS TEXT) WHERE PrecioExacto IS NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS UX_Product_Name_Category ON Product(NombreNormalizado,CategoriaNormalizada);
            CREATE TRIGGER IF NOT EXISTS Product_Insert_Validation BEFORE INSERT ON Product
            WHEN NEW.Stock<0 OR NEW.Stock>2147483647 OR LENGTH(TRIM(NEW.Nombre))=0 OR LENGTH(NEW.Nombre)>100
                OR LENGTH(NEW.Descripcion)>500 OR LENGTH(TRIM(NEW.Categoria))=0
            BEGIN SELECT RAISE(ABORT,'Producto inválido'); END;
            CREATE TRIGGER IF NOT EXISTS Product_Update_Validation BEFORE UPDATE ON Product
            WHEN NEW.Stock<0 OR NEW.Stock>2147483647 OR LENGTH(TRIM(NEW.Nombre))=0 OR LENGTH(NEW.Nombre)>100
                OR LENGTH(NEW.Descripcion)>500 OR LENGTH(TRIM(NEW.Categoria))=0
            BEGIN SELECT RAISE(ABORT,'Producto inválido'); END;
            """, transaction: tx);
        tx.Commit();
        logger.LogInformation("Persistencia de Products inicializada y migrada.");
    }
}
