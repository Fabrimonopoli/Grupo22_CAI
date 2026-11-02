using Microsoft.AspNetCore.Mvc;
using Cart.API.DTOs;
using Cart.API.Services;

namespace Cart.API.Controllers;

/// <summary>Contrato REST público de Cart; delega las reglas a servicios de negocio.</summary>
[ApiController]
[Route("api/cart")]
[Tags("Cart")]
public sealed class CartController(CartService service) : ControllerBase
{
    /// <summary>Obtiene un carrito activo y su fecha real de última modificación.</summary>
    [HttpGet("{userId}")]
    public async Task<IActionResult> Get(Guid userId) => Ok(await service.GetAsync(userId));

    /// <summary>Agrega unidades comprobando la cantidad final acumulada.</summary>
    [HttpPost("{userId}/items")]
    public async Task<IActionResult> Add(Guid userId, [FromBody] CartItemRequest request, CancellationToken cancellationToken)
        => Ok(await service.AddItemAsync(userId, request, cancellationToken));

    /// <summary>Reemplaza la cantidad de un producto existente en el carrito.</summary>
    [HttpPut("{userId}/items/{productId}")]
    public async Task<IActionResult> Update(Guid userId, Guid productId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
        => Ok(await service.UpdateItemAsync(userId, productId, request, cancellationToken));

    /// <summary>Quita un producto y actualiza la fecha del carrito.</summary>
    [HttpDelete("{userId}/items/{productId}")]
    public async Task<IActionResult> Remove(Guid userId, Guid productId)
    {
        await service.RemoveItemAsync(userId, productId);
        return NoContent();
    }

    /// <summary>Vacía el carrito. Un carrito sin ítems se considera inactivo.</summary>
    [HttpDelete("{userId}")]
    public async Task<IActionResult> Clear(Guid userId)
    {
        await service.ClearAsync(userId);
        return NoContent();
    }
}
