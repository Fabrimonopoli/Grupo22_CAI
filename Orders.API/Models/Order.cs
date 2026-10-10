using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Orders.API.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid UsuarioId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Total { get; set; }
    [Required] public string Estado { get; set; } = "Pendiente";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}

public class OrderItem
{
    [JsonIgnore] public Guid OrderId { get; set; }
    [Required] public Guid ProductoId { get; set; }
    [Required] [Range(1, int.MaxValue)] public int Cantidad { get; set; }
    [Required] [Range(0.01, double.MaxValue)] public decimal PrecioUnitario { get; set; }
}
