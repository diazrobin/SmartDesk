using Microsoft.Extensions.Logging;
using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Clasifica un lote de tickets pendientes. Lo invoca el worker en segundo plano.</summary>
public sealed class PendingClassificationProcessor(
    ITicketRepository repository,
    ITicketClassifier classifier,
    ILogger<PendingClassificationProcessor> logger)
{
    /// <summary>Tras este número de fallos, el ticket queda en Failed y no se reintenta.</summary>
    public const int MaxAttempts = 3;

    public async Task<int> ProcessBatchAsync(int batchSize, CancellationToken ct)
    {
        var pendientes = await repository.GetPendingClassificationAsync(batchSize, ct);

        foreach (var ticket in pendientes)
        {
            ct.ThrowIfCancellationRequested();

            // Resuelto o cerrado antes de clasificarse: ya no admite reclasificación
            if (ticket.Status is TicketStatus.Resolved or TicketStatus.Closed)
            {
                ticket.AbandonClassification();
                logger.LogInformation("Ticket {TicketId} resuelto antes de clasificar; se abandona", ticket.Id);
                continue;
            }

            try
            {
                var sugerencia = await classifier.ClassifyAsync(ticket.Title, ticket.Description, ct);
                ticket.Classify(sugerencia.Category, sugerencia.Priority);
                logger.LogInformation("Ticket {TicketId} clasificado: {Category} / {Priority}",
                    ticket.Id, sugerencia.Category, sugerencia.Priority);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                ticket.RecordClassificationFailure(MaxAttempts);
                logger.LogWarning(ex, "Fallo al clasificar el ticket {TicketId} (intento {Attempt} de {Max})",
                    ticket.Id, ticket.ClassificationAttempts, MaxAttempts);
            }
        }

        await repository.SaveChangesAsync(ct);
        return pendientes.Count;
    }
}
