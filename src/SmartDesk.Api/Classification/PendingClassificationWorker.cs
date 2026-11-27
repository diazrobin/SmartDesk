using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Tickets;

namespace SmartDesk.Api.Classification;

/// <summary>Clasifica en segundo plano los tickets pendientes. Cada ciclo usa su propio ámbito y solo procesa
/// si obtiene el bloqueo, para que varias instancias de la API no clasifiquen el mismo ticket.</summary>
public sealed class PendingClassificationWorker(
    IServiceScopeFactory scopes,
    ILogger<PendingClassificationWorker> logger) : BackgroundService
{
    private const int BatchSize = 10;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();

                // Otra instancia tiene el bloqueo: saltamos este ciclo
                await using var permiso = await scope.ServiceProvider
                    .GetRequiredService<IClassificationLock>()
                    .TryAcquireAsync(stoppingToken);
                if (permiso is null) continue;

                var processor = scope.ServiceProvider.GetRequiredService<PendingClassificationProcessor>();
                var procesados = await processor.ProcessBatchAsync(BatchSize, stoppingToken);

                if (procesados > 0)
                    logger.LogInformation("Ciclo de clasificación: {Count} tickets procesados", procesados);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return; // parada normal de la aplicación
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fallo en el ciclo de clasificación; se reintenta en el siguiente");
            }
        }
    }
}
