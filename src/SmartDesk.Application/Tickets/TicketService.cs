using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Casos de uso de tickets: orquesta dominio, persistencia y visibilidad. La clasificación es asíncrona.</summary>
public sealed class TicketService(
    ITicketRepository repository,
    ICurrentUser user,
    TimeProvider clock)
{
    public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
    {
        // El ticket nace Pending: PendingClassificationProcessor lo clasifica en segundo plano
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

    /// <summary>Página de tickets visibles para el usuario actual. Los límites se validan en la API.</summary>
    public async Task<PagedResult<TicketDto>> PageAsync(TicketStatus? status, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await repository.PageAsync(status, user.IsAgent ? null : user.Id, page, pageSize, ct);
        return new PagedResult<TicketDto>(items.Select(TicketDto.FromEntity).ToList(), page, pageSize, total);
    }

    public Task<TicketDto?> StartAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.StartProgress(), ct);

    public Task<TicketDto?> ResolveAsync(Guid id, CancellationToken ct) =>
        ChangeAsync(id, t => t.Resolve(clock), ct);

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
