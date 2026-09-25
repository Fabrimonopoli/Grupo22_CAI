using MiniApi.Data;
using MiniApi.Extensions;

public partial class Program
{
    
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // 1. Logging
        builder.AddAppLogging();
        
        // 2. Servicios (Swagger, HealthChecks, Dapper, etc.)
        builder.Services.AddAppServices(); 
        var app = builder.Build();
        
        // 3. Inicializar DB
        using (var scope = app.Services.CreateScope())
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize();
        // Log de ejemplo
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError("OK");
        
        // 4. Middleware (Swagger UI, Serilog, HealthChecks)
        app.UseAppMiddleware(); 
        
        // 5. Endpoints
        app.MapAppEndpoints();
        app.Run();
    }
}