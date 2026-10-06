namespace Users.API.Models;

/// <summary>
/// Entidad que representa a un usuario registrado en el sistema.
/// </summary>
public class User
{
    /// <summary>Identificador único del usuario (GUID).</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre del usuario.</summary>
    /// <example>María</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellido del usuario.</summary>
    /// <example>González</example>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Correo electrónico único registrado.</summary>
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña almacenada.</summary>
    /// <example>TWlQYXNzd29yZDEyMyE=</example>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Fecha y hora de registro en formato UTC.</summary>
    /// <example>2024-03-10T09:00:00Z</example>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>Indica si el usuario se encuentra activo o bloqueado.</summary>
    /// <example>true</example>
    public bool Activo { get; set; } = true;

    /// <summary>Contador de intentos fallidos de inicio de sesión.</summary>
    /// <example>0</example>
    public int IntentosFallidos { get; set; } = 0;
}