using Microsoft.EntityFrameworkCore;
using SmartDesk.Application.Abstractions;
using SmartDesk.Domain.Tickets;

namespace SmartDesk.Infrastructure.Persistence.Repositories;

internal sealed class TicketRepository(SmartDeskDbContext db) : ITicketRepository
{
    public void Add(Ticket ticket) => db.Tickets.Add(ticket);

    public Task<Ticket?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Tickets.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Ticket>> ListAsync(TicketStatus? status, CancellationToken ct)
    {
        // AsNoTracking: solo lectura, EF no vigila cambios => menos memoria y más rápido
        var query = db.Tickets.AsNoTracking();

        if (status is not null)
            query = query.Where(t => t.Status == status);

        return await query.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
