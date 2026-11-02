using System.Text.Json;
namespace Products.API.Clients;

public class OrdersApiClient(HttpClient client)
{
    public async Task<bool> HasActiveOrdersAsync(Guid productId)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using var response = await client.GetAsync($"/api/orders/active-for-product/{productId}", deadline.Token);
        response.EnsureSuccessStatusCode();
        try
        {
            return (await response.Content.ReadFromJsonAsync<ActiveOrdersResponse>(deadline.Token))?.HasActiveOrders ?? throw new HttpRequestException("Orders.API devolvió una consulta vacía.");
        }
        catch (JsonException ex) { throw new HttpRequestException("Orders.API devolvió una respuesta inválida.", ex); }
    }
    private record ActiveOrdersResponse(bool HasActiveOrders);
}
