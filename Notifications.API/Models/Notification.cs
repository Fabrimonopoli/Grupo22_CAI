namespace Notifications.API.Models;

public static class NotificationTypes
{
    public const string Email = "Email";
    public const string Push = "Push";
    public const string SMS = "SMS";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Email, Push, SMS
    };

    public static bool IsValid(string? tipo) => !string.IsNullOrWhiteSpace(tipo) && All.Contains(tipo);
}

public static class NotificationStates
{
    public const string Pendiente = "Pendiente";
    public const string Enviada = "Enviada";
    public const string Fallida = "Fallida";

    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        Pendiente, Enviada, Fallida
    };

    public static bool IsValid(string? estado) => !string.IsNullOrWhiteSpace(estado) && All.Contains(estado);
}

public sealed class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
    public string Estado { get; set; } = NotificationStates.Enviada;
    public DateTime FechaEnvio { get; set; }
}