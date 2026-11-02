using ECommerce.Shared;
using Cart.API.Data;
using Cart.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddAppLogging();
builder.Services.AddAppServices(builder.Configuration);
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}
app.UseAppMiddleware();
app.MapControllers();
app.Run();
public partial class Program;
