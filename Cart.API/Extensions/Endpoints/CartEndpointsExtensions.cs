using Cart.API.Clients;
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Extensions.Endpoints
{
    public static class CartEndpointsExtensions
    {
        public static void MapCartEndpoints(this WebApplication app)
        {
            app.MapGet("/api/cart/{userId}", async (
                Guid userId,
                CartRepository repo) =>
            {
                if (!await repo.CartExistsAsync(userId))
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");

                return Results.Ok(await repo.GetCartAsync(userId));
            }).WithTags("Cart");

            // ========================================================
            // ENDPOINT 1: Agregar producto al carrito (POST)
            // ========================================================
            app.MapPost("/api/cart/{userId}/items", async (
                Guid userId,
                [FromBody] CartItemRequest req, 
                ProductsApiClient productsClient,
                CartRepository repo) => 
            {
                if (req.Cantidad <= 0)
                    throw new BusinessRuleException("CRT-004", "Cantidad inválida.");

                // 1. Llamamos a TU mensajero para validar stock en Products.API
                var product = await productsClient.GetProductAsync(req.ProductoId);

                if (product == null)
                    throw new NotFoundException("CRT-002", "Producto no encontrado.");

                if (product.Stock < req.Cantidad)
                    throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");

                // 2. Llamamos a la BD (Contrato)
                await repo.AddItemAsync(userId, req.ProductoId, req.Cantidad);

                // 3. Devolvemos el carrito actualizado
                var updatedCart = await repo.GetCartAsync(userId);
                return Results.Ok(updatedCart);

            }).WithTags("Cart");


            // ========================================================
            // ENDPOINT 2: Actualizar cantidad de un item (PUT)
            // ========================================================
            app.MapPut("/api/cart/{userId}/items/{productId}", async (
                Guid userId,
                Guid productId,
                [FromBody] UpdateCartItemRequest req, 
                ProductsApiClient productsClient,
                CartRepository repo) => 
            {
                if (req.Cantidad <= 0)
                    throw new BusinessRuleException("CRT-004", "Cantidad inválida.");

                var product = await productsClient.GetProductAsync(productId);

                if (product == null)
                    throw new NotFoundException("CRT-002", "Producto no encontrado.");

                if (product.Stock < req.Cantidad)
                    throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");

                if (!await repo.CartExistsAsync(userId))
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");

                if (!await repo.ItemExistsAsync(userId, productId))
                    throw new NotFoundException("CRT-001", "Producto no encontrado en el carrito.");

                await repo.UpdateItemQuantityAsync(userId, productId, req.Cantidad);

                var updatedCart = await repo.GetCartAsync(userId);
                return Results.Ok(updatedCart);

            }).WithTags("Cart");

            app.MapDelete("/api/cart/{userId}/items/{productId}", async (
                Guid userId,
                Guid productId,
                CartRepository repo) =>
            {
                if (!await repo.CartExistsAsync(userId))
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");

                if (!await repo.RemoveItemAsync(userId, productId))
                    throw new NotFoundException("CRT-001", "Producto no encontrado en el carrito.");

                return Results.NoContent();
            }).WithTags("Cart");

            app.MapDelete("/api/cart/{userId}", async (
                Guid userId,
                CartRepository repo) =>
            {
                if (!await repo.CartExistsAsync(userId))
                    throw new NotFoundException("CRT-001", "Carrito no encontrado.");

                await repo.ClearCartAsync(userId);
                return Results.NoContent();
            }).WithTags("Cart");
        }
    }
}