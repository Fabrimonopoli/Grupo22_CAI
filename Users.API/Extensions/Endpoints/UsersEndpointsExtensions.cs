using Users.API.Data;
using Users.API.DTOs;
using Users.API.Exceptions;
using Users.API.Models;

namespace Users.API.Extensions.Endpoints;

public static class UsersEndpointsExtensions
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        // ── POST /api/users/register ────────────────────────────────────────
        app.MapPost("/api/users/register", async (RegisterUserRequest? req, UserRepository repo) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Apellido) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                throw new BusinessRuleException(UserErrorCodes.Usr002, "Los datos del usuario son inválidos.");
            }

            var existingUser = await repo.GetByEmailAsync(req.Email);
            if (existingUser is not null)
            {
                throw new BusinessRuleException(UserErrorCodes.Usr001, $"El email '{req.Email}' ya está registrado.");
            }

            var passwordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(req.Password));
            var user = new User
            {
                Nombre = req.Nombre.Trim(),
                Apellido = req.Apellido.Trim(),
                Email = req.Email.Trim(),
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
        .WithSummary("Registrar un nuevo usuario.")
        .WithDescription("Registra un nuevo usuario en la plataforma validando campos obligatorios, formato de email y unicidad del correo electrónico.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **USR-001 (409):** El email ya está registrado.\n" +
                         "- **USR-002 (400):** Los datos del usuario son inválidos.\n" +
                         "- **USR-006 (500):** Error interno inesperado al persistir el usuario.")
        .Produces<RegisterUserResponse>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── POST /api/users/login ───────────────────────────────────────────
        app.MapPost("/api/users/login", async (LoginUserRequest? req, UserRepository repo) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            {
                throw new BusinessRuleException(UserErrorCodes.Usr002, "Los datos del usuario son inválidos.");
            }

            var user = await repo.GetByEmailAsync(req.Email);
            if (user is null)
            {
                throw new BusinessRuleException(UserErrorCodes.Usr003, "Credenciales incorrectas.");
            }

            if (!user.Activo)
            {
                if (user.IntentosFallidos >= 3)
                {
                    throw new BusinessRuleException(UserErrorCodes.Usr004, "Su cuenta fue bloqueada por superar el máximo de intentos fallidos. Contacte a soporte.");
                }
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
            {
                await repo.UpdateFailedAttemptsAsync(user.Id, 0, true);
            }

            return Results.Ok(new LoginUserResponse
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Email = user.Email
            });
        })
        .WithTags("Users")
        .WithSummary("Autenticar usuario en el sistema.")
        .WithDescription("Inicia sesión verificando credenciales, control de intentos fallidos y estado de la cuenta (activa, bloqueada o suspendida).\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **USR-002 (400):** Los datos del usuario son inválidos o faltantes.\n" +
                         "- **USR-003 (401):** Credenciales incorrectas.\n" +
                         "- **USR-004 (403):** Cuenta bloqueada por intentos fallidos.\n" +
                         "- **USR-005 (403):** Cuenta suspendida por razones de seguridad.\n" +
                         "- **USR-006 (500):** Error interno inesperado al autenticar.")
        .Produces<LoginUserResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── Endpoint interno para Orders.API y Cart.API ─────────────────────
        app.MapGet("/api/users/{id}", async (string id, UserRepository repo) =>
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                return Results.NotFound();
            }

            var user = await repo.GetByIdAsync(guidId);
            if (user is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(new { user.Id, user.Nombre, user.Apellido, user.Email, user.Activo });
        })
        .WithTags("Users")
        .ExcludeFromDescription();
    }
}