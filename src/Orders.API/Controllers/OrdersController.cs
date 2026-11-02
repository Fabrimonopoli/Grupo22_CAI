using Microsoft.AspNetCore.Mvc;
using Orders.API.DTOs;
using Orders.API.Services;

namespace Orders.API.Controllers;

/// <summary>Contrato REST público de Orders; delega las reglas a servicios de negocio.</summary>
[ApiController]
[Route("api/orders")]
[Tags("Orders")]
public sealed class OrdersController(OrderService service) : ControllerBase
{
    /// <summary>Lista órdenes y permite filtrar por el usuario que las creó.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? usuarioId) => Ok(await service.GetAllAsync(usuarioId));

    /// <summary>Obtiene una orden con sus ítems y precios capturados.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) => Ok(await service.GetAsync(OrderService.ParseId(id)));

    /// <summary>Crea una orden con reserva atómica de stock y reintento idempotente opcional.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var key = OrderService.ParseIdempotencyKey(Request.Headers["Idempotency-Key"].FirstOrDefault());
        var order = await service.CreateAsync(request, key);
        Response.Headers["Idempotency-Key"] = key.ToString();
        return Created($"/api/orders/{order.Id}", order);
    }

    /// <summary>Actualiza el estado según la matriz de transiciones documentada.</summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        => Ok(await service.UpdateStatusAsync(OrderService.ParseId(id), request));
}
