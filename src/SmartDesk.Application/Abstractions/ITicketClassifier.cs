using SmartDesk.Application.Tickets;

namespace SmartDesk.Application.Abstractions;

/// <summary>Propone categoría y prioridad para un ticket. Infrastructure decide cómo (IA o reglas).</summary>
public interface ITicketClassifier
{
    Task<TicketClassification> ClassifyAsync(string title, string description, CancellationToken ct);
}
