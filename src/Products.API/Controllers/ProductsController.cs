using Microsoft.AspNetCore.Mvc;
using Products.API.DTOs;
using Products.API.Services;

namespace Products.API.Controllers;

/// <summary>Contrato REST público de Products; delega las reglas a servicios de negocio.</summary>
[ApiController]
[Route("api/products")]
[Tags("Products")]
public sealed class ProductsController(ProductService service) : ControllerBase
{
    /// <summary>Lista productos con filtros opcionales de categoría y nombre.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? categoria, [FromQuery] string? nombre)
        => Ok(await service.GetAllAsync(categoria, nombre));

    /// <summary>Obtiene un producto o devuelve PRD-001.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id) => Ok(await service.GetAsync(ProductService.ParseId(id)));

    /// <summary>Crea un producto validado y único por nombre y categoría.</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await service.CreateAsync(request);
        return Created($"/api/products/{product.Id}", product);
    }

    /// <summary>Actualiza los datos y el stock administrativo de un producto.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateProductRequest request)
        => Ok(await service.UpdateAsync(ProductService.ParseId(id), request));

    /// <summary>Elimina un producto si no tiene órdenes ni reservas activas.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await service.DeleteAsync(ProductService.ParseId(id));
        return NoContent();
    }
}
