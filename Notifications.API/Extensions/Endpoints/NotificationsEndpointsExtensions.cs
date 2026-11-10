using Notifications.API.Clients;
using Notifications.API.Data;
using Notifications.API.DTOs;
using Notifications.API.Exceptions;
using Notifications.API.Models;

namespace Notifications.API.Extensions.Endpoints;

public static class NotificationsEndpointsExtensions
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // ── POST /api/notifications/send ────────────────────────────────────
        app.MapPost("/api/notifications/send", async (
            SendNotificationRequest? request,
            NotificationRepository repository,
            UsersApiClient usersClient,
            CancellationToken cancellationToken) =>
        {
            // 1. Validar cuerpo de la petición y tipo de notificación (NTF-002)
            if (request is null ||
                request.UsuarioId == Guid.Empty ||
                string.IsNullOrWhiteSpace(request.Tipo) ||
                !NotificationTypes.IsValid(request.Tipo.Trim()) ||
                string.IsNullOrWhiteSpace(request.Mensaje) ||
                request.Mensaje.Length > 500)
            {
                throw new BusinessRuleException(NotificationErrorCodes.Ntf002, "Los datos de la notificación son inválidos.");
            }

            // 2. Verificar existencia del usuario destinatario en Users.API (NTF-001)
            var user = await usersClient.GetByIdAsync(request.UsuarioId, cancellationToken);
            if (user is null)
            {
                throw new NotFoundException(NotificationErrorCodes.Ntf001, "Usuario no encontrado.");
            }

            // 3. Crear y persistir la notificación
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
        .WithDescription("Registra una notificación y simula su envío (Email, Push, SMS) validando que el usuario destinatario exista en Users.API.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **NTF-001 (404):** Usuario no encontrado.\n" +
                         "- **NTF-002 (400):** Los datos de la notificación son inválidos.\n" +
                         "- **NTF-004 (500):** Error interno al procesar la notificación.")
        .Produces<NotificationResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── GET /api/notifications/{userId} ─────────────────────────────────
        app.MapGet("/api/notifications/{userId}", async (
            string userId,
            NotificationRepository repository,
            CancellationToken cancellationToken) =>
        {
            // Si el ID no es un GUID convertible, se considera que no tiene notificaciones registradas (NTF-003)
            if (!Guid.TryParse(userId, out var userGuid) || userGuid == Guid.Empty)
            {
                throw new NotFoundException(NotificationErrorCodes.Ntf003, "No se encontraron notificaciones para el usuario.");
            }

            var notifications = await repository.GetByUserIdAsync(userGuid, cancellationToken);
            if (!notifications.Any())
            {
                throw new NotFoundException(NotificationErrorCodes.Ntf003, "No se encontraron notificaciones para el usuario.");
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
                         "- **NTF-003 (404):** No se encontraron notificaciones para el usuario.\n" +
                         "- **NTF-004 (500):** Error interno al procesar la notificación.")
        .Produces<IEnumerable<NotificationResponse>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}