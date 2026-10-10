using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "El UsuarioId es requerido.")]
    public Guid UsuarioId { get; set; }

    [Required(ErrorMessage = "La lista de items es requerida.")]
    [MinLength(1, ErrorMessage = "La orden debe contener al menos un item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    [Required(ErrorMessage = "El ProductoId es requerido.")]
    public Guid ProductoId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public int Cantidad { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "El estado es requerido.")]
    public string Estado { get; set; } = string.Empty;
}

public class UpdateOrderStatusResponse
{
    public Guid Id { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaActualizacion { get; set; }
}
