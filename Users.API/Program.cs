using Users.API.Data;
using Users.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();
builder.Services.AddAppServices();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    dbInitializer.Initialize();
}

app.UseAppMiddleware();
app.MapAppEndpoints();
app.Run();
