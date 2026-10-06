using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Data;

public class ProductRepository
{
    private readonly IConfiguration _config;
    public ProductRepository(IConfiguration config) => _config = config;

    private SqliteConnection CreateConnection() =>
        new(_config.GetConnectionString("DefaultConnection") ?? "Data Source=products.db;Foreign Keys=True;");

    public async Task<IEnumerable<Product>> GetAllAsync(string? categoria = null, string? nombre = null)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion
            FROM Product
            WHERE (@categoria IS NULL OR LOWER(Categoria) = LOWER(@categoria))
              AND (@nombre IS NULL OR LOWER(Nombre) LIKE '%' || LOWER(@nombre) || '%')
            ORDER BY FechaCreacion DESC";
        return await conn.QueryAsync<Product>(sql, new { categoria, nombre });
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        var sql = "SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion FROM Product WHERE Id = @id";
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { id = id.ToString() });
    }

    public async Task<Product?> GetByNameAndCategoryAsync(string nombre, string categoria)
    {
        using var conn = CreateConnection();
        var sql = "SELECT Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion FROM Product WHERE LOWER(Nombre) = LOWER(@nombre) AND LOWER(Categoria) = LOWER(@categoria)";
        return await conn.QuerySingleOrDefaultAsync<Product>(sql, new { nombre, categoria });
    }

    public async Task CreateAsync(Product product)
    {
        using var conn = CreateConnection();
        var sql = "INSERT INTO Product (Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion) VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)";
        await conn.ExecuteAsync(sql, new
        {
            Id = product.Id.ToString(),
            product.Nombre,
            product.Descripcion,
            product.Precio,
            product.Stock,
            product.Categoria,
            FechaCreacion = product.FechaCreacion.ToString("O")
        });
    }

    public async Task<bool> UpdateAsync(Product product)
    {
        using var conn = CreateConnection();
        var sql = "UPDATE Product SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio, Stock = @Stock, Categoria = @Categoria WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { Id = product.Id.ToString(), product.Nombre, product.Descripcion, product.Precio, product.Stock, product.Categoria });
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        using var conn = CreateConnection();
        var sql = "DELETE FROM Product WHERE Id = @id";
        var rows = await conn.ExecuteAsync(sql, new { id = id.ToString() });
        return rows > 0;
    }

    public async Task<bool> HasActiveOrdersAsync(Guid productId)
    {
        await Task.CompletedTask;
        return false;
    }
}
