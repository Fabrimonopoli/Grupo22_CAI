namespace Notifications.API.Models;

/// <summary>Registro persistente de una notificación y del resultado de su envío simulado.</summary>
public sealed class Notification
{
    /// <summary>Identificador único de la notificación.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Identificador del usuario destinatario.</summary>
    public Guid UsuarioId
    {
        get; set;
    }
    /// <summary>Canal de envío: Email, Push o SMS.</summary>
    public string Tipo { get; set; } = string.Empty;
    /// <summary>Contenido del mensaje, de hasta 500 caracteres.</summary>
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>Resultado del envío: Pendiente, Enviada o Fallida.</summary>
    public string Estado { get; set; } = "Pendiente";
    /// <summary>Fecha UTC asignada al registrar el envío.</summary>
    public DateTime FechaEnvio
    {
        get; set;
    }
}
