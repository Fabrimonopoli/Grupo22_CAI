using System.Net;

namespace Cart.API.Clients;

/// <summary>Cliente HTTP que consulta únicamente los datos necesarios para el carrito.</summary>
public sealed class ProductsApiClient(HttpClient client)
{
    public async Task<ProductResponse?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await client.GetAsync($"/api/products/{productId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken);
    }
}

/// <summary>Producto informado por Products.API; el carrito no modifica su stock.</summary>
public sealed class ProductResponse
{
    public Guid Id
    {
        get; set;
    }
    public string Nombre { get; set; } = string.Empty;
    public int Stock
    {
        get; set;
    }
}
