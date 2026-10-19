using System.Net;
using System.Net.Http.Json;

namespace Cart.API.Clients
{
    public class ProductsApiClient
    {
        private readonly HttpClient _httpClient;

        public ProductsApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Viaja a Products.API para traer los datos vitales del producto
        public async Task<ProductResponse?> GetProductAsync(Guid productId)
        {
            // Hacemos la llamada HTTP GET a la ruta exacta que programaron en Products.API
            var response = await _httpClient.GetAsync($"/api/products/{productId}");

            // Si Products.API nos devuelve un 404, significa que el producto no existe.
            // Devolvemos null para que luego el endpoint del carrito dispare el error CRT-002.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            // Si hay otro error raro (un 500 por ejemplo), frenamos la ejecución
            response.EnsureSuccessStatusCode();

            // Si salió todo bien (200 OK), leemos el JSON y lo convertimos a nuestro DTO interno
            return await response.Content.ReadFromJsonAsync<ProductResponse>();
        }
    }

    // DTO interno exclusivo del Carrito para leer la respuesta de Productos.
    // Solo pedimos lo que nos importa: Id, Nombre y Stock.
    public class ProductResponse
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}