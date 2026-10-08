using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Casos de uso de tickets: orquesta dominio y persistencia.</summary>
public sealed class TicketService(ITicketRepository repository, TimeProvider clock)
{
    public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
    {
        var ticket = Ticket.Create(title, description, clock);
        repository.Add(ticket);
        await repository.SaveChangesAsync(ct);
        return TicketDto.FromEntity(ticket);
    }

    public async Task<TicketDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var ticket = await repository.GetByIdAsync(id, ct);
        return ticket is null ? null : TicketDto.FromEntity(ticket);
    }

    public async Task<IReadOnlyList<TicketDto>> ListAsync(TicketStatus? status, CancellationToken ct)
    {
        var tickets = await repository.ListAsync(status, ct);
        return tickets.Select(TicketDto.FromEntity).ToList();
    }

    public Task<TicketDto?> StartAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.StartProgress(), ct);

    public Task<TicketDto?> ResolveAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.Resolve(clock), ct);

    // Patrón común: cargar, aplicar la regla de dominio, guardar. Devuelve null si no existe.
    private async Task<TicketDto?> ChangeAsync(Guid id, Action<Ticket> change, CancellationToken ct)
    {
        var ticket = await repository.GetByIdAsync(id, ct);
        if (ticket is null) return null;

        change(ticket);
        await repository.SaveChangesAsync(ct);
        return TicketDto.FromEntity(ticket);
    }
}
