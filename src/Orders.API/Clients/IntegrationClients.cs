using System.Net;
using System.Text.Json;
using Orders.API.Exceptions;
namespace Orders.API.Clients;

public record UserDto(Guid Id, string Nombre, string Apellido, string Email, bool Activo);
public record StockRequestItem(Guid ProductoId, int Cantidad);
public record ReservedItem(Guid ProductoId, string Nombre, int Cantidad, decimal PrecioUnitario);
public record StockReservation(Guid OrderId, string Estado, List<ReservedItem> Items);
public record RemoteError(string ErrorCode, string ErrorMessage);
public class UsersApiClient(HttpClient client)
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var response = await client.GetAsync($"/api/users/{id}", deadline.Token);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        try
        {
            return await response.Content.ReadFromJsonAsync<UserDto>(deadline.Token);
        }
        catch (JsonException ex) { throw new HttpRequestException("Users.API devolvió una respuesta inválida.", ex); }
    }
}
public class ProductsApiClient(HttpClient client)
{
    public async Task<StockReservation> ReserveAsync(Guid orderId, IReadOnlyList<StockRequestItem> items, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var response = await client.PostAsJsonAsync($"/internal/stock/reservations/{orderId}", new
        {
            items
        }, deadline.Token);
        if (!response.IsSuccessStatusCode)
        {
            RemoteError? error;
            try
            {
                error = await response.Content.ReadFromJsonAsync<RemoteError>(deadline.Token);
            }
            catch (JsonException ex) { throw new HttpRequestException("Products.API devolvió un error sin contrato válido.", ex); }
            if (response.StatusCode == HttpStatusCode.NotFound)
                throw new NotFoundException(OrderErrorCodes.Ord004, error?.ErrorMessage ?? "Producto no encontrado al crear la orden.");
            if (error?.ErrorCode == "PRD-006")
                throw new BusinessRuleException(OrderErrorCodes.Ord005, error.ErrorMessage);
            if (error?.ErrorCode == "PRD-007")
                throw new BusinessRuleException(OrderErrorCodes.Ord008, "La reserva de esta orden fue cancelada o tiene otros productos.");
            response.EnsureSuccessStatusCode();
        }
        try
        {
            return await response.Content.ReadFromJsonAsync<StockReservation>(deadline.Token) ?? throw new HttpRequestException("Products.API devolvió una reserva vacía.");
        }
        catch (JsonException ex) { throw new HttpRequestException("Products.API devolvió una reserva inválida.", ex); }
    }
    public async Task ApplyActionAsync(Guid orderId, string action, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        using var request = new HttpRequestMessage(action == "release" ? HttpMethod.Delete : HttpMethod.Put,
            $"/internal/stock/reservations/{orderId}" + (action == "release" ? "" : $"/{action}"));
        using var response = await client.SendAsync(request, deadline.Token);
        response.EnsureSuccessStatusCode();
    }
}
