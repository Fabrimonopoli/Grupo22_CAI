using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>Datos necesarios para enviar una notificación a un usuario.</summary>
public sealed class SendNotificationRequest
{
    /// <summary>Identificador del usuario destinatario.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required]
    public Guid UsuarioId { get; set; }

    /// <summary>Tipo de notificación (Email, Push, SMS).</summary>
    /// <example>Email</example>
    [Required, StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Contenido de la notificación.</summary>
    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    [Required, StringLength(500)]
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>Notificación persistida devuelta por la API.</summary>
public sealed record NotificationResponse(
    Guid Id,
    Guid UsuarioId,
    string Mensaje,
    string Tipo,
    string Estado,
    DateTime FechaEnvio);