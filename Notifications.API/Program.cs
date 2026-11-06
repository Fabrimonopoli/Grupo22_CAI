using Dapper;
using Notifications.API.Data;
using Notifications.API.Extensions;

SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();
builder.Services.AddAppServices(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize();
}

app.UseAppMiddleware();
app.MapAppEndpoints();
app.Run();

public partial class Program;