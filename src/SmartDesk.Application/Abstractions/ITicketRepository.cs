using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Abstractions;

/// <summary>Contrato de persistencia de tickets. Application lo define; Infrastructure lo implementa.</summary>
public interface ITicketRepository
{
    void Add(Ticket ticket);
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <param name="createdBy">null = todos los creadores (solo para agentes).</param>
    Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, string? createdBy, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
