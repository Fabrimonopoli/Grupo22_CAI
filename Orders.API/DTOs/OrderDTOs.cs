using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

/// <summary>Solicitud para la creación de una nueva orden de compra.</summary>
public class CreateOrderRequest
{
    /// <summary>Identificador único del usuario comprador.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    [Required(ErrorMessage = "El UsuarioId es requerido.")]
    public Guid UsuarioId { get; set; }

    /// <summary>Lista de productos y cantidades que componen la orden.</summary>
    [Required(ErrorMessage = "La lista de items es requerida.")]
    [MinLength(1, ErrorMessage = "La orden debe contener al menos un item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

/// <summary>Ítem individual dentro de una solicitud de orden.</summary>
public class CreateOrderItemRequest
{
    /// <summary>Identificador del producto a adquirir.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required(ErrorMessage = "El ProductoId es requerido.")]
    public Guid ProductoId { get; set; }

    /// <summary>Cantidad de unidades solicitadas (debe ser mayor a 0).</summary>
    /// <example>2</example>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}

/// <summary>Solicitud de actualización del estado de una orden.</summary>
public class UpdateOrderStatusRequest
{
    /// <summary>Nuevo estado de la orden (Pendiente, Confirmada, Enviada, Entregada, Cancelada).</summary>
    /// <example>Confirmada</example>
    [Required(ErrorMessage = "El estado es requerido.")]
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Respuesta tras la actualización de estado de una orden.</summary>
public class UpdateOrderStatusResponse
{
    /// <summary>Identificador único de la orden.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>Estado actualizado de la orden.</summary>
    /// <example>Confirmada</example>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Fecha y hora UTC de la actualización.</summary>
    /// <example>2026-11-05T20:30:00Z</example>
    public DateTime FechaActualizacion { get; set; }
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
    /// <example>Orden no encontrada.</example>
    public string Detail { get; init; } = string.Empty;

    /// <summary>Ruta relativa del endpoint invocado.</summary>
    /// <example>/api/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public string Instance { get; init; } = string.Empty;

    /// <summary>Código de error interno del catálogo (ej. ORD-001 a ORD-007).</summary>
    /// <example>ORD-001</example>
    public string ErrorCode { get; init; } = string.Empty;

    /// <summary>Mensaje descriptivo del catálogo de errores.</summary>
    /// <example>Orden no encontrada.</example>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>Identificador único de correlación.</summary>
    /// <example>a1b2c3d4e5f67890123456789abcdef0</example>
    public string CorrelationId { get; init; } = string.Empty;
}