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

/// <summary>Estructura estándar de respuesta de error (RFC 7231 / Problem Details).</summary>
public sealed record ErrorResponse
{
    /// <summary>URI de referencia al estándar RFC del error.</summary>
    /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
    public string Type { get; init; } = string.Empty;

    /// <summary>Título breve del error HTTP.</summary>
    /// <example>Not Found</example>
    public string Title { get; init; } = string.Empty;

    /// <summary>Código de estado HTTP.</summary>
    /// <example>404</example>
    public int Status { get; init; }

    /// <summary>Detalle específico del error ocurrido.</summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Ruta relativa del endpoint invocado.</summary>
    /// <example>/api/products/3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Instance { get; init; } = string.Empty;

    /// <summary>Código de error interno del catálogo (ej. PRD-001, PRD-002, PRD-003, PRD-004, PRD-005).</summary>
    /// <example>PRD-001</example>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje descriptivo del catálogo de errores.</summary>
    /// <example>Producto no encontrado.</example>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Identificador único de correlación.</summary>
    /// <example>d8a4b2c1f3e54a9b8c7d6e5f4a3b2c1d</example>
    public string CorrelationId { get; init; } = string.Empty;
}
