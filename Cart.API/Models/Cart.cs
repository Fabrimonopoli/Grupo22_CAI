using System.Text.Json.Serialization;

namespace Cart.API.Models;

public class Cart
{
    public Guid UsuarioId { get; set; }
    public List<CartItem> Items { get; set; } = new();
    public DateTime FechaActualizacion { get; set; }
}

public class CartItem
{
    [JsonPropertyName("productoId")]
    public Guid ProductId { get; set; }
    public int Cantidad { get; set; }
}
