using Users.API.Data;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Extensions.Endpoints;

public static class UsersEndpointsExtensions
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // ── POST /api/users/register ─────────────────────────────────────────
        app.MapPost("/api/users/register", async (RegisterUserRequest req, UserRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Apellido) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                throw new BusinessRuleException(UserErrorCodes.Usr002, "Los datos del usuario son inválidos.");

            var existingUser = await repo.GetByEmailAsync(req.Email);
            if (existingUser is not null)
                throw new BusinessRuleException(UserErrorCodes.Usr001, $"El email '{req.Email}' ya está registrado.");

            var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req.Password));
            var user = new User 
            { 
                Nombre = req.Nombre.Trim(), 
                Apellido = req.Apellido.Trim(), 
                Email = req.Email.Trim().ToLowerInvariant(), 
                PasswordHash = passwordHash, 
                FechaRegistro = DateTime.UtcNow, 
                Activo = true, 
                IntentosFallidos = 0 
            };

            await repo.CreateAsync(user);
            return Results.Created($"/api/users/{user.Id}", new RegisterUserResponse 
            { 
                Id = user.Id, 
                Nombre = user.Nombre, 
                Apellido = user.Apellido, 
                Email = user.Email, 
                FechaRegistro = user.FechaRegistro, 
                Activo = user.Activo 
            });
        })
        .WithTags("Users")
        .WithSummary("Registrar un nuevo usuario en la plataforma.")
        .WithDescription("Registra un nuevo usuario en el sistema validando la unicidad del correo electrónico.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **USR-001 (409):** El correo electrónico ya se encuentra registrado.\n" +
                         "- **USR-002 (400):** Datos de usuario inválidos o incompletos.\n" +
                         "- **USR-006 (500):** Error interno inesperado al persistir el usuario.")
        .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── POST /api/users/login ────────────────────────────────────────────
        app.MapPost("/api/users/login", async (LoginUserRequest req, UserRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                throw new BusinessRuleException(UserErrorCodes.Usr002, "Los datos del usuario son inválidos.");

            var user = await repo.GetByEmailAsync(req.Email);
            if (user is null)
                throw new BusinessRuleException(UserErrorCodes.Usr003, "Credenciales incorrectas.");

            if (!user.Activo)
            {
                if (user.IntentosFallidos >= 3)
                    throw new BusinessRuleException(UserErrorCodes.Usr004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                throw new BusinessRuleException(UserErrorCodes.Usr005, "Su cuenta fue suspendida por razones de seguridad. Contacte a soporte.");
            }

            var inputPasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req.Password));
            if (user.PasswordHash != inputPasswordHash)
            {
                user.IntentosFallidos++;
                if (user.IntentosFallidos >= 3)
                {
                    user.Activo = false;
                    await repo.UpdateFailedAttemptsAsync(user.Id, user.IntentosFallidos, user.Activo);
                    throw new BusinessRuleException(UserErrorCodes.Usr004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                }
                await repo.UpdateFailedAttemptsAsync(user.Id, user.IntentosFallidos, user.Activo);
                throw new BusinessRuleException(UserErrorCodes.Usr003, "Credenciales incorrectas.");
            }

            if (user.IntentosFallidos > 0)
                await repo.UpdateFailedAttemptsAsync(user.Id, 0, true);

            return Results.Ok(new LoginUserResponse 
            { 
                Id = user.Id, 
                Nombre = user.Nombre, 
                Apellido = user.Apellido, 
                Email = user.Email 
            });
        })
        .WithTags("Users")
        .WithSummary("Autenticar usuario.")
        .WithDescription("Valida credenciales de acceso e implementa la regla de bloqueo automático al acumular 3 intentos fallidos consecutivos.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **USR-002 (400):** Datos de login inválidos o incompletos.\n" +
                         "- **USR-003 (401):** Credenciales incorrectas (email o contraseña no válidos).\n" +
                         "- **USR-004 (403):** Cuenta bloqueada por superar 3 intentos fallidos consecutivos.\n" +
                         "- **USR-005 (403):** Cuenta suspendida preventivamente por detección de fraude.\n" +
                         "- **USR-006 (500):** Error interno inesperado.")
        .Produces<LoginUserResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status401Unauthorized, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status403Forbidden, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── GET /api/users/{id:guid} (Consulta interna para Orders.API y Notifications.API) ─
        app.MapGet("/api/users/{id:guid}", async (Guid id, UserRepository repo) =>
        {
            var user = await repo.GetByIdAsync(id);
            if (user is null) return Results.NotFound();

            return Results.Ok(new { user.Id, user.Nombre, user.Apellido, user.Email, user.Activo });
        })
        .WithTags("Users")
        .ExcludeFromDescription();
    }
}