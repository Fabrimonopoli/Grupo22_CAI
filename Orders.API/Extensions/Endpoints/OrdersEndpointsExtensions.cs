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
        app.MapGet("/api/orders", async (string? usuarioId, OrderRepository repo) =>
        {
            Guid? userGuid = null;
            if (!string.IsNullOrWhiteSpace(usuarioId))
            {
                if (Guid.TryParse(usuarioId, out var parsedGuid))
                {
                    userGuid = parsedGuid;
                }
                else
                {
                    // Un identificador inválido en el filtro de búsqueda simplemente no retorna resultados
                    return Results.Ok(Enumerable.Empty<Order>());
                }
            }

            var orders = await repo.GetAllAsync(userGuid);
            return Results.Ok(orders);
        })
        .WithTags("Orders")
        .WithSummary("Listar órdenes de compra.")
        .WithDescription("Obtiene la lista de órdenes registradas, permitiendo filtrar opcionalmente por identificador de usuario.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **ORD-007 (500):** Error interno inesperado al consultar la persistencia.")
        .Produces<IEnumerable<Order>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── GET /api/orders/{id} ────────────────────────────────────────────
        app.MapGet("/api/orders/{id}", async (string id, OrderRepository repo) =>
        {
            if (!Guid.TryParse(id, out var guidId))
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            var order = await repo.GetByIdAsync(guidId);
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
                         "- **ORD-001 (404):** Orden no encontrada.\n" +
                         "- **ORD-007 (500):** Error interno inesperado al consultar la base de datos.")
        .Produces<Order>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── POST /api/orders ────────────────────────────────────────────────
        app.MapPost("/api/orders", async (
            CreateOrderRequest? req,
            OrderRepository repo,
            UsersApiClient usersClient,
            ProductsApiClient productsClient) =>
        {
            // 1. Validar cuerpo de la petición
            if (req is null || req.UsuarioId == Guid.Empty || req.Items == null || !req.Items.Any())
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos.");
            }

            // 2. Verificar existencia y estado del usuario en Users.API
            var user = await usersClient.GetByIdAsync(req.UsuarioId);
            if (user is null || !user.Activo)
            {
                throw new NotFoundException(OrderErrorCodes.Ord003, "Usuario no encontrado o inactivo al crear la orden.");
            }

            var validatedItems = new List<(ProductDto Product, int Cantidad)>();

            // 3. Validar productos y stock en Products.API
            foreach (var item in req.Items)
            {
                if (item.ProductoId == Guid.Empty || item.Cantidad <= 0)
                {
                    throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos.");
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

            // 4. Descontar stock sincrónicamente en Products.API
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
                         "- **ORD-002 (400):** Los datos de la orden son inválidos.\n" +
                         "- **ORD-003 (404):** Usuario no encontrado o inactivo.\n" +
                         "- **ORD-004 (404):** Uno o más productos solicitados no existen.\n" +
                         "- **ORD-005 (422):** Stock insuficiente para satisfacer el pedido.\n" +
                         "- **ORD-007 (500):** Error interno inesperado al persistir la orden.")
        .Produces<Order>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status422UnprocessableEntity)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── PUT /api/orders/{id}/status ─────────────────────────────────────
        app.MapPut("/api/orders/{id}/status", async (string id, UpdateOrderStatusRequest? req, OrderRepository repo) =>
        {
            // 1. Si el ID no es un GUID válido, la orden no existe (404)
            if (!Guid.TryParse(id, out var guidId))
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            // 2. Validación del cuerpo y del estado
            if (req is null || string.IsNullOrWhiteSpace(req.Estado))
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos.");
            }

            var estadosValidos = new[] { "Pendiente", "Confirmada", "Enviada", "Entregada", "Cancelada" };
            if (!estadosValidos.Contains(req.Estado))
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord002, "Los datos de la orden son inválidos.");
            }

            // 3. Verificación de existencia previa
            var order = await repo.GetByIdAsync(guidId);
            if (order is null)
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            // 4. Regla de transición de estados
            if (order.Estado == "Entregada" || order.Estado == "Cancelada")
            {
                throw new BusinessRuleException(OrderErrorCodes.Ord006, $"Una orden en estado '{order.Estado}' no puede cambiar a '{req.Estado}'.");
            }

            var updated = await repo.UpdateStatusAsync(guidId, req.Estado);
            if (!updated)
            {
                throw new NotFoundException(OrderErrorCodes.Ord001, "Orden no encontrada.");
            }

            var response = new UpdateOrderStatusResponse
            {
                Id = guidId,
                Estado = req.Estado,
                FechaActualizacion = DateTime.UtcNow
            };

            return Results.Ok(response);
        })
        .WithTags("Orders")
        .WithSummary("Actualizar el estado de una orden de compra.")
        .WithDescription("Modifica el estado de una orden validando las transiciones permitidas del flujo de compra.\n\n" +
                         "**Códigos de error posibles:**\n" +
                         "- **ORD-001 (404):** Orden no encontrada.\n" +
                         "- **ORD-002 (400):** Los datos de la orden son inválidos o estado no reconocido.\n" +
                         "- **ORD-006 (409):** El estado de la orden no puede ser modificado (ya Entregada o Cancelada).\n" +
                         "- **ORD-007 (500):** Error interno inesperado al actualizar la orden.")
        .Produces<UpdateOrderStatusResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status409Conflict)
        .Produces(StatusCodes.Status500InternalServerError);

        // ── Endpoint interno para Products.API ──────────────────────────────
        app.MapGet("/api/orders/active-for-product/{productId}", async (string productId, OrderRepository repo) =>
        {
            if (!Guid.TryParse(productId, out var guidProductId))
            {
                return Results.Ok(new { hasActiveOrders = false });
            }

            var hasActiveOrders = await repo.HasActiveOrdersForProductAsync(guidProductId);
            return Results.Ok(new { hasActiveOrders });
        })
        .WithTags("Orders")
        .ExcludeFromDescription();
    }
}