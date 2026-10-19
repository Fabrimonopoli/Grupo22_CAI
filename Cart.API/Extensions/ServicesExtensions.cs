using Cart.API.Clients;
using Cart.API.HealthChecks;

namespace Cart.API.Extensions
{
    public static class ServicesExtensions
    {
        public static void AddAppServices(this IServiceCollection services, IConfiguration config)
        {
                
            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationIdHandler>();
            // 1. Enchufamos al Mensajero HTTP 
            var productsApiUrl = config["Services:ProductsApiUrl"] ?? "http://localhost:5110";
            services.AddHttpClient<ProductsApiClient>(client =>
            {
                client.BaseAddress = new Uri(productsApiUrl);
            }).AddHttpMessageHandler<CorrelationIdHandler>();

            // 2. Preparamos el terreno para la Base de Datos (La parte de tu compañero)
            // TODO: Acá tu compañero va a registrar el DatabaseInitializer y su CartRepository

            // 3. Documentación base (Swagger)
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.OperationFilter<CartOpenApiFilter>();
            });

            // 4. Enchufamos los Sensores de Salud
            services.AddHealthChecks()
                .AddCheck<SqliteHealthCheck>("sqlite-db", tags: ["database"])
                .AddCheck<ApiStatusCheck>("api-status", tags: ["api"]);

            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(600);
                setup.AddHealthCheckEndpoint("CartAPI", "/health");
            }).AddInMemoryStorage();
        }
    }
}