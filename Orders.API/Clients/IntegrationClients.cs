namespace Orders.API.Clients;

public record UserDto(Guid Id, string Nombre, string Apellido, string Email, bool Activo);

public record ProductDto(Guid Id, string Nombre, string? Descripcion, decimal Precio, int Stock, string Categoria);

public class UsersApiClient(HttpClient client)
{
    public async Task<UserDto?> GetByIdAsync(Guid id)
    {
        var response = await client.GetAsync($"/api/users/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>();
    }
}

public class ProductsApiClient(HttpClient client)
{
    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var response = await client.GetAsync($"/api/products/{id}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>();
    }

    public async Task UpdateStockAsync(Guid id, ProductDto currentProduct, int newStock)
    {
        var updatePayload = new
        {
            nombre = currentProduct.Nombre,
            descripcion = currentProduct.Descripcion,
            precio = currentProduct.Precio,
            stock = newStock,
            categoria = currentProduct.Categoria
        };

        var response = await client.PutAsJsonAsync($"/api/products/{id}", updatePayload);
        response.EnsureSuccessStatusCode();
    }
}
