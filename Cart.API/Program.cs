using Cart.API.Extensions;
using Cart.API.Extensions.Endpoints; // Esta carpeta la llenamos en el siguiente paso

var builder = WebApplication.CreateBuilder(args);

// 1. Configuramos Logs y Servicios (Fases 2 y 3)
builder.AddAppLogging();
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

// 2. Encendemos los Middlewares (Fase 3)
app.UseAppMiddleware();

// 3. Abrimos las ventanillas de Endpoints (Fase 4)
app.MapCartEndpoints();

app.Run();