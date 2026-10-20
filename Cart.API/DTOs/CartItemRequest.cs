namespace Cart.API.DTOs;

public class CartItemRequest
{
    public Guid ProductoId { get; set; }
    public int Cantidad { get; set; }
}

public class UpdateCartItemRequest
{
    public int Cantidad { get; set; }
}
