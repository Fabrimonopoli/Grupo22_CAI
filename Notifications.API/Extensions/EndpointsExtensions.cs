using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Extensions;

public static class EndpointsExtensions
{
    public static void MapAppEndpoints(this WebApplication app)
    {
        app.MapPost("/api/notifications/send", async (
            SendNotificationRequest request,
            NotificationRepository repository,
            UsersApiClient usersClient,
            CancellationToken cancellationToken) =>
        {
            if (request.UsuarioId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Tipo) ||
                !NotificationTypes.IsValid(request.Tipo.Trim()) ||
                string.IsNullOrWhiteSpace(request.Mensaje) ||
                request.Mensaje.Length > 500)
            {
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "Los datos de la notificación son inválidos.");
            }

            var user = await usersClient.GetByIdAsync(request.UsuarioId, cancellationToken)
                ?? throw new NotFoundException(NotificationErrorCodes.Ntf001, "El usuario destinatario no fue encontrado.");

            if (!user.Activo)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "El usuario destinatario está inactivo.");

            var notification = new Notification
            {
                UsuarioId = request.UsuarioId,
                Tipo = request.Tipo.Trim(),
                Mensaje = request.Mensaje.Trim(),
                Estado = NotificationStates.Enviada,
                FechaEnvio = DateTime.UtcNow
            };

            await repository.CreateAsync(notification, cancellationToken);

            var response = new NotificationResponse(
                notification.Id,
                notification.UsuarioId,
                notification.Mensaje,
                notification.Tipo,
                notification.Estado,
                notification.FechaEnvio);

            return Results.Created($"/api/notifications/{notification.UsuarioId}", response);
        })
        .WithTags("Notifications")
        .WithSummary("Registrar y simular envío de notificación.")
        .WithDescription("Registra una notificación y simula su envío (Email, Push, SMS) validando que el usuario exista y esté activo en Users.API.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **NTF-001 (404):** El usuario destinatario no fue encontrado en Users.API.\n" +
                         "- **NTF-002 (400):** Datos de notificación inválidos, tipo no reconocido o usuario inactivo.\n" +
                         "- **NTF-004 (500):** Error interno inesperado en el servicio o persistencia.")
        .Produces<NotificationResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        app.MapGet("/api/notifications/{userId:guid}", async (
            Guid userId,
            NotificationRepository repository,
            UsersApiClient usersClient,
            CancellationToken cancellationToken) =>
        {
            if (userId == Guid.Empty)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "El identificador del usuario es inválido.");

            var user = await usersClient.GetByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException(NotificationErrorCodes.Ntf001, "El usuario no fue encontrado.");

            if (!user.Activo)
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "El usuario está inactivo.");

            var notifications = await repository.GetByUserIdAsync(userId, cancellationToken);

            if (notifications.Count == 0)
            {
                throw new NotFoundException(
                    NotificationErrorCodes.Ntf003,
                    "No se encontraron notificaciones para el usuario.");
            }

            return Results.Ok(notifications.Select(n =>
                new NotificationResponse(
                    n.Id,
                    n.UsuarioId,
                    n.Mensaje,
                    n.Tipo,
                    n.Estado,
                    n.FechaEnvio)));
        })
        .WithTags("Notifications")
        .WithSummary("Listar notificaciones de un usuario.")
        .WithDescription("Obtiene el historial de notificaciones ordenadas por fecha descendente asociadas al ID de usuario indicado.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **NTF-001 (404):** El usuario no fue encontrado en Users.API.\n" +
                         "- **NTF-002 (400):** Identificador de usuario inválido o usuario inactivo.\n" +
                         "- **NTF-003 (404):** No se encontraron notificaciones registradas para el usuario.\n" +
                         "- **NTF-004 (500):** Error interno inesperado al consultar la persistencia.")
        .Produces<IEnumerable<NotificationResponse>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");
    }
}