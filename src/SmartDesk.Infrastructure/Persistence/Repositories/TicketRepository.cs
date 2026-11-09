using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Persistence.Repositories;

internal sealed class TicketRepository(SmartDeskDbContext db) : ITicketRepository
{
    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, string? createdBy, CancellationToken ct)
    {
        // AsNoTracking: solo lectura, EF no vigila cambios => menos memoria y más rápido
        var query = db.Tickets.AsNoTracking();

        if (status is not null)
            query = query.Where(t => t.Status == status);

        if (createdBy is not null)
            query = query.Where(t => t.CreatedBy == createdBy);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public async Task<(IReadOnlyList<Ticket> Items, int TotalCount)> PageAsync(
        TicketStatus? status, string? createdBy, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Tickets.AsNoTracking();

        if (status is not null)
            query = query.Where(t => t.Status == status);

        if (createdBy is not null)
            query = query.Where(t => t.CreatedBy == createdBy);

        // El total se cuenta con el mismo filtro, antes de paginar
        var total = await query.CountAsync(ct);

        // Orden estable: el Id desempata tickets con la misma fecha, así una página nunca repite ni salta tickets.
        // El filtro y el orden se ejecutan en PostgreSQL: solo viajan las filas de la página.
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
