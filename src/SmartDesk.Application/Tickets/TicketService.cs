using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Casos de uso de tickets: orquesta dominio, persistencia y visibilidad por usuario.</summary>
public sealed class TicketService(ITicketRepository repository, ICurrentUser user, TimeProvider clock)
{
    public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
    {
        var ticket = Ticket.Create(title, description, user.Id, clock);
        repository.Add(ticket);
        await repository.SaveChangesAsync(ct);
        return TicketDto.FromEntity(ticket);
    }

    public async Task<TicketDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var ticket = await repository.GetByIdAsync(id, ct);
        return ticket is not null && CanSee(ticket) ? TicketDto.FromEntity(ticket) : null;
    }

    public async Task<IReadOnlyList<TicketDto>> ListAsync(TicketStatus? status, CancellationToken ct)
    {
        // El filtro por creador se aplica en la consulta SQL, no en memoria
        var tickets = await repository.ListAsync(status, user.IsAgent ? null : user.Id, ct);
        return tickets.Select(TicketDto.FromEntity).ToList();
    }

    public Task<TicketDto?> StartAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.StartProgress(), ct);

    public Task<TicketDto?> ResolveAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.Resolve(clock), ct);

    // Un ticket ajeno se trata como inexistente: no revelamos que existe
    private bool CanSee(Ticket ticket) => user.IsAgent || ticket.CreatedBy == user.Id;

    private async Task<TicketDto?> ChangeAsync(Guid id, Action<Ticket> change, CancellationToken ct)
    {
        var ticket = await repository.GetByIdAsync(id, ct);
        if (ticket is null || !CanSee(ticket)) return null;

        change(ticket);
        await repository.SaveChangesAsync(ct);
        return TicketDto.FromEntity(ticket);
    }
}
