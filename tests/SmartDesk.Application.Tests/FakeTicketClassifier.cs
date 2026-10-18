using SmartDesk.Application.Abstractions;
using SmartDesk.Application.Tickets;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tests;

/// <summary>Clasificador fijo: los tests de TicketService no dependen de IA ni de reglas reales.</summary>
internal sealed class FakeTicketClassifier : ITicketClassifier
{
    public Task<TicketClassification> ClassifyAsync(string title, string description, CancellationToken ct) =>
        Task.FromResult(new TicketClassification(TicketCategories.Hardware, TicketPriority.High));
}
