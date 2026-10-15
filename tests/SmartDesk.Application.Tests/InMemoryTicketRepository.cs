using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tests;

/// <summary>Repositorio en memoria para tests: sin base de datos, rápido y determinista.</summary>
internal sealed class InMemoryTicketRepository : ITicketRepository
{
    public List<Ticket> Items { get; } = [];
    public int SaveCount { get; private set; }

    public void Add(Ticket ticket) => Items.Add(ticket);

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Ticket>>(
            Items.Where(t => status is null || t.Status == status).ToList());

    public Task SaveChangesAsync(CancellationToken ct)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}
