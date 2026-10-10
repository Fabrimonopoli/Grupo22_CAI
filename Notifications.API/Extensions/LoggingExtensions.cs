using Serilog;
using Serilog.Events;

namespace Notifications.API.Extensions;

public static class LoggingExtensions
{
    public static void AddAppLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [CorrelationId:{CorrelationId}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/audit.log",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} | {CorrelationId} | {RequestMethod} | {RequestPath} | {StatusCode}{NewLine}",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        builder.Host.UseSerilog();
    }
}
