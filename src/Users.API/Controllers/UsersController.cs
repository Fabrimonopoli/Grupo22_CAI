using Microsoft.AspNetCore.Mvc;
using Users.API.DTOs;
using Users.API.Services;

namespace Users.API.Controllers;

/// <summary>Contrato REST público de Users; delega las reglas a servicios de negocio.</summary>
[ApiController]
[Route("api/users")]
[Tags("Users")]
public sealed class UsersController(UserService service) : ControllerBase
{
    /// <summary>Registra un usuario sin exponer su contraseña ni su hash.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var user = await service.RegisterAsync(request);
        return Created($"/api/users/{user.Id}", user);
    }

    /// <summary>Autentica y aplica bloqueo tras tres intentos fallidos consecutivos.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserRequest request)
        => Ok(await service.LoginAsync(request));

    /// <summary>Consulta segura de usuario para la integración entre servicios.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetByIdAsync(id));
}
