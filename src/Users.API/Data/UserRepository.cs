using Dapper;
using Microsoft.Data.Sqlite;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Data;

/// <summary>Persistencia de usuarios y actualización atómica de intentos de acceso.</summary>
public class UserRepository(IConfiguration config)
{
    private const string Columns = "Id, Nombre, Apellido, Email, PasswordHash, FechaRegistro, Activo, IntentosFallidos";
    private SqliteConnection CreateConnection() => new(config.GetConnectionString("DefaultConnection")
        ?? "Data Source=users.db;Foreign Keys=True;");
    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<User?> GetByEmailAsync(string email)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            $"SELECT {Columns} FROM User WHERE NormalizedEmail = @email", new
            {
                email = NormalizeEmail(email)
            });
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        using var connection = CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<User>(
            $"SELECT {Columns} FROM User WHERE Id = @id", new
            {
                id = id.ToString()
            });
    }

    public async Task CreateAsync(User user)
    {
        using var connection = CreateConnection();
        try
        {
            await connection.ExecuteAsync("""
                INSERT INTO User (Id, Nombre, Apellido, Email, NormalizedEmail, PasswordHash, FechaRegistro, Activo, IntentosFallidos)
                VALUES (@Id, @Nombre, @Apellido, @Email, @NormalizedEmail, @PasswordHash, @FechaRegistro, 1, 0)
                """, new
            {
                Id = user.Id.ToString(),
                user.Nombre,
                user.Apellido,
                user.Email,
                NormalizedEmail = NormalizeEmail(user.Email),
                user.PasswordHash,
                FechaRegistro = user.FechaRegistro.ToString("O")
            });
        }
        catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)
        {
            throw new BusinessRuleException(UserErrorCodes.Usr001, "El email ya está registrado.");
        }
    }

    public async Task<LoginAttemptResult> ProcessLoginAsync(string email, Func<User, PasswordCheckResult> checkPassword)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync();
        // BEGIN IMMEDIATE serializa lectura y cambio incluso entre procesos diferentes.
        using var transaction = connection.BeginTransaction(deferred: false);
        var user = await connection.QuerySingleOrDefaultAsync<User>(
            $"SELECT {Columns} FROM User WHERE NormalizedEmail = @email",
            new
            {
                email = NormalizeEmail(email)
            }, transaction);
        if (user is null)
            return new(null, LoginOutcome.InvalidCredentials);
        if (!user.Activo)
            return new(user, user.IntentosFallidos >= 3 ? LoginOutcome.LockedAttempts : LoginOutcome.LockedFraud);
        var check = checkPassword(user);
        if (!check.Success)
        {
            user.IntentosFallidos = Math.Min(3, user.IntentosFallidos + 1);
            user.Activo = user.IntentosFallidos < 3;
            await connection.ExecuteAsync("""
                UPDATE User SET IntentosFallidos = @IntentosFallidos, Activo = @Activo WHERE Id = @Id
                """, new
            {
                Id = user.Id.ToString(),
                user.IntentosFallidos,
                Activo = user.Activo ? 1 : 0
            }, transaction);
            transaction.Commit();
            return new(user, user.Activo ? LoginOutcome.InvalidCredentials : LoginOutcome.LockedAttempts);
        }
        // Activo se verificó dentro de esta misma transacción; un login no desbloquea cuentas.
        await connection.ExecuteAsync("""
            UPDATE User SET IntentosFallidos = 0, PasswordHash = @PasswordHash WHERE Id = @Id
            """, new
        {
            Id = user.Id.ToString(),
            PasswordHash = check.ReplacementHash ?? user.PasswordHash
        }, transaction);
        user.IntentosFallidos = 0;
        transaction.Commit();
        return new(user, LoginOutcome.Success);
    }
}

public enum LoginOutcome
{
    Success, InvalidCredentials, LockedAttempts, LockedFraud
}
public sealed record PasswordCheckResult(bool Success, string? ReplacementHash = null);
public sealed record LoginAttemptResult(User? User, LoginOutcome Outcome);
