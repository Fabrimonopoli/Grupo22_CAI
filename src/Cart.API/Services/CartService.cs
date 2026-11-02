using Cart.API.Clients;
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using CartModel = Cart.API.Models.Cart;

namespace Cart.API.Services;

/// <summary>Reglas de carrito y validación del stock publicado por Products.API.</summary>
public sealed class CartService(CartRepository repository, ProductsApiClient products)
{
    public Task<CartModel> GetAsync(Guid userId)
    {
        ValidateUserId(userId);
        return repository.GetCartAsync(userId);
    }

    public async Task<CartModel> AddItemAsync(Guid userId, CartItemRequest request, CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateQuantity(request?.Cantidad ?? 0);
        var product = await GetProductAsync(request!.ProductoId, cancellationToken);
        return await repository.AddItemAsync(userId, product.Id, request.Cantidad, product.Stock);
    }

    public async Task<CartModel> UpdateItemAsync(Guid userId, Guid productId, UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        ValidateUserId(userId);
        ValidateQuantity(request?.Cantidad ?? 0);
        var product = await GetProductAsync(productId, cancellationToken);
        return await repository.UpdateItemQuantityAsync(userId, product.Id, request!.Cantidad, product.Stock);
    }

    public Task RemoveItemAsync(Guid userId, Guid productId)
    {
        ValidateUserId(userId);
        if (productId == Guid.Empty)
            throw new NotFoundException("CRT-001", "Producto no encontrado en el carrito.");
        return repository.RemoveItemAsync(userId, productId);
    }

    public Task ClearAsync(Guid userId)
    {
        ValidateUserId(userId);
        return repository.ClearCartAsync(userId);
    }

    private async Task<ProductResponse> GetProductAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = productId == Guid.Empty ? null : await products.GetProductAsync(productId, cancellationToken);
        return product ?? throw new NotFoundException("CRT-002", "Producto no encontrado.");
    }

    private static void ValidateUserId(Guid id)
    {
        if (id == Guid.Empty)
            throw new NotFoundException("CRT-001", "Carrito no encontrado.");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new BusinessRuleException("CRT-004", "Cantidad inválida. Debe ser mayor a cero.");
    }
}
