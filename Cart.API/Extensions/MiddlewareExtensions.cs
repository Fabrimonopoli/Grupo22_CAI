using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Cart.API.Middleware;
using Serilog;

namespace Cart.API.Extensions
{
    public static class MiddlewareExtensions
    {
        public static void UseAppMiddleware(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseSerilogRequestLogging(options => {
                options.GetLevel = (httpContext, _, ex) => (ex != null) ? Serilog.Events.LogEventLevel.Error : (httpContext.Request.Path.StartsWithSegments("/health")) ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information;
            });

            // Enchufamos el cronómetro (guardia)
            app.UseMiddleware<AuditMiddleware>();

            app.UseExceptionHandler();

            // Enchufamos los visores de salud
            app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });
            app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");
        }
    }
}