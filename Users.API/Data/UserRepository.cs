using Dapper;
using Microsoft.Data.Sqlite;
using Users.API.Models;

namespace Users.API.Data;

public class UserRepository
{
    private readonly IConfiguration _config;
    public UserRepository(IConfiguration config) => _config = config;

    private SqliteConnection CreateConnection() => new(_config.GetConnectionString("DefaultConnection") ?? "Data Source=users.db;Foreign Keys=True;");

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos
            FROM User
            WHERE LOWER(Email) = LOWER(@email)";
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { email });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var conn = CreateConnection();
        var sql = @"
            SELECT Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos
            FROM User
            WHERE Id = @id";
        return await conn.QuerySingleOrDefaultAsync<User>(sql, new { id = id.ToString() });
    }

    public async Task CreateAsync(User user)
    {
        using var conn = CreateConnection();
        var sql = @"
            INSERT INTO User (Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos)
            VALUES (@Id, @Nombre, @Apellido, @Email, @PasswordHash, @FechaRegistro, @Activo, @IntentosFallidos)";
        await conn.ExecuteAsync(sql, new
        {
            Id = user.Id.ToString(),
            user.Nombre,
            user.Apellido,
            user.Email,
            user.PasswordHash,
            FechaRegistro = user.FechaRegistro.ToString("O"),
            Activo = user.Activo ? 1 : 0,
            user.IntentosFallidos
        });
    }

    public async Task UpdateFailedAttemptsAsync(Guid id, int intentosFallidos, bool activo)
    {
        using var conn = CreateConnection();
        var sql = @"
            UPDATE User
            SET IntentosFallidos = @intentosFallidos,
                Activo = @activo
            WHERE Id = @id";
        await conn.ExecuteAsync(sql, new
        {
            id = id.ToString(),
            intentosFallidos,
            activo = activo ? 1 : 0
        });
    }
}
