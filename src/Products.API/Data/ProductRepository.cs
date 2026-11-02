using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Exceptions;
using Products.API.Models;
namespace Products.API.Data;

public class ProductRepository(IConfiguration config)
{
    internal const string Columns = "Id, Nombre, Descripcion, COALESCE(PrecioExacto, CAST(Precio AS TEXT)) AS Precio, Stock, Categoria, FechaCreacion";
    internal static string Normalize(string value) => value.Trim().ToUpperInvariant();
    internal static string Money(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
    internal SqliteConnection CreateConnection() => new(config.GetConnectionString("DefaultConnection") ?? "Data Source=products.db;Foreign Keys=True;");
    public async Task<IEnumerable<Product>> GetAllAsync(string? categoria = null, string? nombre = null)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<Product>($"""
            SELECT {Columns} FROM Product
            WHERE (@categoria IS NULL OR CategoriaNormalizada = @categoria)
              AND (@nombre IS NULL OR LOWER(Nombre) LIKE '%' || LOWER(@nombre) || '%')
            ORDER BY FechaCreacion DESC
            """, new
        {
            categoria = categoria is null ? null : Normalize(categoria),
            nombre
        });
    }
    public async Task<Product?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Product>($"SELECT {Columns} FROM Product WHERE Id = @id", new
        {
            id = id.ToString()
        });
    }
    public async Task<Product?> GetByNameAndCategoryAsync(string nombre, string categoria)
    {
        using var conn = CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<Product>($"SELECT {Columns} FROM Product WHERE NombreNormalizado = @nombre AND CategoriaNormalizada = @categoria", new
        {
            nombre = Normalize(nombre),
            categoria = Normalize(categoria)
        });
    }
    public async Task CreateAsync(Product product)
    {
        using var conn = CreateConnection();
        try
        {
            await conn.ExecuteAsync("""
            INSERT INTO Product (Id, Nombre, Descripcion, Precio, PrecioExacto, Stock, Categoria, FechaCreacion, NombreNormalizado, CategoriaNormalizada)
            VALUES (@Id, @Nombre, @Descripcion, @Precio, @Precio, @Stock, @Categoria, @FechaCreacion, @NombreNormalizado, @CategoriaNormalizada)
            """, Parameters(product));
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new BusinessRuleException(ProductErrorCodes.Prd003, "Ya existe un producto con ese nombre en la categoría.");
        }
    }
    public async Task<bool> UpdateAsync(Product product)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var refundable = await conn.ExecuteScalarAsync<long>("""
            SELECT COALESCE(SUM(i.Cantidad),0) FROM StockReservationItem i JOIN StockReservation r ON r.OrderId=i.OrderId
            WHERE i.ProductoId=@id AND r.Estado IN ('Reserved','Committed')
            """, new
        {
            id = product.Id.ToString()
        }, tx);
        if ((long)product.Stock + refundable > int.MaxValue)
            throw new BusinessRuleException(ProductErrorCodes.Prd002, "El stock disponible más las reservas activas supera el máximo permitido.");
        try
        {
            var rows = await conn.ExecuteAsync("""
            UPDATE Product SET Nombre=@Nombre, Descripcion=@Descripcion, Precio=@Precio, PrecioExacto=@Precio,
                Stock=@Stock, Categoria=@Categoria, NombreNormalizado=@NombreNormalizado, CategoriaNormalizada=@CategoriaNormalizada WHERE Id=@Id
            """, Parameters(product), tx);
            tx.Commit();
            return rows > 0;
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new BusinessRuleException(ProductErrorCodes.Prd003, "Ya existe un producto con ese nombre en la categoría.");
        }
    }
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = CreateConnection();
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction(deferred: false);
        var reserved = await conn.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM StockReservationItem i JOIN StockReservation r ON r.OrderId=i.OrderId
            WHERE i.ProductoId=@id AND r.Estado IN ('Reserved','Committed')
            """, new
        {
            id = id.ToString()
        }, tx);
        if (reserved > 0)
            throw new BusinessRuleException(ProductErrorCodes.Prd004, "El producto tiene órdenes o reservas activas y no puede eliminarse.");
        var rows = await conn.ExecuteAsync("DELETE FROM Product WHERE Id=@id", new
        {
            id = id.ToString()
        }, tx);
        tx.Commit();
        return rows > 0;
    }
    private static object Parameters(Product p) => new
    {
        Id = p.Id.ToString(),
        p.Nombre,
        p.Descripcion,
        Precio = Money(p.Precio),
        p.Stock,
        p.Categoria,
        FechaCreacion = p.FechaCreacion.ToString("O"),
        NombreNormalizado = Normalize(p.Nombre),
        CategoriaNormalizada = Normalize(p.Categoria)
    };
}
