using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Services;

/// <summary>Reglas de registro, simulación de envío y consulta de notificaciones.</summary>
public sealed class NotificationService(
    NotificationRepository repository,
    UsersApiClient usersClient,
    ILogger<NotificationService> logger)
{
    /// <summary>Valida el destinatario y registra un envío simulado, sin proveedores externos.</summary>
    public async Task<NotificationResponse> SendAsync(
        SendNotificationRequest? request, CancellationToken cancellationToken = default)
    {
        if (request is null || request.UsuarioId == Guid.Empty ||
            !NotificationTypes.Contains(request.Tipo) ||
            string.IsNullOrWhiteSpace(request.Mensaje) || request.Mensaje.Length > 500)
            throw new BusinessRuleException(NotificationErrorCodes.Ntf002,
                "Los datos de la notificación son inválidos. UsuarioId y mensaje son requeridos; el mensaje admite hasta 500 caracteres y el tipo debe ser Email, Push o SMS.");

        _ = await usersClient.GetByIdAsync(request.UsuarioId, cancellationToken)
            ?? throw new BusinessRuleException(NotificationErrorCodes.Ntf001, "El usuario destinatario no fue encontrado.");

        // La consigna pide simulación, no envío real. Solo se devuelve 201 una vez
        // persistido el resultado de la simulación, que en este TP siempre es éxito.
        var notification = new Notification
        {
            UsuarioId = request.UsuarioId,
            Tipo = request.Tipo,
            Mensaje = request.Mensaje.Trim(),
            Estado = "Enviada",
            FechaEnvio = DateTime.UtcNow
        };
        await repository.CreateAsync(notification, cancellationToken);
        logger.LogInformation(
            "Envío simulado {EventCode} de notificación {NotificationId} por {Tipo} para {UsuarioId} con estado {Estado}",
            "NTF-SENT", notification.Id, notification.Tipo, notification.UsuarioId, notification.Estado);
        return ToResponse(notification);
    }

    /// <summary>Consulta el historial local, sin depender del estado actual de Users.API.</summary>
    public async Task<IReadOnlyList<NotificationResponse>> GetByUserIdAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "El identificador del usuario es inválido.");
        var notifications = await repository.GetByUserIdAsync(userId, cancellationToken);
        if (notifications.Count == 0)
            throw new NotificationNotFoundException("No se encontraron notificaciones para el usuario.");
        return notifications.Select(ToResponse).ToArray();
    }

    private static NotificationResponse ToResponse(Notification notification) => new(
        notification.Id, notification.UsuarioId, notification.Tipo,
        notification.Mensaje, notification.Estado, notification.FechaEnvio);
}
