using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>Datos necesarios para enviar una notificación a un usuario.</summary>
public sealed class SendNotificationRequest
{
    /// <summary>Identificador del usuario destinatario.</summary>
    /// <example>6f8f577b-7f9d-4a3f-9a1e-5f15e7a8a001</example>
    [Required]
    public Guid UsuarioId { get; set; }

    /// <summary>Tipo de notificación.</summary>
    /// <example>PedidoConfirmado</example>
    [Required, StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Contenido de la notificación.</summary>
    /// <example>Tu pedido fue confirmado.</example>
    [Required, StringLength(500)]
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>Notificación persistida.</summary>
public sealed record NotificationResponse(
    Guid Id,
    Guid UsuarioId,
    string Tipo,
    string Mensaje,
    DateTime FechaCreacion);
