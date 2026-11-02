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
            INSERT INTO Notification (Id, UsuarioId, Tipo, Mensaje, Estado, FechaEnvio)
            VALUES (@Id, @UsuarioId, @Tipo, @Mensaje, @Estado, @FechaEnvio)
            """, new
        {
            Id = notification.Id.ToString(),
            UsuarioId = notification.UsuarioId.ToString(),
            notification.Tipo,
            notification.Mensaje,
            notification.Estado,
            FechaEnvio = notification.FechaEnvio.ToString("O")
        }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<Notification>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var result = await connection.QueryAsync<Notification>(new CommandDefinition("""
            SELECT Id, UsuarioId, Tipo, Mensaje, Estado, FechaEnvio
            FROM Notification
            WHERE UsuarioId = @userId
            ORDER BY FechaEnvio DESC, Id
            """, new
        {
            userId = userId.ToString()
        }, cancellationToken: cancellationToken));
        return result.AsList();
    }
}
