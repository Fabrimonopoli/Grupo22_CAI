using System.ComponentModel.DataAnnotations;

namespace Products.API.Models;

/// <summary>
/// Entidad que representa un producto en el catálogo.
/// </summary>
public class Product
{
    /// <summary>Identificador único del producto (GUID).</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Nombre comercial del producto.</summary>
    /// <example>Notebook Dell XPS 15</example>
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción detallada del producto.</summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    [StringLength(500)]
    public string? Descripcion { get; set; }

    /// <summary>Precio unitario del producto.</summary>
    /// <example>1500.00</example>
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }

    /// <summary>Unidades disponibles en inventario.</summary>
    /// <example>10</example>
    [Required]
    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    /// <summary>Categoría a la que pertenece el producto.</summary>
    /// <example>Electrónica</example>
    [Required]
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Fecha y hora de creación en formato UTC.</summary>
    /// <example>2024-01-15T10:30:00Z</example>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
