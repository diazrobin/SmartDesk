using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Application.Tickets;

/// <summary>Casos de uso de tickets: orquesta dominio, persistencia, clasificación y visibilidad por usuario.</summary>
public sealed class TicketService(
    ITicketRepository repository,
    ICurrentUser user,
    ITicketClassifier classifier,
    TimeProvider clock)
{
    public async Task<TicketDto> CreateAsync(string title, string description, CancellationToken ct)
    {
        var ticket = Ticket.Create(title, description, user.Id, clock);

        // La clasificación es una sugerencia: el dominio sigue validando sus reglas en Classify()
        var suggestion = await classifier.ClassifyAsync(ticket.Title, ticket.Description, ct);
        ticket.Classify(suggestion.Category, suggestion.Priority);

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
        var tickets = await repository.ListAsync(status, user.IsAgent ? null : user.Id, ct);
        return tickets.Select(TicketDto.FromEntity).ToList();
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
