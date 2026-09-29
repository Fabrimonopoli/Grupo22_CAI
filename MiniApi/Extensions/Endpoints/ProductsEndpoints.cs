using Microsoft.AspNetCore.Mvc;
using MiniApi.Data;
using MiniApi.Models;
using MiniApi.DTOs;
using MiniApi.Exceptions;

namespace MiniApi.Endpoints
{
    public static class ProductsEndpoints
    {
        public static void MapProductsEndpoints(this IEndpointRouteBuilder routes)
        {
            var group = routes.MapGroup("/api/products").WithTags("Products");

            // GET /api/products
            group.MapGet("/", async ([FromQuery] string? categoria, [FromQuery] string? nombre, ProductRepository repo) =>
            {
                var products = await repo.GetAllAsync(categoria, nombre);
                return Results.Ok(products);
            });

            // GET /api/products/{id}
            group.MapGet("/{id:guid}", async (Guid id, ProductRepository repo) =>
            {
                var product = await repo.GetByIdAsync(id);
                if (product is null)
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");

                return Results.Ok(product);
            });

            // POST /api/products
            group.MapPost("/", async (CreateProductRequest request, ProductRepository repo) =>
            {
                if (string.IsNullOrWhiteSpace(request.Nombre) || request.Precio <= 0 || request.Stock < 0)
                    throw new BusinessRuleException("PRD-002", "Los datos del producto son inválidos.");

                var newProduct = new Product
                {
                    Id = Guid.NewGuid(),
                    Nombre = request.Nombre,
                    Descripcion = request.Descripcion,
                    Precio = request.Precio,
                    Stock = request.Stock,
                    Categoria = request.Categoria,
                    FechaCreacion = DateTime.UtcNow
                };

                await repo.CreateAsync(newProduct);
                return Results.Created($"/api/products/{newProduct.Id}", newProduct);
            });

            // PUT /api/products/{id}
            group.MapPut("/{id:guid}", async (Guid id, UpdateProductRequest request, ProductRepository repo) =>
            {
                var existingProduct = await repo.GetByIdAsync(id);
                if (existingProduct is null)
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");

                existingProduct.Nombre = request.Nombre;
                existingProduct.Descripcion = request.Descripcion;
                existingProduct.Precio = request.Precio;
                existingProduct.Stock = request.Stock;
                existingProduct.Categoria = request.Categoria;

                await repo.UpdateAsync(existingProduct);

                return Results.Ok(existingProduct);
            });

            // DELETE /api/products/{id}
            group.MapDelete("/{id:guid}", async (Guid id, ProductRepository repo) =>
            {
                var existingProduct = await repo.GetByIdAsync(id);
                if (existingProduct is null)
                    throw new NotFoundException("PRD-001", "Producto no encontrado.");

                await repo.DeleteAsync(id);
                return Results.NoContent();
            });
        }
    }
}
