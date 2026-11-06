using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>Datos necesarios para enviar una notificación a un usuario.</summary>
public sealed class SendNotificationRequest
{
    /// <summary>Identificador único del usuario destinatario.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required]
    public Guid UsuarioId { get; set; }

    /// <summary>Canal de notificación permitido: Email, Push, SMS.</summary>
    /// <example>Email</example>
    [Required, StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Contenido textual del mensaje.</summary>
    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    [Required, StringLength(500)]
    public string Mensaje { get; set; } = string.Empty;
}

/// <summary>Notificación persistida devuelta por la API.</summary>
public sealed record NotificationResponse
{
    /// <summary>Identificador único de la notificación.</summary>
    /// <example>11112222-3333-4444-5555-666677778888</example>
    public Guid Id { get; init; }

    /// <summary>Identificador del usuario destinatario.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId { get; init; }

    /// <summary>Contenido del mensaje enviado.</summary>
    /// <example>Su orden #f1e2d3c4 fue confirmada.</example>
    public string Mensaje { get; init; } = string.Empty;

    /// <summary>Canal de envío (Email, Push, SMS).</summary>
    /// <example>Email</example>
    public string Tipo { get; init; } = string.Empty;

    /// <summary>Estado de la notificación (Pendiente, Enviada, Fallida).</summary>
    /// <example>Enviada</example>
    public string Estado { get; init; } = string.Empty;

    /// <summary>Fecha y hora UTC del registro de envío.</summary>
    /// <example>2026-11-04T12:01:00Z</example>
    public DateTime FechaEnvio { get; init; }

    public NotificationResponse() { }

    public NotificationResponse(Guid id, Guid usuarioId, string mensaje, string tipo, string estado, DateTime fechaEnvio)
    {
        Id = id;
        UsuarioId = usuarioId;
        Mensaje = mensaje;
        Tipo = tipo;
        Estado = estado;
        FechaEnvio = fechaEnvio;
    }
}

/// <summary>Estructura estándar de respuesta de error (RFC 7231 / Problem Details).</summary>
public sealed record ErrorResponse
{
    /// <summary>URI de referencia al estándar RFC del error.</summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public string Type { get; init; } = string.Empty;

    /// <summary>Título breve del error HTTP.</summary>
    /// <example>Not Found</example>
    public string Title { get; init; } = string.Empty;

    /// <summary>Código de estado HTTP.</summary>
    /// <example>404</example>
    public int Status { get; init; }

    /// <summary>Detalle específico del error ocurrido.</summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Ruta relativa del endpoint invocado.</summary>
    /// <example>/api/notifications/a1b2c3d4-0000-0000-0000-111122223333</example>
    public string Instance { get; init; } = string.Empty;

    /// <summary>Código de error interno del catálogo (ej. NTF-001, NTF-002, NTF-003, NTF-004).</summary>
    /// <example>NTF-003</example>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje descriptivo del catálogo de errores.</summary>
    /// <example>No se encontraron notificaciones para el usuario.</example>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Identificador único de correlación propagado en el request.</summary>
    /// <example>dfc5335cf8d849b19003b0dbb41ee057</example>
    public string CorrelationId { get; init; } = string.Empty;
}