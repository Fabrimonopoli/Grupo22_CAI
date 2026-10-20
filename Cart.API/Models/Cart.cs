namespace Cart.API.Models;

public class Cart
{
    public List<CartItem> Items { get; set; } = new();
}

public class CartItem
{
    public Guid ProductId { get; set; }
    public int Cantidad { get; set; }
}
