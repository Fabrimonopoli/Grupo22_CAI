using Dapper;
using Microsoft.Data.Sqlite;
using MiniApi.Models;

namespace MiniApi.Data
{
    public class ProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") ?? "Data Source=app.db";
        }

        public async Task<IEnumerable<Product>> GetAllAsync(string? categoria, string? nombre)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = @"SELECT * FROM Products 
                        WHERE (@Categoria IS NULL OR Categoria = @Categoria) 
                        AND (@Nombre IS NULL OR Nombre LIKE '%' || @Nombre || '%')";
            return await connection.QueryAsync<Product>(sql, new { Categoria = categoria, Nombre = nombre });
        }

        public async Task<Product?> GetByIdAsync(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Product>("SELECT * FROM Products WHERE Id = @Id", new { Id = id.ToString().ToUpper() });
        }

        public async Task CreateAsync(Product product)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = @"INSERT INTO Products (Id, Nombre, Descripcion, Precio, Stock, Categoria, FechaCreacion) 
                        VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)";
            await connection.ExecuteAsync(sql, product);
        }

        /* public async Task UpdateAsync(Product product)
         {
             using var connection = new SqliteConnection(_connectionString);
             var sql = @"UPDATE Products SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio, 
                         Stock = @Stock, Categoria = @Categoria WHERE Id = @Id";
             await connection.ExecuteAsync(sql, product);
         }*/
        public async Task UpdateAsync(Product product)
        {
            using var connection = new SqliteConnection(_connectionString);
            var sql = @"UPDATE Products SET Nombre = @Nombre, Descripcion = @Descripcion, Precio = @Precio,
                Stock = @Stock, Categoria = @Categoria WHERE Id = @Id";

            // Desarmamos el objeto para obligar al Id a viajar en mayúsculas
            await connection.ExecuteAsync(sql, new
            {
                Id = product.Id.ToString().ToUpper(),
                Nombre = product.Nombre,
                Descripcion = product.Descripcion,
                Precio = product.Precio,
                Stock = product.Stock,
                Categoria = product.Categoria
            });
        }

        public async Task DeleteAsync(Guid id)
        {
            using var connection = new SqliteConnection(_connectionString);

            // Le agregamos el .ToUpper() al parámetro
            await connection.ExecuteAsync("DELETE FROM Products WHERE Id = @Id", new { Id = id.ToString().ToUpper() });
        }
    }
}
