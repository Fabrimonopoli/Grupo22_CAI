using Products.API.Clients;
using Products.API.Data;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Models;
namespace Products.API.Services;

public sealed class ProductService(ProductRepository repo, StockReservationRepository reservations, OrdersApiClient orders)
{
    public Task<IEnumerable<Product>> GetAllAsync(string? categoria, string? nombre) => repo.GetAllAsync(categoria, nombre);
    public async Task<Product> GetAsync(Guid id) => await repo.GetByIdAsync(id) ?? throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
    public async Task<Product> CreateAsync(CreateProductRequest request)
    {
        Validate(request?.Nombre, request?.Descripcion, request?.Categoria, request?.Precio ?? 0, request?.Stock ?? -1);
        var product = new Product { Nombre = request!.Nombre.Trim(), Descripcion = request.Descripcion?.Trim(), Categoria = request.Categoria.Trim(), Precio = request.Precio, Stock = request.Stock, FechaCreacion = DateTime.UtcNow };
        await repo.CreateAsync(product);
        return product;
    }
    public async Task<Product> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        Validate(request?.Nombre, request?.Descripcion, request?.Categoria, request?.Precio ?? 0, request?.Stock ?? -1);
        var product = await GetAsync(id);
        product.Nombre = request!.Nombre.Trim();
        product.Descripcion = request.Descripcion?.Trim();
        product.Categoria = request.Categoria.Trim();
        product.Precio = request.Precio;
        product.Stock = request.Stock;
        if (!await repo.UpdateAsync(product))
            throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
        return product;
    }
    public async Task DeleteAsync(Guid id)
    {
        await GetAsync(id);
        if (await orders.HasActiveOrdersAsync(id))
            throw new BusinessRuleException(ProductErrorCodes.Prd004, "El producto tiene órdenes activas y no puede eliminarse.");
        if (!await repo.DeleteAsync(id))
            throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
    }
    public Task<StockReservationResponse> ReserveAsync(Guid id, ReserveStockRequest request)
    {
        if (id == Guid.Empty || request?.Items is null || request.Items.Count == 0 || request.Items.Any(i => i is null || i.ProductoId == Guid.Empty || i.Cantidad <= 0))
            throw new BusinessRuleException(ProductErrorCodes.Prd002, "La reserva debe incluir identificadores y cantidades válidas.");
        List<ReserveStockItem> items;
        try
        {
            items = request.Items.GroupBy(i => i.ProductoId).Select(g => new ReserveStockItem(g.Key, checked(g.Sum(i => i.Cantidad)))).ToList();
        }
        catch (OverflowException) { throw new BusinessRuleException(ProductErrorCodes.Prd002, "La cantidad acumulada supera el máximo permitido."); }
        return reservations.ReserveAsync(id, items);
    }
    public Task SetReservationStateAsync(Guid id, string action) => reservations.SetStateAsync(id, action);
    public static Guid ParseId(string value) => Guid.TryParse(value, out var id) && id != Guid.Empty ? id : throw new NotFoundException(ProductErrorCodes.Prd001, "Producto no encontrado.");
    private static void Validate(string? nombre, string? descripcion, string? categoria, decimal precio, int stock)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(nombre))
            errors.Add("El nombre es requerido.");
        if (nombre?.Length > 100)
            errors.Add("El nombre no puede superar los 100 caracteres.");
        if (descripcion?.Length > 500)
            errors.Add("La descripción no puede superar los 500 caracteres.");
        if (string.IsNullOrWhiteSpace(categoria))
            errors.Add("La categoría es requerida.");
        if (precio <= 0)
            errors.Add("El precio debe ser mayor a cero.");
        if (stock < 0)
            errors.Add("El stock no puede ser negativo.");
        if (errors.Count > 0)
            throw new BusinessRuleException(ProductErrorCodes.Prd002, string.Join("; ", errors));
    }
}
