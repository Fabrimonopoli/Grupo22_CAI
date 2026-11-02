using System.ComponentModel.DataAnnotations;

namespace Cart.API.DTOs;

/// <summary>Producto y cantidad a sumar al carrito.</summary>
public sealed class CartItemRequest
{
    /// <summary>Identificador del producto existente en Products.API.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required]
    public Guid ProductoId
    {
        get; set;
    }

    /// <summary>Unidades adicionales; la suma también debe respetar el stock disponible.</summary>
    /// <example>2</example>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad
    {
        get; set;
    }
}

/// <summary>Nueva cantidad absoluta del ítem.</summary>
public sealed class UpdateCartItemRequest
{
    /// <summary>Unidades que tendrá el ítem después de actualizar.</summary>
    /// <example>4</example>
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad
    {
        get; set;
    }
}
