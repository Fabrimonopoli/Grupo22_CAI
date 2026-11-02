using Orders.API.Data;
namespace Orders.API.Services;
/// <summary>Reanuda intenciones durables y acciones de inventario tras fallas o reinicios.</summary>
public sealed class OrderRecoveryWorker(IServiceScopeFactory scopes, ILogger<OrderRecoveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay startup so all APIs can begin accepting requests first.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                using var scope = scopes.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<OrderRepository>();
                var service = scope.ServiceProvider.GetRequiredService<OrderService>();
                foreach (var intent in await repo.GetPendingIntentsAsync())
                {
                    try
                    {
                        await service.ResumeAsync(intent);
                    }
                    catch (Exception ex) { logger.LogWarning(ex, "Recuperación de orden {OrderId} {ErrorCode}", intent.OrderId, "ORD-007"); }
                }
                await service.FlushStockActionsAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "No se pudo ejecutar la recuperación de órdenes {ErrorCode}", "ORD-007"); }
        }
    }
}
