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

public sealed record ErrorResponse
{
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public int Status { get; init; }
    public string Detail { get; init; } = string.Empty;
    public string Instance { get; init; } = string.Empty;
    public string ErrorCode { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
}