using Cart.API.Extensions;
using Cart.API.Extensions.Endpoints; // Esta carpeta la llenamos en el siguiente paso
using Cart.API.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuramos Logs y Servicios (Fases 2 y 3)
builder.AddAppLogging();
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await dbInitializer.InitializeAsync();
}

// 2. Encendemos los Middlewares (Fase 3)
app.UseAppMiddleware();

// 3. Abrimos las ventanillas de Endpoints (Fase 4)
app.MapCartEndpoints();

app.Run();