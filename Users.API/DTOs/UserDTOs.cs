using System.ComponentModel.DataAnnotations;

namespace Users.API.DTOs;

// ── REGISTRO DE USUARIO ──────────────────────────────────────────────────

/// <summary>
/// Solicitud para registrar un nuevo usuario en el sistema.
/// </summary>
public class RegisterUserRequest
{
    /// <summary>Nombre del usuario.</summary>
    /// <example>María</example>
    [Required(ErrorMessage = "El nombre es requerido.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellido del usuario.</summary>
    /// <example>González</example>
    [Required(ErrorMessage = "El apellido es requerido.")]
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Correo electrónico único del usuario.</summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña de acceso para el usuario.</summary>
    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta tras un registro de usuario exitoso.
/// </summary>
public class RegisterUserResponse
{
    /// <summary>Identificador único generado para el usuario (GUID).</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; set; }

    /// <summary>Nombre del usuario registrado.</summary>
    /// <example>María</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellido del usuario registrado.</summary>
    /// <example>González</example>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Correo electrónico registrado.</summary>
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;

    /// <summary>Fecha y hora en la que se completó el registro (UTC).</summary>
    /// <example>2024-03-10T09:00:00Z</example>
    public DateTime FechaRegistro { get; set; }

    /// <summary>Indica si la cuenta se encuentra activa.</summary>
    /// <example>true</example>
    public bool Activo { get; set; }
}

// ── AUTENTICACIÓN / LOGIN ─────────────────────────────────────────────────

/// <summary>
/// Solicitud de inicio de sesión de usuario.
/// </summary>
public class LoginUserRequest
{
    /// <summary>Correo electrónico registrado.</summary>
    /// <example>maria@email.com</example>
    [Required(ErrorMessage = "El email es requerido.")]
    [EmailAddress(ErrorMessage = "El formato del email es inválido.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Contraseña de acceso.</summary>
    /// <example>MiPassword123!</example>
    [Required(ErrorMessage = "La contraseña es requerida.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta tras un inicio de sesión exitoso.
/// </summary>
public class LoginUserResponse
{
    /// <summary>Identificador único del usuario autenticado (GUID).</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid Id { get; set; }

    /// <summary>Nombre del usuario.</summary>
    /// <example>María</example>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Apellido del usuario.</summary>
    /// <example>González</example>
    public string Apellido { get; set; } = string.Empty;

    /// <summary>Correo electrónico del usuario.</summary>
    /// <example>maria@email.com</example>
    public string Email { get; set; } = string.Empty;
}