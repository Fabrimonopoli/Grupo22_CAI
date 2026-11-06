using Products.API.Clients;
using Products.API.Data;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;

namespace Products.API.Extensions.Endpoints;

public static class ProductsEndpointsExtensions
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        // ── GET /api/products ───────────────────────────────────────────────
        app.MapGet("/api/products", async (string? categoria, string? nombre, ProductRepository repo) =>
        {
            var products = await repo.GetAllAsync(categoria, nombre);
            return Results.Ok(products);
        })
        .WithTags("Products")
        .WithSummary("Listar productos con filtros opcionales.")
        .WithDescription("Obtiene la lista de productos disponibles en el catálogo, permitiendo filtrar opcionalmente por categoría o coincidencia de nombre.")
        .Produces<IEnumerable<Product>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── GET /api/products/{id} ──────────────────────────────────────────
        app.MapGet("/api/products/{id:guid}", async (Guid id, ProductRepository repo) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            return Results.Ok(product);
        })
        .WithTags("Products")
        .WithSummary("Obtener detalle de un producto por su ID.")
        .WithDescription("Devuelve la información detallada de un producto a partir de su GUID.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-001 (404):** El producto especificado no existe.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al consultar la base de datos.")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── POST /api/products ──────────────────────────────────────────────
        app.MapPost("/api/products", async (CreateProductRequest req, ProductRepository repo) =>
        {
            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd002, "Los datos del producto son inválidos.");
            }

            var duplicate = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
            if (duplicate is not null)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd003, $"Ya existe un producto con el nombre '{req.Nombre}' en la categoría '{req.Categoria}'.");
            }

            var product = new Product
            {
                Nombre = req.Nombre.Trim(),
                Descripcion = req.Descripcion?.Trim(),
                Precio = req.Precio,
                Stock = req.Stock,
                Categoria = req.Categoria.Trim(),
                FechaCreacion = DateTime.UtcNow
            };

            await repo.CreateAsync(product);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithTags("Products")
        .WithSummary("Crear un nuevo producto en el catálogo.")
        .WithDescription("Crea y persiste un nuevo producto validando unicidad de nombre dentro de la categoría y consistencia de precios y stock.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-002 (400):** Datos de producto inválidos (nombre/categoría vacíos, precio <= 0 o stock negativo).\n" +
                         "- **PRD-003 (409):** Ya existe un producto con el mismo nombre en la categoría indicada.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al persistir.")
        .Produces<Product>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── PUT /api/products/{id} ──────────────────────────────────────────
        app.MapPut("/api/products/{id:guid}", async (Guid id, UpdateProductRequest req, ProductRepository repo) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd002, "Los datos del producto son inválidos.");
            }

            var duplicate = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
            if (duplicate is not null && duplicate.Id != id)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd003, $"Ya existe un producto con el nombre '{req.Nombre}' en la categoría '{req.Categoria}'.");
            }

            product.Nombre = req.Nombre.Trim();
            product.Descripcion = req.Descripcion?.Trim();
            product.Precio = req.Precio;
            product.Stock = req.Stock;
            product.Categoria = req.Categoria.Trim();

            await repo.UpdateAsync(product);
            return Results.Ok(product);
        })
        .WithTags("Products")
        .WithSummary("Actualizar un producto existente.")
        .WithDescription("Actualiza los datos de un producto existente validando reglas de negocio y unicidad.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-001 (404):** El producto especificado no existe.\n" +
                         "- **PRD-002 (400):** Datos de actualización inválidos.\n" +
                         "- **PRD-003 (409):** Ya existe otro producto con ese nombre en la categoría.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al actualizar.")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── DELETE /api/products/{id} ───────────────────────────────────────
        app.MapDelete("/api/products/{id:guid}", async (Guid id, ProductRepository repo, OrdersApiClient ordersClient) =>
        {
            var product = await repo.GetByIdAsync(id);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            var hasActiveOrders = await ordersClient.HasActiveOrdersAsync(id);
            if (hasActiveOrders)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd004, "El producto tiene órdenes activas y no puede eliminarse.");
            }

            await repo.DeleteAsync(id);
            return Results.NoContent();
        })
        .WithTags("Products")
        .WithSummary("Eliminar un producto del catálogo.")
        .WithDescription("Elimina un producto del catálogo verificando previamente contra Orders.API que no tenga órdenes activas asociadas.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-001 (404):** El producto especificado no existe.\n" +
                         "- **PRD-004 (409):** El producto posee órdenes activas y no puede eliminarse.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al eliminar.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");
    }
}