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
        .WithDescription("Obtiene la lista de productos disponibles en el catálogo, permitiendo filtrar opcionalmente por categoría o coincidencia de nombre.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-005 (500):** Error interno inesperado al consultar la persistencia.")
        .Produces<IEnumerable<Product>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── GET /api/products/{id} ──────────────────────────────────────────
        app.MapGet("/api/products/{id}", async (string id, ProductRepository repo) =>
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            var product = await repo.GetByIdAsync(guidId);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            return Results.Ok(product);
        })
        .WithTags("Products")
        .WithSummary("Obtener detalle de un producto por su ID.")
        .WithDescription("Devuelve la información detallada de un producto a partir de su ID.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-001 (404):** Producto no encontrado.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al consultar la base de datos.")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── POST /api/products ──────────────────────────────────────────────
        app.MapPost("/api/products", async (CreateProductRequest? req, ProductRepository repo) =>
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
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
                         "- **PRD-002 (400):** Los datos del producto son inválidos.\n" +
                         "- **PRD-003 (409):** Ya existe un producto con el mismo nombre en la categoría indicada.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al persistir.")
        .Produces<Product>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── PUT /api/products/{id} ──────────────────────────────────────────
        app.MapPut("/api/products/{id}", async (string id, UpdateProductRequest? req, ProductRepository repo) =>
        {
            // 1. Si el ID no es un GUID válido, el recurso no existe (404)
            if (!Guid.TryParse(id, out var guidId))
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            // 2. Validación de datos del body (400 si está incompleto o con valores inválidos)
            if (req is null || string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Categoria) || req.Precio <= 0 || req.Stock < 0)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd002, "Los datos del producto son inválidos.");
            }

            // 3. Verificación de existencia del producto
            var product = await repo.GetByIdAsync(guidId);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            // 4. Regla de duplicados en la categoría
            var duplicate = await repo.GetByNameAndCategoryAsync(req.Nombre, req.Categoria);
            if (duplicate is not null && duplicate.Id != guidId)
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
                         "- **PRD-001 (404):** Producto no encontrado.\n" +
                         "- **PRD-002 (400):** Los datos del producto son inválidos.\n" +
                         "- **PRD-003 (409):** Ya existe otro producto con ese nombre en la categoría.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al actualizar.")
        .Produces<Product>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── DELETE /api/products/{id} ───────────────────────────────────────
        app.MapDelete("/api/products/{id}", async (string id, ProductRepository repo, OrdersApiClient ordersClient) =>
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            var product = await repo.GetByIdAsync(guidId);
            if (product is null)
            {
                throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
            }

            var hasActiveOrders = await ordersClient.HasActiveOrdersAsync(guidId);
            if (hasActiveOrders)
            {
                throw new BusinessRuleException(ProductErrorCodes.Prd004, "El producto tiene órdenes activas y no puede eliminarse.");
            }

            await repo.DeleteAsync(guidId);
            return Results.NoContent();
        })
        .WithTags("Products")
        .WithSummary("Eliminar un producto del catálogo.")
        .WithDescription("Elimina un producto del catálogo verificando previamente contra Orders.API que no tenga órdenes activas asociadas.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **PRD-001 (404):** Producto no encontrado.\n" +
                         "- **PRD-004 (409):** El producto tiene órdenes activas y no puede eliminarse.\n" +
                         "- **PRD-005 (500):** Error interno inesperado al eliminar.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);
    }
}