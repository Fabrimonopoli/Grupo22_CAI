using ECommerce.Shared;
using Products.API.Data;
using Products.API.Extensions;
using Products.API.Extensions.Endpoints;

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
app.MapGroup("").AddEndpointFilter<RequestValidationFilter>().MapProductEndpoints();
app.Run();
public partial class Program;
