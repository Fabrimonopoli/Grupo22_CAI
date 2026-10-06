using Products.API.Data;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Extensions.Endpoints;

public static class ProductsEndpointsExtensions
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products", async (string? categoria, string? nombre, ProductRepository repo) =>
        {
            var products = await repo.GetAllAsync(categoria, nombre);
            return Results.Ok(products);
        }).WithTags("Products");

        app.MapGet("/api/products/{id:guid}", async (Guid id, ProductRepository repo) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            return Results.Ok(product);
        }).WithTags("Products");

        app.MapPost("/api/products", async (CreateProductRequest req, ProductRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
                throw new BusinessRuleException(ProductErrorCodes.Prd002, "Los datos del producto son inválidos.");

            var duplicate = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
            if (duplicate is not null)
                throw new BusinessRuleException(ProductErrorCodes.Prd003, $"Ya existe un producto con ese nombre en la categoría '{req.Categoria}'.");

            var product = new Product
            {
                Nombre = req.Nombre,
                Descripcion = req.Descripcion,
                Precio = req.Precio,
                Stock = req.Stock,
                Categoria = req.Categoria,
                FechaCreacion = DateTime.UtcNow
            };

            await repo.CreateAsync(product);
            return Results.Created($"/api/products/{product.Id}", product);
        }).WithTags("Products");

        app.MapPut("/api/products/{id:guid}", async (Guid id, UpdateProductRequest req, ProductRepository repo) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");

            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
                throw new BusinessRuleException(ProductErrorCodes.Prd002, "Los datos del producto son inválidos.");

            var duplicate = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
            if (duplicate is not null && duplicate.Id != id)
                throw new BusinessRuleException(ProductErrorCodes.Prd003, $"Ya existe un producto con ese nombre en la categoría '{req.Categoria}'.");

            product.Nombre = req.Nombre;
            product.Descripcion = req.Descripcion;
            product.Precio = req.Precio;
            product.Stock = req.Stock;
            product.Categoria = req.Categoria;

            await repo.UpdateAsync(product);
            return Results.Ok(product);
        }).WithTags("Products");

        app.MapDelete("/api/products/{id:guid}", async (Guid id, ProductRepository repo) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");

            var hasActiveOrders = await repo.HasActiveOrdersAsync(id);
            if (hasActiveOrders)
                throw new BusinessRuleException(ProductErrorCodes.Prd004, "El producto tiene órdenes activas y no puede eliminarse.");

            await repo.DeleteAsync(id);
            return Results.NoContent();
        }).WithTags("Products");
    }
}
