using Cart.API.Clients;
using Cart.API.Data;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Extensions.Endpoints;

public static class CartEndpointsExtensions
{
    public static void MapCartEndpoints(this WebApplication app)
    {
        // ── GET /api/cart/{userId} ──────────────────────────────────────────
        app.MapGet("/api/cart/{userId:guid}", async (Guid userId, CartRepository repo) =>
        {
            if (!await repo.CartExistsAsync(userId))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            return Results.Ok(await repo.GetCartAsync(userId));
        })
        .WithTags("Cart")
        .WithSummary("Obtener carrito de un usuario.")
        .WithDescription("Obtiene el carrito activo del usuario junto con sus ítems.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El usuario no posee un carrito activo.\n" +
                         "- **CRT-005 (500):** Error interno inesperado al consultar la persistencia.")
        .Produces<Models.Cart>(StatusCodes.Status200OK) // <-- ACÁ ESTÁ EL CAMBIO
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── POST /api/cart/{userId}/items ───────────────────────────────────
        app.MapPost("/api/cart/{userId:guid}/items", async (
            Guid userId,
            [FromBody] CartItemRequest req, 
            ProductsApiClient productsClient,
            CartRepository repo) => 
        {
            if (req.Cantidad <= 0)
                throw new BusinessRuleException("CRT-004", "Cantidad inválida.");

            var product = await productsClient.GetProductAsync(req.ProductoId);
            if (product == null)
                throw new NotFoundException("CRT-002", "Producto no encontrado.");

            if (product.Stock < req.Cantidad)
                throw new BusinessRuleException("CRT-003", $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");

            await repo.AddItemAsync(userId, req.ProductoId, req.Cantidad);
            var updatedCart = await repo.GetCartAsync(userId);
            return Results.Ok(updatedCart);
        })
        .WithTags("Cart")
        .WithSummary("Agregar un producto al carrito.")
        .WithDescription("Agrega un producto validando existencia y stock suficiente en Products.API.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-002 (404):** El producto no existe en Products.API.\n" +
                         "- **CRT-003 (422):** Stock insuficiente para satisfacer la cantidad demandada.\n" +
                         "- **CRT-004 (400):** Cantidad inválida (menor o igual a cero).\n" +
                         "- **CRT-005 (500):** Error interno inesperado.")
        .Produces<Models.Cart>(StatusCodes.Status200OK) // <-- ACÁ ESTÁ EL CAMBIO
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── PUT /api/cart/{userId}/items/{productId} ────────────────────────
        app.MapPut("/api/cart/{userId:guid}/items/{productId:guid}", async (
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
        })
        .WithTags("Cart")
        .WithSummary("Actualizar cantidad de un producto en el carrito.")
        .WithDescription("Modifica la cantidad de un ítem existente en el carrito validando disponibilidad de stock.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El carrito o el producto no existen.\n" +
                         "- **CRT-002 (404):** El producto no existe en Products.API.\n" +
                         "- **CRT-003 (422):** Stock insuficiente.\n" +
                         "- **CRT-004 (400):** Cantidad inválida (menor o igual a cero).\n" +
                         "- **CRT-005 (500):** Error interno inesperado.")
        .Produces<Models.Cart>(StatusCodes.Status200OK) // <-- ACÁ ESTÁ EL CAMBIO
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── DELETE /api/cart/{userId}/items/{productId} ─────────────────────
        app.MapDelete("/api/cart/{userId:guid}/items/{productId:guid}", async (
            Guid userId,
            Guid productId,
            CartRepository repo) =>
        {
            if (!await repo.CartExistsAsync(userId))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            if (!await repo.RemoveItemAsync(userId, productId))
                throw new NotFoundException("CRT-001", "Producto no encontrado en el carrito.");

            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Eliminar un producto del carrito.")
        .WithDescription("Quita un ítem específico del carrito del usuario.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El carrito o el producto no existen.\n" +
                         "- **CRT-005 (500):** Error interno inesperado.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── DELETE /api/cart/{userId} ───────────────────────────────────────
        app.MapDelete("/api/cart/{userId:guid}", async (Guid userId, CartRepository repo) =>
        {
            if (!await repo.CartExistsAsync(userId))
                throw new NotFoundException("CRT-001", "Carrito no encontrado.");

            await repo.ClearCartAsync(userId);
            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Vaciar el carrito de compras.")
        .WithDescription("Elimina todos los productos asociados al carrito del usuario indicado.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El usuario no posee un carrito activo.\n" +
                         "- **CRT-005 (500):** Error interno inesperado.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");
    }
}