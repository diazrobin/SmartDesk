using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Abstractions;

/// <summary>Contrato de persistencia de tickets. Application lo define; Infrastructure lo implementa.</summary>
public interface ITicketRepository
{
    void Add(Ticket ticket);
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
