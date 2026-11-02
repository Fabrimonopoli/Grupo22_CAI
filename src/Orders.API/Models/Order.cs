using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Orders.API.Models;

/// <summary>Orden persistida con sus precios capturados, total y estado de negocio.</summary>
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required]
    public Guid UsuarioId
    {
        get; set;
    }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total
    {
        get; set;
    }
    [Required] public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    [JsonIgnore]
    public DateTime? FechaActualizacion
    {
        get; set;
    }
}

/// <summary>Producto incluido en la orden al precio vigente en el momento de reservar.</summary>
public class OrderItem
{
    [JsonIgnore]
    public Guid OrderId
    {
        get; set;
    }
    [Required]
    public Guid ProductoId
    {
        get; set;
    }
    [Required]
    [Range(1, int.MaxValue)]
    public int Cantidad
    {
        get; set;
    }
    [Required]
    [Range(typeof(decimal), "0.0000000000000000000000000001", "79228162514264337593543950335", ParseLimitsInInvariantCulture = true)]
    public decimal PrecioUnitario
    {
        get; set;
    }
}
