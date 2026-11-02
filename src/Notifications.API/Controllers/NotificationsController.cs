using Microsoft.AspNetCore.Mvc;
using Notifications.API.DTOs;
using Notifications.API.Services;

namespace Notifications.API.Controllers;

/// <summary>Contrato REST público de Notifications; delega las reglas a servicios de negocio.</summary>
[ApiController]
[Route("api/notifications")]
[Tags("Notifications")]
public sealed class NotificationsController(NotificationService service) : ControllerBase
{
    /// <summary>Registra y simula el envío de una notificación Email, Push o SMS.</summary>
    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequest request, CancellationToken cancellationToken)
    {
        var notification = await service.SendAsync(request, cancellationToken);
        return Created($"/api/notifications/{notification.UsuarioId}", notification);
    }

    /// <summary>Lista el historial persistido o devuelve NTF-003 si está vacío.</summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
        => Ok(await service.GetByUserIdAsync(userId, cancellationToken));
}
