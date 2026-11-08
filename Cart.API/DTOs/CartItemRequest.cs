using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>Solicitud para agregar un producto al carrito.</summary>
public class CartItemRequest
{
    /// <summary>Identificador único del producto.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "El ProductoId es requerido.")]
    public Guid ProductoId { get; set; }

    /// <summary>Cantidad requerida del producto (debe ser mayor a 0).</summary>
    /// <example>2</example>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}

/// <summary>Solicitud para actualizar la cantidad de un ítem en el carrito.</summary>
public class UpdateCartItemRequest
{
    /// <summary>Nueva cantidad solicitada (debe ser mayor a 0).</summary>
    /// <example>5</example>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
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

    /// <summary>Detalle genérico del error ocurrido.</summary>
    /// <example>El recurso solicitado no fue encontrado.</example>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Ruta relativa del endpoint invocado.</summary>
    /// <example>/api/cart/3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Instance { get; init; } = string.Empty;

    /// <summary>Código de error interno del catálogo (ej. CRT-001 a CRT-005).</summary>
    /// <example>CRT-001</example>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje dinámico contextual del error.</summary>
    /// <example>Carrito no encontrado.</example>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Identificador único de correlación.</summary>
    /// <example>a1b2c3d4e5f67890123456789abcdef0</example>
    public string CorrelationId { get; init; } = string.Empty;
}