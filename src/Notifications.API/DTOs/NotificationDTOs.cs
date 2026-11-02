using System.ComponentModel.DataAnnotations;

namespace Notifications.API.DTOs;

/// <summary>Datos necesarios para enviar una notificación a un usuario.</summary>
public sealed class SendNotificationRequest : IValidatableObject
{
    /// <summary>Identificador del usuario destinatario.</summary>
    /// <example>6f8f577b-7f9d-4a3f-9a1e-5f15e7a8a001</example>
    [Required]
    public Guid UsuarioId
    {
        get; set;
    }

    /// <summary>Tipo de notificación.</summary>
    /// <example>Email</example>
    [Required]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Contenido de la notificación.</summary>
    /// <example>Tu pedido fue confirmado.</example>
    [Required, StringLength(500)]
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Valida el identificador y los canales permitidos por el contrato.</summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (UsuarioId == Guid.Empty)
            yield return new ValidationResult("El UsuarioId es requerido.", [nameof(UsuarioId)]);
        if (!NotificationTypes.Contains(Tipo))
            yield return new ValidationResult("El tipo debe ser Email, Push o SMS.", [nameof(Tipo)]);
    }
}

/// <summary>Notificación persistida.</summary>
/// <param name="Id">Identificador único de la notificación.</param>
/// <param name="UsuarioId">Identificador del usuario destinatario.</param>
/// <param name="Tipo">Canal de envío: Email, Push o SMS.</param>
/// <param name="Mensaje">Contenido del mensaje.</param>
/// <param name="Estado">Estado del envío simulado.</param>
/// <param name="FechaEnvio">Fecha UTC en la que se registró el envío.</param>
public sealed record NotificationResponse(
    Guid Id,
    Guid UsuarioId,
    string Tipo,
    string Mensaje,
    string Estado,
    DateTime FechaEnvio);

/// <summary>Canales de notificación admitidos por la consigna.</summary>
public static class NotificationTypes
{
    /// <summary>Comprueba un canal con el nombre exacto definido por el contrato.</summary>
    public static bool Contains(string? tipo) => tipo is "Email" or "Push" or "SMS";
}
