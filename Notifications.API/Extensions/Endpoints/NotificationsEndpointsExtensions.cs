using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Extensions.Endpoints;

public static class NotificationsEndpointsExtensions
{
    /// <summary>
    /// Registra los endpoints para enviar y consultar notificaciones.
    /// </summary>
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/notifications/send", async (
            SendNotificationRequest request,
            NotificationRepository repository,
            UsersApiClient usersClient,
            CancellationToken cancellationToken) =>
        {
            if (request.UsuarioId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Tipo) ||
                string.IsNullOrWhiteSpace(request.Mensaje) ||
                request.Tipo.Length > 50 ||
                request.Mensaje.Length > 500)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "Los datos de la notificación son inválidos.");

            var user = await usersClient.GetByIdAsync(request.UsuarioId, cancellationToken)
                ?? throw new BusinessRuleException(NotificationErrorCodes.Ntf001, "El usuario destinatario no existe.");
            if (!user.Activo)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf001, "El usuario destinatario está inactivo.");

            var notification = new Notification
            {
                UsuarioId = request.UsuarioId,
                Tipo = request.Tipo.Trim(),
                Mensaje = request.Mensaje.Trim(),
                FechaCreacion = DateTime.UtcNow
            };
            await repository.CreateAsync(notification, cancellationToken);
            var response = new NotificationResponse(notification.Id, notification.UsuarioId,
                notification.Tipo, notification.Mensaje, notification.FechaCreacion);
            return Results.Created($"/api/notifications/{notification.UsuarioId}", response);
        })
        .WithTags("Notifications")
        .Produces<NotificationResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        app.MapGet("/api/notifications/{userId:guid}", async (
            Guid userId,
            NotificationRepository repository,
            UsersApiClient usersClient,
            CancellationToken cancellationToken) =>
        {
            if (userId == Guid.Empty)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "El identificador del usuario es inválido.");

            var user = await usersClient.GetByIdAsync(userId, cancellationToken)
                ?? throw new BusinessRuleException(NotificationErrorCodes.Ntf001, "El usuario no existe.");
            if (!user.Activo)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf001, "El usuario está inactivo.");

            var notifications = await repository.GetByUserIdAsync(userId, cancellationToken);
            return Results.Ok(notifications.Select(n =>
                new NotificationResponse(n.Id, n.UsuarioId, n.Tipo, n.Mensaje, n.FechaCreacion)));
        })
        .WithTags("Notifications")
        .Produces<IEnumerable<NotificationResponse>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }
}
