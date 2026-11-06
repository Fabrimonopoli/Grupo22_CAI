using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Extensions.Endpoints;

public static class OrdersEndpointsExtensions
{
    public static void MapOrderEndpoints(this WebApplication app)
    {
        // ── GET /api/orders ─────────────────────────────────────────────────
        app.MapGet("/api/orders", async (Guid? usuarioId, OrderRepository repo) =>
        {
            var orders = await repo.GetAllAsync(usuarioId);
            return Results.Ok(orders);
        })
        .WithTags("Orders")
        .WithSummary("Listar órdenes de compra.")
        .WithDescription("Obtiene la lista de órdenes registradas, permitiendo filtrar opcionalmente por identificador de usuario.")
        .Produces<IEnumerable<Order>>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── GET /api/orders/{id} ────────────────────────────────────────────
        app.MapGet("/api/orders/{id:guid}", async (Guid id, OrderRepository repo) =>
        {
            var order = await repo.GetByIdAsync(id);
            if (order is null)
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }
            return Results.Ok(order);
        })
        .WithTags("Orders")
        .WithSummary("Obtener detalle de una orden por ID.")
        .WithDescription("Devuelve la información completa de una orden de compra junto con sus ítems asociados.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **ORD-001 (404):** La orden especificada no existe.\n" +
                         "- **ORD-007 (500):** Error interno inesperado al consultar la base de datos.")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── POST /api/orders ────────────────────────────────────────────────
        app.MapPost("/api/orders", async (
            CreateOrderRequest req,
            OrderRepository repo,
            UsersApiClient usersClient,
            ProductsApiClient productsClient) =>
        {
            if (req.Items == null || !req.Items.Any())
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos. La orden debe contener al menos un item.");
            }

            var user = await usersClient.GetByIdAsync(req.UsuarioId);
            if (user is null || !user.Activo)
            {
                throw new NotFoundException(OrderErrorCodes.Ord003, "Usuario no encontrado o inactivo al crear la orden.");
            }

            var validatedItems = new List<(ProductDto Product, int Cantidad)>();

            foreach (var item in req.Items)
            {
                if (item.Cantidad <= 0)
                {
                    throw new BusinessRuleException(OrderErrorCodes.Ord002, "La cantidad de cada ítem debe ser mayor a 0.");
                }

                var product = await productsClient.GetByIdAsync(item.ProductoId);
                if (product is null)
                {
                    throw new NotFoundException(OrderErrorCodes.Ord004, $"El producto con ID '{item.ProductoId}' no fue encontrado al crear la orden.");
                }

                if (product.Stock < item.Cantidad)
                {
                    throw new BusinessRuleException(
                        OrderErrorCodes.Ord005,
                        $"Stock insuficiente para '{product.Nombre}'. Disponible: {product.Stock}, solicitado: {item.Cantidad}."
                    );
                }

                validatedItems.Add((product, item.Cantidad));
            }

            var order = new Order
            {
                UsuarioId = req.UsuarioId,
                Estado = "Pendiente",
                FechaCreacion = DateTime.UtcNow
            };

            decimal totalOrder = 0;
            foreach (var (product, cantidad) in validatedItems)
            {
                var precioUnitario = product.Precio;
                order.Items.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductoId = product.Id,
                    Cantidad = cantidad,
                    PrecioUnitario = precioUnitario
                });

                totalOrder += precioUnitario * cantidad;
            }
            order.Total = totalOrder;

            // Descontar stock sincrónicamente en Products.API
            foreach (var (product, cantidad) in validatedItems)
            {
                var nuevoStock = product.Stock - cantidad;
                await productsClient.UpdateStockAsync(product.Id, product, nuevoStock);
            }

            await repo.CreateAsync(order);

            return Results.Created($"/api/orders/{order.Id}", order);
        })
        .WithTags("Orders")
        .WithSummary("Crear una nueva orden de compra.")
        .WithDescription("Crea una nueva orden validando la existencia y estado activo del usuario en Users.API, disponibilidad de productos y stock suficiente en Products.API, y descuenta el inventario de forma sincrónica.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **ORD-002 (400):** Datos de la orden inválidos (sin ítems o cantidad <= 0).\n" +
                         "- **ORD-003 (404):** Usuario destinatario no encontrado o inactivo.\n" +
                         "- **ORD-004 (404):** Uno o más productos solicitados no existen.\n" +
                         "- **ORD-005 (422):** Stock insuficiente para satisfacer el pedido.\n" +
                         "- **ORD-007 (500):** Error interno inesperado al persistir la orden.")
        .Produces<Order>(StatusCodes.Status201Created)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status422UnprocessableEntity, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── PUT /api/orders/{id}/status ─────────────────────────────────────
        app.MapPut("/api/orders/{id:guid}/status", async (Guid id, UpdateOrderStatusRequest req, OrderRepository repo) =>
        {
            var order = await repo.GetByIdAsync(id);
            if (order is null)
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            var estadosValidos = new[] { "Pendiente", "Confirmada", "Enviada", "Entregada", "Cancelada" };
            if (!estadosValidos.Contains(req.Estado))
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos.");
            }

            if (order.Estado == "Entregada" || order.Estado == "Cancelada")
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord006, $"Una orden en estado '{order.Estado}' no puede cambiar a '{req.Estado}'.");
            }

            var updated = await repo.UpdateStatusAsync(id, req.Estado);
            if (!updated)
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            var response = new UpdateOrderStatusResponse
            {
                Id = id,
                Estado = req.Estado,
                FechaActualizacion = DateTime.UtcNow
            };

            return Results.Ok(response);
        })
        .WithTags("Orders")
        .WithSummary("Actualizar el estado de una orden de compra.")
        .WithDescription("Modifica el estado de una orden validando las transiciones permitidas del flujo de compra.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **ORD-001 (404):** La orden especificada no existe.\n" +
                         "- **ORD-002 (400):** Estado no reconocido.\n" +
                         "- **ORD-006 (409):** La orden ya está Entregada o Cancelada y no admite transiciones.\n" +
                         "- **ORD-007 (500):** Error interno inesperado al actualizar.")
        .Produces<UpdateOrderStatusResponse>(StatusCodes.Status200OK)
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status409Conflict, "application/problem+json")
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError, "application/problem+json");

        // ── Endpoint interno para Products.API ──────────────────────────────
        app.MapGet("/api/orders/active-for-product/{productId:guid}", async (Guid productId, OrderRepository repo) =>
        {
            var hasActiveOrders = await repo.HasActiveOrdersForProductAsync(productId);
            return Results.Ok(new { hasActiveOrders });
        })
        .WithTags("Orders")
        .ExcludeFromDescription();
    }
}