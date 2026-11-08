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
        app.MapGet("/api/cart/{userId}", async (string userId, CartRepository repo) =>
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!await repo.CartExistsAsync(userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            return Results.Ok(await repo.GetCartAsync(userGuid));
        })
        .WithTags("Cart")
        .WithSummary("Obtener carrito de un usuario.")
        .WithDescription("Obtiene el carrito activo del usuario junto con sus ítems.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El usuario no posee un carrito activo.\n" +
                         "- **CRT-005 (500):** Error interno inesperado al consultar la persistencia.")
        .Produces<Models.Cart>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── POST /api/cart/{userId}/items ───────────────────────────────────
        app.MapPost("/api/cart/{userId}/items", async (
            string userId,
            [FromBody] CartItemRequest? req, 
            ProductsApiClient productsClient,
            CartRepository repo) => 
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (req is null || req.ProductoId == Guid.Empty || req.Cantidad <= 0)
            {
                throw new BusinessRuleException(CartErrorCodes.Crt004, "Cantidad inválida.");
            }

            var product = await productsClient.GetProductAsync(req.ProductoId);
            if (product is null)
            {
                throw new NotFoundException(CartErrorCodes.Crt002, "Producto no encontrado.");
            }

            if (product.Stock < req.Cantidad)
            {
                throw new BusinessRuleException(CartErrorCodes.Crt003, $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");
            }

            await repo.AddItemAsync(userGuid, req.ProductoId, req.Cantidad);
            var updatedCart = await repo.GetCartAsync(userGuid);
            return Results.Ok(updatedCart);
        })
        .WithTags("Cart")
        .WithSummary("Agregar un producto al carrito.")
        .WithDescription("Agrega un producto al carrito validando existencia y stock suficiente en Products.API.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** Carrito no encontrado.\n" +
                         "- **CRT-002 (404):** El producto no existe en Products.API.\n" +
                         "- **CRT-003 (422):** Stock insuficiente para satisfacer la cantidad demandada.\n" +
                         "- **CRT-004 (400):** Cantidad inválida (menor o igual a cero).\n" +
                         "- **CRT-005 (500):** Error interno inesperado al agregar el producto.")
        .Produces<Models.Cart>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── PUT /api/cart/{userId}/items/{productId} ────────────────────────
        app.MapPut("/api/cart/{userId}/items/{productId}", async (
            string userId,
            string productId,
            [FromBody] UpdateCartItemRequest? req, 
            ProductsApiClient productsClient,
            CartRepository repo) => 
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!Guid.TryParse(productId, out var productGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Producto no encontrado en el carrito.");
            }

            if (req is null || req.Cantidad <= 0)
            {
                throw new BusinessRuleException(CartErrorCodes.Crt004, "Cantidad inválida.");
            }

            var product = await productsClient.GetProductAsync(productGuid);
            if (product is null)
            {
                throw new NotFoundException(CartErrorCodes.Crt002, "Producto no encontrado.");
            }

            if (product.Stock < req.Cantidad)
            {
                throw new BusinessRuleException(CartErrorCodes.Crt003, $"Stock insuficiente. Disponible: {product.Stock}, solicitado: {req.Cantidad}.");
            }

            if (!await repo.CartExistsAsync(userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!await repo.ItemExistsAsync(userGuid, productGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Producto no encontrado en el carrito.");
            }

            await repo.UpdateItemQuantityAsync(userGuid, productGuid, req.Cantidad);
            var updatedCart = await repo.GetCartAsync(userGuid);
            return Results.Ok(updatedCart);
        })
        .WithTags("Cart")
        .WithSummary("Actualizar cantidad de un producto en el carrito.")
        .WithDescription("Modifica la cantidad de un ítem existente en el carrito validando disponibilidad de stock en Products.API.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El carrito o el producto no existen.\n" +
                         "- **CRT-002 (404):** El producto no existe en Products.API.\n" +
                         "- **CRT-003 (422):** Stock insuficiente.\n" +
                         "- **CRT-004 (400):** Cantidad inválida (menor o igual a cero).\n" +
                         "- **CRT-005 (500):** Error interno inesperado al actualizar.")
        .Produces<Models.Cart>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── DELETE /api/cart/{userId}/items/{productId} ─────────────────────
        app.MapDelete("/api/cart/{userId}/items/{productId}", async (
            string userId,
            string productId,
            CartRepository repo) =>
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!Guid.TryParse(productId, out var productGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Producto no encontrado en el carrito.");
            }

            if (!await repo.CartExistsAsync(userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!await repo.RemoveItemAsync(userGuid, productGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Producto no encontrado en el carrito.");
            }

            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Eliminar un producto del carrito.")
        .WithDescription("Quita un ítem específico del carrito del usuario.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El carrito o el producto no existen.\n" +
                         "- **CRT-005 (500):** Error interno inesperado al eliminar el producto.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── DELETE /api/cart/{userId} ───────────────────────────────────────
        app.MapDelete("/api/cart/{userId}", async (string userId, CartRepository repo) =>
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            if (!await repo.CartExistsAsync(userGuid))
            {
                throw new NotFoundException(CartErrorCodes.Crt001, "Carrito no encontrado.");
            }

            await repo.ClearCartAsync(userGuid);
            return Results.NoContent();
        })
        .WithTags("Cart")
        .WithSummary("Vaciar el carrito de compras.")
        .WithDescription("Elimina todos los productos asociados al carrito del usuario indicado.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **CRT-001 (404):** El usuario no posee un carrito activo.\n" +
                         "- **CRT-005 (500):** Error interno inesperado al vaciar el carrito.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}