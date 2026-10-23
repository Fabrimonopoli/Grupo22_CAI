namespace Products.API.Clients;

public class OrdersApiClient(HttpClient client)
{
    public async Task<bool> HasActiveOrdersAsync(Guid productId)
    {
        var response = await client.GetAsync($"/api/orders/active-for-product/{productId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ActiveOrdersResponse>();
        return result?.HasActiveOrders ?? false;
    }

    private record ActiveOrdersResponse(bool HasActiveOrders);
}