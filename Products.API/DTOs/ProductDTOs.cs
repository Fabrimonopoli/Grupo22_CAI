using System.ComponentModel.DataAnnotations;

namespace Products.API.DTOs;

/// <summary>
/// Solicitud para la creación de un nuevo producto.
/// </summary>
public class CreateProductRequest
{
    /// <summary>Nombre del producto.</summary>
    /// <example>Notebook Dell XPS 15</example>
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción opcional del producto.</summary>
    /// <example>Laptop 15 pulgadas, 32GB RAM</example>
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Precio unitario en moneda local.</summary>
    /// <example>1500.00</example>
    [Required(ErrorMessage = "El precio es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    /// <summary>Cantidad inicial en stock.</summary>
    /// <example>10</example>
    [Required(ErrorMessage = "El stock es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un entero mayor o igual a 0.")]
    public int Stock { get; set; }

    /// <summary>Categoría del producto.</summary>
    /// <example>Electrónica</example>
    [Required(ErrorMessage = "La categoría es requerida.")]
    public string Categoria { get; set; } = string.Empty;
}

/// <summary>
/// Solicitud para actualizar un producto existente.
/// </summary>
public class UpdateProductRequest
{
    /// <summary>Nombre del producto.</summary>
    /// <example>Notebook Dell XPS 15</example>
    [Required(ErrorMessage = "El nombre es requerido.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Descripción del producto.</summary>
    /// <example>Laptop 15 pulgadas, 64GB RAM</example>
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    public string? Descripcion { get; set; }

    /// <summary>Precio unitario actualizado.</summary>
    /// <example>1750.00</example>
    [Required(ErrorMessage = "El precio es requerido.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
    public decimal Precio { get; set; }

    /// <summary>Stock actualizado.</summary>
    /// <example>8</example>
    [Required(ErrorMessage = "El stock es requerido.")]
    [Range(0, int.MaxValue, ErrorMessage = "El stock debe ser un entero mayor o igual a 0.")]
    public int Stock { get; set; }

    /// <summary>Categoría del producto.</summary>
    /// <example>Electrónica</example>
    [Required(ErrorMessage = "La categoría es requerida.")]
    public string Categoria { get; set; } = string.Empty;
}
