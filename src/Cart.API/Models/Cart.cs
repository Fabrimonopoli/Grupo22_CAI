using System.Text.Json.Serialization;

namespace Cart.API.Models;

/// <summary>Carrito activo del usuario con fecha de su última modificación persistida.</summary>
public class Cart
{
    /// <summary>Identificador del dueño del carrito.</summary>
    /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
    public Guid UsuarioId
    {
        get; set;
    }
    /// <summary>Productos y cantidades acumuladas.</summary>
    public List<CartItem> Items { get; set; } = new();
    /// <summary>Fecha UTC del último agregado, actualización o eliminación, no de la consulta.</summary>
    /// <example>2026-11-01T18:00:00Z</example>
    public DateTime FechaActualizacion
    {
        get; set;
    }
}

/// <summary>Cantidad acumulada de un producto en un carrito.</summary>
public class CartItem
{
    /// <summary>Identificador del producto, serializado como productoId.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [JsonPropertyName("productoId")]
    public Guid ProductId
    {
        get; set;
    }
    /// <summary>Unidades del producto.</summary>
    /// <example>2</example>
    public int Cantidad
    {
        get; set;
    }
}
