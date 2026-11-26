using Microsoft.EntityFrameworkCore;
using SmartDesk.Domain.Tickets;
using SmartDesk.Infrastructure.Persistence.Repositories;

namespace SmartDesk.Integration.Tests;

[Collection("Postgres")]
public class ClassificationPersistenceTests(PostgresFixture pg)
{
    [Fact]
    public async Task TicketPendiente_SePersisteComoPendiente_NoComoDefaultDeBd()
    {
        await using var db = pg.CreateDb();
        var repo = new TicketRepository(db);
        var ticket = Ticket.Create("Persistencia", "Comprobar clasificacion pendiente", "ana", TimeProvider.System);
        repo.Add(ticket);
        await repo.SaveChangesAsync(CancellationToken.None);

        await using var lectura = pg.CreateDb();
        var leido = await lectura.Tickets.AsNoTracking().SingleAsync(t => t.Id == ticket.Id);

        Assert.Equal(ClassificationStatus.Pending, leido.Classification);
    }
}
