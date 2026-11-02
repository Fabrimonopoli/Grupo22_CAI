using ECommerce.Shared;
using Notifications.API.Data;
using Notifications.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();
builder.Services.AddAppServices(builder.Configuration);
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    initializer.Initialize();
}
app.UseAppMiddleware();
app.MapControllers();
app.Run();
public partial class Program;
