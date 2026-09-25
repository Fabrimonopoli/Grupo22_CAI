using Dapper;
using Microsoft.Data.Sqlite;
using MiniApi.Models;

namespace MiniApi.Data
{
    public class ItemRepository
    {
        private readonly IConfiguration _config;

        public ItemRepository(IConfiguration config) => _config = config;

        private SqliteConnection CreateConnection() =>
            new(_config.GetConnectionString("DefaultConnection") ?? "Data Source=app.db");

        // ── GET ALL ───────────────────────────────────────────────────────────
        public async Task<IEnumerable<Item>> GetAllAsync()
        {
            using var conn = CreateConnection();
            return await conn.QueryAsync<Item>("""
            SELECT id, name, description, price, stock,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM items
            ORDER BY id DESC
        """);
        }

        // ── GET BY ID ─────────────────────────────────────────────────────────
        public async Task<Item?> GetByIdAsync(int id)
        {
            using var conn = CreateConnection();
            return await conn.QuerySingleOrDefaultAsync<Item>("""
            SELECT id, name, description, price, stock,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM items
            WHERE id = @id
        """, new { id });
        }

        // ── CREATE ────────────────────────────────────────────────────────────
        public async Task<Item> CreateAsync(CreateItemRequest request)
        {
            using var conn = CreateConnection();

            var id = await conn.ExecuteScalarAsync<int>("""
            INSERT INTO items (name, description, price, stock)
            VALUES (@Name, @Description, @Price, @Stock);
            SELECT last_insert_rowid();
        """, request);

            return (await GetByIdAsync(id))!;
        }

        // ── UPDATE ────────────────────────────────────────────────────────────
        public async Task<bool> UpdateAsync(int id, UpdateItemRequest request)
        {
            using var conn = CreateConnection();

            var rows = await conn.ExecuteAsync("""
            UPDATE items
            SET name        = @Name,
                description = @Description,
                price       = @Price,
                stock       = @Stock,
                updated_at  = datetime('now')
            WHERE id = @Id
        """, new { request.Name, request.Description, request.Price, request.Stock, Id = id });

            return rows > 0;
        }

        // ── DELETE ────────────────────────────────────────────────────────────
        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = CreateConnection();
            var rows = await conn.ExecuteAsync("DELETE FROM items WHERE id = @id", new { id });
            return rows > 0;
        }
    }
}