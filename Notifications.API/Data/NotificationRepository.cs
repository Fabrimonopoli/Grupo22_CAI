using Dapper;
using Microsoft.Data.Sqlite;
using Notifications.API.Models;

namespace Notifications.API.Data;

public sealed class NotificationRepository(IConfiguration config)
{
    private SqliteConnection CreateConnection() => new(
        config.GetConnectionString("DefaultConnection") ?? "Data Source=notifications.db;Foreign Keys=True;");

    public async Task CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO Notification (Id, UsuarioId, Tipo, Mensaje, FechaCreacion)
            VALUES (@Id, @UsuarioId, @Tipo, @Mensaje, @FechaCreacion)
            """, new
        {
            Id = notification.Id.ToString(),
            UsuarioId = notification.UsuarioId.ToString(),
            notification.Tipo,
            notification.Mensaje,
            FechaCreacion = notification.FechaCreacion.ToString("O")
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var result = await connection.QueryAsync<Notification>(new CommandDefinition("""
            SELECT Id, UsuarioId, Tipo, Mensaje, FechaCreacion
            FROM Notification
            WHERE UsuarioId = @userId
            ORDER BY FechaCreacion DESC
            """, new { userId = userId.ToString() }, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
