using System.Net;

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
            return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
        }
        catch (UsersApiUnavailableException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogError(ex, "No se pudo consultar Users.API para el usuario {UserId}", id);
            throw new UsersApiUnavailableException("Users.API no está disponible.", ex);
        }
    }
}
