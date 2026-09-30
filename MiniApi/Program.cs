using MiniApi.Data;
using MiniApi.Extensions;

public partial class Program
{
    
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Le enseña a Dapper a traducir los textos de SQLite a Guids de C#
        Dapper.SqlMapper.AddTypeHandler(new MiniApi.Data.GuidTypeHandler());

        // 1. Logging
        builder.AddAppLogging();
        
        // 2. Servicios (Swagger, HealthChecks, Dapper, etc.)
        builder.Services.AddAppServices();
        // --- MANEJO GLOBAL DE ERRORES (Apéndice B) ---
        builder.Services.AddExceptionHandler<MiniApi.ExceptionHandlers.NotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<MiniApi.ExceptionHandlers.BusinessRuleExceptionHandler>();
        builder.Services.AddProblemDetails();
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
        app.UseExceptionHandler();
        app.MapAppEndpoints();
        app.Run();
    }
}