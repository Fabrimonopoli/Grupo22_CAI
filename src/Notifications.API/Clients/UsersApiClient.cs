using System.Net;
using System.Text.Json;

namespace Notifications.API.Clients;

public sealed record UserDto(Guid Id, string Nombre, string Apellido, string Email, bool Activo);

public sealed class UsersApiUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class UsersApiClient(HttpClient client, ILogger<UsersApiClient> logger)
{
    public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await client.GetAsync($"/api/users/{id}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken)
                ?? throw new UsersApiUnavailableException("No se pudo verificar el destinatario.");
            if (user.Id != id)
                throw new UsersApiUnavailableException("No se pudo verificar el destinatario.");
            return user;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or NotSupportedException)
        {
            logger.LogError(ex, "No se pudo consultar Users.API para el usuario {UserId}", id);
            throw new UsersApiUnavailableException("No se pudo verificar el destinatario.", ex);
        }
    }
}
