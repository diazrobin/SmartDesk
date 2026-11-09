using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Abstractions;

/// <summary>Contrato de persistencia de tickets. Application lo define; Infrastructure lo implementa.</summary>
public interface ITicketRepository
{
    void Add(Ticket ticket);
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Una página ordenada por fecha descendente, con el total de tickets que cumplen el filtro.</summary>
    /// <param name="createdBy">null = todos los creadores (solo para agentes).</param>
    Task<(IReadOnlyList<Ticket> Items, int TotalCount)> PageAsync(
        TicketStatus? status, string? createdBy, int page, int pageSize, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
